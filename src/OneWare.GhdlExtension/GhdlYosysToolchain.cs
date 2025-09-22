using OneWare.Essentials.Services;
using OneWare.GhdlExtension;
using OneWare.GhdlExtension.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;
using OneWare.OssCadSuiteIntegration.Yosys;
using Prism.Ioc;
using SkiaSharp;

namespace OneWare.GhdlExtension;


public class GhdlYosysToolchain(GhdlVhdlToVerilogPreCompileStep ghdlPreCompiler, YosysToolchain yosysToolchain, YosysService yosysService) : IFpgaToolchain
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
        
       
      try
      {
          var verilogFileName = ghdlPreCompiler.VerilogFileName ?? throw new Exception("Invalid verilog file name!");
          var ghdlOutputPath = Path.Combine(project.FullPath, ghdlPreCompiler.BuildDir, 
              ghdlPreCompiler.GhdlOutputDir, verilogFileName);
          var mandatoryFileList = new List<string>(1) {ghdlOutputPath};
          
          success = await yosysService.CompileAsync(project, fpga, mandatoryFileList);
          return success;
      }
      catch (Exception e)
      {
          ContainerLocator.Container.Resolve<ILogger>().Error(e.Message, e);
          return false;
      }
    }

    public string Name => "GHDL_Yosys";

    public static void SubscribeToSettings(ISettingsService settingsService)
    {
        settingsService.GetSettingObservable<string>("OssCadSuite_Path").Subscribe(x => _val = x);
    }
}
