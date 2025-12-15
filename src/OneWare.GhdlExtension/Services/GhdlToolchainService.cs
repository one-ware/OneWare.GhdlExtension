using OneWare.Essentials.Services;
using OneWare.OssCadSuiteIntegration.Yosys;
using OneWare.UniversalFpgaProjectSystem.Models;
using Prism.Ioc;

namespace OneWare.GhdlExtension.Services;

public class GhdlToolchainService
{
    private static string? _val;
    private readonly GhdlVhdlToVerilogPreCompileStep _ghdlPreCompiler;
    private readonly YosysService _yosysService;
    private readonly ISettingsService _settingsService;

    public GhdlToolchainService(GhdlVhdlToVerilogPreCompileStep ghdlPreCompiler, YosysService yosysService, ISettingsService settingsService)
    {
        _ghdlPreCompiler = ghdlPreCompiler;
        _yosysService = yosysService;
        _settingsService = settingsService;
    }
    
    
    public async Task<bool> CompileAsync(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        var success = await SynthAsync(project, fpga);
        success = success && await FitAsync(project, fpga);
        success = success && await AssembleAsync(project, fpga);
        return success;
    }

    public async Task<bool> SynthAsync(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        if (_val == null)
        {
            ContainerLocator.Container.Resolve<ILogger>().Error("Yosys binary not found");
            return false;
        }

        bool success = await _ghdlPreCompiler.PerformPreCompileStepAsync(project, fpga);
        if (!success) return false;
        
       
        try
        {
            var verilogFileName = _ghdlPreCompiler.VerilogFileName ?? throw new Exception("Invalid verilog file name!");
            var ghdlOutputPath = Path.Combine(project.FullPath, _ghdlPreCompiler.BuildDir, 
                _ghdlPreCompiler.GhdlOutputDir, verilogFileName);
            var mandatoryFileList = new List<string>(1) {ghdlOutputPath};
          
            success = await _yosysService.SynthAsync(project, fpga, mandatoryFileList);
            return success;
        }
        catch (Exception e)
        {
            ContainerLocator.Container.Resolve<ILogger>().Error(e.Message, e);
            return false;
        }
    }

    public async Task<bool> FitAsync(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        return await _yosysService.FitAsync(project, fpga);
    }

    public async Task<bool> AssembleAsync(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        return await _yosysService.AssembleAsync(project, fpga);
    }

    public void SubscribeToSettings()
    {
        _settingsService.GetSettingObservable<string>("OssCadSuite_Path").Subscribe(x => _val = x);
    }
}