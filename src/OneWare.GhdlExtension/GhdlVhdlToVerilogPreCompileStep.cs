using OneWare.Essentials.Services;
using OneWare.GhdlExtension.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;

namespace OneWare.GhdlExtension;

public class GhdlVhdlToVerilogPreCompileStep(GhdlService ghdlService, ILogger logger) : IFpgaPreCompileStep
{
    public string Name => "GHDL Vhdl to Verilog";

    public readonly string BuildDir = "build";
    public readonly string GhdlOutputDir = "ghdl-output";
    public string? VerilogFileName;
    
    public async Task<bool> PerformPreCompileStepAsync(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        try
        {
            var buildPath = Path.Combine(project.FullPath, BuildDir);
            Directory.CreateDirectory(buildPath);
            var ghdlOutputPath = Path.Combine(buildPath, GhdlOutputDir);
            if(Directory.Exists(ghdlOutputPath)) Directory.Delete(ghdlOutputPath, true);
            Directory.CreateDirectory(ghdlOutputPath);
            

            var vhdlFile = project.Files.First(x => x == project.TopEntity);
            VerilogFileName = Path.GetFileNameWithoutExtension(vhdlFile.FullPath)+".v";
            
            var success = await ghdlService.SynthAsync(vhdlFile, "verilog", ghdlOutputPath);
            return success;
        }
        catch (Exception e)
        {
            logger.Error(e.Message, e);
            return false;
        }
    }
}