using OneWare.Essentials.Services;
using OneWare.GhdlExtension;
using OneWare.GhdlExtension.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;
using OneWare.OssCadSuiteIntegration.Yosys;
using Prism.Ioc;

namespace OneWare.GhdlExtension;


public class GhdlYosysToolchain(GhdlVhdlToVerilogPreCompileStep ghdlPreCompiler, YosysToolchain yosysToolchain) : IFpgaToolchain
{
    static string? _val;
    
    public void OnProjectCreated(UniversalFpgaProjectRoot project)
    {
    }

    public void LoadConnections(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        yosysToolchain.LoadConnections(project, fpga);
    }

    public void SaveConnections(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        yosysToolchain.SaveConnections(project, fpga);
    }

    public async Task<bool> CompileAsync(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        if (_val == null)
        {
            ContainerLocator.Container.Resolve<ILogger>().Error("Yosys binary not found");
            return false;
        }

        bool success = await ghdlPreCompiler.PerformPreCompileStepAsync(project, fpga);
        if (!success) return false;
        success = await yosysToolchain.CompileAsync(project, fpga);
        return success;
    }

    public string Name => "GHDL_Yosys";

    public static void SubscribeToSettings(ISettingsService settingsService)
    {
        settingsService.GetSettingObservable<string>("OssCadSuite_Path").Subscribe(x => _val = x);
    }
}
