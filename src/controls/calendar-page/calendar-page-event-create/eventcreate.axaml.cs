using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Manager;
using Newtonsoft.Json;
using System.IO;
using Calendar;
using System.Data;
using Microsoft.Data.Sqlite;
using System.Linq;
using Avalonia.VisualTree;

namespace project_main;

#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.
#pragma warning disable CS8604 // Compiler Warning Suppressed --  Possible null reference argument for parameter. 
#pragma warning disable CS8629 // Compiler Warning Suppressed -- Nullable value type may be null.

public partial class EventCreate : UserControl {

    public string ECTitleTextBinding => "Input Title: ";
    public string ECTitleWatermark => "Give your event a title!";
    public string ECDetailsTextBinding => "Input Description: ";
    public string ECDetailsWatermark => "Lorem Ipsum. Describe your event!";

    public string ECDStartTextBinding => "Start Date and Time";
    public string ECDEndTextBinding => "End Date and Time";
    public string ECDTypeTextBinding => "Event Type";
    public string ECDCompletableTextBinding => "Event Is Completable?";
    public string ECDPriorityTextBinding => "Priority";

    public string ECARTextBinding => "Repeated Events";
    public string ECARDescription1Binding => "This event will repeat every:";
    public string ECARDescription2Binding => "Excluding:";
    public string ECARDescription3Binding => "Repeated Event Ends: ";
    public string ECARButtonAddExclusionBinding => "Add Exclusion Range";

    public string ECASTextBinding => "Automatic Schedulling";
    public string ECASStatusBinding => "The automatic scheduller has no suggestions";

    public string CloseButtonTextBinding => "Cancel Event Creation";
    public string CreateButtonTextBinding => "Create Event";

    private bool pageIsInit = false; // Used to prevent some methods being called too early and causing a crash. 

    public ObservableCollection<string> typeOptions = new ObservableCollection<string> { };
    public ObservableCollection<string> ECARIntervalClassesBinding => new ObservableCollection<string> {
        "Does Not Repeat",
        "Day", 
        "Week",
        "Month",
        "Year"
    };
    
    // For event types
    private string[] typeStrings = [ // Used to set up the graphical element for a selection box only. 
        "Event Type: Study",
        "Event Type: Work",
        "Event Type: School/Homework",
        "Event Type: Free Time"
    ];

