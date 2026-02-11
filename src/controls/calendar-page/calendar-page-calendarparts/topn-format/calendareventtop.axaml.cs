using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;
using Calendar;
using Manager;

namespace project_main;

#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.

public partial class CalendarEventTop : UserControl, ICalendarEvent {

    Event linkedEvent = new Event("", new DateTime { }, new DateTime { }, "", "", false, false, "", 0, 0);
    string text = "";

    public CalendarEventTop(string colour, Event le, string showTime) {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);
        // The link to the event to open when this element is clicked on. 
        linkedEvent = le;

        // Initialisation Code
        Button button = this.FindControl<Button>("CETButton");
        TextBlock textBlock = this.FindControl<TextBlock>("CETButtonText");

        // This looks unusual but all its doing is converting a string to a colour brush then casting that to the 
        // ImmutableSolidColourBrush type which is required since it is the type used for the background property of Avalonia elements. 
        if (colour != "") {
            button.Background = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(colour);
            textBlock.Background = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(colour);
        }
        else { // Colour is null, defaults to schemeAccent as the colour for this. 
            button.Background = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAccent"]);
            textBlock.Background = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAccent"]);
        }


        // Begin to sort out the text for the event. 
        text = linkedEvent.eventTitle;

        // Truncate text if neccasary. 
                                    // CALENDAREVENTTITLELIMIT is a constant value which determines the maximum length of an event title before it is visually truncated. 
        if (text.Length > Constants.CALENDAREVENTTITLELIMIT) {
            text = text.Substring(0, Constants.CALENDAREVENTTITLELIMIT) + "...";
        }

        // If this event is one where the time should be displayed,also amend the time stamp to the text. 
        if (showTime == "short") {
            text += "\n" + linkedEvent.ReturnStamp();
        }
        else if (showTime == "long") { // For an expanded time stamp. 
            text += "\n" + linkedEvent.ReturnFullStamp();
        }

        textBlock.Text = text;
    }

    public void OnPress(object sender, RoutedEventArgs args) {
        // Open view page with details of the linked event.
        this.FindAncestorOfType<MainWindow>().ChangeViewEvent(linkedEvent, null);
    }
}

#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings