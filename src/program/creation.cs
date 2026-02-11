using System;
using System.Collections.Generic;
using System.IO;
using Manager;
using Setup;
using Newtonsoft.Json;
using Microsoft.Data.Sqlite;

#pragma warning disable CS8602 // Compiler Warning Suppressed -- Dereference of a possibly null reference.

namespace Creation {

    public static class CalendarCreator {

        public static void InitialiseCalendar(string name) {
            // OBJECT
            ManagerValues.CurrentCalendarPreferences.calendarName = name;
            ManagerValues.CurrentCalendarPreferences.filename = $"Calendar_{name}.json";
            // DEBUG NOTE: CHANGE Path.Join to GetFullPath once you've finished using the debugger
            // After DEVELOPMENT is over.  // COMPLETED -- Changed these back 
            ManagerValues.CurrentCalendarPreferences.thisPath = Path.GetFullPath($@"userresources/Calendar_{name}.json");

            ManagerValues.CurrentCalendarPreferences.database = Path.GetFullPath(@$"userresources/{name}.db");
            ManagerValues.CurrentCalendarPreferences.scheduler = Path.GetFullPath(@$"userresources/SchedulerContainer_{name}.json");

            ManagerValues.CurrentCalendarPreferences.databaseInit = "false";
            ManagerValues.CurrentCalendarPreferences.dateInit = DateTime.Now.ToString("yyyy-MM-dd"); // Note the default date format. 

            ManagerValues.CurrentCalendarPreferences.streamlined = "false";

            ManagerValues.CurrentCalendarPreferences.preferenceColours = new Dictionary<string, string> {
                
                    // These colours are default and can be adjusted by the user. 
                {"schemePrimary", "#f4f8f7"},
                {"schemeSecondary", "#70a49d"},
                {"schemeText", "#192622"},
                {"schemeAccent", "#19fcd7"},
                {"schemeAlertTop", "#00de04"},
                {"schemeAlertMid", "#decf00"},
                {"schemeAlertLow", "#d62004"},
            };

            ManagerValues.CurrentCalendarPreferences.typeColours = new Dictionary<string, string> {
                {"eventStudy", "#f59342"},
                {"eventSchool", "#39cef7"},
                {"eventWork", "#8742f5"},
                {"eventFree", "#f542b9"},
            };

            ManagerValues.CurrentCalendarPreferences.goals = new Dictionary<string, string> {
                // Once again this is default and adjustable by the user. 
                {"type", "weekly"},
                {"magnitude", "0"},
            };

            // JSON
            string toJson = JsonConvert.SerializeObject(ManagerValues.CurrentCalendarPreferences);
            File.WriteAllText(ManagerValues.CurrentCalendarPreferences.thisPath, toJson);

            // Update Management
            if (ManagerValues.RootManager != null) {
                ManagerValues.RootManager.calendars.Add(ManagerValues.CurrentCalendarPreferences.calendarName, ManagerValues.CurrentCalendarPreferences.thisPath);
            }
            // Write
            string toJson2 = JsonConvert.SerializeObject(ManagerValues.RootManager);
            File.WriteAllText(SetupMain.managerPath, toJson2);
            
            // Note that this routine does not create the scheduler or the database. 
            // That is the role of LoadCalendar which does so for any calendar that is marked as not fully initialised.
        }

