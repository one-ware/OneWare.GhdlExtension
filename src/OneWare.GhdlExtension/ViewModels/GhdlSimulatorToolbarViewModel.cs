using CommunityToolkit.Mvvm.ComponentModel;
using OneWare.Essentials.Models;
using OneWare.Essentials.Services;
using OneWare.UniversalFpgaProjectSystem.Context;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;
using ReactiveUI;

namespace OneWare.GhdlExtension.ViewModels;

public class GhdlSimulatorToolbarViewModel(TestBenchContext context, IFpgaSimulator simulator) : ObservableObject
{
    public string SimulationStopTime
    {
        get => context.GetBenchProperty(nameof(SimulationStopTime)) ?? "";
        set
        {
            if(string.IsNullOrWhiteSpace(value)) context.RemoveBenchProperty(nameof(SimulationStopTime));
            else context.SetBenchProperty(nameof(SimulationStopTime), value);
            OnPropertyChanged();
        }
    }
    
    public string[] AvailableVhdlStandards => ["87", "93", "93c", "00", "02", "08", "19"];
    public string VhdlStandard
    {
        get
        {
            if (ContainerLocator.Container.Resolve<IProjectExplorerService>().GetRootFromFile(context.FilePath) is UniversalFpgaProjectRoot root)
            {
                return root.Properties.GetString("vhdlStandard") ?? context.GetBenchProperty(nameof(VhdlStandard)) ?? "93c";
            }
            
            return context.GetBenchProperty(nameof(VhdlStandard)) ?? "93c";
        }
        set
        {
            if (ContainerLocator.Container.Resolve<IProjectExplorerService>().GetRootFromFile(context.FilePath) is UniversalFpgaProjectRoot root)
            {
                root.Properties.SetString("vhdlStandard", value);
            }
            
            context.SetBenchProperty(nameof(VhdlStandard), value);
            OnPropertyChanged();
        }
    }
    
    public string[] AvailableWaveOutputFormats => ["VCD", "GHW", "FST"];
    public string WaveOutputFormat
    {
        get => context.GetBenchProperty(nameof(WaveOutputFormat)) ?? "VCD";
        set
        {
            context.SetBenchProperty(nameof(WaveOutputFormat), value);
            OnPropertyChanged();
        }
    }
    
    public string[] AvailableAssertLevels => ["default", "warning", "error", "failure", "none"];
    public string AssertLevel
    {
        get => context.GetBenchProperty(nameof(AssertLevel)) ?? "default";
        set
        {
            if(value == "default") context.RemoveBenchProperty(nameof(AssertLevel));
            else context.SetBenchProperty(nameof(AssertLevel), value);
            OnPropertyChanged();
        }
    }
    
    public string AdditionalGhdlOptions
    {
        get => context.GetBenchProperty(nameof(AdditionalGhdlOptions)) ?? "";
        set
        {
            if(string.IsNullOrWhiteSpace(value)) context.RemoveBenchProperty(nameof(AdditionalGhdlOptions));
            else context.SetBenchProperty(nameof(AdditionalGhdlOptions), value);
            OnPropertyChanged();
        }
    }
    
    public string AdditionalGhdlSimOptions
    {
        get => context.GetBenchProperty(nameof(AdditionalGhdlSimOptions)) ?? "";
        set
        {
            if(string.IsNullOrWhiteSpace(value)) context.RemoveBenchProperty(nameof(AdditionalGhdlSimOptions));
            else context.SetBenchProperty(nameof(AdditionalGhdlSimOptions), value);
            OnPropertyChanged();
        }
    }

    public string GtkwSaveFile
    {
        get => context.GetBenchProperty(nameof(GtkwSaveFile)) ?? "";
        set
        {
            if (string.IsNullOrWhiteSpace(value)) context.RemoveBenchProperty(nameof(GtkwSaveFile));
            else context.SetBenchProperty(nameof(GtkwSaveFile), value);
            OnPropertyChanged();
        }
    }

    public string GtkwWaveArgs
    {
        get => context.GetBenchProperty(nameof(GtkwWaveArgs)) ?? "";
        set
        {
            if (string.IsNullOrWhiteSpace(value)) context.RemoveBenchProperty(nameof(GtkwWaveArgs));
            else context.SetBenchProperty(nameof(GtkwWaveArgs), value);
            OnPropertyChanged();
        }
    }
}