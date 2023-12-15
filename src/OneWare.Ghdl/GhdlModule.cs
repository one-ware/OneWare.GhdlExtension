using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using OneWare.Ghdl.Services;
using OneWare.Ghdl.Views;
using OneWare.SDK.Helpers;
using OneWare.SDK.Models;
using OneWare.SDK.Services;
using OneWare.SDK.ViewModels;
using Prism.Ioc;
using Prism.Modularity;

namespace OneWare.Ghdl;

public class GhdlModule : IModule
{
    public const string GhdlPathSetting = "GhdlModule_GhdlPath";
    
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<GhdlService>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        var nativeToolService = containerProvider.Resolve<INativeToolService>();
        var nativeTool = nativeToolService.Register("ghdl");

        nativeTool.AddPlatform(PlatformId.WinX64,
                "https://github.com/ghdl/ghdl/releases/download/v3.0.0/ghdl-MINGW32.zip")
            .WithShortcut("ghdlExecutable", "GHDL/bin/ghdl.exe", GhdlPathSetting);
        
        nativeTool.AddPlatform(PlatformId.LinuxX64,
                "https://cdn.vhdplus.com/ghdl/ghdl2.0.0-ubuntu20.zip")
            .WithShortcut("ghdlExecutable", "ghdl/bin/ghdl", GhdlPathSetting);
        
        nativeTool.AddPlatform(PlatformId.OsxX64,
                "https://github.com/ghdl/ghdl/releases/download/v3.0.0/ghdl-macos-11-mcode.tgz")
            .WithShortcut("ghdlExecutable", "ghdl/bin/ghdl", GhdlPathSetting);
        
        containerProvider.Resolve<ISettingsService>().RegisterTitledPath("Simulator", "GHDL", GhdlPathSetting, "GHDL Path", "Path for GHDL executable", 
            nativeTool.GetShortcutPathOrEmpty("ghdlExecutable"),
            null, containerProvider.Resolve<IPaths>().NativeToolsDirectory, File.Exists);
        
        var ghdlService = containerProvider.Resolve<GhdlService>();
        
        containerProvider.Resolve<IWindowService>().RegisterMenuItem("MainWindow_MainMenu/Ghdl",
            new MenuItemViewModel("SimulateGHDL")
            {
                Header = "Simulate project with GHDL",
                Command = ghdlService.SimulateCommand,
            });

        containerProvider.Resolve<IWindowService>().RegisterUiExtension("MainWindow_LeftToolBarExtension",
            new GhdlMainWindowToolBarExtension()
            {
                DataContext = ghdlService
            });
        
        containerProvider.Resolve<IProjectExplorerService>().RegisterConstructContextMenu(x =>
        {
            if (x is [IProjectFile{Extension: ".vhd" or ".vhdl"} file])
            {
                return new[]
                {
                    new MenuItemViewModel("GHDL")
                    {
                        Header = "GHDL",
                        Items =
                        [
                            new MenuItemViewModel("SimulateWithGHDL")
                            {
                                Header = "Simulate with GHDL",
                                Command = new AsyncRelayCommand(() => ghdlService.SimulateFileAsync(file)),
                                IconObservable = Application.Current!.GetResourceObservable("Material.Pulse"),
                            },

                            new MenuItemViewModel("SynthGhdlToVerilog")
                            {
                                Header = "Convert to Verilog Netlist",
                                Command = new AsyncRelayCommand(() => ghdlService.SynthAsync(file, "verilog")),
                            },

                            new MenuItemViewModel("SynthGhdlToVerilog")
                            {
                                Header = "Convert to Dot Netlist",
                                Command = new AsyncRelayCommand(() => ghdlService.SynthAsync(file, "dot")),
                            }
                        ]
                    }
                };
            }
            return null;
        });
    }
}