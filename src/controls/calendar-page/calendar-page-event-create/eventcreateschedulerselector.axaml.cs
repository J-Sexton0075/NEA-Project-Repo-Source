using Avalonia.Controls;
using System;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;


namespace project_main;


#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.

public partial class EventCreateSchedulerSelector : UserControl {
// This class is used as a template repeatable graphical element I can add whenever I need to. 
    DateTime connected; 
    string connectedString;

    public EventCreateSchedulerSelector(DateTime c) {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);

        TextBlock text = this.FindControl<TextBlock>("ECSSText");
        connected = c;

        connectedString = connected.ToString("dd/MM/yyyy HH:mm");
        text.Text = connectedString;
    }

    public void ButtonClick(object sender, RoutedEventArgs args) {
        // Access the AutomaticSchedullerSet and set the value as neccassary. 
        this.FindAncestorOfType<EventCreate>().AutomaticSchedulerSet(connected);
    }
}

#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings