using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Calendar;
using System.Collections.Generic;
using Avalonia.Interactivity;
using Manager;
using Avalonia.VisualTree;

namespace project_main;

#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.

public partial class CalendarEventChunk : UserControl, ICalendarEvent {
    Event linkedEvent = new Event("", new DateTime { }, new DateTime { }, "", "", false, false, "", 0, 0);
    List<Event> overlap = new List<Event>{};
    string text = "";

    public CalendarEventChunk(string colour, Event le, List<Event> overlaps, bool isFirst, bool isLast, bool fullDetail) {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);

        linkedEvent = le;
        overlap = overlaps;

        // Initialisation Code 

        Border border = this.FindControl<Border>("CECBorder");
        TextBlock textBlock = this.FindControl<TextBlock>("CECText");

        // Style will depend on FullDetail since there is less space for text if you have FullDetail compared to if you don't. 
        textBlock.Classes.Add(fullDetail ? "CECSmaller" : "CECLarger");

        if (colour != "") {
            border.Background = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(colour);
            textBlock.Background = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(colour);
        }
        else {
            border.Background = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAccent"]);
            textBlock.Background = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAccent"]);
        }


        if (isFirst) { // There is both a neccasary style class to add
                       // And the button text which needs to be added for the first 'thing' in the whole 'run'. 
            border.Classes.Add("CECFirst");
            text = linkedEvent.eventTitle;
            if (text.Length > Constants.CALENDAREVENTTITLELIMIT) // Once again truncation if text is too long. 
            {
                text = text.Substring(0, Constants.CALENDAREVENTTITLELIMIT) + "...";
            }

            text += " " + linkedEvent.ReturnDetailedStamp();

        }
        else if (isLast) {
            border.Classes.Add("CECLast");
        }
        textBlock.Text = text;
    }


    public void OnPress(object sender, RoutedEventArgs args) { // This will have to be adjusted to take into account that now these do not implement a button.
                            // There are still built in Avalonia events that I can use to detect button presses. 
         this.FindAncestorOfType<MainWindow>().ChangeViewEvent(linkedEvent, overlap);   
    }
}
#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings