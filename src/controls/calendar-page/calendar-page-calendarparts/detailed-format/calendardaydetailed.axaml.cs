using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace project_main;

#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.

public partial class CalendarDayDetail : UserControl, ICalendarDay {
    public CalendarDayDetail(string rowdef, string rowdefexc , DateTime day, bool firstInRange, bool fullDetail) {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);

        // Initialisation Code

        // For this type of day the grid is made out of various 'chunks' of the day.
        // However this will still be structured very similarly to the top_n days. 
        // Also, if this is the first day in the range an additional sidebar is appended to the side. 

        Grid outerGrid = this.FindControl<Grid>("CDDMainOuterGrid");
                             
        TextBlock text = this.FindControl<TextBlock>("CDDTextHeader");

        Grid innerGrid = this.FindControl<Grid>("CDDMainInnerGrid");
        Grid sidebar = this.FindControl<Grid>("CDDTimeSidebar");


        if (firstInRange) {
            outerGrid.ColumnDefinitions = new ColumnDefinitions("50, *");

            // Define the sidebar. 
            sidebar.RowDefinitions = new RowDefinitions(rowdefexc); // Changed this from rowdef to rowdef 2. 


            // Iterate through each row of the sidebar. 
            // For half detailed, you add an hourly mark every 2 'chunks'
            // For full detailed, you add an hourly mark every 4 'chunks' 
            int interval = fullDetail ? 4 : 2;
            int time = 0;
            for (int i = 0; i < sidebar.RowDefinitions.Count; i++) {  
                if ((i+1) % interval == 0) {
                    DockPanel innerContainer = new DockPanel() {
                        [Grid.RowProperty] = i
                    };
                    TextBlock innerBlock = new TextBlock();

                    innerBlock.Text = (time < 10 ? $"0{time}" : time.ToString()) + ":00"; // A one digit time will still show as two digits.
                    innerBlock.Classes.Add("CDDSidebarText");
                    innerContainer.Classes.Add("CDDSidebarTextContainer");

                    innerContainer.Children.Add(innerBlock);
                    sidebar.Children.Add(innerContainer); 

                    time++;
                }
            }

        }
        else {
            // No sidebar 
            outerGrid.ColumnDefinitions = new ColumnDefinitions("0,*");
        }
        innerGrid.RowDefinitions = new RowDefinitions(rowdef); // Inner added right afterwards whether or not there is a sidebar. 

        // Set header. 
        if (fullDetail) {
            text.Text = day.ToString("dddd") + " " + day.ToString("dd") + " " + day.ToString("MMMM");
        } 
        else { // Uses a very slightly shorter title to fit a bit better. 
            text.Text = day.ToString("dddd") + " " + day.ToString("dd") + " " + day.ToString("MMM");
        }
    }
}
#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings