using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Calendar;
using Manager;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System;
using Microsoft.Data.Sqlite;

namespace project_main;

#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.
#pragma warning disable CS8604 // Compiler Warning Suppressed --  Possible null reference argument for parameter. 

public partial class GoalsPage : UserControl, IInitialisable {
    

    // Goals Bindings
    public string GoalDescText => "Set a study goal. Goals are long term targets you want to reach over a period of time. You can change this at any time.";
    public string GoalDefault => "Your goal will be displayed here";
    public string TypeText => "Goal Type: ";
    public string MagText => "Goal Magnitude (Hours): ";
    public string GoalButtonClearText => "Revert Changes";
    public string GoalButtonUpdateText => "Update Goal";

    // Goals Combo Box
    public ObservableCollection<string> goalsRanges = [
        "Daily",
        "Weekly",
        "4 Weeks"
    ];

    public IDictionary<string, int> goalsBoundries = 
    new Dictionary<string ,int> {
        {"daily", 24},
        {"weekly", 168},
        {"4weekly", 672},
    };


    // Loggings Bindings
    public string LogStudyHeadText => "Study from the past four weeks:";
    public string LogSurplusHeadText => "Log Surplus Study";
    public string LSATextBinding => "Hours of Surplus Study";
    public string LSAButtonText => "Log";

    public GoalsPage() {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);

        // Set defaults for goals combobox. 
        ComboBox box = this.FindControl<ComboBox>("GITypeInput");
        box.ItemsSource = goalsRanges;
        box.SelectedIndex = 0;

    }

    public void InitialisePage() {
        InitialiseGoals();
        InitialiseLogging();
    }

    // Goals Code
    
    public void InitialiseGoals() {
        RevertGoalInterface();
    }

    // Event handlers. 
    public void TriggerUpdateGoal(object sender, RoutedEventArgs args) {
        UpdateGoal();
    }

    public void TriggerRevertGoal(object sender, RoutedEventArgs args) {
        RevertGoalInterface();
    }

    public void UpdateGoal() {
        // Get required graphical elements. 
        Grid block = this.FindControl<Grid>("GoalMainBlock");
        
        ComboBox inputType = this.FindControl<ComboBox>("GITypeInput");

        TextBox inputMag = this.FindControl<TextBox>("GIMagnitudeInput");


        // Fetch from UI the chosen settings. 
        string type = Converters.goalName_TO_goalRange[(string)inputType.SelectedItem];
        bool isValid = double.TryParse(inputMag.Text, out double mag); // Checks that this is of a valid input format.

        int bound = goalsBoundries[type]; // Gets the boundry based on the type. 

        // Validation conditions, valid input and valid range. 
        if (!isValid) {
            NotifyError_Goals("Your magnitude must be of a numeric type.");
        }
        else if (mag > bound || mag < 0) {
            NotifyError_Goals("Your goal magnitude must be less than the total length and cannot be negative."); 
        }
        else {
            // Validated, so update the goal.

            // I make a dictionary to add the values to, setting the magnitude back to a string since the settings require that. 
            IDictionary<string, string> settingUpdate = 
            new Dictionary<string, string> {
                {"type", type},
                {"magnitude", mag.ToString()},
            };
            // Update the settings on CurrentCalendarPreferences. 
            ManagerValues.CurrentCalendarPreferences.goals = settingUpdate; 
            
            // Commit to JSON file. 
            string toJson = JsonConvert.SerializeObject(ManagerValues.CurrentCalendarPreferences);
            File.WriteAllText(ManagerValues.CurrentCalendarPreferences.thisPath, toJson);

            // Update page
            RevertGoalInterface(); // Resets interface. 
        }
    
        
    }

    public void RevertGoalInterface() {
        // Reverts the interface to the selected settings


        // Fetches required graphical elements. 

        TextBlock text = this.FindControl<TextBlock>("GoalCurrent");
        TextBlock errors =  this.FindControl<TextBlock>("GoalError");
        
        ComboBox inputType = this.FindControl<ComboBox>("GITypeInput");

        TextBox inputMag = this.FindControl<TextBox>("GIMagnitudeInput");
        
        // Gets the required details from settings. 
        string type = ManagerValues.CurrentCalendarPreferences.goals["type"];
        string magnitude = ManagerValues.CurrentCalendarPreferences.goals["magnitude"];

        // Reset error text.
        errors.Text = "";

        // Update boxes
        inputType.SelectedItem = Converters.goalRange_TO_goalName[type];
        inputMag.Text = magnitude;

        // Update text
        text.Text = $"Your current goal: {magnitude} Hours of Study / {Converters.goalRange_TO_goalName[type]}";
    }

    public void NotifyError_Goals(string error) { // Serves the same purpose as the other NotifyError methods, although for this page
    // I'll have a different one for goals and logging seperately. 
        this.FindControl<TextBlock>("GoalError").Text = error;
    }


    // Logging Code

    public void InitialiseLogging() {
        DockPanel eventsPanel = this.FindControl<DockPanel>("LogStudyEvents");
        TextBlock errorText = this.FindControl<TextBlock>("LogStudyErrorBlock");

     /*   List<GoalsPageStudyBlock> eventsPanelEvents = eventsPanel.Children.Where(x => x is GoalsPageStudyBlock)
                                                                          .Select(x => (GoalsPageStudyBlock)x)
                                                                          .ToList(); // Old code
    */
        // Past range based on constant value. 
        S_Range days = RangeLoader.ReturnSpecialRange();

        // Clear panel;
        eventsPanel.Children.Clear();
        eventsPanel.Children.Add(new GoalsPageHeaderBlock { });

        // Clear error box.
        errorText.Text = "";


        // If this isn't already on the panel and is a study event, add it to the panel. 
        foreach (Day day in days.fullRange) {
            foreach (Event event_ in day.events) {
                if ((event_.eventType == "eventStudy" || event_.eventType == "eventSurplus") && eventsPanel.Children.Where(x => x is GoalsPageStudyBlock)
                                         .Select(x => ((GoalsPageStudyBlock)x).linkedEvent)
                                         .Count(x => x.eventID == event_.eventID) == 0) {
                    eventsPanel.Children.Add(new GoalsPageStudyBlock(event_));
                }
            }
        }
    }

    public void LogSurplusClick(object sender, RoutedEventArgs args) {
        TextBox box = this.FindControl<TextBox>("LSABox");

        bool isValid = double.TryParse(box.Text, out double val);

        // Validate
        if (!isValid) {
            NotifyError_Logging("Surplus study must be of a numeric value in hours.");
        }
        else if (val <= 0) {
            NotifyError_Logging("Surplus study must be positive in length.");
        }
        else if (val > 1000) { // 1000 is a bit of an arbitrary value but it prevents an overflow from occuring by the user inputting an absurdly long  time. 
            NotifyError_Logging("Surplus study is too long.");
        }
        else {
            // This is valid, so you can continue. 
            DateTime start = DateTime.Now;
            DateTime end = start.Add(TimeSpan.FromHours(val));

            box.Text = "";

            using (SqliteConnection connection = new SqliteConnection($"Data Source={ManagerValues.CurrentCalendarPreferences.database}")) {
                connection.Open();
                // Get event's ID, covered days and update covered days before creating the new event. 
                SqliteCommand command_Count = connection.CreateCommand();
                command_Count.CommandText = 
                @$"
                    SELECT COUNT(*)
                    FROM table_events
                    WHERE StartDay = '{start:yyyy-MM-dd}';
                ";
                SqliteDataReader reader = command_Count.ExecuteReader();
                reader.Read();
                string str = reader.GetString(0); 
                // Generate ID and daysCovered. Notice the ID is slightly different for surplus events. 
                string id = "SURPLUS" + start.ToString("yyyyMMdd") + str;

                string daysCovered = string.Join(" ", RangeLoader.GetDatesInRange(start, end)
                                                                    .Select(x => x.ToString("yyyy-MM-dd")).ToList());
                string[] daysCoveredArr = daysCovered.Split(' ');

                // This code can be found where events otherwise are committed to the database and is used to ensure that the correct days are also present in the database.
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

                    reader2.Close();
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

                // Create event
                Event event_ = new Event(id, start, end, "Surplus Study", "eventSurplus", false, false, "", 1, 100);

                // Commit event. Code copied from other places where a new event is committed. 
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
            // Refresh logging page. 
            InitialiseLogging();
        }

    }

    // Deletes a surplus event from the panel
    public void RemoveSurplus(Control removing) {
        DockPanel eventsPanel = this.FindControl<DockPanel>("LogStudyEvents");
        eventsPanel.Children.Remove(removing);
    }
    

    // The notification method for the logging part of this page. 
    public void NotifyError_Logging(string text) {
        TextBlock errorText = this.FindControl<TextBlock>("LogStudyErrorBlock");
        errorText.Text = text;
    }
}   
#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings
#pragma warning restore CS8604 // Re-enabling Warnings
