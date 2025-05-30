using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using OneWare.Essentials.Helpers;
using OneWare.Essentials.Models;
using OneWare.Essentials.PackageManager;
using OneWare.Essentials.Services;
using OneWare.Essentials.ViewModels;
using OneWare.GhdlExtension.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
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
    
    IProjectExplorerService? _projectExplorerService;

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
        
        containerProvider.Resolve<IProjectSettingsService>().AddProjectSetting("GHDL_Libraries", new ListBoxSetting("GHDL libraries", []), 
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
        
        containerProvider.Resolve<IProjectExplorerService>().RegisterConstructContextMenu(((list, models) =>
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

                    if (libfiles.Contains(file.RelativePath))
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
        
        containerProvider.Resolve<IProjectExplorerService>().RegisterConstructContextMenu(((list, models) =>
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
        
        containerProvider.Resolve<FpgaService>().RegisterPreCompileStep<GhdlVhdlToVerilogPreCompileStep>();
        
        _projectExplorerService = containerProvider.Resolve<IProjectExplorerService>();
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
            root.AddToProjectPropertyArray($"GHDL-LIB_{library}", file.RelativePath);

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
                root.GetProjectPropertyArray($"GHDL-LIB_{library}")!.Where(x => !x.Equals(file.RelativePath))
                    .ToArray());
            
            // Save project so that the modifications are stored to disk
            await _projectExplorerService?.SaveProjectAsync(root)!;
        }
    }
}