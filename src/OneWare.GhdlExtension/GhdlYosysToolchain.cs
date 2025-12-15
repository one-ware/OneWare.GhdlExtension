using OneWare.Essentials.Services;
using OneWare.GhdlExtension;
using OneWare.GhdlExtension.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;
using OneWare.OssCadSuiteIntegration.Yosys;
using Prism.Ioc;
using SkiaSharp;

namespace OneWare.GhdlExtension;


public class GhdlYosysToolchain(GhdlToolchainService ghdlToolchainService, YosysToolchain yosysToolchain) : IFpgaToolchain
{
    
    public void OnProjectCreated(UniversalFpgaProjectRoot project)
    {
        yosysToolchain.OnProjectCreated(project);
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
        return await ghdlToolchainService.CompileAsync(project, fpga);
    }

    public string Name => "GHDL_Yosys";
}