    public EventCreate() {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);
    }

    public void InitialisePage() {
        // Upon initialising the page, I really only want to clear all the previous values and then update the
        // Possible types in the ComboBox

        // Get elements then clears their value. 
        /*
            Title Box
            Description Box
            Start Time and Date
            End Time and Date
            Completable
            Priority
            Type
            Repeated events -- Excluded, And Dates
            Repeated events -- End Date
            Repeated events -- Interval
            Repeated events -- Interval Class
            Schedulled events
        */

        TextBox title = this.FindControl<TextBox>("ECTitleInput");
        TextBlock  alertBox = this.FindControl<TextBlock>("ECAlertText");
        TextBox description = this.FindControl<TextBox>("ECDescriptionInput");
        DatePicker startDate = this.FindControl<DatePicker>("ECDStartPicker");
        TimePicker startTime = this.FindControl<TimePicker>("ECDStartPickerTime");
        DatePicker endDate = this.FindControl<DatePicker>("ECDEndPicker");
        TimePicker endTime = this.FindControl<TimePicker>("ECDEndPickerTime");
        CheckBox completable = this.FindControl<CheckBox>("ECDCompletableBox");
        TextBox priority = this.FindControl<TextBox>("ECDPriorityBox");
        ComboBox types = this.FindControl<ComboBox>("ECDTypeComboBox");
        DatePicker excludeRangeStart = this.FindControl<DatePicker>("ExcludeRangeStartPicker");
        DatePicker excludeRangeEnd = this.FindControl<DatePicker>("ExcludeRangeEndPicker");
        DatePicker repeatedEnd = this.FindControl<DatePicker>("RepeatRangeEnd");
        TextBox repeatedIntervalBox = this.FindControl<TextBox>("ECARInterval");
        ComboBox repeatedIntervalClass = this.FindControl<ComboBox>("ECARClass");
        DockPanel excluded =  this.FindControl<DockPanel>("ECARExcluded");
        DockPanel suggestions = this.FindControl<DockPanel>("ECASSuggestions");

        // Clear alert box 
        alertBox.Text = "";

        // Now I clear each element of content. For some of these (such as the date times) I set this to a default value rather than clearing it.  
        title.Text = "";
        description.Text = "";
        startDate.SelectedDate = DateTime.Now.Date;
        endDate.SelectedDate = DateTime.Now.Date;
        startTime.SelectedTime = DateTime.Now.Date.TimeOfDay;
        endTime.SelectedTime = DateTime.Now.Date.AddHours(1).TimeOfDay; // So there's a slight difference between the two.

        completable.IsChecked = false;
        priority.Text = "";
        types.ItemsSource = null;
        excludeRangeStart.SelectedDate = DateTime.Now.AddDays(1).Date; // Same reasoning, I just add a slight difference in (arbitrary value. )
        excludeRangeEnd.SelectedDate = DateTime.Now.AddDays(8).Date;

        excluded.Children.Clear();
        suggestions.Children.Clear();

        repeatedIntervalClass.SelectedIndex = 0;
        repeatedIntervalBox.Text = "";

        repeatedEnd.SelectedDate = DateTime.Now.AddDays(14).Date; // Again a bit of an arbitrary value, but I just wanted this to by default be shifted a bit further along.

        // Setup the type combobox. I can do this in the same way I do in the CalendarPage
        typeOptions.Clear();
        foreach (string i in typeStrings) { // These are the inbuilt types which will never change. 
                                            // I have defined an array in this file for the sole purpose of providing the text for the selection box. 
            typeOptions.Add(i);
        }
        foreach (Dictionary<string, string> i in ManagerValues.CurrentCalendarPreferences.customTypes) {
            // Since custom types are defined by the user I'll have to fetch these from the dictionary of custom types I keep in CurrentCalendarPreferences 
            typeOptions.Add(i["name"]);
        }

        types.ItemsSource = typeOptions;
        types.SelectedIndex = 0;

        // Default initialisation of the Automatic Scheduller
        AutomaticSchedulerShow();
        pageIsInit = true;
    }

    public void ClosePage(object sender, RoutedEventArgs args) {
        pageIsInit = false;
        this.FindAncestorOfType<MainWindow>().DisableCreateEvent();
    }
    
    public void CreateEvent(object sender, RoutedEventArgs args) {
        // Start and end have to be generated from both the date picker and time picker

        // There is potential to neaten this up a bit, but it should work. 
        DateTime start = (
        (DateTimeOffset)this.FindControl<DatePicker>("ECDStartPicker")
                            .SelectedDate).DateTime.Add(
        (TimeSpan)this.FindControl<TimePicker>("ECDStartPickerTime")
                            .SelectedTime
        );



        DateTime end = (
        (DateTimeOffset)this.FindControl<DatePicker>("ECDEndPicker")
                            .SelectedDate).DateTime.Add(
        (TimeSpan)this.FindControl<TimePicker>("ECDEndPickerTime")
                            .SelectedTime
        );
        
        string oldType = (string)this.FindControl<ComboBox>("ECDTypeComboBox").SelectedItem;
        oldType ??= "eventFree"; // Defaults if a null value occurs, which it shouldn't. 
        Converters.typeName_TO_eventName.TryGetValue(oldType, out string type); // Converts to the correct format of string.
        type ??= oldType;

        bool IsCompletable = (bool)this.FindControl<CheckBox>("ECDCompletableBox").IsChecked;
        
        string title = this.FindControl<TextBox>("ECTitleInput").Text;

        string desc = this.FindControl<TextBox>("ECDescriptionInput").Text;
        

        // Validation work for priority and repeated events

        bool isValidPrior = int.TryParse(this.FindControl<TextBox>("ECDPriorityBox").Text, out int priority);
        
        DockPanel repeats = this.FindControl<DockPanel>("ECAdditionsRepeated");

        DateTime repeatedEndForValidation = ((DateTimeOffset)this.FindControl<DatePicker>("RepeatRangeEnd").SelectedDate).Date;

        Grid repeatsDetails = this.FindControl<Grid>("ECARRepeat");
        TextBox repeatsInterval = this.FindControl<TextBox>("ECARInterval");
        ComboBox repeatsClass = this.FindControl<ComboBox>("ECARClass");

        bool isValidRepeatInterval = true;
        bool isValidRepeatEnd = true;

        if ((string)repeatsClass.SelectedItem != "Does Not Repeat") {
            isValidRepeatInterval = int.TryParse(repeatsInterval.Text, out int o);
            if (repeatedEndForValidation <= end) {
                isValidRepeatEnd = false;
            }
        }
     

        // Validate input values. 
        if (!isValidPrior) {
            NotifyError("Priority must be an integer!");
        }
        else if (!isValidRepeatInterval) {
            NotifyError("Invalid Repetition Interval"!);
        }
        else if (!isValidRepeatEnd) {
            NotifyError("Repetition ranges must end after the event does!");
        }
        else if (end <= start) {
            NotifyError("The event must start before it finishes!");
        }
        else if (string.IsNullOrWhiteSpace(title)) {
            NotifyError("You must provide a title!");
        }
        else if (priority < 0 || priority > 999) {
            NotifyError("The event cannot have a priority less than 0 or greater than 999!”");
        }
        else {
            // body
            string startString = start.ToString("yyyy-MM-dd");
            string stringId = start.ToString("yyyyMMdd"); // Query database to get the number of events occuring on this day already. 
            using (SqliteConnection connection = new SqliteConnection($"Data Source={ManagerValues.CurrentCalendarPreferences.database}")) {
                connection.Open();
                
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = 
                @$"
                    SELECT COUNT(*)
                    FROM table_events
                    WHERE StartDay = '{startString}';
                ";
                SqliteDataReader reader = command.ExecuteReader();
                reader.Read();
                string val = reader.GetString(0); 
                stringId += val;
                connection.Close();
            }

            Event event_ = new Event(
                stringId, start, end, title, type, IsCompletable, false, desc, 0, priority
            );
            // Find covered days. 
            string daysCovered = string.Join(" ", RangeLoader.GetDatesInRange(start, end)
                                                                .Select(x => x.ToString("yyyy-MM-dd"))
                                                                .ToList()
            );
            string[] daysCoveredArr = daysCovered.Split(' ');
            // Add required days to database then commit to database. 
            using (SqliteConnection connection = new SqliteConnection($"Data Source={ManagerValues.CurrentCalendarPreferences.database}")) {
                connection.Open();

                // Check what days are not currently in the database. 
                SqliteCommand command_CheckDays = connection.CreateCommand();
                List<string> requireDays = new List<string> { };
                SqliteDataReader reader;
                foreach (string day in daysCoveredArr) {
                    command_CheckDays.CommandText = 
                    @$"
                        SELECT COUNT(*) FROM table_days
                        WHERE FullDate = '{day}'
                    ";
                    reader = command_CheckDays.ExecuteReader();
                    reader.Read();
                    int count = reader.GetInt32(0);
                    if (count == 0) {
                        // Day is not found so needs to be added.
                        requireDays.Add(day);
                    }
                    reader.Close();
                }

                // Add any required days to the database. 
                SqliteCommand command_AddRequiredDays = connection.CreateCommand();
                foreach (string day in requireDays) {
                    command_AddRequiredDays.CommandText = 
                    @$"
                        INSERT INTO table_days
                        (FullDate, StartTime, EndTime)
                        VALUES
                        ('{day}', '00:00:00', '23:59:59')
                    "; 
                    command_AddRequiredDays.ExecuteNonQuery();
                }

                // Commit event to database. 
                SqliteCommand command_Commit = connection.CreateCommand();

                // Interpolation Method -- OLD AND CAUSES A CRASH SINCE YOU CAN INJECT THIS WITH ' CHARACTERS
              /*  command_Commit.CommandText = 
                @$"
                    INSERT INTO table_events
                    (EventID, EventTitle, StartDay, StartTime, DaysCovered, EndDay, EndTime,
                    EventType, EventDesc, IsCompletable, IsComplete, StudyComplete, EventPriority)
                    VALUES ('{event_.eventID}', '{event_.eventTitle}', '{event_.startTime:yyyy-MM-dd}', '{event_.startTime:HH:mm:ss}',
                    '{daysCovered}', '{event_.endTime:yyyy-MM-dd}', '{event_.endTime:HH:mm:ss}', '{event_.eventType}', '{event_.eventDesc}', 
                    {event_.eventIsCompleteable}, {event_.eventIsCompleted}, {event_.studyCompleted}, {event_.eventPriority});
                ";
                */

                // Parameters Method
                command_Commit.CommandText = 
                @"
                    INSERT INTO table_events
                    (EventID, EventTitle, StartDay, StartTime, DaysCovered, EndDay, EndTime,
                    EventType, EventDesc, IsCompletable, IsComplete, StudyComplete, EventPriority)
                    VALUES (@eventID, @eventTitle, @eventStartDate, @eventStartTime,
                    @daysCovered, @eventEndDate, @eventEndTime, @eventType, @eventDesc, 
                    @eventIsCompletable, @eventIsCompleted, @eventStudyCompleted, @eventPriority);
                ";
                // Add parameters
                command_Commit.Parameters.AddWithValue("@eventID", event_.eventID);
                command_Commit.Parameters.AddWithValue("@eventTitle", event_.eventTitle);
                command_Commit.Parameters.AddWithValue("@eventStartDate", event_.startTime.ToString("yyyy-MM-dd"));
                command_Commit.Parameters.AddWithValue("@eventStartTime", event_.startTime.ToString("HH:mm:ss"));
                command_Commit.Parameters.AddWithValue("@daysCovered", daysCovered);
                command_Commit.Parameters.AddWithValue("@eventEndDate", event_.endTime.ToString("yyyy-MM-dd"));
                command_Commit.Parameters.AddWithValue("@eventEndTime", event_.endTime.ToString("HH:mm:ss"));
                command_Commit.Parameters.AddWithValue("@eventType", event_.eventType);
                command_Commit.Parameters.AddWithValue("@eventDesc", event_.eventDesc);
                command_Commit.Parameters.AddWithValue("@eventIsCompletable", event_.eventIsCompleteable);
                command_Commit.Parameters.AddWithValue("@eventIsCompleted", event_.eventIsCompleted);
                command_Commit.Parameters.AddWithValue("@eventStudyCompleted", event_.studyCompleted);
                command_Commit.Parameters.AddWithValue("@eventPriority", event_.eventPriority);

                command_Commit.ExecuteNonQuery();


                connection.Close();
            }

            // Update anything required. 
            if (event_.eventType.Length >= 5 && event_.eventType[0..5] == "event") {
                    UpdateScheduler(event_.eventType, start);
            }
            SetRepeatedEvents(event_);
            
            // Refresh range
            (Parent.Parent as Grid).FindControl<CalendarPage>("PageCalendarUC").UpdateCalendarPage(null!, null!);

            // Close Page
            ClosePage(null!,null!);
        }

        
    }   


    public void NotifyError(string error) {
        // Display any errors so the user can identify incorrect information. 
        TextBlock alertBox = this.FindControl<TextBlock>("ECAlertText");
        alertBox.Text = error;
    }

    // Repeated Events ------------
    public void SetRepeatedEvents(Event event_) {
        RepeatedBlock repetitionRange = GetRepeatedBlock();
        if (repetitionRange.rCode != "Does Not Repeat") {
            List<DateTime> datesToRepeat = new List<DateTime> { };
            int increment = 0;
            DateTime current = repetitionRange.rangeStart;

            // First find the days that require repeating on. 
            if (repetitionRange.rCode == "Day" || repetitionRange.rCode == "Week") {
                increment = repetitionRange.rCode == "Day" ? 1 * repetitionRange.rInterval : 7 * repetitionRange.rInterval;
                while (current <= repetitionRange.rangeEnd) {
                    // Development Note -- Will have to change design here slightly and havae two sections, one to validate whether it is in an exclusion
                    // And another to add it into the repeated dates. 
                    bool valid = true; 
                    foreach ((DateTime, DateTime) excl in repetitionRange.exclusions) { // Check if invalid
                        if (current >= excl.Item1 && current <= excl.Item2) {
                            valid = false;
                        }
                    } 
                    if (valid) {
                        datesToRepeat.Add(current);
                    }
                    current = current.AddDays(increment);
                }
            } // Now I just do the same things for months and years as I did for days and weeks. 
            else if (repetitionRange.rCode == "Month") {
                increment = 1 * repetitionRange.rInterval;
                    while (current <= repetitionRange.rangeEnd) {
                    bool valid = true; 
                    foreach ((DateTime, DateTime) excl in repetitionRange.exclusions) { // Check if invalid
                        if (current >= excl.Item1 && current <= excl.Item2) {
                            valid = false;
                        }
                    } 
                    if (valid) {
                        datesToRepeat.Add(current);
                    }
                    current = current.AddMonths(increment);
                }
            }
            else {
                increment = 1 * repetitionRange.rInterval;
                    while (current <= repetitionRange.rangeEnd) {
                    bool valid = true; 
                    foreach ((DateTime, DateTime) excl in repetitionRange.exclusions) { // Check if invalid
                        if (current >= excl.Item1 && current <= excl.Item2) {
                            valid = false;
                        }
                    } 
                    if (valid) {
                        datesToRepeat.Add(current);
                    }
                    current = current.AddYears(increment);
                }
            }

            // Now get the events being repeated themself. 
            List<Event> repeatedEvents = new List<Event> { };
            Event tempEvent;
            foreach (DateTime date in datesToRepeat) {
                if (date != event_.startTime.Date) { // Ensures that an event doesn't get set twice on the initial day. 
                    TimeSpan shift = date.Date -  event_.startTime.Date;
                    tempEvent = (Event)event_.Clone();
                    tempEvent.startTime = date.Add(event_.startTime.TimeOfDay);
                    tempEvent.endTime = event_.endTime.Add(shift);
                    repeatedEvents.Add(tempEvent);
                }
            }

            // Now commit those events to the database. 
            using (SqliteConnection connection = new SqliteConnection($"Data Source={ManagerValues.CurrentCalendarPreferences.database}")) {
                connection.Open();
                foreach (Event eventToAdd in repeatedEvents) {
                    // Get count for the event ID. 
                    SqliteCommand command_Count = connection.CreateCommand();
                    command_Count.CommandText = 
                    @$"
                        SELECT COUNT(*)
                        FROM table_events
                        WHERE StartDay = '{eventToAdd.startTime:yyyy-MM-dd}';
                    ";
                    SqliteDataReader reader = command_Count.ExecuteReader();
                    reader.Read();
                    string val = reader.GetString(0); 
                    // Generate ID and daysCovered
                    eventToAdd.eventID = eventToAdd.startTime.ToString("yyyyMMdd") + val;

                    string daysCovered = string.Join(" ", RangeLoader.GetDatesInRange(eventToAdd.startTime, eventToAdd.endTime)
                                                                     .Select(x => x.ToString("yyyy-MM-dd")).ToList());
                    string[] daysCoveredArr = daysCovered.Split(' ');

                    // This is copied over from the main body for committing to the database, and serves the same purpose 
                    // Of identifying and adding days that don't exist in the database to the database. 
                    SqliteCommand command_CheckDays = connection.CreateCommand();
                    List<string> requireDays = new List<string> { };
                    SqliteDataReader reader2;
                    foreach (string day in daysCoveredArr) {
                        command_CheckDays.CommandText = 
                        @$"
                            SELECT COUNT(*) FROM table_days
                            WHERE FullDate = '{day}'
                        ";
                        reader2 = command_CheckDays.ExecuteReader();
                        reader2.Read();
                        int count = reader2.GetInt32(0);
                        if (count == 0) {
                            requireDays.Add(day);
                        }
                    }
                    SqliteCommand command_AddRequiredDays = connection.CreateCommand();
                    foreach (string day in requireDays) {
                        command_AddRequiredDays.CommandText = 
                        @$"
                            INSERT INTO table_days
                            (FullDate, StartTime, EndTime)
                            VALUES
                            ('{day}', '00:00:00', '23:59:59')
                        "; 
                        command_AddRequiredDays.ExecuteNonQuery();
                    }



                    // Now we commit to database. 
                    SqliteCommand command_Commit = connection.CreateCommand();
                    command_Commit.CommandText = 
                    @"
                        INSERT INTO table_events
                        (EventID, EventTitle, StartDay, StartTime, DaysCovered, EndDay, EndTime,
                        EventType, EventDesc, IsCompletable, IsComplete, StudyComplete, EventPriority)
                        VALUES (@eventID, @eventTitle, @eventStartDate, @eventStartTime,
                        @daysCovered, @eventEndDate, @eventEndTime, @eventType, @eventDesc, 
                        @eventIsCompletable, @eventIsCompleted, @eventStudyCompleted, @eventPriority);
                    ";
                    // Add parameters
                    command_Commit.Parameters.AddWithValue("@eventID", eventToAdd.eventID);
                    command_Commit.Parameters.AddWithValue("@eventTitle", eventToAdd.eventTitle);
                    command_Commit.Parameters.AddWithValue("@eventStartDate", eventToAdd.startTime.ToString("yyyy-MM-dd"));
                    command_Commit.Parameters.AddWithValue("@eventStartTime", eventToAdd.startTime.ToString("HH:mm:ss"));
                    command_Commit.Parameters.AddWithValue("@daysCovered", daysCovered);
                    command_Commit.Parameters.AddWithValue("@eventEndDate", eventToAdd.endTime.ToString("yyyy-MM-dd"));
                    command_Commit.Parameters.AddWithValue("@eventEndTime", eventToAdd.endTime.ToString("HH:mm:ss"));
                    command_Commit.Parameters.AddWithValue("@eventType", eventToAdd.eventType);
                    command_Commit.Parameters.AddWithValue("@eventDesc", eventToAdd.eventDesc);
                    command_Commit.Parameters.AddWithValue("@eventIsCompletable", eventToAdd.eventIsCompleteable);
                    command_Commit.Parameters.AddWithValue("@eventIsCompleted", eventToAdd.eventIsCompleted);
                    command_Commit.Parameters.AddWithValue("@eventStudyCompleted", eventToAdd.studyCompleted);
                    command_Commit.Parameters.AddWithValue("@eventPriority", eventToAdd.eventPriority);
                    command_Commit.ExecuteNonQuery();
                    }

                connection.Close();
            }
        }
    }

    public RepeatedBlock GetRepeatedBlock() { // Gets the details from the interface and creates a RepeatedBlock. 

        DateTime start = ((DateTimeOffset)this.FindControl<DatePicker>("ECDStartPicker").SelectedDate).DateTime; // Also the start of the event itself.
        DateTime end = ((DateTimeOffset)this.FindControl<DatePicker>("RepeatRangeEnd").SelectedDate).DateTime;

        // More specific value, provided by a special DatePicker.
        string rCode =  this.FindControl<ComboBox>("ECARClass").SelectedItem.ToString();
        int.TryParse(this.FindControl<TextBox>("ECARInterval").Text, out int rInterval);

        List<(DateTime, DateTime)> exclusions = GetExcludedRangeRepeatedEvents();
        return new RepeatedBlock (start, end, rCode, rInterval, exclusions);
    }

    public List<(DateTime, DateTime)> GetExcludedRangeRepeatedEvents() { // Gets the excluded ranges for repeated events from the graphical interface. 
        DockPanel excluded = this.FindControl<DockPanel>("ECARExcluded");
        List<(DateTime, DateTime)> list = new List<(DateTime, DateTime)> { };
        foreach (EventCreateRepeatExclude exclude in excluded.Children) {
            list.Add((exclude.startOfExclusion, exclude.endOfExclusion));
        }
        return list;
    }

    public void UpdateEventCompletableBox(bool onoff) { // Special method that hides or shows the EventCompletable box. 
        DockPanel box = this.FindControl<DockPanel>("ECDetailsCompletable");
        box.IsVisible = onoff;
    }

    public void ExcludeRange(object sender, RoutedEventArgs args) { // Adds an exclusion range for repeated events. 
        // Get start and end 
        DockPanel exclusionPanel = this.FindControl<DockPanel>("ECARExcluded");

        DatePicker start = this.FindControl<DatePicker>("ExcludeRangeStartPicker");
        DatePicker end = this.FindControl<DatePicker>("ExcludeRangeEndPicker");

        DateTime startTime = ((DateTimeOffset)start.SelectedDate).DateTime;
        DateTime endTime = ((DateTimeOffset)end.SelectedDate).DateTime;

        // Validate
        if (endTime <= startTime) {
            NotifyError("Exclusions must start before they end!");
        }
        else {
            // Add to dockpanel 
            int index = exclusionPanel.Children.Count;
            exclusionPanel.Children.Add(
                new EventCreateRepeatExclude(startTime, endTime, index)
            );
        }
    } 

    public void RemoveExclusion(Control removing) { // Removes an exclusion range for repeated events
       DockPanel exclusionPanel = this.FindControl<DockPanel>("ECARExcluded");
       exclusionPanel.Children.Remove(removing);
    }

    // Scheduller ------------

    // Provides automatic suggestions for potential times. 

    public void TriggerAutomaticSchedulerShow(object sender, RoutedEventArgs args) { // Triggered when the event is updated. 
        if (pageIsInit) {AutomaticSchedulerShow();}
    }


    public void AutomaticSchedulerShow() {
        DateTime startOfToday = DateTime.Now.Date;

        // Development Note                    // You'll have to use a converter dictionary to convert the types.
                                            // Ensure that if the type isn't in the dictionary use the string directly as this wpuld be one of those 
                                            // That are a customtype. 
                                                        // Additional Note -- Using TryGetValue to do this. 
        string typeString = this.FindControl<ComboBox>("ECDTypeComboBox").SelectedItem.ToString();

        // Since this is always called when the event type is updated, I may as well update this here. All this does is makes sure you cannot set a Study event as completable.
        UpdateEventCompletableBox(typeString != "Event Type: Study");

        bool isCustom = !typeStrings.Contains(typeString);  // Checks whether or not this is a custom type.  

        // Back to scheduller 
        if (Converters.typeName_TO_schedulerName.TryGetValue(typeString, out string? value)) {
            typeString = value;
        } 

        if (!isCustom) { // Scheduller only works with non-custom types. 
            double boundry = ManagerValues.CurrentSchedulerContainer.ReturnSignificanceBoundry(typeString);
            List<(DayOfWeek, string)> validFrames = new List<(DayOfWeek, string)> { };

            // There's likely a neater way to do this, but I've planned to do this in the design and it should work, so there's no need to rework it.
            // I repeat the same block of code for each day of the week, just changing what day it operates on. 

            // Monday
            foreach (KeyValuePair<string, Dictionary<string, double>> monFrame in 
            ManagerValues.CurrentSchedulerContainer.Monday) {
                foreach (KeyValuePair<string, double> typeSig in monFrame.Value) {
                    if (typeSig.Key == typeString) {
                        if (typeSig.Value >= boundry) {
                            validFrames.Add((DayOfWeek.Monday, monFrame.Key));
                        }
                    }
                }
            }
            // Tuesday
            foreach (KeyValuePair<string, Dictionary<string, double>> tueFrame in 
            ManagerValues.CurrentSchedulerContainer.Tuesday) {
                foreach (KeyValuePair<string, double> typeSig in tueFrame.Value) {
                    if (typeSig.Key == typeString) {
                        if (typeSig.Value >= boundry) {
                            validFrames.Add((DayOfWeek.Tuesday, tueFrame.Key));
                        }
                    }
                }
            }
            // Wednesday
            foreach (KeyValuePair<string, Dictionary<string, double>> wedFrame in 
            ManagerValues.CurrentSchedulerContainer.Wednesday) {
                foreach (KeyValuePair<string, double> typeSig in wedFrame.Value) {
                    if (typeSig.Key == typeString) {
                        if (typeSig.Value >= boundry) {
                            validFrames.Add((DayOfWeek.Wednesday, wedFrame.Key));
                        }
                    }
                }
            }
            // Thursday
            foreach (KeyValuePair<string, Dictionary<string, double>> thurFrame in 
            ManagerValues.CurrentSchedulerContainer.Thursday) {
                foreach (KeyValuePair<string, double> typeSig in thurFrame.Value) {
                    if (typeSig.Key == typeString) {
                        if (typeSig.Value >= boundry) {
                            validFrames.Add((DayOfWeek.Thursday, thurFrame.Key));
                        }
                    }
                }
            }
            // Friday
            foreach (KeyValuePair<string, Dictionary<string, double>> friFrame in 
            ManagerValues.CurrentSchedulerContainer.Friday) {
                foreach (KeyValuePair<string, double> typeSig in friFrame.Value) {
                    if (typeSig.Key == typeString) {
                        if (typeSig.Value >= boundry) {
                            validFrames.Add((DayOfWeek.Friday, friFrame.Key));
                        }
                    }
                }
            }
            // Saturday
            foreach (KeyValuePair<string, Dictionary<string, double>> satFrame in 
            ManagerValues.CurrentSchedulerContainer.Saturday) {
                foreach (KeyValuePair<string, double> typeSig in satFrame.Value) {
                    if (typeSig.Key == typeString) {
                        if (typeSig.Value >= boundry) {
                            validFrames.Add((DayOfWeek.Saturday, satFrame.Key));
                        }
                    }
                }
            }
            // Sunday
            foreach (KeyValuePair<string, Dictionary<string, double>> sunFrame in 
            ManagerValues.CurrentSchedulerContainer.Sunday) {
                foreach (KeyValuePair<string, double> typeSig in sunFrame.Value) {
                    if (typeSig.Key == typeString) {
                        if (typeSig.Value >= boundry) {
                            validFrames.Add((DayOfWeek.Sunday, sunFrame.Key));
                        }
                    }
                }
            }
            // Determine potential event times.     
            List<DateTime> potentialTimes = new List<DateTime> { };
            foreach ((DayOfWeek, string) i in validFrames) {
                if (i.Item1 == startOfToday.DayOfWeek) {potentialTimes.Add(startOfToday.Add(Converters.SchedulingTime[i.Item2]));}
                else if (i.Item1 == startOfToday.AddDays(1).DayOfWeek) {potentialTimes.Add(startOfToday.AddDays(1).Add(Converters.SchedulingTime[i.Item2]));}
                else if (i.Item1 == startOfToday.AddDays(2).DayOfWeek) {potentialTimes.Add(startOfToday.AddDays(2).Add(Converters.SchedulingTime[i.Item2]));}
                else if (i.Item1 == startOfToday.AddDays(3).DayOfWeek) {potentialTimes.Add(startOfToday.AddDays(3).Add(Converters.SchedulingTime[i.Item2]));}
                else if (i.Item1 == startOfToday.AddDays(4).DayOfWeek) {potentialTimes.Add(startOfToday.AddDays(4).Add(Converters.SchedulingTime[i.Item2]));}
                else if (i.Item1 == startOfToday.AddDays(5).DayOfWeek) {potentialTimes.Add(startOfToday.AddDays(1).Add(Converters.SchedulingTime[i.Item2]));}
                else {potentialTimes.Add(startOfToday.AddDays(6).Add(Converters.SchedulingTime[i.Item2]));}
            }

            // Add potential time suggestions to the UI. 
                // Development Note -- Make a seperate method for this
            AutomaticSchedulerUI(potentialTimes);
        }
    }

    // Updates the scheduller when a new event is made.
    public void UpdateScheduler(string type, DateTime start) {

        if (Converters.eventName_TO_schedulerName.TryGetValue(type, out string? value)) { // Convert to right format. 
            type = value;
        } 

        string timeframe;
        TimeSpan tod = start.TimeOfDay;
        
        // Determine what part of the day the event lies in.
        if (tod >= new TimeSpan(0,0,0) && tod < new TimeSpan(6,0,0)) { timeframe = "VEMorning"; }
        else if (tod >= new TimeSpan(6,0,0) && tod < new TimeSpan(9,0,0)) { timeframe = "EMorning"; }
        else if (tod >= new TimeSpan(9,0,0) && tod < new TimeSpan(12,0,0)) { timeframe = "LMorning"; }
        else if (tod >= new TimeSpan(12,0,0) && tod < new TimeSpan(15,0,0)) { timeframe = "EAfternoon"; }
        else if (tod >= new TimeSpan(15,0,0) && tod < new TimeSpan(17,0,0)) { timeframe = "LAfternoon"; }
        else if (tod >= new TimeSpan(17,0,0) && tod < new TimeSpan(19,0,0)) { timeframe = "EEvening"; }
        else if (tod >= new TimeSpan(19,0,0) && tod < new TimeSpan(21,0,0)) { timeframe = "LEvening"; }
        else { timeframe = "Night"; }

        ManagerValues.CurrentSchedulerContainer.IncreaseSignificance(start.DayOfWeek.ToString(), timeframe, type, true);

        // Save new scheduler container to file.
        string newJson = JsonConvert.SerializeObject(ManagerValues.CurrentSchedulerContainer);
        File.WriteAllText(ManagerValues.CurrentCalendarPreferences.scheduler, newJson);
    }

    public void AutomaticSchedulerUI(List<DateTime> availible) {
        DockPanel suggestionsDockPanel = this.FindControl<DockPanel>("ECASSuggestions");
        TextBlock suggestionsText = this.FindControl<TextBlock>("ECASTextStatus");
        
        if (availible.Count != 0) {
            suggestionsText.Text = "Time suggestions are below:";
        }
        else {
            suggestionsText.Text = "The automatic scheduller has no suggestions";
        }

        suggestionsDockPanel.Children.Clear();
        for (int count = 0; count < availible.Count; count++ ) {
            suggestionsDockPanel.Children.Add(
                new EventCreateSchedulerSelector(availible[count]) { }
            );
        }
    }

    public void AutomaticSchedulerSet(DateTime time) {
        // Sets the current event time to the one chosen in the scheduller. 
        
        DatePicker startDate = this.FindControl<DatePicker>("ECDStartPicker");

        DatePicker endDate =  this.FindControl<DatePicker>("ECDEndPicker");
        TimePicker startTime = this.FindControl<TimePicker>("ECDStartPickerTime");
        TimePicker endTime = this.FindControl<TimePicker>("ECDEndPickerTime");

        TimeSpan currentTimeDif = (TimeSpan)endTime.SelectedTime - (TimeSpan)startTime.SelectedTime;

        startDate.SelectedDate = time.Date;
        startTime.SelectedTime = time.TimeOfDay;
        endDate.SelectedDate = time.Date;
        endTime.SelectedTime = time.TimeOfDay.Add(currentTimeDif);
    }
}

#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings
#pragma warning restore CS8604 // Re-enabling Warnings
#pragma warning restore CS8629 // Re-enabling Warnings