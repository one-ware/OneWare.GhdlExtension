using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using OneWare.Essentials.Helpers;
using OneWare.Essentials.Models;
using OneWare.Essentials.PackageManager;
using OneWare.Essentials.Services;
using OneWare.Essentials.ViewModels;
using OneWare.GhdlExtension.Services;
using OneWare.UniversalFpgaProjectSystem.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace OneWare.GhdlExtension;

public class GhdlExtensionModule : IModule
{
    public static readonly Package GhdlPackage = new()
    {
        Category = "Binaries",
        Id = "ghdl",
        Type = "NativeTool",
        Name = "GHDL",
        Description = "Open Source VHDL Simulator",
        License = "GPL 2.0",
        IconUrl = "https://raw.githubusercontent.com/ghdl/ghdl/master/logo/icon.png",
        Links = 
        [
            new PackageLink()
            {
                Name = "GitHub",
                Url = "https://github.com/ghdl/ghdl"
            }
        ],
        Tabs = 
        [
            new PackageTab()
            {
                Title = "License",
                ContentUrl = "https://raw.githubusercontent.com/ghdl/ghdl/master/COPYING.md"
            }
        ],
        Versions =
        [
            new PackageVersion()
            {
                Version = "4.0.0",
                Targets =
                [
                    new PackageTarget()
                    {
                        Target = "win-x64",
                        Url = "https://github.com/ghdl/ghdl/releases/download/v4.0.0/ghdl-UCRT64.zip",
                        AutoSetting =
                        [
                            new PackageAutoSetting()
                            {
                                RelativePath = "GHDL/bin/ghdl.exe",
                                SettingKey = GhdlPathSetting
                            }
                        ]
                    },
                    new PackageTarget()
                    {
                        Target = "linux-x64",
                        Url = "https://cdn.vhdplus.com/ghdl/ghdl2.0.0-ubuntu20.zip",
                        AutoSetting =
                        [
                            new PackageAutoSetting()
                            {
                                RelativePath = "bin/ghdl",
                                SettingKey = GhdlPathSetting
                            }
                        ]
                    },
                    new PackageTarget()
                    {
                        Target = "osx-x64",
                        Url = "https://github.com/ghdl/ghdl/releases/download/v4.0.0/ghdl-macos-11-mcode.tgz",
                        AutoSetting =
                        [
                            new PackageAutoSetting()
                            {
                                RelativePath = "bin/ghdl",
                                SettingKey = GhdlPathSetting
                            }
                        ]
                    }
                ]
            },
            new PackageVersion()
            {
                Version = "4.1.0",
                Targets =
                [
                    new PackageTarget()
                    {
                        Target = "win-x64",
                        Url = "https://github.com/ghdl/ghdl/releases/download/v4.1.0/ghdl-UCRT64.zip",
                        AutoSetting =
                        [
                            new PackageAutoSetting()
                            {
                                RelativePath = "GHDL/bin/ghdl.exe",
                                SettingKey = GhdlPathSetting
                            }
                        ]
                    },
                    new PackageTarget()
                    {
                        Target = "linux-x64",
                        Url = "https://github.com/ghdl/ghdl/releases/download/v4.1.0/ghdl-gha-ubuntu-20.04-mcode.tgz",
                        AutoSetting =
                        [
                            new PackageAutoSetting()
                            {
                                RelativePath = "bin/ghdl",
                                SettingKey = GhdlPathSetting
                            }
                        ]
                    },
                    new PackageTarget()
                    {
                        Target = "osx-x64",
                        Url = "https://github.com/ghdl/ghdl/releases/download/v4.1.0/ghdl-macos-11-mcode.tgz",
                        AutoSetting =
                        [
                            new PackageAutoSetting()
                            {
                                RelativePath = "bin/ghdl",
                                SettingKey = GhdlPathSetting
                            }
                        ]
                    }
                ]
            },
            new PackageVersion()
            {
                Version = "5.0.1",
                Targets =
                [
                    new PackageTarget()
                    {
                        Target = "win-x64",
                        Url = "https://github.com/ghdl/ghdl/releases/download/v5.0.1/ghdl-mcode-5.0.1-ucrt64.zip",
                        AutoSetting =
                        [
                            new PackageAutoSetting()
                            {
                                RelativePath = "bin/ghdl.exe",
                                SettingKey = GhdlPathSetting
                            }
                        ]
                    },
                    new PackageTarget()
                    {
                        Target = "linux-x64",
                        Url = "https://github.com/ghdl/ghdl/releases/download/v5.0.1/ghdl-mcode-5.0.1-ubuntu24.04-x86_64.tar.gz",
                        AutoSetting =
                        [
                            new PackageAutoSetting()
                            {
                                RelativePath = "bin/ghdl",
                                SettingKey = GhdlPathSetting
                            }
                        ]
                    },
                    new PackageTarget()
                    {
                        Target = "osx-x64",
                        Url = "https://github.com/ghdl/ghdl/releases/download/v5.0.1/ghdl-mcode-5.0.1-macos13-x86_64.tar.gz",
                        AutoSetting =
                        [
                            new PackageAutoSetting()
                            {
                                RelativePath = "bin/ghdl",
                                SettingKey = GhdlPathSetting
                            }
                        ]
                    }
                ]
            }
        ]
    };
    
    public const string GhdlPathSetting = "GhdlModule_GhdlPath";

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<GhdlService>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        containerProvider.Resolve<IPackageService>().RegisterPackage(GhdlPackage);
        
        containerProvider.Resolve<ISettingsService>().RegisterTitledFilePath("Simulator", "GHDL", GhdlPathSetting,
            "GHDL Path", "Path for GHDL executable", "",
            null, containerProvider.Resolve<IPaths>().NativeToolsDirectory, File.Exists);

        var ghdlService = containerProvider.Resolve<GhdlService>();

        // containerProvider.Resolve<IWindowService>().RegisterMenuItem("MainWindow_MainMenu/Ghdl",
        //     new MenuItemViewModel("SimulateGHDL")
        //     {
        //         Header = "Simulate project with GHDL",
        //         Command = ghdlService.SimulateCommand,
        //     });

        // containerProvider.Resolve<IWindowService>()
        //     .RegisterUiExtension("MainWindow_LeftToolBarExtension", new UiExtension(x => new GhdlMainWindowToolBarExtension()
        //     {
        //         DataContext = ghdlService
        //     }));
        
        containerProvider.Resolve<FpgaService>().RegisterSimulator<GhdlSimulator>();

        containerProvider.Resolve<IProjectExplorerService>().RegisterConstructContextMenu((x,l) =>
        {
            if (x is [IProjectFile { Extension: ".vhd" or ".vhdl" } file])
            {
                l.Add(new MenuItemViewModel("GHDL")
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
                            Command = new AsyncRelayCommand(() => ghdlService.SynthAsync(file, "verilog", file.TopFolder!.FullPath)),
                        },

                        new MenuItemViewModel("SynthGhdlToVerilog")
                        {
                            Header = "Convert to Dot Netlist",
                            Command = new AsyncRelayCommand(() => ghdlService.SynthAsync(file, "dot", file.TopFolder!.FullPath)),
                        }
                    ]
                });
            }
        });
        
        containerProvider.Resolve<FpgaService>().RegisterPreCompileStep<GhdlVhdlToVerilogPreCompileStep>();
    }
}