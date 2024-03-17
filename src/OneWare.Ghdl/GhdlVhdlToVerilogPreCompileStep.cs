﻿using OneWare.Essentials.Helpers;
using OneWare.Essentials.Services;
using OneWare.Ghdl.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;

namespace OneWare.Ghdl;

public class GhdlVhdlToVerilogPreCompileStep(GhdlService ghdlService, ILogger logger) : IFpgaPreCompileStep
{
    public string Name => "GHDL Vhdl to Verilog";
    
    public async Task PerformPreCompileStepAsync(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        try
        {
            var buildPath = Path.Combine(project.FullPath, "ghdl-output");
            if(Directory.Exists(buildPath)) Directory.Delete(buildPath, true);
            Directory.CreateDirectory(buildPath);
            var buildDir = project.AddFolder("ghdl-output");

            foreach (var vhdlFile in project.Files
                         .Where(x => x.Extension is ".vhd" or ".vhdl")
                         .Where(x => !project.CompileExcluded.Contains(x))
                         .Where(x => !project.TestBenches.Contains(x)))
            {
                await ghdlService.SynthAsync(vhdlFile, "verilog", buildPath);
            }
            ProjectHelper.ImportEntries(buildPath, buildDir);
        }
        catch (Exception e)
        {
            logger.Error(e.Message, e);
        }
    }
}