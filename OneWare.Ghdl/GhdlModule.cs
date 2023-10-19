﻿using OneWare.Ghdl.Services;
using OneWare.Ghdl.Views;
using OneWare.Shared.Models;
using OneWare.Shared.Services;
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

        containerProvider.Resolve<IWindowService>().RegisterMenuItem("MainWindow_MainMenu/Simulator",
            new MenuItemModel("SimulateGHDL")
            {
                Header = "Simulate with GHDL",
                Command = ghdlService.SimulateCommand,
            },
            new MenuItemModel("SynthGHDL")
            {
                Header = "Synth with GHDL",
                Command = ghdlService.SynthCommand,
            });

        containerProvider.Resolve<IWindowService>().RegisterUiExtension("MainWindow_LeftToolBarExtension",
            new GhdlMainWindowToolBarExtension()
            {
                DataContext = ghdlService
            });
        
        containerProvider.Resolve<IModuleTracker>().RecordModuleInitialized(GetType().Name);
    }
}