using OneWare.Essentials.Models;
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
    
    public UiExtension? TestBenchToolbarTopUiExtension { get; } 

    public GhdlSimulator(GhdlService ghdlService)
    {
        _ghdlService = ghdlService;
        
        TestBenchToolbarTopUiExtension = new UiExtension(x =>
        {
            if (x is TestBenchContext tb)
            {
                //Set the default VHDL standard to the project's standard if possible
                var project = (tb.File as IProjectFile)?.Root;
                var globalVhdlVersion = (project as UniversalFpgaProjectRoot)?.GetProjectProperty("VHDL_Standard");

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
    
    public Task<bool> SimulateAsync(IFile file)
    {
        if (file is IProjectFile projectFile) return _ghdlService.SimulateFileAsync(projectFile);
        return Task.FromResult(false);
    }
}