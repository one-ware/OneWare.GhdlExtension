using OneWare.Essentials.Models;
using OneWare.Ghdl.Services;
using OneWare.Ghdl.ViewModels;
using OneWare.Ghdl.Views;
using OneWare.UniversalFpgaProjectSystem.Context;
using OneWare.UniversalFpgaProjectSystem.Services;

namespace OneWare.Ghdl;

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
                return new GhdlSimulatorToolbarView()
                {
                    DataContext = new GhdlSimulatorToolbarViewModel(tb, this)
                };
            return null;
        });
    }
    
    public Task SimulateAsync(IFile file)
    {
        if (file is IProjectFile projectFile) return _ghdlService.SimulateFileAsync(projectFile);
        return Task.CompletedTask;
    }
}