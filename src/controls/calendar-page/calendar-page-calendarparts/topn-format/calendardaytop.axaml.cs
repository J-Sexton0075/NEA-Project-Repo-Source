using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace project_main;

#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.

public partial class CalendarDayTop : UserControl, ICalendarDay {
    public CalendarDayTop(string rowdef, DateTime day)
    {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);

        // Initialisation Code

        // Initialisation -- Set grid definitions for events
        Grid grid = this.FindControl<Grid>("CDTMainGrid");
        grid.RowDefinitions = new RowDefinitions(rowdef);

        // Initialisation -- Set text timestamp to correct string 
        TextBlock text = this.FindControl<TextBlock>("CDTTextHeader");
        text.Text = day.ToString("ddd") + " " + day.ToString("dd") + " " + day.ToString("MMM");
    }

        // Unused
    //public void SetTextStampFromDate(DateTime day) {
    //
    //}
    /*
        This is unused since I just did funcitonally the same thing above in initialisation.  
    */
}

#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings