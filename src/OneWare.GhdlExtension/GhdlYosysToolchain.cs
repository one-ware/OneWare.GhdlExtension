using OneWare.Essentials.Services;
using OneWare.GhdlExtension.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;
using Prism.Ioc;

namespace OneWare.GhdlExtension;

public class GhdlYosysToolchain(GhdlService ghdlService, FpgaService fpgaService) : IFpgaToolchain
{
    static string? val;
    IFpgaToolchain? yosysToolchain;
    IFpgaPreCompileStep? preStep;
    
    public void OnProjectCreated(UniversalFpgaProjectRoot project)
    {
        yosysToolchain = fpgaService.Toolchains.First(x => x.Name == "Yosys");
        preStep = fpgaService.PreCompileSteps.First(x => x.Name == "GHDL Vhdl to Verilog");
    }

    public void LoadConnections(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        //Code aus Yosys-Toolchain kopieren oder sie direkt rufen?
        throw new NotImplementedException();
    }

    public void SaveConnections(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        //Code aus Yosys-Toolchain kopieren oder sie direkt rufen?
        throw new NotImplementedException();
    }

    public Task<bool> CompileAsync(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        if (val == null)
        {
            ContainerLocator.Container.Resolve<ILogger>().Error("Yosys binary not found");
        }
        else
        {

            bool success;
            GhdlVhdlToVerilogPreCompileStep task = new GhdlVhdlToVerilogPreCompileStep(ghdlService, logger);
            success = await task;
            //Code aus Yosys-Toolchain kopieren oder sie direkt rufen?
            return ;
        }
    }

    public string Name => "GHDL_Yosys";

    public static void SubscribeToSettings(ISettingsService settingsService)
    {
        settingsService.GetSettingObservable<string>("OssCadSuite_Path").Subscribe(x => val = x);
    }
}