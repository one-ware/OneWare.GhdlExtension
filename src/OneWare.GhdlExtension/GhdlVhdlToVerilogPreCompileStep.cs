using OneWare.Essentials.Helpers;
using OneWare.Essentials.Services;
using OneWare.GhdlExtension.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;

namespace OneWare.GhdlExtension;

public class GhdlVhdlToVerilogPreCompileStep(GhdlService ghdlService, ILogger logger) : IFpgaPreCompileStep
{
    public string Name => "GHDL Vhdl to Verilog";
    
    public async Task<bool> PerformPreCompileStepAsync(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        try
        {
            var buildPath = Path.Combine(project.FullPath, "build");
            Directory.CreateDirectory(buildPath);
            var ghdlOutputPath = Path.Combine(buildPath, "ghdl-output");
            if(Directory.Exists(ghdlOutputPath)) Directory.Delete(ghdlOutputPath, true);
            Directory.CreateDirectory(ghdlOutputPath);
            var ghdlOutputDir = project.AddFolder(Path.Combine("build", "ghdl-output"));

            var vhdlFile = project.Files.First(x => x == project.TopEntity);
            
                var success = await ghdlService.SynthAsync(vhdlFile, "verilog", ghdlOutputPath);
                if (!success) return false;
            
            ProjectHelper.ImportEntries(ghdlOutputPath, ghdlOutputDir);
            return true;
        }
        catch (Exception e)
        {
            logger.Error(e.Message, e);
            return false;
        }
    }
}