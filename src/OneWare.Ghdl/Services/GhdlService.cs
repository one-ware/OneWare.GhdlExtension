using System.Diagnostics;
using System.Reflection;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using OneWare.SDK.Enums;
using OneWare.SDK.Helpers;
using OneWare.SDK.Models;
using OneWare.SDK.Services;
using OneWare.SDK.ViewModels;

namespace OneWare.Ghdl.Services;

public class GhdlService
{
    private readonly ILogger _logger;
    private readonly IActive _active;
    private readonly IDockService _dockService;
    private readonly IProjectExplorerService _projectExplorerService;

    public AsyncRelayCommand SimulateCommand { get; }
    
    public AsyncRelayCommand SynthToDotCommand { get; }
    
    public AsyncRelayCommand SynthToVerilogCommand { get; }

    private readonly string _path;
    
    public GhdlService(ILogger logger, IActive active, IDockService dockService, IProjectExplorerService projectExplorerService)
    {
        _logger = logger; 
        _active = active;
        _dockService = dockService;
        _projectExplorerService = projectExplorerService;
        
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        _path = PlatformHelper.Platform switch
        {
            PlatformId.WinX64 => $"{assemblyPath}/native_tools/win-x64/ghdl/GHDL/bin/ghdl.exe",
            PlatformId.LinuxX64 => $"{assemblyPath}/native_tools/linux-x64/ghdl/bin/ghdl",
            PlatformId.OsxX64 => $"{assemblyPath}/native_tools/osx-x64/ghdl/bin/ghdl",
            PlatformId.OsxArm64 => $"{assemblyPath}/native_tools/osx-arm64/ghdl/bin/ghdl",
            _ => throw new NotSupportedException("GHDL not supported on this platform"),
        };
        
        Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + $":{Path.GetDirectoryName(_path)}");
        
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
    
    private ProcessStartInfo GetGhdlProcessStartInfo(string workingDirectory, string arguments)
    {
        return new ProcessStartInfo
        {
            FileName = _path,
            Arguments = $"{arguments}",
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
    }
    
    private async Task<(bool success, string output)> ExecuteGhdlShellAsync(string workingDirectory, string arguments, string status = "Running GHDL", AppState state = AppState.Loading)
    {
        var success = true;
        
        _logger.Log($"ghdl {arguments}", ConsoleColor.DarkCyan, true, Brushes.CornflowerBlue);

        var output = string.Empty;
        
        var startInfo = GetGhdlProcessStartInfo(workingDirectory, arguments);

        using var activeProcess = new Process();
        activeProcess.StartInfo = startInfo;
        var key = _active.AddState(status, state, () => activeProcess?.Kill());

        activeProcess.OutputDataReceived += (o, i) =>
        {
            if (string.IsNullOrEmpty(i.Data)) return;
            if (i.Data.Contains("error"))
            {
                success = false;
                _logger.Error(i.Data);
            }
            else if (i.Data.Contains("warning"))
            {
                _logger.Warning(i.Data);
            }
            else
            {
                _logger.Log(i.Data);
            }
            output += i.Data + '\n';
        };
        activeProcess.ErrorDataReceived += (o, i) =>
        {
            if (!string.IsNullOrWhiteSpace(i.Data))
            {
                if (i.Data.Contains("warning", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Warning("[GHDL Warning]: " + i.Data);
                    //ParseGhdlError(i.Data, ErrorType.Warning);
                }
                else
                {
                    success = false;
                    _logger.Error("[GHDL Error]: " + i.Data);
                    //ParseGhdlError(i.Data, ErrorType.Error);
                }
            }
        };

        try
        {
            activeProcess.Start();
            activeProcess.BeginOutputReadLine();
            activeProcess.BeginErrorReadLine();

            await Task.Run(() => activeProcess.WaitForExit());
        }
        catch (Exception e)
        {
            _logger.Error(e.Message, e);
            success = false;
        }

        if (key.Terminated) success = false;
        _active.RemoveState(key);

        return (success,output);
    }

    private Task SynthCurrentFileAsync(string output)
    {
        if (_dockService.CurrentDocument?.CurrentFile is IProjectFile selectedFile)
            return SynthAsync(selectedFile, output);
        return Task.CompletedTask;
    }
    
    public async Task SynthAsync(IProjectFile file, string output)
    {
        _dockService.Show<IOutputService>();

        var vhdlFiles = string.Join(' ',
            file.Root.Files.Where(x => x.Extension is ".vhd" or ".vhdl")
                .Select(x => "\"" + x.FullPath + "\""));

        var top = Path.GetFileNameWithoutExtension(file.FullPath);
        var ghdlOptions = "--std=02";
        var folder = file.TopFolder!.FullPath;

        var initFiles = await ExecuteGhdlShellAsync(folder, $"-i {ghdlOptions} {vhdlFiles}",
            "GHDL Initializing generated files");
        if (!initFiles.success) return;
        var make = await ExecuteGhdlShellAsync(folder, $"-m {ghdlOptions} {top}", "Running GHDL Make");
        if (!make.success) return;
        var elaboration = await ExecuteGhdlShellAsync(folder, $"-e {ghdlOptions} {top}",
            "Running GHDL Elaboration");
        if (!elaboration.success) return;
        var synth = await ExecuteGhdlShellAsync(folder, $"--synth {ghdlOptions} --out={output} {top}",
            "Running GHDL Synth");
        if (!synth.success) return;

        var extension = output switch
        {
            "dot" => ".dot",
            "verilog" => ".v",
            _ => ".file"
        };
        
        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(file.FullPath) ?? "", Path.GetFileNameWithoutExtension(file.FullPath)+ extension), synth.output);
    }

    private Task SimulateCurrentFileAsync()
    {
        if (_dockService.CurrentDocument?.CurrentFile is IProjectFile selectedFile)
            return SimulateFileAsync(selectedFile);
        return Task.CompletedTask;
    }
    
    public async Task SimulateFileAsync(IProjectFile file)
    {
        _dockService.Show<IOutputService>();
        
        var vhdlFiles = string.Join(' ',
            file.Root.Files.Where(x => x.Extension is ".vhd" or ".vhdl")
                .Select(x => "\"" + x.FullPath + "\""));

        var top = Path.GetFileNameWithoutExtension(file.FullPath);
        var vcdPath = $"{top}.vcd";
        var waveFormFileArgument = $"--vcd={vcdPath}";
        var ghdlOptions = "--std=02";
        var simulatingOptions = "--ieee-asserts=disable";
        var folder = file.TopFolder!.FullPath;
        
        var initFiles = await ExecuteGhdlShellAsync(folder, $"-i {ghdlOptions} {vhdlFiles}",
            "GHDL Initializing generated files");
        if (!initFiles.success) return;
        var make = await ExecuteGhdlShellAsync(folder, $"-m {ghdlOptions} {top}", "Running GHDL Make");
        if (!make.success) return;
        var elaboration = await ExecuteGhdlShellAsync(folder, $"-e {ghdlOptions} {top}",
            "Running GHDL Elaboration");
        if (!elaboration.success) return;
                    
        var openFile = file.TopFolder.Search($"{top}.vcd") as IProjectFile;
        openFile ??= file.TopFolder.AddFile(vcdPath, true);
        
        var doc = await _dockService.OpenFileAsync(openFile);
        if (doc is IStreamableDocument vcd)
        {   
            vcd.PrepareLiveStream();
        }
        
        var run = await ExecuteGhdlShellAsync(folder,
            $"-r {ghdlOptions} {top} {waveFormFileArgument} {simulatingOptions}",
            "Running GHDL Simulation");
    }
}