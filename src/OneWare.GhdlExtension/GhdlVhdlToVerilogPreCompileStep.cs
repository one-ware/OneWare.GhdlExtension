using Microsoft.Extensions.Logging;
using OneWare.Essentials.Services;
using OneWare.GhdlExtension.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;

namespace OneWare.GhdlExtension;

public class GhdlVhdlToVerilogPreCompileStep(GhdlService ghdlService, ILogger logger) : IFpgaPreCompileStep
{
    public string Name => "GHDL: VHDL to Verilog";

    public readonly string BuildDir = "build";
    public readonly string GhdlOutputDir = "gen_verilog";

    public async Task<bool> PerformPreCompileStepAsync(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        var vhdlFiles = project.GetFiles("*.vhd").Concat(project.GetFiles("*.vhdl")).ToList();
        if (!vhdlFiles.Any()) return true;

        try
        {
            var buildPath = Path.Combine(project.FullPath, BuildDir);
            Directory.CreateDirectory(buildPath);
            var ghdlOutputPath = Path.Combine(buildPath, GhdlOutputDir);
            if (Directory.Exists(ghdlOutputPath)) Directory.Delete(ghdlOutputPath, true);
            Directory.CreateDirectory(ghdlOutputPath);

            var success = await ghdlService.SynthAsync(project, "verilog", ghdlOutputPath);
            return success;
        }
        catch (Exception e)
        {
            logger.Error(e.Message, e);
            return false;
        }
    }
}