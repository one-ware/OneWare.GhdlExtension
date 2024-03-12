using OneWare.Essentials.Models;
using OneWare.Ghdl.Services;
using OneWare.UniversalFpgaProjectSystem.Services;

namespace OneWare.Ghdl;

public class GhdlSimulator(GhdlService ghdlService) : IFpgaSimulator
{
    public string Name => "GHDL";
    
    public UiExtension TestBenchToolbarTopUiExtension { get; } = null;
    
    public Task SimulateAsync(IFile file)
    {
        if (file is IProjectFile projectFile) return ghdlService.SimulateFileAsync(projectFile);
        return Task.CompletedTask;
    }
}