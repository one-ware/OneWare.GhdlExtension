using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using OneWare.Essentials.Enums;
using OneWare.Essentials.Models;
using OneWare.Essentials.Services;
using OneWare.Essentials.ViewModels;
using OneWare.Ghdl.ViewModels;
using OneWare.UniversalFpgaProjectSystem.Context;

namespace OneWare.Ghdl.Services;

public class GhdlService
{
    private readonly ILogger _logger;
    private readonly IDockService _dockService;
    private readonly IPackageService _packageService;
    private readonly IChildProcessService _childProcessService;
    private readonly IEnvironmentService _environmentService;
    private readonly IOutputService _outputService;
    private readonly ISettingsService _settingsService;

    public AsyncRelayCommand SimulateCommand { get; }

    public AsyncRelayCommand SynthToDotCommand { get; }

    public AsyncRelayCommand SynthToVerilogCommand { get; }

    private string _path = string.Empty;

    public GhdlService(ILogger logger, IDockService dockService, ISettingsService settingsService, IPackageService packageService, IChildProcessService childProcessService, IEnvironmentService environmentService,
        IOutputService outputService)
    {
        _logger = logger;
        _dockService = dockService;
        _packageService = packageService;
        _childProcessService = childProcessService;
        _environmentService = environmentService;
        _outputService = outputService;
        _settingsService = settingsService;

        settingsService.GetSettingObservable<string>(GhdlModule.GhdlPathSetting).Subscribe(x =>
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

    private async Task<(bool success, string output)> ExecuteGhdlAsync(IReadOnlyCollection<string> arguments, string workingDirectory, string status,
        AppState state = AppState.Loading, bool showTimer = false)
    {
        if (!File.Exists(_path) || (_settingsService.GetSettingValue<bool>("Experimental_AutoDownloadBinaries") && 
                                    _packageService.Packages.GetValueOrDefault(GhdlModule.GhdlPackage.Id!) is {Status: PackageStatus.Available or PackageStatus.UpdateAvailable or PackageStatus.Installing}))
        {
            var install = await InstallGhdlAsync();
            if (!install)
            {
                _logger.Warning("GHDL not found. Please set the path in the settings or (re)install it from the package manager.");
                return (false,string.Empty);
            }
        }

        return await _childProcessService.ExecuteShellAsync("ghdl", arguments, workingDirectory,
            status, state, showTimer, x =>
            {
                if (x.StartsWith("ghdl:error:"))
                {
                    _logger.Error(x);
                    return false;
                }

                _outputService.WriteLine(x);
                return true;
            });
    }

    private async Task<bool> InstallGhdlAsync()
    {
        if (_packageService.Packages.GetValueOrDefault(GhdlModule.GhdlPackage.Id!) is {Status: PackageStatus.Available or PackageStatus.UpdateAvailable or PackageStatus.Installing})
        {
            if (!_settingsService.GetSettingValue<bool>("Experimental_AutoDownloadBinaries")) return false;
            if(!await _packageService.InstallAsync(GhdlModule.GhdlPackage)) return false;
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
        var vhdlFiles = file.Root.Files
            .Where(x => x.Extension is ".vhd" or ".vhdl")
            .Select(x => x.RelativePath);

        var top = Path.GetFileNameWithoutExtension(file.FullPath);
        var workingDirectory = file.Root!.FullPath;
        
            
        List<string> ghdlOptions = [];
        
        var vhdlStandard = context.GetBenchProperty(nameof(GhdlSimulatorToolbarViewModel.VhdlStandard));
        if(vhdlStandard != null) ghdlOptions.Add($"--std={vhdlStandard}");
    
        var additionalGhdlOptions = context.GetBenchProperty(nameof(GhdlSimulatorToolbarViewModel.AdditionalGhdlOptions));
        if(additionalGhdlOptions != null) ghdlOptions.AddRange(additionalGhdlOptions.Split(' '));
        
        List<string> ghdlInitArguments = ["-i"];
        ghdlInitArguments.AddRange(ghdlOptions);
        ghdlInitArguments.AddRange(vhdlFiles);

        List<string> ghdlMakeArguments = ["-m"];
        ghdlMakeArguments.AddRange(ghdlOptions);
        ghdlMakeArguments.Add(top);

        List<string> ghdlElaborateArguments = ["-e"];
        ghdlElaborateArguments.AddRange(ghdlOptions);
        ghdlElaborateArguments.Add(top);

        var initFiles = await ExecuteGhdlAsync(ghdlInitArguments, workingDirectory,
            "GHDL Init...",
            AppState.Loading, true);
        if (!initFiles.success) return false;

        var make = await ExecuteGhdlAsync(ghdlMakeArguments, workingDirectory,
            "Running GHDL Make...", AppState.Loading, true);
        if (!make.success) return false;

        var elaboration = await ExecuteGhdlAsync(ghdlElaborateArguments, workingDirectory,
            "Running GHDL Elaboration...", AppState.Loading, true);
        if (!elaboration.success) return false;

        return true;
    }

    public async Task<bool> SynthAsync(IProjectFile file, string outputType, string outputDirectory)
    {
        _dockService.Show<IOutputService>();
        
        var settings = await TestBenchContextManager.LoadContextAsync(file);

        var top = Path.GetFileNameWithoutExtension(file.FullPath);
        var workingDirectory = file.Root!.FullPath;
        List<string> ghdlOptions = ["--std=02"];

        var elaborateResult = await ElaborateAsync(file, settings);
        if (!elaborateResult) return false;

        List<string> ghdlSynthArguments = ["--synth"];
        ghdlSynthArguments.AddRange(ghdlOptions);
        ghdlSynthArguments.Add($"--out={outputType}");
        ghdlSynthArguments.Add(top);

        var synth = await ExecuteGhdlAsync(ghdlSynthArguments, workingDirectory,
            "Running GHDL Synth...", AppState.Loading, true);
        if (!synth.success) return false;

        var extension = outputType switch
        {
            "dot" => ".dot",
            "verilog" => ".v",
            _ => ".file"
        };

        await File.WriteAllTextAsync(Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(file.FullPath) + extension), synth.output);

        return true;
    }

    private Task SimulateCurrentFileAsync()
    {
        if (_dockService.CurrentDocument?.CurrentFile is IProjectFile selectedFile)
            return SimulateFileAsync(selectedFile);
        return Task.CompletedTask;
    }

    public async Task<bool> SimulateFileAsync(IProjectFile file)
    {
        _dockService.Show<IOutputService>();
        
        var settings = await TestBenchContextManager.LoadContextAsync(file);
        
        var top = Path.GetFileNameWithoutExtension(file.FullPath);
        var workingDirectory = file.Root!.FullPath;
        List<string> ghdlOptions = [];
        var vcdPath = Path.Combine(file.TopFolder!.RelativePath,$"{top}.vcd");
        var waveFormFileArgument = $"--vcd={vcdPath}";
        List<string> simulatingOptions = [];
        
        var additionalGhdlOptions = settings.GetBenchProperty(nameof(GhdlSimulatorToolbarViewModel.AdditionalGhdlOptions));
        if(additionalGhdlOptions != null) ghdlOptions.AddRange(additionalGhdlOptions.Split(' '));
        
        var additionalGhdlSimOptions = settings.GetBenchProperty(nameof(GhdlSimulatorToolbarViewModel.AdditionalGhdlSimOptions));
        if(additionalGhdlSimOptions != null) simulatingOptions.AddRange(additionalGhdlSimOptions.Split(' '));
        
        var vhdlStandard = settings.GetBenchProperty(nameof(GhdlSimulatorToolbarViewModel.VhdlStandard));
        if(vhdlStandard != null) ghdlOptions.Add($"--std={vhdlStandard}");
        
        var assertLevel = settings.GetBenchProperty(nameof(GhdlSimulatorToolbarViewModel.AssertLevel));
        if(assertLevel != null) simulatingOptions.Add($"--assert-level={assertLevel}");
        
        var stopTime = settings.GetBenchProperty(nameof(GhdlSimulatorToolbarViewModel.SimulationStopTime));
        if(stopTime != null) simulatingOptions.Add($"--stop-time={stopTime}");

        var elaborateResult = await ElaborateAsync(file, settings);
        if (!elaborateResult) return false;

        var openFile = file.Root.SearchRelativePath(vcdPath) as IProjectFile;
        openFile ??= file.Root.AddFile(vcdPath, true);

        var doc = await _dockService.OpenFileAsync(openFile);
        if (doc is IStreamableDocument vcd)
        {
            vcd.PrepareLiveStream();
        }

        List<string> ghdlRunArguments = ["-r"];
        ghdlRunArguments.AddRange(ghdlOptions);
        ghdlRunArguments.Add(top);
        ghdlRunArguments.Add(waveFormFileArgument);
        ghdlRunArguments.AddRange(simulatingOptions);

        var run = await ExecuteGhdlAsync(ghdlRunArguments, workingDirectory,
            "Running GHDL Simulation...", AppState.Loading, true);

        return run.success;
    }
}