        public static void LoadCalendar(string name) {
            // Check if calendar is fully initialised.

            string? path = ManagerValues.RootManager.calendars[name];
            // Load into object.
            if (path != null) {
                string fromJson = File.ReadAllText(path);
                ManagerValues.CurrentCalendarPreferences = JsonConvert.DeserializeObject<Calendar_CalendarPreferences>(fromJson);

                ManagerValues.CurrentSchedulerContainer = new SchedulerContainer([], [], [], [], [], [], []);
                
                if (ManagerValues.CurrentCalendarPreferences != null && !ManagerValues.CurrentCalendarPreferences.ReturnBoolInit()) {
                    // Is the case when calendar is not fully initialised. 

                    // Initialise Scheduler File

                    // This code is just really long and slightly annoying but I've used for loops here just 
                    // To keep it shorter and more readable. It's still too long but would be shorter than adding each line by line.
                    // Once again a victim of the choice to use seperate variables rather than a dictionary for days. 
                    string[] frames = ["VEMorning", "EMorning", "LMorning", "EAfternoon", "LAfternoon", "EEvening", "LEvening", "Night"];
                    string[] defTypes = ["Study", "School", "Work", "Free"];
                    foreach (string frame in frames) {
                        ManagerValues.CurrentSchedulerContainer.Monday.Add(frame, new Dictionary<string, double> { });
                        foreach (string type in defTypes) {
                            ManagerValues.CurrentSchedulerContainer.Monday[frame].Add(type, 0);
                        }
                    }
                    foreach (string frame in frames) {
                        ManagerValues.CurrentSchedulerContainer.Tuesday.Add(frame, new Dictionary<string, double> { });
                        foreach (string type in defTypes) {
                            ManagerValues.CurrentSchedulerContainer.Tuesday[frame].Add(type, 0);
                        }
                    }
                    foreach (string frame in frames) {
                        ManagerValues.CurrentSchedulerContainer.Wednesday.Add(frame, new Dictionary<string, double> { });
                        foreach (string type in defTypes) {
                            ManagerValues.CurrentSchedulerContainer.Wednesday[frame].Add(type, 0);
                        }
                    }
                    foreach (string frame in frames) {
                        ManagerValues.CurrentSchedulerContainer.Thursday.Add(frame, new Dictionary<string, double> { });
                        foreach (string type in defTypes) {
                            ManagerValues.CurrentSchedulerContainer.Thursday[frame].Add(type, 0);
                        }
                    }
                    foreach (string frame in frames) {
                        ManagerValues.CurrentSchedulerContainer.Friday.Add(frame, new Dictionary<string, double> { });
                        foreach (string type in defTypes) {
                            ManagerValues.CurrentSchedulerContainer.Friday[frame].Add(type, 0);
                        }
                    }
                    foreach (string frame in frames) {
                        ManagerValues.CurrentSchedulerContainer.Saturday.Add(frame, new Dictionary<string, double> { });
                        foreach (string type in defTypes) {
                            ManagerValues.CurrentSchedulerContainer.Saturday[frame].Add(type, 0);
                        }
                    }
                    foreach (string frame in frames) {
                        ManagerValues.CurrentSchedulerContainer.Sunday.Add(frame, new Dictionary<string, double> { });
                        foreach (string type in defTypes) {
                            ManagerValues.CurrentSchedulerContainer.Sunday[frame].Add(type, 0);
                        }
                    }

                    // Json
                    string toJson = JsonConvert.SerializeObject(ManagerValues.CurrentSchedulerContainer);
                    File.WriteAllText(ManagerValues.CurrentCalendarPreferences.scheduler, toJson);

                    // Initialise Database file. 
                    //  File.Create(ManagerValues.CurrentCalendarPreferences.database);
                    // Setup database. 

                    using (var connection = new SqliteConnection($"Data Source={ManagerValues.CurrentCalendarPreferences.database}")) {
                        connection.Open();

                        SqliteCommand command = connection.CreateCommand();
                        command.CommandText =
                        @"
                            CREATE TABLE table_days (
                                FullDate TEXT NOT NULL,
                                StartTime TEXT NOT NULL,
                                EndTime TEXT NOT NULL, 
                                PRIMARY KEY (FullDate)
                            );
                        ";
                        command.ExecuteNonQuery(); // ExecuteNonQuery doesn't return any values. 

                        command.CommandText =
                        @"
                            CREATE TABLE table_events (
                                EventID TEXT NOT NULL,
                                EventTitle TEXT NOT NULL,
                                StartDay TEXT NOT NULL,
                                StartTime TEXT NOT NULL,
                                DaysCovered TEXT NOT NULL,
                                EndDay TEXT NOT NULL,
                                EndTime TEXT NOT NULL,
                                EventType TEXT NOT NULL,
                                EventDesc TEXT,
                                IsCompletable BOOLEAN NOT NULL,
                                IsComplete BOOLEAN NOT NULL,
                                StudyComplete REAL,
                                EventPriority INTEGER NOT NULL,
                                PRIMARY KEY (EventID),
                                FOREIGN KEY (StartDay) REFERENCES table_days(FullDate),
                                FOREIGN KEY (EndDay) REFERENCES table_days(FullDate)
                            );
                        ";
                        command.ExecuteNonQuery();

                        // I made the decision not to index any of these fields
                        // Even though originally in the design I was going to. 

                        connection.Close();
                    }
                    // Change init to true so this process is only done once. 
                    // Otherwise, all of this will be rewritten each time I load a calendar. 
                    ManagerValues.CurrentCalendarPreferences.databaseInit = "true";
                    // Edit and update preferences JSON
                    toJson = JsonConvert.SerializeObject(ManagerValues.CurrentCalendarPreferences);
                    File.WriteAllText(ManagerValues.CurrentCalendarPreferences.thisPath, toJson);
                }
                // Loads the calendar page now.
                fromJson = File.ReadAllText(ManagerValues.CurrentCalendarPreferences.scheduler);
                ManagerValues.CurrentSchedulerContainer = JsonConvert.DeserializeObject<SchedulerContainer>(fromJson);

                // Here I need to check if there's a temporary file for this calendar, and if there is I have to delete that file.
                string potentialTempPath = ManagerValues.CurrentCalendarPreferences.thisPath + ".TEMPFILE.json";
                bool tempExists = File.Exists(potentialTempPath);
                if (tempExists) {
                    // There is a temporary file which must be deleted that has for whatever reason not been deleted yet. 
                    File.Delete(potentialTempPath);
                }

                // At this point we just return back to the initialpage.axaml.cs routine and from there I'll switch the page
                // To the CalendarPage. 

            }
            else { // Once again this should never get thrown but will be useful for debug if it does. 
                throw new Exception("Null Error! Path of file loaded is somehow null!");
            }
        }

        
    }
}
#pragma warning restore CS8602 // Re-enabling Warnings
