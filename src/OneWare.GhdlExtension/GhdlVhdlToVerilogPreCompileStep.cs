using OneWare.Essentials.Helpers;
using OneWare.Essentials.Services;
using OneWare.GhdlExtension.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;

namespace OneWare.GhdlExtension;

public class GhdlVhdlToVerilogPreCompileStep(GhdlService ghdlService, ILogger logger) : IFpgaPreCompileStep
{
    public string Name => "GHDL Vhdl to Verilog";

    public string buildDir = "build";
    public string ghdlOutputDir = "ghdl-output";
    public string? verilogFileName;
    
    public async Task<bool> PerformPreCompileStepAsync(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        try
        {
            var buildPath = Path.Combine(project.FullPath, buildDir);
            Directory.CreateDirectory(buildPath);
            var ghdlOutputPath = Path.Combine(buildPath, ghdlOutputDir);
            if(Directory.Exists(ghdlOutputPath)) Directory.Delete(ghdlOutputPath, true);
            Directory.CreateDirectory(ghdlOutputPath);
            

            var vhdlFile = project.Files.First(x => x == project.TopEntity);
            verilogFileName = Path.GetFileNameWithoutExtension(vhdlFile.FullPath)+".v";
            
                var success = await ghdlService.SynthAsync(vhdlFile, "verilog", ghdlOutputPath);
                if (!success) return false;
            
            
            return true;
        }
        catch (Exception e)
        {
            logger.Error(e.Message, e);
            return false;
        }
    }
}