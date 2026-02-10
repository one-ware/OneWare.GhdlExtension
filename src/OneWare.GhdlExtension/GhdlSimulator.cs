using OneWare.Essentials.Models;
using OneWare.Essentials.Services;
using OneWare.GhdlExtension.Services;
using OneWare.GhdlExtension.ViewModels;
using OneWare.GhdlExtension.Views;
using OneWare.UniversalFpgaProjectSystem.Context;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;

namespace OneWare.GhdlExtension;

public class GhdlSimulator : IFpgaSimulator
{
    private readonly GhdlService _ghdlService;

    public string Name => "GHDL";
    
    public OneWareUiExtension? TestBenchToolbarTopUiExtension { get; } 

    public GhdlSimulator(GhdlService ghdlService, IProjectExplorerService projectExplorerService)
    {
        _ghdlService = ghdlService;
        
        TestBenchToolbarTopUiExtension = new OneWareUiExtension(x =>
        {
            if (x is TestBenchContext tb)
            {
                //Set the default VHDL standard to the project's standard if possible
                var root = projectExplorerService.GetRootFromFile(tb.FilePath);
                var globalVhdlVersion = (root as UniversalFpgaProjectRoot)?.Properties.GetString("vhdlStandard");

                var vhdlSetting = tb.GetBenchProperty(nameof(GhdlSimulatorToolbarViewModel.VhdlStandard));
                if(vhdlSetting == null && globalVhdlVersion != null) 
                    tb.SetBenchProperty(nameof(GhdlSimulatorToolbarViewModel.VhdlStandard), globalVhdlVersion);
                
                return new GhdlSimulatorToolbarView()
                {
                    DataContext = new GhdlSimulatorToolbarViewModel(tb, this)
                };
            }
            return null;
        });
    }
    
    public Task<bool> SimulateAsync(string fullPath)
    {
        return _ghdlService.SimulateFileAsync(fullPath);
    }
}