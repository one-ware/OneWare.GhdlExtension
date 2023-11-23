using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using OneWare.Ghdl.Services;
using OneWare.Ghdl.Views;
using OneWare.SDK.Models;
using OneWare.SDK.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace OneWare.Ghdl;

public class GhdlModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<GhdlService>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        var ghdlService = containerProvider.Resolve<GhdlService>();

        containerProvider.Resolve<IWindowService>().RegisterMenuItem("MainWindow_MainMenu/Ghdl",
            new MenuItemModel("SimulateGHDL")
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
            if (x.Count == 1 && x.First() is IProjectFile { Extension: ".vhd" or ".vhdl" } file)
            {
                return new[]
                {
                    new MenuItemModel("GHDL")
                    {
                        Header = "GHDL",
                        Items = new IMenuItem[]
                        {
                            new MenuItemModel("SimulateWithGHDL")
                            {
                                Header = "Simulate with GHDL",
                                Command = new AsyncRelayCommand(() => ghdlService.SimulateFileAsync(file)),
                                ImageIconObservable = Application.Current?.GetResourceObservable("Material.Pulse"),
                            },
                            new MenuItemModel("SynthGhdlToVerilog")
                            {
                                Header = "Convert to Verilog Netlist",
                                Command = new AsyncRelayCommand(() => ghdlService.SynthAsync(file, "verilog")),
                            },
                            new MenuItemModel("SynthGhdlToVerilog")
                            {
                                Header = "Convert to Dot Netlist",
                                Command = new AsyncRelayCommand(() => ghdlService.SynthAsync(file, "dot")),
                            },
                        }
                    }
                };
            }
            return null;
        });
    }
}