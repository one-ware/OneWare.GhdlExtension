using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.Utils;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
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
    private readonly IChildProcessService _childProcessService;

    public AsyncRelayCommand SimulateCommand { get; }

    public AsyncRelayCommand SynthToDotCommand { get; }

    public AsyncRelayCommand SynthToVerilogCommand { get; }

    private string _path = string.Empty;

    private readonly NativeToolContainer _nativeToolContainer;

    public GhdlService(ILogger logger, IApplicationStateService applicationStateService, IDockService dockService,
        IProjectExplorerService projectExplorerService, ISettingsService settingsService,
        INativeToolService nativeToolService, IChildProcessService childProcessService)
    {
        _logger = logger;
        _applicationStateService = applicationStateService;
        _dockService = dockService;
        _projectExplorerService = projectExplorerService;
        _nativeToolService = nativeToolService;
        _childProcessService = childProcessService;
        _nativeToolContainer = nativeToolService.Get("ghdl")!;

        settingsService.GetSettingObservable<string>(GhdlModule.GhdlPathSetting).Subscribe(x =>
        {
            _path = x;
            if (File.Exists(x))
            {
                var environmentPathSetting = PlatformHelper.Platform switch
                {
                    PlatformId.WinX64 or PlatformId.WinArm64 => $";{Path.GetDirectoryName(x)};",
                    _ => $":{Path.GetDirectoryName(x)}:"
                };
                var currentPath = Environment.GetEnvironmentVariable("PATH");
                Environment.SetEnvironmentVariable("PATH", $"{environmentPathSetting}{currentPath}");
            }
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
        AppState state = AppState.Loading, bool showTimer = false, Action<string>? outputAction = null, Func<string, bool>? errorAction = null)
    {
        if (!File.Exists(_path))
        {
            var install = await InstallGhdlAsync();
            if (!install) return (false,string.Empty);
        }
        return await _childProcessService.ExecuteShellAsync("ghdl", arguments, workingDirectory,
            status, state, showTimer, outputAction, errorAction);
    }

    private async Task<bool> InstallGhdlAsync()
    {
        return await _nativeToolService.InstallAsync(_nativeToolContainer);
    }

    private Task SynthCurrentFileAsync(string output)
    {
        if (_dockService.CurrentDocument?.CurrentFile is IProjectFile selectedFile)
            return SynthAsync(selectedFile, output);
        return Task.CompletedTask;
    }

    private async Task<bool> ElaborateAsync(IProjectFile file)
    {
        var vhdlFiles = file.Root.Files
            .Where(x => x.Extension is ".vhd" or ".vhdl")
            .Select(x => x.RelativePath);

        var top = Path.GetFileNameWithoutExtension(file.FullPath);
        var workingDirectory = file.Root!.FullPath;
        List<string> ghdlOptions = ["--std=02"];

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

    public async Task SynthAsync(IProjectFile file, string outputType)
    {
        _dockService.Show<IOutputService>();

        var top = Path.GetFileNameWithoutExtension(file.FullPath);
        var workingDirectory = file.Root!.FullPath;
        List<string> ghdlOptions = ["--std=02"];

        var elaborateResult = await ElaborateAsync(file);
        if (!elaborateResult) return;

        List<string> ghdlSynthArguments = ["--synth"];
        ghdlSynthArguments.AddRange(ghdlOptions);
        ghdlSynthArguments.Add($"--out={outputType}");
        ghdlSynthArguments.Add(top);

        var synth = await ExecuteGhdlAsync(ghdlSynthArguments, workingDirectory,
            "Running GHDL Synth...", AppState.Loading, true);
        if (!synth.success) return;

        var extension = outputType switch
        {
            "dot" => ".dot",
            "verilog" => ".v",
            _ => ".file"
        };

        await File.WriteAllTextAsync(
            Path.Combine(Path.GetDirectoryName(file.FullPath) ?? "",
                Path.GetFileNameWithoutExtension(file.FullPath) + extension), synth.output);
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

        var top = Path.GetFileNameWithoutExtension(file.FullPath);
        var workingDirectory = file.Root!.FullPath;
        List<string> ghdlOptions = ["--std=02"];
        var vcdPath = Path.Combine(file.TopFolder!.RelativePath,$"{top}.vcd");
        var waveFormFileArgument = $"--vcd={vcdPath}";
        List<string> simulatingOptions = ["--ieee-asserts=disable"];

        var elaborateResult = await ElaborateAsync(file);
        if (!elaborateResult) return;

        var openFile = file.TopFolder!.SearchName($"{top}.vcd") as IProjectFile;
        openFile ??= file.TopFolder.AddFile(vcdPath, true);

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
    }
}