using System.Collections.Specialized;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using OneWare.GhdlExtension.Services;
using OneWare.Essentials.Models;
using OneWare.UniversalFpgaProjectSystem;
using OneWare.UniversalFpgaProjectSystem.Models;
using Prism.Ioc;

namespace OneWare.GhdlExtension.Helpers;

public class CologneChipSettingsHelper
{
    static IImage? icon = Application.Current!.FindResource(ThemeVariant.Dark, "ForkAwesome.Check") as IImage;

    public static void UpdateProjectProperties(UniversalFpgaProjectRoot project, string? constraintFile)
    {
        bool ccfInclude = true;
        var test = project.Properties["Include"]?.AsArray()!;
        foreach (var t in test)
        {
            if (t.ToString() == "*.ccf")
                ccfInclude = false;
        }

        if (ccfInclude)
        {

            project.Properties["Include"]?.AsArray().Add("*.ccf");
        }

        JsonNode js = new JsonObject();
        if (constraintFile != null)
        {
            js["ConstraintFile"] = constraintFile;
        }
        else
            js["ConstraintFile"] = CologneChipConstantService.CologneChipDefaultConstraintFile;

        project.Properties["CologneChip"] = js;
    }

    public static bool HasProjectProperties(UniversalFpgaProjectRoot project)
    {
        if (project.Properties.ContainsKey("CologneChip"))
        {
            if (project.Properties["CologneChip"]?.AsObject().ContainsKey("ConstraintFile") ?? false)
            {
                return true;
            }
        }

        return false;
    }

    public static string GetConstraintFile(UniversalFpgaProjectRoot project)
    {
        if (!HasProjectProperties(project)) return CologneChipConstantService.CologneChipDefaultConstraintFile;

        var path = project.Properties["CologneChip"]?.AsObject()?["ConstraintFile"]?.ToString();
        return path ?? CologneChipConstantService.CologneChipDefaultConstraintFile;
    }

    public static void UpdateProjectOverlay(UniversalFpgaProjectRoot root)
    {
        if (root.Properties["Toolchain"]?.ToString() != "CologneChip")
            return;

        var ccfFile = GetConstraintFile(root);
        
        foreach (var projectFile in root.Files)
        {
            if (icon != null)
            {
                projectFile.IconOverlays.Remove(icon);

                if (projectFile.RelativePath == ccfFile)
                {
                    projectFile.IconOverlays.Add(icon);
                }
            }
        }
    }

    public static Task UpdateProjectProperties(IProjectFile file)
    {
        if (file.Root is not UniversalFpgaProjectRoot universalFpgaProjectRoot)
            return Task.CompletedTask;

        var path = GetConstraintFile(universalFpgaProjectRoot);

        // var icon = Application.Current!.FindResource(ThemeVariant.Dark, "VsImageLib.Test") as IImage;

        foreach (var projectFile in file.Root.Files)
        {
            if (icon != null) projectFile.IconOverlays.Remove(icon);
        }

        if (icon != null && !file.IconOverlays.Contains(icon))
            file.IconOverlays.Add(icon);

        if (file.RelativePath == path)
            return Task.CompletedTask;

        UpdateProjectProperties(universalFpgaProjectRoot, file.RelativePath);
        return ContainerLocator.Container.Resolve<UniversalFpgaProjectManager>()
            .SaveProjectAsync(universalFpgaProjectRoot);
    }

    public static void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Überprüfen, ob Elemente hinzugefügt wurden
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (var newItem in e.NewItems)
            {
                if (newItem is not UniversalFpgaProjectRoot universalFpgaProjectRoot) 
                    continue;
                
                UpdateProjectOverlay(universalFpgaProjectRoot);
            }
        }
    }
}
