using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Calendar;
using Manager;

namespace project_main;

#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.
#pragma warning disable CS8604 // Compiler Warning Suppressed --  Possible null reference argument for parameter. 
#pragma warning disable CS8629 // Compiler Warning Suppressed -- Nullable value type may be null.

public partial class CalendarPage : UserControl, IInitialisable {

    public string NewButtonText => "New Event";
    public string UpdateButtonText => "Update View";
    public string FilterButtonText => "Filter View";

    private string[] typeStrings = [ // Used to set up the graphical element for a selection box only. 
            "Filter Event: No Filter",
            "Filter Event: Study",
            "Filter Event: Work",
            "Filter Event: School/Homework",
            "Filter Event: Free Time"
    ];

    // I need two ObservableCollections here for the two combo boxes
    // One is just strings of each type set by the user. 
    // One is strings of each possible DR_Length.

    public ObservableCollection<string> rangeOptions = new ObservableCollection<string> { };
    public ObservableCollection<string> filterTypeOptions = new ObservableCollection<string> { };


    public CalendarPage() {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);

        // Get ranges
        foreach (DR_Length l in Enum.GetValues(typeof(DR_Length))) {
            rangeOptions.Add(Converters.length_TO_string[l]);
        }

        // Update visuals. This only occurs once since the options for ranges do not change ever.
        ComboBox rangePicker = this.FindControl<ComboBox>("Sidebar2RangePicker");

