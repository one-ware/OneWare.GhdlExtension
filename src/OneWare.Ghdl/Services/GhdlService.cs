using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using OneWare.Essentials.Enums;
using OneWare.Essentials.Helpers;
using OneWare.Essentials.Models;
using OneWare.Essentials.NativeTools;
using OneWare.Essentials.Services;
using OneWare.Essentials.ViewModels;

namespace OneWare.Ghdl.Services;

public class GhdlService
{
    private readonly ILogger _logger;
    private readonly IApplicationStateService _applicationStateService;
    private readonly IDockService _dockService;
    private readonly IProjectExplorerService _projectExplorerService;
    private readonly INativeToolService _nativeToolService;

    public AsyncRelayCommand SimulateCommand { get; }
    
    public AsyncRelayCommand SynthToDotCommand { get; }
    
    public AsyncRelayCommand SynthToVerilogCommand { get; }

    private string _path = string.Empty;
    
    private readonly NativeToolContainer _nativeToolContainer;
    
    public GhdlService(ILogger logger, IApplicationStateService applicationStateService, IDockService dockService, IProjectExplorerService projectExplorerService, ISettingsService settingsService, INativeToolService nativeToolService)
    {
        _logger = logger; 
        _applicationStateService = applicationStateService;
        _dockService = dockService;
        _projectExplorerService = projectExplorerService;
        _nativeToolService = nativeToolService;
        _nativeToolContainer = nativeToolService.Get("ghdl")!;

        settingsService.GetSettingObservable<string>(GhdlModule.GhdlPathSetting).Subscribe(x =>
        {
            _path = x;
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

    private async Task<bool> InstallGhdlAsync()
    {
        var result = await _nativeToolService.InstallAsync(_nativeToolContainer);
        
        Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + $":{Path.GetDirectoryName(_path)}");
        
        return result;
    }
    
    private async Task<(bool success, string output)> ExecuteGhdlShellAsync(string workingDirectory, string arguments, string status = "Running GHDL", AppState state = AppState.Loading)
    {
        var success = true;

        if (!File.Exists(_path))
        {
            if(!await InstallGhdlAsync()) return (false, string.Empty);
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            PlatformHelper.ChmodFile(_path);
        }
        
        _logger.Log($"ghdl {arguments}", ConsoleColor.DarkCyan, true, Brushes.CornflowerBlue);

        var output = string.Empty;
        
        var startInfo = GetGhdlProcessStartInfo(workingDirectory, arguments);

        using var activeProcess = new Process();
        activeProcess.StartInfo = startInfo;
        var key = _applicationStateService.AddState(status, state, () => activeProcess?.Kill());

        activeProcess.OutputDataReceived += (o, i) =>
        {
            if (string.IsNullOrEmpty(i.Data)) return;
            if (i.Data.Contains("error"))
            {
                success = false;
                Dispatcher.UIThread.Post(() => _logger.Error(i.Data));
            }
            else if (i.Data.Contains("warning"))
            {
                Dispatcher.UIThread.Post(() => _logger.Warning(i.Data));
            }
            else
            {
                Dispatcher.UIThread.Post(() => _logger.Log(i.Data));
            }
            output += i.Data + '\n';
        };
        activeProcess.ErrorDataReceived += (o, i) =>
        {
            if (!string.IsNullOrWhiteSpace(i.Data))
            {
                if (i.Data.Contains("warning", StringComparison.OrdinalIgnoreCase))
                {
                    Dispatcher.UIThread.Post(() => _logger.Error("[GHDL Warning]: " + i.Data));
                    //ParseGhdlError(i.Data, ErrorType.Warning);
                }
                else
                {
                    success = false;
                    Dispatcher.UIThread.Post(() => _logger.Error("[GHDL Error]: " + i.Data));
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
        _applicationStateService.RemoveState(key);

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