using Avalonia.Media;
using Avalonia.Threading;
using OneWare.GhdlExtension.Helpers;
using OneWare.Essentials.Enums;
using OneWare.Essentials.Models;
using OneWare.Essentials.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Parser;
using Prism.Ioc;

namespace OneWare.GhdlExtension.Services;

public class CologneChipService(
    IChildProcessService childProcessService,
    ILogger logger,
    IOutputService outputService,
    IDockService dockService)
{
    public async Task<bool> SynthAsync(UniversalFpgaProjectRoot project, FpgaModel fpgaModel)
    {
        try
        {
            var properties = FpgaSettingsParser.LoadSettings(project, fpgaModel.Fpga.Name);
            var top = project.TopEntity?.Header ?? throw new Exception("TopEntity not set!");
            
            var buildDir = Path.Combine(project.FullPath, "build");
            Directory.CreateDirectory(buildDir);

            dockService.Show<IOutputService>();
            
            var start = DateTime.Now;

            var yosysSynthTool = properties.GetValueOrDefault("yosysToolchainYosysSynthTool") ??
                                 throw new Exception("Yosys Tool not set!");
            
            var (topName, topLanguage) = (top.Split('.').First(), top.Split('.').Last());

            List<string> yosysArguments = [];
            List<string> includedExtensions = [];
            
            switch (topLanguage)
            {
                case "vhd":
                    outputService.WriteLine("VHDL Synthesis...\n===============");
                    yosysArguments = ["-q","-l ./../synth.log",  "-p", $"ghdl --warn-no-binding -C --ieee=synopsys ./../{top} -e {topName}; {yosysSynthTool} -nomx8 -top {topName} -vlog {topName}_synth.v"];
                    includedExtensions = [];
                    break;
                case "v": 
                    outputService.WriteLine("Verilog Synthesis...\n==============");
                    yosysArguments = ["-ql", "./../log/synth.log", "-p", $"{yosysSynthTool} -nomx8 -top {topName} -vlog {topName}_synth.v"];
                    includedExtensions = [".v", ".sv"];
                    break;
            }
            
            var includedFiles = project.Files
                .Where(x => includedExtensions.Contains(x.Extension))
                .Where(x => !project.CompileExcluded.Contains(x))
                .Where(x => !project.TestBenches.Contains(x))
                .Select(x => $"./../{x.RelativePath}");
            
            yosysArguments.AddRange(properties.GetValueOrDefault("yosysToolchainYosysFlags")?.Split(' ',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? []);
            yosysArguments.AddRange(includedFiles);
            
            var (success, _) = await childProcessService.ExecuteShellAsync("yosys", yosysArguments, $"{project.FullPath}/build",
                "Running yosys...", AppState.Loading, true, x =>
                {
                    if (x.StartsWith("Error:"))
                    {
                        logger.Error(x);
                        return false;
                    }

                    outputService.WriteLine(x);
                    return true;
                });
            
            
            if (!success) {
                var ignoreSynthExitCode = ContainerLocator.Container.Resolve<ISettingsService>().GetSettingValue<bool>(CologneChipConstantService.CologneChipSettingsIgnoreSynthExitCode);
                if (ignoreSynthExitCode)
                {
                    ContainerLocator.Container.Resolve<ILogger>().Warning("The synthesis was terminated with an exit code other than zero");
                    ContainerLocator.Container.Resolve<ILogger>().Warning($"Setting '{CologneChipConstantService.CologneChipSettingsIgnoreSynthExitCode}' to true");
                    ContainerLocator.Container.Resolve<ILogger>().Warning($"Because of this setting, the route and placing tool is started anyway");
                    success = true;
                } 
            }
            
            
            var compileTime = DateTime.Now - start;
            if (success)
                outputService.WriteLine(
                    $"==================\n\nSynthesis finished after {(int)compileTime.TotalMinutes:D2}:{compileTime.Seconds:D2}\n");
            else
                outputService.WriteLine(
                    $"==================\n\nSynthesis failed after {(int)compileTime.TotalMinutes:D2}:{compileTime.Seconds:D2}\n",
                    Brushes.Red);

            return success;
        }
        catch (Exception e)
        {
            logger.Error(e.Message, e);
            return false;
        }
    }

    public async Task<bool> PRAysnc(UniversalFpgaProjectRoot project, FpgaModel fpgaModel)
    {
        var start = DateTime.Now;
        var top = project.TopEntity?.Header ?? throw new Exception("TopEntity not set!");
        var (topName, topLanguage) = (top.Split('.').First(), top.Split('.').Last());
        var ccfFile = CologneChipSettingsHelper.GetConstraintFile(project);
        
        List<string> prArguments = ["-i", $"{topName}_synth.v", "-o", topName, $"-ccf ./../{ccfFile} -cCP"];
        
        var success = (await childProcessService.ExecuteShellAsync("p_r", prArguments,
            $"{project.FullPath}/build", $"Running P_R...", AppState.Loading, true, null, s =>
            {
                Dispatcher.UIThread.Post(() => { outputService.WriteLine(s); });
                return true;
            })).success;
        
        var compileTime = DateTime.Now - start;
        if (success)
            outputService.WriteLine(
                $"==================\n\nPlace and Route finished after {(int)compileTime.TotalMinutes:D2}:{compileTime.Seconds:D2}\n");
        else
            outputService.WriteLine(
                $"==================\n\nPlace and Route failed after {(int)compileTime.TotalMinutes:D2}:{compileTime.Seconds:D2}\n",
                Brushes.Red);
        
        return success;
    }
    
    public async Task CreateNetListJsonAsync(IProjectFile verilog)
    {
        await childProcessService.ExecuteShellAsync("yosys", [
                "-p", "hierarchy -auto-top; proc; opt; memory -nomap; wreduce -memx; opt_clean", "-o",
                $"{verilog.Header}.json", verilog.Header
            ],
            Path.GetDirectoryName(verilog.FullPath)!, "Create Netlist...");
    }

    public void SaveConnections(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        var pcfPath = Path.Combine(project.FullPath, CologneChipSettingsHelper.GetConstraintFile(project));
        
        try
        {
            List<string> lines = [];
            List<string> result = [];
            if (File.Exists(pcfPath))
            {
                lines = [..File.ReadAllLines(pcfPath)];
            }
            
            var pinModels = fpga.PinModels.Where(x => x.Value.ConnectedNode is not null).Select(conn => conn.Value).ToList();
            var pinModelsCache = pinModels.ToList();

            foreach (var line in lines)
            {
                if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line))
                {
                    result.Add(line);
                    continue;
                }

                if (!line.StartsWith("NET")) continue;
                
                var found = false;
                foreach (var pin in pinModels)
                {
                    if (!line.Contains(pin.ConnectedNode!.Node.Name)) continue;
                        
                    var commentIndex = line.IndexOf('#', StringComparison.Ordinal);
                    var comment = commentIndex != -1 ? line[commentIndex..] : string.Empty;
                    
                    var constraintIndex = line.IndexOf('|');
                    var semicolonIndex = line.IndexOf(';');
                    
                    var constraint = constraintIndex != -1 
                                     && constraintIndex < semicolonIndex
                                     && (constraintIndex < commentIndex || commentIndex < 0 )
                        ? line[constraintIndex..semicolonIndex] : string.Empty;
                    
                    var newLine = $"NET \"{pin.ConnectedNode!.Node.Name}\" Loc =  \"{pin.Pin.Name}\"";
                    
                    if (constraint != string.Empty) newLine += $" {constraint}";
                    
                    newLine += ";";
                    
                    if (commentIndex != -1) newLine += $" {comment}";
                    
                    result.Add(newLine.Trim());
                    pinModelsCache.Remove(pin);
                    found = true;
                    break;
                }

                if (!found)
                {
                    result.Add($"# {line}");
                }
            }

            result.AddRange(pinModelsCache.Select(pin => $"NET \"{pin.ConnectedNode!.Node.Name}\" Loc =  \"{pin.Pin.Name}\";"));
            File.WriteAllLines(pcfPath, result);
            CologneChipSettingsHelper.UpdateProjectOverlay(project);
        }
        catch (Exception e)
        {
            ContainerLocator.Container.Resolve<ILogger>().Error(e.Message, e);
        }
    }
    
    
    public async Task<bool> CompileAsync(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        var start = DateTime.Now;
        outputService.WriteLine("Starting CC Toolchain...\n===============");
        
        var success = await SynthAsync(project, fpga);
        success &= await PRAysnc(project, fpga);
        
        var endTime = DateTime.Now - start;
        if (success)
            outputService.WriteLine(
                $"==================\n\nCC Toolchain finished after {(int)endTime.TotalMinutes:D2}:{endTime.Seconds:D2}\n");
        else
            outputService.WriteLine(
                $"==================\n\nCC Toolchain failed after {(int)endTime.TotalMinutes:D2}:{endTime.Seconds:D2}\n",
                Brushes.Red);
        
        return success;
    }
}