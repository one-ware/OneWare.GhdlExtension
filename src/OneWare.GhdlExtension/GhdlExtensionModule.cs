using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneWare.Essentials.Models;
using OneWare.Essentials.PackageManager;
using OneWare.Essentials.Services;
using OneWare.Essentials.ViewModels;
using OneWare.GhdlExtension.Services;
using OneWare.GhdlExtension.ViewModels;
using OneWare.GhdlExtension.Views;
using OneWare.OssCadSuiteIntegration.ViewModels;
using OneWare.OssCadSuiteIntegration.Views;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;
using OneWare.UniversalFpgaProjectSystem.ViewModels;

namespace OneWare.GhdlExtension;

public class GhdlExtensionModule : OneWareModuleBase
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
                                RelativePath = "ghdl-mcode-5.0.1-ubuntu24.04-x86_64/bin/ghdl",
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
                                RelativePath = "ghdl-mcode-5.0.1-macos13-x86_64/bin/ghdl",
                                SettingKey = GhdlPathSetting
                            }
                        ]
                    }
                ]
            },
            new PackageVersion()
            {
                Version = "5.1.1",
                Targets =
                [
                    new PackageTarget()
                    {
                        Target = "win-x64",
                        Url = "https://github.com/ghdl/ghdl/releases/download/v5.1.1/ghdl-mcode-5.1.1-ucrt64.zip",
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
                        Url = "https://github.com/ghdl/ghdl/releases/download/v5.1.1/ghdl-mcode-5.1.1-ubuntu24.04-x86_64.tar.gz",
                        AutoSetting =
                        [
                            new PackageAutoSetting()
                            {
                                RelativePath = "ghdl-mcode-5.0.1-ubuntu24.04-x86_64/bin/ghdl",
                                SettingKey = GhdlPathSetting
                            }
                        ]
                    },
                    new PackageTarget()
                    {
                        Target = "osx-x64",
                        Url = "https://github.com/ghdl/ghdl/releases/download/v5.1.1/ghdl-mcode-5.1.1-macos13-x86_64.tar.gz",
                        AutoSetting =
                        [
                            new PackageAutoSetting()
                            {
                                RelativePath = "ghdl-mcode-5.0.1-macos13-x86_64/bin/ghdl",
                                SettingKey = GhdlPathSetting
                            }
                        ]
                    },
                    new PackageTarget()
                    {
                        Target = "osx-arm",
                        Url = "https://github.com/ghdl/ghdl/releases/download/v5.1.1/ghdl-mcode-5.1.1-macos13-arm.tar.gz",
                        AutoSetting =
                        [
                            new PackageAutoSetting()
                            {
                                RelativePath = "ghdl-mcode-5.0.1-macos13-x86_64/bin/ghdl",
                                SettingKey = GhdlPathSetting
                            }
                        ]
                    }
                ]
            }
        ]
    };
    
    public const string GhdlPathSetting = "GhdlModule_GhdlPath";
    
    IProjectExplorerService? _projectExplorerService;

    public override void RegisterServices(IServiceCollection containerRegistry)
    {
        containerRegistry.AddSingleton<GhdlService>();
        containerRegistry.AddSingleton<GhdlToolchainService>();
        containerRegistry.AddSingleton<GhdlVhdlToVerilogPreCompileStep>();
    }

    public override void Initialize(IServiceProvider serviceProvider)
    {
        var windowService = serviceProvider.Resolve<IWindowService>();
        var projectExplorerService = serviceProvider.Resolve<IProjectExplorerService>();
        var fpgaService = serviceProvider.Resolve<FpgaService>();
        var toolService = serviceProvider.Resolve<IToolService>();
        
        
        
        serviceProvider.Resolve<IPackageService>().RegisterPackage(GhdlPackage);
        
        serviceProvider.Resolve<ISettingsService>().RegisterTitledFilePath("Simulator", "GHDL", GhdlPathSetting,
            "GHDL Path", "Path for GHDL executable", "",
            null, serviceProvider.Resolve<IPaths>().NativeToolsDirectory, File.Exists);

        var ghdlService = serviceProvider.Resolve<GhdlService>();
        var ghdlToolchainService = serviceProvider.Resolve<GhdlToolchainService>();
        
        serviceProvider.Resolve<GhdlToolchainService>().SubscribeToSettings();
        
        serviceProvider.Resolve<FpgaService>().RegisterSimulator<GhdlSimulator>();

        serviceProvider.Resolve<IProjectExplorerService>().RegisterConstructContextMenu((x,l) =>
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
        
        serviceProvider.Resolve<IProjectSettingsService>().AddProjectSetting("GHDL_Libraries", new ListBoxSetting("GHDL libraries", []), 
            file =>
            {
                if (file is UniversalFpgaProjectRoot root)
                {
                    return root.Files.Exists(projectFile => projectFile.Extension == ".vhd" || projectFile.Extension == ".vhdl");
                }
                else
                {
                    return false;
                }
            });
        
        serviceProvider.Resolve<IProjectExplorerService>().RegisterConstructContextMenu(((list, models) =>
        {
            if (list[0] is IProjectFile {Extension: ".vhd" or ".vhdl", Root: UniversalFpgaProjectRoot root } file)
            {
                string associatedLibrary = "";
                
                IEnumerable<string>? libs = root.GetProjectPropertyArray("GHDL_Libraries");

                if (libs is null)
                {
                    return;
                }
                
                foreach (string libname in libs)
                {
                    IEnumerable<string>? libfiles = root.GetProjectPropertyArray($"GHDL-LIB_{libname}");

                    if (libfiles is null || !libfiles.Any())
                    {
                        continue;
                    }

                    if (libfiles.Contains(file.RelativePath.Replace('\\', '/')))
                    {
                        associatedLibrary = libname;
                        break;
                    }
                }

                if (associatedLibrary.Length == 0)
                {
                    ObservableCollection<MenuItemViewModel>? items = new ObservableCollection<MenuItemViewModel>();

                    foreach (string lib in libs)
                    {
                        items.Add(new MenuItemViewModel($"GHDL_Library_{lib}")
                        {
                            Header = lib,
                            Command = new AsyncRelayCommand(() => AddFileToLibraryAsync(lib, file))
                        });
                    }

                    models.Add(new MenuItemViewModel("GHDL_Library_Add")
                    {
                        Header = "Add to library",
                        Items = items
                    });
                }
                else
                {
                    models.Add(new MenuItemViewModel("GHDL_Library_Remove")
                    {
                        Header = $"Remove from library {associatedLibrary}",
                        Command = new AsyncRelayCommand(() => RemoveFileFromLibraryAsync(associatedLibrary, file))
                    });
                }
            }
        }));
        
        serviceProvider.Resolve<IProjectExplorerService>().RegisterConstructContextMenu(((list, models) =>
        {
            if (list[0] is IProjectFolder { Root: UniversalFpgaProjectRoot root } folder && folder.Children.Any(x => x is IProjectFile file))
            {
                IEnumerable<string>? libs = root.GetProjectPropertyArray("GHDL_Libraries");

                if (libs is null)
                {
                    return;
                }
                
                ObservableCollection<MenuItemViewModel>? items = new ObservableCollection<MenuItemViewModel>();

                foreach (string lib in libs)
                {
                    items.Add(new MenuItemViewModel($"GHDL_Library_{lib}")
                    {
                        Header = lib,
                        Command = new AsyncRelayCommand(() => AddFolderToLibraryAsync(lib, folder))
                    });
                }
                
                models.Add(new MenuItemViewModel("GHDL_Folder_Add")
                {
                    Header = "Add files in folder to library",
                    Items = items
                });
            }
        } ));
        
        serviceProvider.Resolve<FpgaService>().RegisterPreCompileStep<GhdlVhdlToVerilogPreCompileStep>();
        
        _projectExplorerService = serviceProvider.Resolve<IProjectExplorerService>();
        
        serviceProvider.Resolve<FpgaService>().RegisterToolchain<GhdlYosysToolchain>();
        
        
        
        serviceProvider.Resolve<IWindowService>().RegisterUiExtension("CompileWindow_TopRightExtension",
            new OneWareUiExtension(x =>
            {
                if (x is not UniversalFpgaProjectPinPlannerViewModel cm) return null;
                return new GhdlYosysCompileWindowExtensionView
                {
                    DataContext =
                        serviceProvider.Resolve<GhdlYosysCompileWindowExtensionViewModel>((
                            typeof(UniversalFpgaProjectPinPlannerViewModel), cm))
                };
            }));


        var ghdlPreCompiler = serviceProvider.Resolve<GhdlVhdlToVerilogPreCompileStep>();
        serviceProvider.Resolve<IWindowService>().RegisterUiExtension("UniversalFpgaToolBar_CompileMenuExtension",
            new OneWareUiExtension(
                x =>
                {
                    if (x is not UniversalFpgaProjectRoot { Toolchain: GhdlYosysToolchain } root) return null;

                    var name = root.Properties["Fpga"]?.ToString();
                    var fpgaPackage = fpgaService.FpgaPackages.FirstOrDefault(obj => obj.Name == name);
                    var fpga = fpgaPackage?.LoadFpga();
                    
                    return new StackPanel()
                    {
                        Orientation = Orientation.Vertical,
                        Children =
                        {
                            new MenuItem()
                            {
                                Header = "Run Synthesis",
                                Command = new AsyncRelayCommand(async () =>
                                {
                                    await projectExplorerService.SaveOpenFilesForProjectAsync(root);
                                    await ghdlToolchainService.SynthAsync(root, new FpgaModel(fpga!));
                                    
                                }, () => fpga != null)
                            },
                            new MenuItem()
                            {
                                Header = "Run Fit",
                                Command = new AsyncRelayCommand(async () =>
                                {
                                    await projectExplorerService.SaveOpenFilesForProjectAsync(root);
                                    await ghdlToolchainService.FitAsync(root, new FpgaModel(fpga!));
                                }, () => fpga != null)
                            },
                            new MenuItem()
                            {
                                Header = "Run Assemble",
                                Command = new AsyncRelayCommand(async () =>
                                {
                                    await projectExplorerService.SaveOpenFilesForProjectAsync(root);
                                    await ghdlToolchainService.AssembleAsync(root, new FpgaModel(fpga!));
                                }, () => fpga != null)
                            },
                            new Separator(),
                            new MenuItem()
                            {
                                Header = "Yosys Settings",
                                Icon = new Image()
                                {
                                    Source = Application.Current!.FindResource(
                                        Application.Current!.RequestedThemeVariant,
                                        "Material.SettingsOutline") as IImage
                                },
                                Command = new AsyncRelayCommand(async () =>
                                {
                                    if (projectExplorerService
                                            .ActiveProject is UniversalFpgaProjectRoot fpgaProjectRoot)
                                    {
                                        var selectedFpga = root.Properties["Fpga"]?.ToString();
                                        var selectedFpgaPackage =
                                            fpgaService.FpgaPackages.FirstOrDefault(obj => obj.Name == selectedFpga);

                                        if (selectedFpgaPackage == null)
                                        {
                                            serviceProvider.Resolve<ILogger>()
                                                .Warning("No FPGA Selected. Open Pin Planner first!");
                                            return;
                                        }

                                        await windowService.ShowDialogAsync(
                                            new YosysCompileSettingsView
                                            {
                                                DataContext = new YosysCompileSettingsViewModel(fpgaProjectRoot,
                                                    selectedFpgaPackage.LoadFpga())
                                            });
                                    }
                                })
                            }
                        }
                    };
                }));

    }

    private async Task AddFolderToLibraryAsync(string library, IProjectFolder folder)
    {
        if (folder.Root is UniversalFpgaProjectRoot root)
        {
            foreach (var file in folder.Children.Where(x =>
                         x is IProjectFile { Extension: ".vhd" or ".vhdl" }))
            {
                await AddFileToLibraryAsync(library, (file as IProjectFile)!);
            }
        }
    }

    private async Task AddFileToLibraryAsync(string library, IProjectFile file)
    {
        if (file.Root is UniversalFpgaProjectRoot root && !root.CompileExcluded.Contains(file) && !(root.GetProjectPropertyArray($"GHDL-LIB_{library}") ?? Array.Empty<string>()).Any(x => x.Equals(file.RelativePath)))
        {
            // Prefix library collections with "GHDL-LIB" to reduce chance of collisions with other keys
            root.AddToProjectPropertyArray($"GHDL-LIB_{library}", file.RelativePath.Replace('\\', '/'));

            // Save project so that the modifications are stored to disk
            // Use the UI Thread to prevent file access violations
            await Dispatcher.UIThread.Invoke(async () => await _projectExplorerService?.SaveProjectAsync(root)!);
        }
    }

    private async Task RemoveFileFromLibraryAsync(string library, IProjectFile file)
    {
        if (file.Root is UniversalFpgaProjectRoot root)
        {
            root.SetProjectPropertyArray($"GHDL-LIB_{library}",
                root.GetProjectPropertyArray($"GHDL-LIB_{library}")!.Where(x => !x.Equals(file.RelativePath.Replace('\\', '/')))
                    .ToArray());
            
            // Save project so that the modifications are stored to disk
            await _projectExplorerService?.SaveProjectAsync(root)!;
        }
        
    }
}