using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Manager;
using Calendar;
using System.Data;
using Microsoft.Data.Sqlite;
using System.Linq;

namespace project_main;

#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.
#pragma warning disable CS8604 // Compiler Warning Suppressed --  Possible null reference argument for parameter. 
#pragma warning disable CS8629 // Compiler Warning Suppressed -- Nullable value type may be null.

public partial class EventEdit : UserControl {
    // Naturally as a whole this is very similar to the event create page.
    // Most of this is copied over, very little is actually unique to the page, I have just edited values to make it fit
    // The purpose of editing more. 

    public Event? linkedEvent; 

    private bool isInitialised; 

    public string ECTitleTextBinding => "Edit Title: ";
    public string ECTitleWatermark => "Give your event a title!";
    public string ECDetailsTextBinding => "Edit Description: ";
    public string ECDetailsWatermark => "Lorem Ipsum. Describe your event!";

    public string ECDStartTextBinding => "Edit Start Date and Time";
    public string ECDEndTextBinding => "Edit End Date and Time";
    public string ECDTypeTextBinding => "Edit Event Type";
    public string ECDCompletableTextBinding => "Event Is Completable?";
    public string ECDPriorityTextBinding => "Edit Priority";

    public string CloseButtonTextBinding => "Close";
    public string CreateButtonTextBinding => "Update Event";

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

    public EventEdit() {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);
        isInitialised = false;
    }

    public void InitialisePage(Event linked) {

        isInitialised = false;

        // Get elements then set their value to the linked event's value. 
        /*
            Title Box
            Description Box
            Start Time and Date
            End Time and Date
            Completable
            Priority
            Type
        */

        linkedEvent = linked;

        TextBox title = this.FindControl<TextBox>("ECTitleInput");

        Grid detailsSidebar = this.FindControl<Grid>("ECDetailsSidebar");

        TextBlock  alertBox = this.FindControl<TextBlock>("ECAlertText");

        TextBox description = this.FindControl<TextBox>("ECDescriptionInput");

        DatePicker startDate = this.FindControl<DatePicker>("ECDStartPicker");
        TimePicker startTime = this.FindControl<TimePicker>("ECDStartPickerTime");

        DatePicker endDate = this.FindControl<DatePicker>("ECDEndPicker");
        TimePicker endTime = this.FindControl<TimePicker>("ECDEndPickerTime");

        CheckBox completable = this.FindControl<CheckBox>("ECDCompletableBox");

        TextBox priority = this.FindControl<TextBox>("ECDPriorityBox");

        ComboBox types = this.FindControl<ComboBox>("ECDTypeComboBox");

        

        // Clear alert box 
        alertBox.Text = "";

        title.Text = linkedEvent.eventTitle;
        description.Text = linkedEvent.eventDesc;
        startDate.SelectedDate = linkedEvent.startTime.Date;
        endDate.SelectedDate = linkedEvent.endTime.Date;
        startTime.SelectedTime = linkedEvent.startTime.TimeOfDay;
        endTime.SelectedTime = linkedEvent.endTime.TimeOfDay; 

        completable.IsChecked = linkedEvent.eventIsCompleteable;
        priority.Text = linkedEvent.eventPriority.ToString();
        types.ItemsSource = null;

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
        Converters.eventName_TO_typeName.TryGetValue(linkedEvent.eventType, out string typeString);
        typeString ??= linkedEvent.eventType;
        types.SelectedItem = typeString;
        if (types.SelectedItem == null) {
            types.SelectedIndex = 3; // Set this to Free Time to prevent other errors from occuring.
        }

        isInitialised = true;

    }

    public void ClosePage(object sender, RoutedEventArgs args) {
        isInitialised = false;
        (Parent.Parent.Parent as MainWindow).DisableEditEvent();
    }
    
    public void UpdateEvent(object sender, RoutedEventArgs args) {
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
        
        string oldType = (string)this.FindControl<ComboBox>("ECDTypeComboBox")
                                  .SelectedItem;
        oldType ??= "eventFree"; // Defaults if a null value occurs, which it shouldn't. 
        Converters.typeName_TO_eventName.TryGetValue(oldType, out string type); // Converts to the correct format of string.
        type ??= oldType;

        bool IsCompletable = (bool)this.FindControl<CheckBox>("ECDCompletableBox").IsChecked;
        
        string title = this.FindControl<TextBox>("ECTitleInput").Text;

        string desc = this.FindControl<TextBox>("ECDescriptionInput").Text;
        

        // Validation work for priority and repeated events

        bool isValidPrior = int.TryParse(this.FindControl<TextBox>("ECDPriorityBox").Text, out int priority);
        
        // Validate input values. 
        if (!isValidPrior) {
            NotifyError("Priority must be an integer!");
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
            Event event_ = new Event(
                linkedEvent.eventID, start, end, title, type, IsCompletable, false, desc, 0, priority
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

                // Parameters Method
                command_Commit.CommandText = 
                @"
                    UPDATE table_events
                    SET
                        EventTitle = @eventTitle,
                        StartDay = @eventStartDate,
                        StartTime = @eventStartTime,
                        DaysCovered = @daysCovered,
                        EndDay = @eventEndDate,
                        EndTime = @eventEndTime,
                        EventType = @eventType,
                        EventDesc = @eventDesc,
                        IsCompletable = @eventIsCompletable,
                        EventPriority = @eventPriority
                    WHERE EventID = @eventID;
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
                command_Commit.Parameters.AddWithValue("@eventPriority", event_.eventPriority);

                command_Commit.ExecuteNonQuery();

                connection.Close();
            }
            
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

    public void TriggerCheckIfStudy(object sender, RoutedEventArgs args) {
        // Serves the same purpose as in the EventCreate page to toggle the checkbox depending on whether the event is a study event or not. 
        if (isInitialised) {
            string typeString = this.FindControl<ComboBox>("ECDTypeComboBox").SelectedItem.ToString();

            UpdateEventCompletableBox(typeString != "Event Type: Study");
        }   
    }
       
    public void UpdateEventCompletableBox(bool onoff) { // Special method that hides or shows the EventCompletable box. 
        
        DockPanel box = this.FindControl<DockPanel>("ECDetailsCompletable");
        box.IsVisible = onoff;
    }
}
#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings
#pragma warning restore CS8604 // Re-enabling Warnings
#pragma warning restore CS8629 // Re-enabling Warnings