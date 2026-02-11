using Avalonia.Controls;
using System;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Immutable;
using Avalonia.Media;
using Overview;
using Manager;

namespace project_main;

#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.

public partial class OverviewBlock : UserControl {
// This class is used as a template repeatable graphical element I can add whenever I need to. 
    public DateTime startOfExclusion; 
    public DateTime endOfExclusion;
    public string? idOfExclusion;

    public OverviewBlock(string name, string colour, double stat) {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);

        Border border  = this.FindControl<Border>("OBBorder");
        
        TextBlock textTitle = this.FindControl<TextBlock>("OBTextName");
        TextBlock textStat = this.FindControl<TextBlock>("OBTextStat");
        TextBlock textDesc = this.FindControl<TextBlock>("OBTextDesc");

        // Set Values
        // Colour
        try {
            border.BorderBrush = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(colour);
        }
        catch (FormatException) {
            border.BorderBrush = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAccent"]);
        }


        // Text
        textTitle.Text = name;
        textStat.Text = Math.Round(stat, 2).ToString();
        textDesc.Text = GetDescription(name);
    }

    public OverviewBlock(string name, string colour, string stat) { // Special constructor that has a string based statitstic instead. 
        DataContext = this;
        AvaloniaXamlLoader.Load(this);

        Border border  = this.FindControl<Border>("OBBorder");
        
        TextBlock textTitle = this.FindControl<TextBlock>("OBTextName");
        TextBlock textStat = this.FindControl<TextBlock>("OBTextStat");
        TextBlock textDesc = this.FindControl<TextBlock>("OBTextDesc");

        // Set Values
        // Colour
        border.BorderBrush = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(colour);

        // Text
        textTitle.Text = name;
        textStat.Text = stat;
        textDesc.Text = GetDescription(name);
    }

    public string GetDescription(string name) {
        string text = "";
        if (OverviewConstants.DescriptionTable.TryGetValue(name, out string? value)) {
            text = value;
        }
        return text;
    }
}

#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings