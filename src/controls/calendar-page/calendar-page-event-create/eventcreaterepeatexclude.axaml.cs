using Avalonia.Controls;
using System;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;


namespace project_main;


#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.

public partial class EventCreateRepeatExclude : UserControl {
// This class is used as a template repeatable graphical element I can add whenever I need to. 
    public DateTime startOfExclusion; 
    public DateTime endOfExclusion;
    public string? idOfExclusion;

    public EventCreateRepeatExclude(DateTime start, DateTime end, int index) {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);

        startOfExclusion = start;
        endOfExclusion = end;
        // Find some way to individually identify an exclusion, or just get rid of the last one.     
        
        TextBlock text = this.FindControl<TextBlock>("ECREText");
        text.Text = "Excluding " + start.ToString("yyyy-MM-dd") + " To " + end.ToString("yyyy-MM-dd");
    }

    public void ButtonClick(object sender, RoutedEventArgs args) {
        // Remove this from the list. 
        this.FindAncestorOfType<EventCreate>().RemoveExclusion(this);
    }

}   
#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings