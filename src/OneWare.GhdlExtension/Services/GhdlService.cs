﻿using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using OneWare.Essentials.Enums;
using OneWare.Essentials.Models;
using OneWare.Essentials.Services;
using OneWare.Essentials.ViewModels;
using OneWare.GhdlExtension.ViewModels;
using OneWare.UniversalFpgaProjectSystem.Context;
using OneWare.UniversalFpgaProjectSystem.Models;

namespace OneWare.GhdlExtension.Services;

public class GhdlService
{
    private readonly ILogger _logger;
    private readonly IDockService _dockService;
    private readonly IPackageService _packageService;
    private readonly IChildProcessService _childProcessService;
    private readonly IEnvironmentService _environmentService;
    private readonly IOutputService _outputService;
    private readonly ISettingsService _settingsService;
    private readonly IProjectExplorerService _projectExplorerService;

    public AsyncRelayCommand SimulateCommand { get; }

    public AsyncRelayCommand SynthToDotCommand { get; }

    public AsyncRelayCommand SynthToVerilogCommand { get; }

    private string _path = string.Empty;

    public GhdlService(ILogger logger, IDockService dockService, ISettingsService settingsService, IPackageService packageService, IChildProcessService childProcessService, IEnvironmentService environmentService,
        IOutputService outputService, IProjectExplorerService projectExplorerService)
    {
        _logger = logger;
        _dockService = dockService;
        _packageService = packageService;
        _childProcessService = childProcessService;
        _environmentService = environmentService;
        _outputService = outputService;
        _settingsService = settingsService;
        _projectExplorerService = projectExplorerService;

        settingsService.GetSettingObservable<string>(GhdlExtensionModule.GhdlPathSetting).Subscribe(x =>
        {
            _path = x;
            SetEnvironment();
        });

        SimulateCommand = new AsyncRelayCommand(SimulateCurrentFileAsync,
            () => _dockService.CurrentDocument?.CurrentFile?.Extension is ".vhd" or ".vhdl");

        SynthToDotCommand = new AsyncRelayCommand(() => SynthCurrentFileAsync("dot"),
            () => _dockService.CurrentDocument?.CurrentFile?.Extension is ".vhd" or ".vhdl");

        SynthToVerilogCommand = new AsyncRelayCommand(() => SynthCurrentFileAsync("verilog"),
            () => _dockService.CurrentDocument?.CurrentFile?.Extension is ".vhd" or ".vhdl");

        _dockService.WhenValueChanged(x => x.CurrentDocument).Subscribe(x =>
        {
            SimulateCommand.NotifyCanExecuteChanged();
        });
    }

    private async Task<(bool success, string stdout, string stderr)> ExecuteGhdlAsync(IReadOnlyCollection<string> arguments, string workingDirectory, string status,
        AppState state = AppState.Loading, bool showTimer = false)
    {
        if (!File.Exists(_path) || (_settingsService.GetSettingValue<bool>("Experimental_AutoDownloadBinaries") && 
                                    _packageService.Packages.GetValueOrDefault(GhdlExtensionModule.GhdlPackage.Id!) is {Status: PackageStatus.Available or PackageStatus.UpdateAvailable or PackageStatus.Installing}))
        {
            var install = await InstallGhdlAsync();
            if (!install)
            {
                _logger.Warning("GHDL not found. Please set the path in the settings or (re)install it from the package manager.");
                return (false,string.Empty, string.Empty);
            }
        }
        
        string stdout = string.Empty, stderr = string.Empty;

        (bool success, _) = await _childProcessService.ExecuteShellAsync(_path, arguments, workingDirectory,
            status, state, showTimer, x =>
            {
                if (x.StartsWith("ghdl:error:"))
                {
                    _logger.Error(x);
                    return false;
                }

                _outputService.WriteLine(x);
                stdout += x + "\n";
                return true;
            }, x =>
            {
                if (x.StartsWith("ghdl:error:"))
                {
                    _logger.Error(x);
                    return false;
                }
                
                _logger.Warning(x);
                stderr += x + "\n";
                return true;
            });
        
        return (success, stdout, stderr);
    }

    private async Task<bool> InstallGhdlAsync()
    {
        if (_packageService.Packages.GetValueOrDefault(GhdlExtensionModule.GhdlPackage.Id!) is {Status: PackageStatus.Available or PackageStatus.UpdateAvailable or PackageStatus.Installing})
        {
            if (!_settingsService.GetSettingValue<bool>("Experimental_AutoDownloadBinaries")) return false;
            if(!await _packageService.InstallAsync(GhdlExtensionModule.GhdlPackage)) return false;
            SetEnvironment();
            return true;
        }

        return false;
    }

    private void SetEnvironment()
    {
        if (File.Exists(_path))
        {
            _environmentService.SetPath("GHDL_PATH", Path.GetDirectoryName(_path));
            _environmentService.SetEnvironmentVariable("GHDL_PREFIX", Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(_path))!, "lib", $"ghdl"));
        }
    }

    private Task SynthCurrentFileAsync(string output)
    {
        if (_dockService.CurrentDocument?.CurrentFile is IProjectFile selectedFile)
            return SynthAsync(selectedFile, output, selectedFile.TopFolder!.FullPath);
        return Task.CompletedTask;
    }

    private async Task<bool> ElaborateAsync(IProjectFile file, TestBenchContext context)
    {
        if(file.Root is not UniversalFpgaProjectRoot root) return false;

        IEnumerable<string> libfiles = GetAllLibraryFiles(root);

        IEnumerable<string>? libnames = root.GetProjectPropertyArray("GHDL_Libraries");
        
        var vhdlFiles = root.Files
            .Where(x => !root.CompileExcluded.Contains(x))
            .Where(x => !libfiles.Contains(x.RelativePath))
            .Where(x => x.Extension is ".vhd" or ".vhdl")
            .Select(x => x.RelativePath);

        var top = Path.GetFileNameWithoutExtension(file.FullPath);
        var workingDirectory = file.Root!.FullPath;
        var buildDirectory = Path.Combine(workingDirectory, "build");
        
        // Ensure existence of build directory
        if (!Directory.Exists(buildDirectory))
        {
            Directory.CreateDirectory(buildDirectory);
        }
        else
        {
            // Remove pre-existing *.cf files from build directory
            foreach (string configFile in Directory.EnumerateFiles(buildDirectory)
                         .Where(x => Path.GetExtension(x) is ".cf"))
            {
                File.Delete(configFile);
            }
        }

        List<string> ghdlOptions = [];
        
        ghdlOptions.Add($"--workdir={buildDirectory}");
        ghdlOptions.Add($"-P{buildDirectory}");
        
        var vhdlStandard = context.GetBenchProperty(nameof(GhdlSimulatorToolbarViewModel.VhdlStandard)) ?? root.GetProjectProperty("VHDL_Standard");
        if (vhdlStandard != null)
        {
            ghdlOptions.Add($"--std={vhdlStandard}");
        }
    
        var additionalGhdlOptions = context.GetBenchProperty(nameof(GhdlSimulatorToolbarViewModel.AdditionalGhdlOptions));
        if(additionalGhdlOptions != null) ghdlOptions.AddRange(additionalGhdlOptions.Split(' '));
        
        List<string> ghdlInitArguments = ["-i"];
        ghdlInitArguments.AddRange(ghdlOptions);
        ghdlInitArguments.AddRange(vhdlFiles);

        List<string> ghdlMakeArguments = ["-m"];
        ghdlMakeArguments.AddRange(ghdlOptions);
        ghdlMakeArguments.Add($"{GetLibraryPrefixForToplevel(root)}{top}");

        List<string> ghdlElaborateArguments = ["-e"];
        ghdlElaborateArguments.AddRange(ghdlOptions);
        ghdlElaborateArguments.Add($"{GetLibraryPrefixForToplevel(root)}{top}");

        var initFiles = await ExecuteGhdlAsync(ghdlInitArguments, workingDirectory,
            "GHDL Init...",
            AppState.Loading, true);
        if (!initFiles.success) return false;
        
        if (libnames is not null)
        {
            foreach (string libname in libnames)
            {
                bool success = await ImportLibraryAsync(root, context, libname, workingDirectory, ghdlOptions);
                
                if (!success)
                {
                    return false;
                }
            }
        }

        var make = await ExecuteGhdlAsync(ghdlMakeArguments, workingDirectory,
            "Running GHDL Make...", AppState.Loading, true);
        if (!make.success) return false;
        
        if (libnames is not null)
        {
            foreach (string libname in libnames)
            {
                bool success = await MakeLibraryAsync(root, context, libname, workingDirectory, ghdlOptions);

                if (!success)
                {
                    return false;
                }
            }
        }

        var elaboration = await ExecuteGhdlAsync(ghdlElaborateArguments, workingDirectory,
            "Running GHDL Elaboration...", AppState.Loading, true);
        if (!elaboration.success) return false;

        return true;
    }

    private async Task<bool> ImportLibraryAsync(UniversalFpgaProjectRoot root, TestBenchContext context, string libname,
        string workingDirectory, List<string> ghdlOptions)
    {
        string buildDirectory = Path.Combine(workingDirectory, "build");
        
        // Get files contained in library
        IEnumerable<string>? libraryFiles = root.GetProjectPropertyArray($"GHDL-LIB_{libname}");

        if (libraryFiles is null)
        {
            _logger.Warning($"Library {libname} is empty");
            
            return true;
        }
        
        IEnumerable<string> vhdlFiles = root.Files
            .Where(x => !root.CompileExcluded.Contains(x))
            .Where(x => x.Extension is ".vhd" or ".vhdl")
            .Where(x => libraryFiles.Contains(x.RelativePath))
            .Select(x => x.RelativePath);
        
        ghdlOptions.Add($"--work={libname}");
        
        List<string> ghdlInitArguments = ["-i"];
        ghdlInitArguments.AddRange(ghdlOptions);
        ghdlInitArguments.AddRange(vhdlFiles);
        
        var initFiles = await ExecuteGhdlAsync(ghdlInitArguments, workingDirectory,
            $"GHDL Init for library {libname}...",
            AppState.Loading, true);
        
        return initFiles.success;
    }

    private async Task<bool> MakeLibraryAsync(UniversalFpgaProjectRoot root, TestBenchContext context, string libname,
        string workingDirectory, List<string> ghdlOptions)
    {
        string buildDirectory = Path.Combine(workingDirectory, "build");
        
        string? top = Path.GetFileNameWithoutExtension(root.TopEntity?.FullPath);

        if (top is null)
        {
            _logger.Error("No toplevel entity has been set");
            
            return false;
        }
        
        ghdlOptions.Add($"--work={libname}");
        
        List<string> ghdlMakeArguments = ["-m"];
        ghdlMakeArguments.AddRange(ghdlOptions);
        ghdlMakeArguments.Add($"{GetLibraryPrefixForToplevel(root)}{top}");
        
        var make = await ExecuteGhdlAsync(ghdlMakeArguments, workingDirectory,
            $"Running GHDL Make for library {libname}...", AppState.Loading, true);
        return make.success;
    }

    private IEnumerable<string> GetAllLibraryFiles(UniversalFpgaProjectRoot root)
    {
        IEnumerable<string>? libnames = root.GetProjectPropertyArray("GHDL_Libraries");

        if (libnames is null || !libnames.Any())
        {
            return [];
        }

        var ret = new List<string>();
        
        foreach (string lib in libnames)
        {
            IEnumerable<string>? libfiles = root.GetProjectPropertyArray($"GHDL-LIB_{lib}");

            if (libfiles is null || !libfiles.Any())
            {
                _logger.Warning($"Library {lib} is empty");
                
                continue;
            }
            
            foreach (string file in libfiles)
            {
                ret.Add(file);
            }
        }
        
        return ret;
    }

    private string GetLibraryPrefixForToplevel(UniversalFpgaProjectRoot root)
    {
        IEnumerable<string>? libnames = root.GetProjectPropertyArray("GHDL_Libraries");

        if (libnames is null)
        {
            return "";
        }
        
        string? top = root.TopEntity?.RelativePath;

        if (top is null)
        {
            _logger.Error("No toplevel entity has been set");
            
            return "";
        }

        foreach (string libname in libnames)
        {
            IEnumerable<string>? libfiles = root.GetProjectPropertyArray($"GHDL-LIB_{libname}");

            if (libfiles is null || !libfiles.Any())
            {
                continue;
            }
            
            if (libfiles.Contains(top))
            {
                return $"{libname}.";
            }
        }
        
        return "";
    }

    public async Task<bool> SynthAsync(IProjectFile file, string outputType, string outputDirectory)
    {
        if (file.Root is UniversalFpgaProjectRoot root)
        {
            PackageModel? ghdlPackagemodel = _packageService.Packages.GetValueOrDefault("ghdl");
            Version ghdlVersion = new Version(5, 0, 1);
            bool directVerilogOutput = (ghdlPackagemodel is not null &&
                 ghdlVersion.CompareTo(Version.Parse(ghdlPackagemodel.InstalledVersion!.Version!)) <= 0 && outputType.Equals("verilog"));
            
            _dockService.Show<IOutputService>();

            var settings = await TestBenchContextManager.LoadContextAsync(file);

            var top = Path.GetFileNameWithoutExtension(file.FullPath);
            var workingDirectory = root.FullPath;
            string buildDirectory = Path.Combine(workingDirectory, "build");

            var vhdlStandard = root.GetProjectProperty("VHDL_Standard") ?? "93c";
            List<string> ghdlOptions = [$"--std={vhdlStandard}"];
            ghdlOptions.Add($"--workdir={buildDirectory}");
            ghdlOptions.Add($"-P{buildDirectory}");

            var elaborateResult = await ElaborateAsync(file, settings);
            if (!elaborateResult) return false;

            List<string> ghdlSynthArguments = ["--synth"];
            ghdlSynthArguments.AddRange(ghdlOptions);
            ghdlSynthArguments.Add($"--out={outputType}");
            
            var extension = outputType switch
            {
                "dot" => ".dot",
                "verilog" => ".v",
                _ => ".file"
            };

            if (directVerilogOutput)
            {
                ghdlSynthArguments.Add($"-o={Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(file.FullPath))}{extension}");
            }
            
            ghdlSynthArguments.Add($"{GetLibraryPrefixForToplevel(root)}{top}");

            var synth = await ExecuteGhdlAsync(ghdlSynthArguments, workingDirectory,
                "Running GHDL Synth...", AppState.Loading, true);
            if (!synth.success) return false;

            if (!directVerilogOutput)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(file.FullPath) + extension),
                    synth.stdout);
            }

            return true;
        }

        return false;
    }

    private Task SimulateCurrentFileAsync()
    {
        if (_dockService.CurrentDocument?.CurrentFile is IProjectFile selectedFile)
            return SimulateFileAsync(selectedFile);
        return Task.CompletedTask;
    }

    public async Task<bool> SimulateFileAsync(IProjectFile file)
    {
        if (file.Root is UniversalFpgaProjectRoot root)
        {
            _dockService.Show<IOutputService>();

            var settings = await TestBenchContextManager.LoadContextAsync(file);

            var top = Path.GetFileNameWithoutExtension(file.FullPath);
            var workingDirectory = root.FullPath;
            string buildDirectory = Path.Combine(workingDirectory, "build");
            List<string> ghdlOptions = [];
            ghdlOptions.Add($"--workdir={buildDirectory}");
            ghdlOptions.Add($"-P{buildDirectory}");

            List<string> simulatingOptions = [];

            var waveOutput = settings.GetBenchProperty(nameof(GhdlSimulatorToolbarViewModel.WaveOutputFormat)) ?? "VCD";

            var waveOutputArgument = waveOutput switch
            {
                "VCD" => "vcd",
                "GHW" => "wave",
                "FST" => "fst",
                _ => string.Empty
            };
            
            await AddSimFileExtensionToIncludesAsync(root, waveOutput);

            var waveFilePath = Path.Combine(file.TopFolder!.RelativePath, $"{top}.{waveOutput.ToLower()}");
            var waveFormFileArgument = $"--{waveOutputArgument}={waveFilePath}";

            var additionalGhdlOptions =
                settings.GetBenchProperty(nameof(GhdlSimulatorToolbarViewModel.AdditionalGhdlOptions));
            if (additionalGhdlOptions != null) ghdlOptions.AddRange(additionalGhdlOptions.Split(' '));

            var additionalGhdlSimOptions =
                settings.GetBenchProperty(nameof(GhdlSimulatorToolbarViewModel.AdditionalGhdlSimOptions));
            if (additionalGhdlSimOptions != null) simulatingOptions.AddRange(additionalGhdlSimOptions.Split(' '));

            var vhdlStandard = root.GetProjectProperty("VHDL_Standard") ?? settings.GetBenchProperty(nameof(GhdlSimulatorToolbarViewModel.VhdlStandard));
            if (vhdlStandard != null) ghdlOptions.Add($"--std={vhdlStandard}");

            var assertLevel = settings.GetBenchProperty(nameof(GhdlSimulatorToolbarViewModel.AssertLevel));
            if (assertLevel != null) simulatingOptions.Add($"--assert-level={assertLevel}");

            var stopTime = settings.GetBenchProperty(nameof(GhdlSimulatorToolbarViewModel.SimulationStopTime));
            if (stopTime != null) simulatingOptions.Add($"--stop-time={stopTime}");

            var elaborateResult = await ElaborateAsync(file, settings);
            if (!elaborateResult) return false;

            if (file.Root.SearchRelativePath(waveFilePath) is not IFile waveFormFile)
            {
                waveFormFile =
                    _projectExplorerService.GetTemporaryFile(Path.Combine(file.Root.RootFolderPath, waveFilePath));
            }

            //Open VCD inside IDE and prepare to stream
            if (waveOutput == "VCD")
            {
                if (!File.Exists(waveFormFile.FullPath)) await File.Create(waveFormFile.FullPath).DisposeAsync();

                var doc = await _dockService.OpenFileAsync(waveFormFile);

                // ReSharper disable once SuspiciousTypeConversion.Global
                if (doc is IStreamableDocument vcd)
                {
                    vcd.PrepareLiveStream();
                }
            }

            List<string> ghdlRunArguments = ["-r"];
            ghdlRunArguments.AddRange(ghdlOptions);
            ghdlRunArguments.Add($"{GetLibraryPrefixForToplevel(root)}{top}");
            ghdlRunArguments.Add(waveFormFileArgument);
            ghdlRunArguments.AddRange(simulatingOptions);

            var run = await ExecuteGhdlAsync(ghdlRunArguments, workingDirectory,
                "Running GHDL Simulation...", AppState.Loading, true);

            if (run.success && waveOutput is "GHW" or "FST")
            {
                _ = await _dockService.OpenFileAsync(waveFormFile);
            }

            return run.success;
        }

        return false;
    }

    private async Task AddSimFileExtensionToIncludesAsync(UniversalFpgaProjectRoot root, string extension)
    {
        string actualExtension = extension switch
        {
            "VCD" => "vcd",
            "GHW" => "ghw",
            "FST" => "fst",
            _ => string.Empty
        };

        if (actualExtension == string.Empty)
        {
            return;
        }

        var includedFileTypes = root.GetProjectPropertyArray("Include");

        if (includedFileTypes is null)
        {
            return;
        }

        if (includedFileTypes.Contains($"*.{actualExtension}"))
        {
            return;
        }
        
        root.AddToProjectPropertyArray("Include", $"*.{actualExtension}");

        await _projectExplorerService.SaveProjectAsync(root);
    }
}