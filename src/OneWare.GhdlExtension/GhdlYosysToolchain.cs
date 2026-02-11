using OneWare.GhdlExtension.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.OssCadSuiteIntegration.Yosys;


namespace OneWare.GhdlExtension;


public class GhdlYosysToolchain(GhdlToolchainService ghdlToolchainService, YosysService yosysService) : YosysToolchain(yosysService)
{
    public const string ToolchainId = "ghdl_yosys";
    
    public override async Task<bool> CompileAsync(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        return await ghdlToolchainService.CompileAsync(project, fpga);
    }

    public override string Id => ToolchainId;
    public override string Name => "GHDL_Yosys";
}