        rangePicker.ItemsSource = rangeOptions;
        rangePicker.SelectedIndex = 0;
    }

    public void InitialisePage() {
        // Page initialisation code here.

        // Update the combo box for calendar 'types' 
        filterTypeOptions.Clear();
        foreach (string i in typeStrings) { // These are the inbuilt types which will never change. 
                                            // I have defined an array in this file for the sole purpose of providing the text for the selection box. 
            filterTypeOptions.Add(i);
        }
        foreach (Dictionary<string, string> i in ManagerValues.CurrentCalendarPreferences.customTypes) {
            // Since custom types are defined by the user I'll have to fetch these from the dictionary of custom types I keep in CurrentCalendarPreferences 
            filterTypeOptions.Add(i["name"]);
        }

        ComboBox filterTypePicker = this.FindControl<ComboBox>("SidebarFilterTypes");

        filterTypePicker.ItemsSource = filterTypeOptions;
        filterTypePicker.SelectedIndex = 0;
    }
    // Note an amendment to the design, this method does not have parameters.   
    public void UpdateCalendarPage(object sender, RoutedEventArgs args) {
        // Get the data
        // Initial Day
        DateTime initialDay = this.FindControl<Avalonia.Controls.Calendar>("Sidebar1CalendarPicker").SelectedDate ?? DateTime.Today.Date;
        // Here if no day is selected, we instead just provide 'today' 

        // Length -- Fetching before loading the range. 
        DR_Length length = Converters.string_TO_length[
            this.FindControl<ComboBox>("Sidebar2RangePicker").SelectedItem.ToString()
        ];
        // Similar method to fetch the filter. If there is no selected filter this just is null.  
        // The null value is accounted for later down the line directly when fetching the required events. 
        string? filter = Converters.filterName_TO_eventName[
            this.FindControl<ComboBox>("SidebarFilterTypes").SelectedItem.ToString()
        ];

        // This calls the method to fetch the range that will be displayed. 
        D_Range range = RangeLoader.ReturnDateRange(length, initialDay);

        // Visual calendar loading. Setting up the graphical area. 
        ((int, int), CalendarResolutionRule) resolutionRuleset = Converters.CalendarResolutionTable[length];
        Grid mainElementGrid = this.FindControl<Grid>("MainElement");

        /* Structure of resolutionRuleset for reference 
            resolutionRuleset -- Tuple
            Item 1 | 
                Anonymous Tuple -- Tuple
                Item 1 | 
                    Number of rows the calendar needs to load in -- Integer
                Item 2 | 
                    Number of columns the calendar needs to load in -- Integer
            Item2 |
                CalendarResolutionRule -- Enum type
        */


        // Clear last grid to prevent overlap errors. 
        mainElementGrid.Children.Clear();

        // Set up the grid based on rules fetched. 
        // This is done by generating the string of rules (same way I would set this up in a .axaml file) 
        // Which can be converted to a RowDefinitions or a ColumnDefinitions. 
        // This is done at runtime since these strings would be long and really difficult to adjust in the future if I wrote them directly into code. 
        string rowString = "";
        string columnString = "";
        for (int i = 1; i <= resolutionRuleset.Item1.Item1; i++) { // Sets up rows
            rowString += "*";
            if (i != resolutionRuleset.Item1.Item1) { // If is not last row
                rowString += ",";
            }
        }
        for (int i = 1; i <= resolutionRuleset.Item1.Item2; i++) { // Sets up columns 
            columnString += "*";
            if (i != resolutionRuleset.Item1.Item2) { // Similarly, if not last column. 
                columnString += ",";
            }
        }


        // Set grid dimensions.
        mainElementGrid.RowDefinitions = new RowDefinitions(rowString);
        mainElementGrid.ColumnDefinitions = new ColumnDefinitions(columnString);


        // Add the relevant type of day element to the calendar.
        if (resolutionRuleset.Item2 == CalendarResolutionRule.CRR_Top10_TD ||
            resolutionRuleset.Item2 == CalendarResolutionRule.CRR_Top5_TD ||
            resolutionRuleset.Item2 == CalendarResolutionRule.CRR_Top4_T) {
            // These resolutions use a top_n format. 

            for (int i = 1; i <= resolutionRuleset.Item1.Item1; i++) { // Through each row. 
                for (int j = 1; j <= resolutionRuleset.Item1.Item2; j++) {
                    mainElementGrid.Children.Add(
                        /* 
                            Here I specify four things, the row definitions of the grid within the CalendarDayTop
                            Which are provided by a converter dictionary, the day object it represents (for the header element),
                            Along with the row and column of this element 
                            In the parent calendar view space. 
                        */
                        new CalendarDayTop(Converters.rule_TO_gridSetup[resolutionRuleset.Item2],
                                           range.fullRange[(i - 1) * resolutionRuleset.Item1.Item2 + j - 1].startTime) {
                            [Grid.RowProperty] = i - 1,
                            [Grid.ColumnProperty] = j - 1,
                        }


                    );
                }
            }

        }
        else { // While these resolutions used a detailed format. 
               // This means the grid needs to be set up differently. 


            // I've decided to generate the row definition strings for these days during runtime (also see, previous definition strings for the entire range above). 
            // Since they're really quite long and this is significantly neater looking and easier to read and adjust values for. 
            string rowDef = "";
            string rowDefExclusive = ""; // A second string which excludes some values. Used to set up a sidebar that only has elements every few rows. 
            // 48 rows when half detailed, 96 when fully detailed. 
            bool fullDetail = resolutionRuleset.Item2 == CalendarResolutionRule.CRR_FullDetail;
            int rowTotal = fullDetail ? 96 : 48;
            int interval = fullDetail ? 4 : 2;
            for (int r = 1; r <= rowTotal; r++) {
                rowDef += "*";
                if (r % interval == 0) {
                    rowDefExclusive += "*";
                }
                else {
                    rowDefExclusive += "1";
                }
                if (r != rowTotal) {
                    rowDef += ",";
                    rowDefExclusive += ",";
                }
            }

            for (int i = 1; i <= resolutionRuleset.Item1.Item2; i++) {
                // Notice that this format of days can only have a singular row. 
                mainElementGrid.Children.Add(
                    // See above for definition of a CalendarDayTop
                    /*
                        CalendarDayDetailed is very similar, but has a few more parameters to pass into the constructor. 
                        These are the row definitions, the day, whether or not this is the first day in the range, 
                        Whether or not we're setting this up for a fully detailed day, 
                        And I also include the column property of the day for the grid.
                        RowProperty is not neccasary since detailed days will always be in one row (there is not enough space visually to have two+ rows)  
                    */
                    new CalendarDayDetail(rowDef, rowDefExclusive, range.fullRange[i - 1].startTime, i == 1, fullDetail) {
                        [Grid.ColumnProperty] = i - 1
                    }
                );

            }
        }

        /*   Definitions of the graphical area to display the days is now done.
            Next however is actually deciding which of the events in the provided range to display and to display them
            on the calendar. 
        */



        // Iterate through and set up each day appropriately 
        for (int i = 0; i < range.fullRange.Count; i++) {

            // Variable declaration for the following code. 
            Grid calendarDayDisplay;
            List<Event> dayEvents;
            DateTime lowerBound;
            DateTime upperBound;
            string lastid;
            string thisid;
            string nextid;
            List<DayChunk> parsed;

            // Our current day. 
            Day day = range.fullRange[i];

            switch (resolutionRuleset.Item2) {
                // top_n sets up to top_10. 
                case CalendarResolutionRule.CRR_Top4_T:
                    calendarDayDisplay = mainElementGrid.Children[i].FindControl<Grid>("CDTMainGrid");

                    // We only want the events of the highest priority, and for this rule we sort them by priority first. 
                    dayEvents = EventSorter.GetEventsOfHighestPriorityInDay(day, 4, filter);
                    dayEvents = EventSorter.SortByPriority(dayEvents);
                    // Set up the grid to display events. 
                    SetupTopEventInternalGrid(dayEvents.Count, calendarDayDisplay);

                    // Add events to the grid. 
                    for (int j = 0; j < dayEvents.Count; j++) {
                        // Get colour for event
                        string colour = "";
                        if (ManagerValues.CurrentCalendarPreferences.typeColours.ContainsKey(dayEvents[j].eventType)) {
                            colour = ManagerValues.CurrentCalendarPreferences.typeColours[dayEvents[j].eventType];
                        }
                        else {
                            foreach (Dictionary<string, string> customType in ManagerValues.CurrentCalendarPreferences.customTypes) {
                                if (customType["name"] == dayEvents[j].eventType) {
                                    colour = customType["colour"];
                                }
                            }
                        }

                        // Add event to the calendar. Provides the colour, the specific linked event, an instruction to
                        // Not show the time in the calendar and the row of the grid we want to position the event in. 
                        calendarDayDisplay.Children.Add(
                        new CalendarEventTop(colour, dayEvents[j], "none") {
                            [Grid.RowProperty] = j
                        });
                    }
                    break;

                case CalendarResolutionRule.CRR_Top5_TD:
                    // There are only very minor adjustments from top_4. 
                    // These include fetching five days instead of four and sorting them by chronology. 
                    dayEvents = EventSorter.GetEventsOfHighestPriorityInDay(day, 5, filter);
                    dayEvents = EventSorter.SortByChronology(dayEvents, day);
                    calendarDayDisplay = mainElementGrid.Children[i].FindControl<Grid>("CDTMainGrid");
                    // Set up internal grid. 
                    SetupTopEventInternalGrid(dayEvents.Count, calendarDayDisplay);

                    for (int j = 0; j < dayEvents.Count; j++) {
                        // Get colour hex string
                        string colour = "";
                        if (ManagerValues.CurrentCalendarPreferences.typeColours.ContainsKey(dayEvents[j].eventType)) {
                            colour = ManagerValues.CurrentCalendarPreferences.typeColours[dayEvents[j].eventType];
                        }
                        else {
                            foreach (Dictionary<string, string> customType in ManagerValues.CurrentCalendarPreferences.customTypes) {
                                if (customType["name"] == dayEvents[j].eventType) {
                                    colour = customType["colour"];
                                }
                            }
                        }

                        // Add event
                        calendarDayDisplay.Children.Add(
                        new CalendarEventTop(colour, dayEvents[j], "short") {
                            [Grid.RowProperty] = j
                        });

                    }
                    break;

                case CalendarResolutionRule.CRR_Top10_TD:
                    // Very similar to top_5. 
                    dayEvents = EventSorter.GetEventsOfHighestPriorityInDay(day, 10, filter);
                    dayEvents = EventSorter.SortByChronology(dayEvents, day);
                    calendarDayDisplay = mainElementGrid.Children[i].FindControl<Grid>("CDTMainGrid");
                    // Set up internal grid. 
                    SetupTopEventInternalGrid(dayEvents.Count, calendarDayDisplay);

                    for (int j = 0; j < dayEvents.Count; j++) {
                        // Get colour
                        string colour = "";
                        if (ManagerValues.CurrentCalendarPreferences.typeColours.ContainsKey(dayEvents[j].eventType)) {
                            colour = ManagerValues.CurrentCalendarPreferences.typeColours[dayEvents[j].eventType];
                        }
                        else {
                            foreach (Dictionary<string, string> customType in ManagerValues.CurrentCalendarPreferences.customTypes) {
                                if (customType["name"] == dayEvents[j].eventType) {
                                    colour = customType["colour"];
                                }
                            }
                        }

                        // Add event
                        calendarDayDisplay.Children.Add(
                        new CalendarEventTop(colour, dayEvents[j], "long") {
                            [Grid.RowProperty] = j
                        });

                    }
                    break;

                // Below are the detailed sets of which there are two. 
                // These work differently in the way they load in new events but both have similar structures to eachother
                // With only minor adjustments in value. 
                case CalendarResolutionRule.CRR_HalfDetail:
                    calendarDayDisplay = mainElementGrid.Children[i].FindControl<Grid>("CDDMainInnerGrid");
                    dayEvents = day.events;
                    lowerBound = day.startTime;
                    upperBound = day.startTime.AddMinutes(30); // For half detail, we maintain a half hour difference between the upper and lower bounds. 
                    parsed = new List<DayChunk> { };
                    // Iterate through each availible 'chunk' of the day. 
                    // // Parse this chunk and add to the parsed outline of the day. 
                    for (int x = 0; x < 48; x++) {
                        List<Event> eventsInChunk = EventSorter.GetEventInChunk(lowerBound, upperBound, dayEvents, filter);
                        parsed.Add(EventSorter.ParseChunk(eventsInChunk));
                        // Increment bounds
                        lowerBound = lowerBound.AddMinutes(30);
                        upperBound = upperBound.AddMinutes(30);
                    }
                    // The second loop happens so I can first parse all the chunks, and then set up the graphics correctly 
                    // While being able to identify both the next chunk and the previous chunk. 
                    for (int x = 0; x < 48; x++) {
                        if (!parsed[x].blank) {
                            // Use to work out if this is the first or last row in a run. 
                            // This information is passed in when instantiating the CalendarEventChunk
                            lastid = x != 0 && !parsed[x - 1].blank ? parsed[x - 1].linkedEvent.eventID : "";
                            thisid = parsed[x].linkedEvent.eventID;
                            nextid = x != 47 && !parsed[x + 1].blank ? parsed[x + 1].linkedEvent.eventID : "";

                            // Get colour hex string. This is a bit more complicated than with top_n events since if there is an overlap
                            // I slightly reduce the saturation of the colour using the DimColours(); method. 
                            string colour = "";
                            if (ManagerValues.CurrentCalendarPreferences.typeColours.ContainsKey(parsed[x].linkedEvent.eventType)) {
                                colour = ManagerValues.CurrentCalendarPreferences.typeColours[parsed[x].linkedEvent.eventType];
                            }
                            else {
                                foreach (Dictionary<string, string> customType in ManagerValues.CurrentCalendarPreferences.customTypes) {
                                    if (customType["name"] == parsed[x].linkedEvent.eventType) {
                                        colour = customType["colour"];
                                    }
                                }
                            }
                            if ((bool)parsed[x].isOverlap) { // Dims the colour of a chunk if there is an overlap.
                                colour = EventSorter.DimColours(colour);
                            }

                            // Add event to calendar. There's a few more details I need to pass in to CalendarEventChunk than 
                            // CalendarEventChunk

                            // colour -- linked event -- is first -- is last -- is fully detailed. 
                            calendarDayDisplay.Children.Add(
                                new CalendarEventChunk(colour, parsed[x].linkedEvent, parsed[x].overlappedEvents, lastid != thisid, thisid != nextid, false) {
                                    [Grid.RowProperty] = x
                                }
                            );

                        }

                    }

                    break;
                // Minor adjustments from half detail to some values. 
                // These include having more rows in the grid and the interval between the upper and lower bounds being only 15 minutes. 
                case CalendarResolutionRule.CRR_FullDetail:
                    calendarDayDisplay = mainElementGrid.Children[i].FindControl<Grid>("CDDMainInnerGrid");
                    dayEvents = day.events;
                    lowerBound = day.startTime;
                    upperBound = day.startTime.AddMinutes(15);
                    parsed = new List<DayChunk> { };
                    // Iterate through each availible 'chunk' of the day. 
                    // // Parse this chunk and add to the parsed outline of the day. 
                    for (int x = 0; x < 96; x++) {
                        List<Event> eventsInChunk = EventSorter.GetEventInChunk(lowerBound, upperBound, dayEvents, filter);
                        parsed.Add(EventSorter.ParseChunk(eventsInChunk));
                        // Increment bounds
                        lowerBound = lowerBound.AddMinutes(15);
                        upperBound = upperBound.AddMinutes(15);
                    }
                    // The second loop happens so I can first parse all the chunks, and then set up the graphics correctly 
                    // While being able to identify both the next chunk and the previous chunk. 

                    for (int x = 0; x < 96; x++) {
                        if (!parsed[x].blank) {
                            // Use to work out if this is the first or last row in a run. 
                            lastid = x != 0 && !parsed[x - 1].blank ? parsed[x - 1].linkedEvent.eventID : "";
                            thisid = parsed[x].linkedEvent.eventID;
                            nextid = x != 95 && !parsed[x + 1].blank ? parsed[x + 1].linkedEvent.eventID : "";

                            // Get colour
                            string colour = "";
                            if (ManagerValues.CurrentCalendarPreferences.typeColours.ContainsKey(parsed[x].linkedEvent.eventType)) {
                                colour = ManagerValues.CurrentCalendarPreferences.typeColours[parsed[x].linkedEvent.eventType];
                            }
                            else {
                                foreach (Dictionary<string, string> customType in ManagerValues.CurrentCalendarPreferences.customTypes) {
                                    if (customType["name"] == parsed[x].linkedEvent.eventType) {
                                        colour = customType["colour"];
                                    }
                                }
                            }
                            if ((bool)parsed[x].isOverlap) { // Dims the colour of a chunk if there is an overlap.
                                colour = EventSorter.DimColours(colour);
                            }

                            // Add event to calendar. 
                            calendarDayDisplay.Children.Add(
                                new CalendarEventChunk(colour, parsed[x].linkedEvent,parsed[x].overlappedEvents, lastid != thisid, thisid != nextid, true) {
                                    [Grid.RowProperty] = x
                                }
                            );

                        }

                    }
                    break;
            }

        }
    }
     

    
    // Just a method to set up the internal grid for top_n events to keep things neat. 
    private void SetupTopEventInternalGrid(int count, Grid grid)
    {
        // Sets up the internal grid to display the top events. 
        string setup = "";

        for (int i = 1; i <= count; i++)
        {
            setup += "*";
            if (i != count)
            {
                setup += ",";
            }
        }
        grid.RowDefinitions = new RowDefinitions(setup);
    }

    public void CreateButtonPress (object sender, RoutedEventArgs args) {
        this.FindAncestorOfType<MainWindow>().EnableCreateEvent();
    }
}
#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings
#pragma warning restore CS8604 // Re-enabling Warnings
#pragma warning restore CS8629 // Re-enabling Warnings