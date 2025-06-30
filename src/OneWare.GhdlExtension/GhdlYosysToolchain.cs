using OneWare.GhdlExtension.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;

namespace OneWare.GhdlExtension;

public class GhdlYosysToolchain(GhdlService ghdlService) : IFpgaToolchain
{
    public void OnProjectCreated(UniversalFpgaProjectRoot project)
    {
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
        //Code aus Yosys-Toolchain kopieren oder sie direkt rufen?
        throw new NotImplementedException();
    }

    public string Name => "GHDL_Yosys";
}