using System.Text.RegularExpressions;
using OneWare.GhdlExtension.Helpers;
using OneWare.GhdlExtension.Services;
using OneWare.Essentials.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;
using Prism.Ioc;


namespace OneWare.GhdlExtension;

public class CologneChipToolchain(CologneChipService cologneChipService) : IFpgaToolchain
{

    public string Name => "CologneChip";
    
    private static string Pattern => @"NET\s+""(?<pinName>[^""]+)""\s+Loc\s+=\s+""(?<pinLocation>[^""]+)""(?:\s*\|\s*(?<constraints>[^;]*))?;";

    public void OnProjectCreated(UniversalFpgaProjectRoot project)
    {  
        CologneChipSettingsHelper.UpdateProjectProperties(project, $"{project.Name}.ccf");
        var file = Directory.GetParent(project.ProjectFilePath)?.FullName + @"\" + CologneChipSettingsHelper.GetConstraintFile(project);
        
        File.WriteAllText(file, CologneChipConstantService.CcfTemplate);
        project.ImportFile(file, false);
    }
    
    public void LoadConnections(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        try
        {
            var pcfPath = Directory.GetParent(project.ProjectFilePath)?.FullName + @"\" +CologneChipSettingsHelper.GetConstraintFile(project);
            
            if (File.Exists(pcfPath))
            {
                var pcf = File.ReadAllText(pcfPath);
                var lines = pcf.Split('\n');
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("NET"))
                    {
                        // remove comments
                        var index = trimmedLine.IndexOf('#');
                        var result = (index >= 0) ? trimmedLine.Substring(0, index).Trim() : trimmedLine;
                        
                        var match = Regex.Match(result, Pattern);
                        
                        if (match.Success)
                        {
                            // extract pin-name, pin-location and opt.-constraints
                            var signal = match.Groups["pinName"].Value;
                            var pin = match.Groups["pinLocation"].Value;
                            // var constraints = match.Groups["constraints"].Success ? match.Groups["constraints"].Value : null;
                            
                            if (fpga.PinModels.TryGetValue(pin, out var pinModel) &&
                                fpga.NodeModels.TryGetValue(signal, out var signalModel))
                                fpga.Connect(pinModel, signalModel);

                            // fpga.Constraint(constraints);
                        }
                        else
                        {
                            ContainerLocator.Container.Resolve<ILogger>().Warning("CCF Line invalid: " + trimmedLine);
                        }
                    }
                }
            }
        } 
        catch (Exception e)
        {
            ContainerLocator.Container.Resolve<ILogger>().Error(e.Message, e);
        }
    }

    public void SaveConnections(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        var ignoreGui = ContainerLocator.Container.Resolve<ISettingsService>().GetSettingValue<bool>(CologneChipConstantService.CologneChipSettingsIgnoreGuiKey);
        if (ignoreGui)
        {
            ContainerLocator.Container.Resolve<ILogger>().Warning($"Setting '{CologneChipConstantService.CologneChipSettingsIgnoreGuiKey}' to true");
            ContainerLocator.Container.Resolve<ILogger>().Warning("Your Changes will not be saved. Using the CCF-File");
            return;
        } 
        
        cologneChipService.SaveConnections(project, fpga);
        
    }

    public Task<bool> CompileAsync(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        return cologneChipService.CompileAsync(project, fpga);
    }
}