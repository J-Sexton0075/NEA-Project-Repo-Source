using System;
using System.Collections.Generic;
using System.Linq;
using Manager;
using Microsoft.Data.Sqlite;

#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.

namespace Calendar {

    // These are the static classes containing the code for the calendar.

    public static class RangeLoader {
        // The goal of this is to get the range of days as neccasary. 

        public static D_Range ReturnDateRange(DR_Length length, DateTime start) {
            DateTime lowerBound = start.Date;
            DateTime upperBound = lowerBound.AddDays((int)length - 1);

            List<Day> daysInRange = GetDayObjectForDates(
                GetDatesInRange(lowerBound, upperBound)
            );

            return new D_Range(lowerBound, upperBound, length, daysInRange);
        }

        public static List<DateTime> GetDatesInRange(DateTime lb, DateTime ub) {
            // Returns a list of all dates in a given range. 
            return Enumerable.Range(0, 1 + ub.Subtract(lb).Days)
                             .Select(increment => lb.AddDays(increment))
                             .ToList();
        }

        public static List<Day> GetDayObjectForDates(List<DateTime> dates) {
            List<Day> objectDays = new List<Day> { };

            foreach (DateTime day in dates) {
                string dayString = day.Date.ToString("yyyy-MM-dd");
                int count = 0;
                DateTime startTime;
                DateTime endTime;

                // Query the database and check that a day exists with that date as a string.
                using (SqliteConnection connection = new SqliteConnection($"Data Source={ManagerValues.CurrentCalendarPreferences.database}")) {
                    connection.Open();
                    SqliteCommand command = connection.CreateCommand();
                    command.CommandText =
                    @"
                        SELECT COUNT(FullDate)
                        FROM table_days
                        WHERE FullDate = $fDate
                    ";
                    command.Parameters.AddWithValue("$fDate", dayString);

                    SqliteDataReader reader = command.ExecuteReader(); // ExecuteReader returns a structure called a DataReader. 
                    reader.Read(); // Advances to the next 'row' in the DataReader (there should only be one).
                    count = reader.GetInt32(0); // Gets the value in the first 'column' as an integer (once again, there should only be one).
                    connection.Close();
                }

                if (count != 0) { // The day exists exists in the database. We take data from the entry. 

                    // Query database again for data.
                    using (SqliteConnection connection = new SqliteConnection($"Data Source={ManagerValues.CurrentCalendarPreferences.database}")) {
                        connection.Open();
                        // Setup commands
                        SqliteCommand command_GetStart = connection.CreateCommand();
                        command_GetStart.CommandText =
                        @"
                            SELECT StartTime
                            FROM table_days
                            WHERE FullDate = $fDate;
                        ";
                        SqliteCommand command_GetEnd = connection.CreateCommand();
                        command_GetEnd.CommandText =
                        @"
                            SELECT EndTime
                            FROM table_days
                            WHERE FullDate = $fDate;
                        ";
                        // Insert parameters
                        command_GetStart.Parameters.AddWithValue("$fDate", dayString);
                        command_GetEnd.Parameters.AddWithValue("$fDate", dayString);
                        // Run commands and get values as TimeSpans to add to the day. 
                        SqliteDataReader reader_GetStart = command_GetStart.ExecuteReader();
                        SqliteDataReader reader_GetEnd = command_GetEnd.ExecuteReader();
                        reader_GetStart.Read(); reader_GetEnd.Read(); // Increment the 'row' 
                        startTime = day.Date.Add(TimeSpan.Parse(reader_GetStart.GetString(0)));
                        endTime = day.Date.Add(TimeSpan.Parse(reader_GetEnd.GetString(0)));

                        connection.Close();
                    }
                    List<Event> events = GetEventObjectsForDay(dayString);
                    objectDays.Add(new Day(dayString, startTime, endTime, events));
                }
                else { // There are no events set on that day.
                       // In this case, we calculate the start and end times and add a blank day to the list.
                    startTime = day.Date;
                    endTime = day.Date.AddDays(1).AddTicks(-1);
                    objectDays.Add(
                        new Day(dayString, startTime, endTime, new List<Event> { })
                    );
                }
            }
            return objectDays;
        }

        public static List<Event> GetEventObjectsForDay(string dayString) {
            List<Event> events = new List<Event> { };

            // Query to get the events
            using (SqliteConnection connection = new SqliteConnection($"Data Source={ManagerValues.CurrentCalendarPreferences.database}")) {
                connection.Open();

                SqliteCommand command = connection.CreateCommand();
                command.CommandText = // Note here use of a LIKE statement to compare against the list of possible days. 
                $@"
                    SELECT *
                    FROM table_events
                    WHERE DaysCovered LIKE '%{dayString}%';
                ";
                // command.Parameters.AddWithValue("$mString", dayString);
                SqliteDataReader reader = command.ExecuteReader();
                // Iterate through each row in the DataReader and add the details of each column to
                while (reader.Read()) { // SqliteDataReader.Read() returns a bool depending on whether there's still rows left. 

                    // Here we have unlabled indexes to access data from the datareader. Since these are unclear I'll detail the structure of the DB below. 

                    /*  Index | - | Field | Type
                        0 - EventID Text
                        1 - EventTitle Text
                        2 - StartDay Text
                        3 - StartTime Text
                        4 - DaysCovered Text
                        5 - EndDay Text
                        6 - EndTime Text
                        7 - EventType Text
                        8 - EventDesc Text
                        9 - IsCompletable Boolean
                        10 - IsComplete Boolean
                        11 - StudyComplete Real
                        12 - EventPriority Integer 
                    */
                    DateTime startTime = DateTime.Parse(reader.GetString(2) + "T" + reader.GetString(3));
                    DateTime endTime = DateTime.Parse(reader.GetString(5) + "T" + reader.GetString(6));

                    events.Add( // Refer to the table above for the meanings behind these magic numbers.
                        new Event(
                            reader.GetString(0),
                            startTime,
                            endTime,
                            reader.GetString(1),
                            reader.GetString(7),
                            reader.GetBoolean(9),
                            reader.GetBoolean(10),
                            reader.GetString(8),
                            reader.GetDouble(11),
                            reader.GetInt32(12)
                        )
                    );
                }
                connection.Close();
            }

            return events;
        }
        // The following methods are used for P_Ranges. 
        public static P_Range ReturnPastRange(PR_Length rangeDepth) {
            DateTime lowerBound = new DateTime { };
            DateTime upperBound = new DateTime { };

            switch (rangeDepth) {
                case PR_Length.pr_Week:
                    lowerBound = FirstDayOfCurrentWeek();
                    upperBound = LastDayOfCurrentWeek();
                    break;
                case PR_Length.pr_Fortnight:
                    lowerBound = FirstDayOfLastWeek();
                    upperBound = LastDayOfCurrentWeek();
                    break;
                case PR_Length.pr_Month:
                    lowerBound = FirstDayOfCurrentMonth();
                    upperBound = LastDayOfCurrentMonth();
                    break;
                case PR_Length.pr_ThreeMonths:
                    lowerBound = FirstDayOfPreviousMonth(2);
                    upperBound = LastDayOfCurrentMonth();
                    break;
                case PR_Length.pr_Year:
                    lowerBound = FirstDayOfCurrentYear();
                    upperBound = LastDayOfCurrentYear();
                    break;
                case PR_Length.pr_TwoYears:
                    lowerBound = FirstDayOfPreviousYear(1);
                    upperBound = LastDayOfCurrentYear();
                    break;
                case PR_Length.pr_Max:
                    lowerBound = GetFirstCalendarDay();
                    upperBound = DateTime.Now.Date;
                    break;
                default:
                    break;
            }
            List<Day> daysInRange = GetDayObjectForDates(GetDatesInRange(lowerBound, upperBound));
            return new P_Range(lowerBound, upperBound, rangeDepth, daysInRange);
        }

        public static DateTime FirstDayOfCurrentWeek() {
            DayOfWeek firstDay = System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            int difference = (7 + DateTime.Now.DayOfWeek - firstDay) % 7;
            return DateTime.Now.AddDays(-difference);
        }
        public static DateTime LastDayOfCurrentWeek() {
            return FirstDayOfCurrentWeek().AddDays(6);
        }
        public static DateTime FirstDayOfLastWeek() {
            return FirstDayOfCurrentWeek().AddDays(-7);
        }
        public static DateTime FirstDayOfCurrentMonth() {
            DateTime now = DateTime.Now;
            return new DateTime(now.Year, now.Month, 1);
        }
        public static DateTime LastDayOfCurrentMonth() {
            DateTime now = DateTime.Now;
            DateTime lastDay = new DateTime(now.Year, now.Month+1, 1);
            return lastDay.AddDays(-1);
        }
        public static DateTime FirstDayOfPreviousMonth(int offset) {
            DateTime month = FirstDayOfCurrentMonth();
            return month.AddMonths(-offset);
        }
        public static DateTime FirstDayOfCurrentYear() {
            DateTime now = DateTime.Now;
            return new DateTime(now.Year, 1, 1);
        }
        public static DateTime LastDayOfCurrentYear() {
            DateTime now = DateTime.Now;
            DateTime lastDay = new DateTime(now.Year+1, 1, 1);
            return lastDay.AddDays(-1);
        }
        public static DateTime FirstDayOfPreviousYear(int offset) {
            DateTime year = FirstDayOfCurrentYear();
            return year.AddYears(-offset);
        }
        public static DateTime GetFirstCalendarDay() {
            string stamp = ManagerValues.CurrentCalendarPreferences.dateInit;
            return DateTime.Parse(stamp);
        }

        // The following method is used for S_Ranges
        // It fetches a past number of days based on a constant value for its depth and is used in the goals/logging page.
        public static S_Range ReturnSpecialRange() {
            DateTime end = DateTime.Now;
            DateTime start = end.AddDays(-(Constants.S_RANGEDEPTH-1));

            List<Day> daysInRange = GetDayObjectForDates(GetDatesInRange(start, end));
            return new S_Range(start, end, Constants.S_RANGEDEPTH, daysInRange);
        }
    }
    public static class EventSorter {
        // Contains various methods used to sort events in days in certain ways. 

        public static List<Event> GetEventsOfHighestPriorityInDay(Day day, int number, string? filter) {
            // Will get up to (number) events from a provided day. If there are more than (number) events it picks
            // Events of the highest priority. If there is a filter provided, it'll also only providing events matching
            // The filter. 
            List<Event> highestPriority = new List<Event> { };
            foreach (Event event_ in day.events) {
                if (highestPriority.Count < number) {
                    if ((filter == null || filter == event_.eventType) && event_.eventType != "eventSurplus") {
                        highestPriority.Add(event_);
                    }
                }
                else {
                    int lowestViableIndex = 0;
                    for (int i = 0; i < highestPriority.Count; i++) {
                        if (event_.eventPriority > highestPriority[i].eventPriority) {
                            if (highestPriority[i].eventPriority < highestPriority[lowestViableIndex].eventPriority) {
                                lowestViableIndex = i;
                            }
                        }
                    }
                    if (filter == null || filter == event_.eventType && event_.eventType != "eventSurplus") {
                        highestPriority[lowestViableIndex] = event_;
                    }
                }
            }
            return highestPriority;
        }

        public static List<Event> SortByPriority(List<Event> list) {
            // Orders a provided list of events from the highest to the lowest priority using LINQ. 
            return list.OrderByDescending(event_ => event_.eventPriority).ToList();
        }

        public static List<Event> SortByChronology(List<Event> list, Day day) {
            // Orders a provided list of events based on the order which they occur in within a provided day. 
            // If an event begins before the provided day, it is added to the start of the list. 
            // Once again similar to SortByPriority, this uses LINQ to sort from soonest to latest event with those events
            // Which occur on the day. 
            List<Event> sorted = new List<Event> { };
            List<Event> secondary = new List<Event> { };
            foreach (Event event_ in list) {
                if (event_.startTime.Date != day.startTime.Date) {
                    sorted.Add(event_);
                }
                else {
                    secondary.Add(event_);
                }
            }
            secondary = secondary.OrderBy(ev => ev.startTime.TimeOfDay).ToList();
            sorted.AddRange(secondary);
            return sorted;
        }

        public static List<Event> GetEventInChunk(DateTime lowerBound, DateTime upperBound, List<Event> events, string? filter) {
            // Gets the events from a given list that are in between two bounds.
            // Will also if a filte ris present check against the filter and provide only events that match with the filter. 

            List<Event> viable = new List<Event> { };
            foreach (Event _event in events) {
                /* 
                    All these conditions check filter first (either no filter, or a valid matching filter returns true)
                    An event is viable if:
                        - It begins before and ends after the provided bounds.
                        - It begins within the two bounds. 
                        - It ends within the two bounds. 

                */                      // As of adding surplus events I have also introduced a condition on this if statement block to ensure that surplus events aren't added 
                if ((filter == null || filter == _event.eventType) && _event.startTime < lowerBound && _event.endTime > upperBound && _event.eventType != "eventSurplus") {
                    viable.Add(_event);
                }
                else if ((filter == null || filter == _event.eventType) && _event.endTime >= lowerBound && _event.endTime <= upperBound && _event.eventType != "eventSurplus") {
                    viable.Add(_event);
                }
                else if ((filter == null || filter == _event.eventType) && _event.startTime >= lowerBound && _event.startTime <= upperBound && _event.eventType != "eventSurplus") {
                    viable.Add(_event);
                }
            }
            return viable;
        }

        public static DayChunk ParseChunk(List<Event> potentialEvents) {
            // Converts a list of potential events into a single part of the day. 
            // The main event in the 'chunk' of the day is the event in the list of the highest priority.
            // Any other events are counted as overlapped events and are also detialed. 
            // If there are no events, the 'chunk' is considered blank. 
            if (potentialEvents.Count == 0) {
                return new DayChunk(true);
            }
            else if (potentialEvents.Count == 1) {
                return new DayChunk(
                    false,
                    potentialEvents[0].eventType,
                    potentialEvents[0].eventTitle,
                    potentialEvents[0].startTime,
                    potentialEvents[0].endTime,
                    potentialEvents[0],
                    false,
                    new List<Event> { }
                );
            }
            else {
                List<Event> overlapped = new List<Event> { };
                Event highestPriority = new Event("", new DateTime(), new DateTime(), "", "", false, false, "", 0, -1);
                foreach (Event event_ in potentialEvents) {
                    if (event_.eventPriority > highestPriority.eventPriority) {
                        if (highestPriority.eventID != "") {
                            overlapped.Add(highestPriority);
                        }
                        highestPriority = event_;
                    }
                    else {
                        overlapped.Add(event_);
                    }
                }
                return new DayChunk(
                    false,
                    highestPriority.eventType,
                    highestPriority.eventTitle,
                    highestPriority.startTime,
                    highestPriority.endTime,
                    highestPriority,
                    true,
                    overlapped
                );
            }
        }

        public static string DimColours(string original) {
            // Decreases saturation of a provided hex code string by a provided value. This is done by moving the colour RGB values
            // By the constant value towards 128,128,128. 

            // Validate that this is not null -- which can occur if custom types are involved:
            if (original == "") {
                original = ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAccent"];
            }

            // Convert to integers. 
            // Remember to cut out the # prepend with the ranges.  
            int red = int.Parse(original[1..3], System.Globalization.NumberStyles.AllowHexSpecifier);
            int green = int.Parse(original[3..5], System.Globalization.NumberStyles.AllowHexSpecifier);
            int blue = int.Parse(original[5..], System.Globalization.NumberStyles.AllowHexSpecifier);

            // The ternary statements ensure the value does not drop below zero with this subtraction. 
            red = (red - Constants.DIMMINGVALUE > 128) ? red - Constants.DIMMINGVALUE : red + Constants.DIMMINGVALUE;
            green = (green - Constants.DIMMINGVALUE > 128) ? green - Constants.DIMMINGVALUE : green + Constants.DIMMINGVALUE;
            blue = (blue - Constants.DIMMINGVALUE > 128) ? blue - Constants.DIMMINGVALUE : blue + Constants.DIMMINGVALUE;

            // Convert back to hex string.
            return "#" + red.ToString("x2") + green.ToString("x2") + blue.ToString("x2");
        }
    }


    // These are the instance classes detailing the types for the calendar.
    public class Day {
        // Represents a day in time that can be loaded and displayed on the calendar. 
        // Days can contain any number of events.

        public string dateString;
        public DateTime startTime;
        public DateTime endTime;
        public List<Event> events;

        public Day(string ds, DateTime st, DateTime en, List<Event> ev) {
            dateString = ds;
            startTime = st;
            endTime = en;
            events = ev;
        }
    }

    public class Event : ICloneable {
        // Represents an event in time. These start at a certain time on a day, and end at another certain time on a day. 
        // Events can have details provided by a user, including type, title, description and priority. They also have certain properties
        // Such as being able to be marked as 'completed'. 

        public string eventID;
        public DateTime startTime;
        public DateTime endTime;
        public string eventTitle;
        public string eventType;

        public bool eventIsStudy;
        public bool eventIsCompleteable;
        public bool eventIsCompleted;

        public string eventDesc;
        public double studyCompleted;
        public int eventPriority;

        public Event(string id, DateTime st, DateTime ed, string tt, string ty, bool cm, bool cmp, string de, double sc, int pr) {
            eventID = id;
            startTime = st;
            endTime = ed;
            eventTitle = tt;
            eventType = ty;
            eventIsStudy = ty == "eventStudy"; // True if event type is study
            eventIsCompleteable = cm;
            eventIsCompleted = cmp;
            eventDesc = de;
            studyCompleted = sc;
            eventPriority = pr;
        }

        // Methods to return timestamp strings to be displayed on the graphical section of the calendar. 
        public string ReturnStamp() {
            return startTime.ToString("dddd dd HH:mm");
        }
        public string ReturnFullStamp() {
            return startTime.ToString("dddd dd HH:mm") + "\nTo" + " " + endTime.ToString("dddd dd HH:mm");
        }
        public string ReturnDetailedStamp() {
            return startTime.ToString("dddd dd HH:mm") + " To " + endTime.ToString("dddd dd HH:mm");
        }
        public object Clone() {
            return this.MemberwiseClone();
        }


    }

    // Ranges are collections of days over a certain period of time. 

    // There are three types of ranges, D_Range, P_Range and S_Range (Date, Past, Special).

    // S_Range is like an 'inverted' D_Range that takes a point X as an end at X - y where is the numerical length.

    // This is an amendment from design, the three ranges are extremely similar so I've decided to use inheritance here. 
    public abstract class Range {
        // Default abstract idea of a range. 
        public DateTime rangeStart;
        public DateTime rangeEnd;
        public List<Day>? fullRange;
    }

    public class D_Range : Range // A date range is a range between two points. It has a specific length which is constant
    // One value of type DR_Length, which is an enum. 
    {
        public DR_Length rangeLength;

        public D_Range(DateTime s, DateTime e, DR_Length r, List<Day> f) {
            rangeStart = s;
            rangeEnd = e;
            rangeLength = r;

            fullRange = f;
        }
    }

    public class P_Range : Range {
        // The past range covers a specific portion of the past, the contents of this range aren't of a fixed length but are rather
        // Calculated adaptively based on the idea that certain portions of the past (e.g. a month) may vary in length from one month to the next. 
        public PR_Length rangeLength;

        public P_Range(DateTime s, DateTime e, PR_Length r, List<Day> f) {
            rangeStart = s;
            rangeEnd = e;
            rangeLength = r;

            fullRange = f;
        }
    }

    public class S_Range : Range {
        // The special range. Almost like an inverted D_Range. When being calcualted will take a specific end date
        // Then find the start date based on a numerical depth, that start being the depth days before the end date. 
        public int rangeDepth;

        public S_Range(DateTime s, DateTime e, int r, List<Day> f) {
            rangeStart = s;
            rangeEnd = e;
            rangeDepth = r;

            fullRange = f;
        }
    }


    // Used for repeated events. 
    // Describes the repetition of an event. 
    public struct RepeatedBlock {
        public DateTime rangeStart;
        public DateTime rangeEnd;
        public string rCode;
        public int rInterval;
        public List<(DateTime, DateTime)> exclusions;

        public RepeatedBlock(DateTime s, DateTime e, string c, int i, List<(DateTime, DateTime)> x) {
            rangeStart = s;
            rangeEnd = e;
            rCode = c;
            rInterval = i;
            exclusions = x;
        }
    }

    // Used to describe part of the detailed day. 
    public struct DayChunk {
        public bool blank;

        public string? type;
        public string? title;

        public DateTime? chunkContainerStart;
        public DateTime? chunkContainerEnd;

        public Event? linkedEvent;

        public bool? isOverlap;
        public List<Event>? overlappedEvents;

        public DayChunk(bool bl) // Constructor 1. For events which are blank. 
        {
            blank = bl;
        }

        public DayChunk( // Constructor 2. For events which are not blank. 
            bool bl,
            string ty,
            string ti,
            DateTime ccStart,
            DateTime ccEnd,
            Event link,
            bool isOv,
            List<Event> ov
        ) {
            blank = bl;
            type = ty;
            title = ti;
            chunkContainerStart = ccStart;
            chunkContainerEnd = ccEnd;
            linkedEvent = link;
            isOverlap = isOv;
            overlappedEvents = ov;
        }
    }



    // These are the enum structures required by the calendar as planned.

    // Describes the potential length of 'date ranges' 
    // Date ranges are the type of ranges that start at a point X and end at X + y where y is the numerical value of the 
    // Length of the range. 
    public enum DR_Length {
        day_1 = 1,
        day_2 = 2,
        day_5 = 5,
        week_1 = 7,
        week_2 = 14,
        week_4 = 28
    }
    /* Describes the potential length of 'past ranges'
    Past ranges are the type of ranges that go between two points defined by the end point and the length. */
    public enum PR_Length {
        pr_Week,
        pr_Fortnight,
        pr_Month,
        pr_ThreeMonths,
        pr_Year,
        pr_TwoYears,
        pr_Max,
    }

    // Describes the different options for comparison sets within the Overview & Insights page
    public enum ComparisonSet {
        Goals_Complete,
        Goals_Plans,
        Plans_Complete,
        Completable,
        Insight
    }

    // Describes specific rules about the layout of the calendar. 
    // Each rule will have certain properties associated with it when the calendar is being loaded. 
    public enum CalendarResolutionRule {
        CRR_Top4_T,
        CRR_Top5_TD,
        CRR_Top10_TD,
        CRR_HalfDetail,
        CRR_FullDetail
    }

    public static class Converters { // Since a namespace cannot directly contain non-types
                                     // I'm putting any dictionaries used to convert between two values in this static class. 

        public static IDictionary<DR_Length, string> length_TO_string =
        new Dictionary<DR_Length, string> {
            {DR_Length.day_1, "One Day"},
            {DR_Length.day_2, "Two Days"},
            {DR_Length.day_5, "Five Days"},
            {DR_Length.week_1, "One Week"},
            {DR_Length.week_2, "Two Weeks"},
            {DR_Length.week_4, "Four Weeks"}
        };


        // Decided to have two for both ways as theoretically it's better this way with a hash table implementation
        // Which I think C# uses. 
        public static IDictionary<string, DR_Length> string_TO_length =
        new Dictionary<string, DR_Length> {
            {"One Day", DR_Length.day_1},
            {"Two Days", DR_Length.day_2},
            {"Five Days", DR_Length.day_5},
            {"One Week", DR_Length.week_1},
            {"Two Weeks", DR_Length.week_2},
            {"Four Weeks", DR_Length.week_4}
        };

        
        // Used to convert between the 'user friendly' strings on the calendar page with the actual functional strings used to determine event types. 
        public static IDictionary<string, string?> filterName_TO_eventName =
        new Dictionary<string, string?> {
            {"Filter Event: No Filter", null},
            {"Filter Event: Study", "eventStudy" },
            {"Filter Event: Work", "eventWork"},
            {"Filter Event: School/Homework", "eventSchool"},
            {"Filter Event: Free Time", "eventFree"}
        };

        // Used for the same purpose, but with a different combo box!
        public static IDictionary<string, string?> typeName_TO_eventName =
        new Dictionary<string, string?> {
            {"Event Type: Study", "eventStudy"},
            {"Event Type: Work", "eventWork"},
            {"Event Type: School/Homework", "eventSchool"},
            {"Event Type: Free Time", "eventFree"},
        };

        public static IDictionary<string, string> eventName_TO_typeName = 
        new Dictionary<string, string> {
            {"eventStudy", "Event Type: Study"},
            {"eventWork", "Event Type: Work"},
            {"eventSchool", "Event Type: School/Homework"},
            {"eventFree", "Event Type: Free Time"}
        };


        // Used for the same purpose, but when I'm using the automatic scheduler. 
        public static IDictionary<string, string?> typeName_TO_schedulerName =
        new Dictionary<string, string?> {
            {"Event Type: Study", "Study"},
            {"Event Type: Work", "Work"},
            {"Event Type: School/Homework", "School"},
            {"Event Type: Free Time", "Free"},
        };

        public static IDictionary<string, string> eventName_TO_schedulerName = 
        new Dictionary<string, string> {
            {"eventStudy", "Study"},
            {"eventWork", "Work"},
            {"eventSchool", "School"},
            {"eventFree", "Free"}
        };


        // Used to convert between the functional string to a string that can be displayed on the EventView page. 
        public static IDictionary<string, string> eventName_TO_eventTypeString = 
        new Dictionary<string, string> {
            {"eventStudy", "Study"},
            {"eventWork", "Work"},
            {"eventSchool", "School/Homework"},
            {"eventFree", "Free Time"}
        };

        // Used for the Overview & Insights page to convert between the strings used in the ComboBox and the Enum values for PR_Length
        public static IDictionary<string, PR_Length> rangeName_TO_rangeLength = 
        new Dictionary<string, PR_Length> {
          {"Week", PR_Length.pr_Week},
          {"Fortnight", PR_Length.pr_Fortnight},
          {"Month", PR_Length.pr_Month},
          {"Three Months", PR_Length.pr_ThreeMonths},
          {"Year", PR_Length.pr_Year},
          {"Two Years", PR_Length.pr_TwoYears},
          {"Max", PR_Length.pr_Max},   
        };

        // Used for the same purpose but instead to convert to the ComparisonSet Enum
        public static IDictionary<string, ComparisonSet> compTypeName_TO_comparisonSet = 
        new Dictionary<string, ComparisonSet> {
            {"Goals-Complete", ComparisonSet.Goals_Complete},
            {"Goals-Plans", ComparisonSet.Goals_Plans},
            {"Plans-Complete", ComparisonSet.Plans_Complete},
            {"Completable", ComparisonSet.Completable},
            {"Time Insight", ComparisonSet.Insight}
        };

        // Used to convert the length of a goal from a string to a number in the Overview & Insights page
        public static IDictionary<string, int> compString_TO_dateLength = 
        new Dictionary<string, int> {
            {"daily", 1},
            {"weekly", 7},
            {"4weekly", 28},
        };

        // Used in the Goals & Logging page for various conversion purposes.

        public static IDictionary<string, string> goalName_TO_goalRange = 
        new Dictionary<string, string> {
            {"Daily", "daily"},
            {"Weekly", "weekly"},
            {"4 Weeks", "4weekly"},
        };
        public static IDictionary<string, string> goalRange_TO_goalName = 
        new Dictionary<string, string> {
            {"daily", "Daily"},
            {"weekly", "Weekly"},
            {"4weekly", "4 Weeks"},
        };

        // Maps DR_Lengths to specific sets of calendar resolution rules (dimensions and rule enum value). 
        public static IDictionary<DR_Length, ((int, int), CalendarResolutionRule)> CalendarResolutionTable =
        new Dictionary<DR_Length, ((int, int), CalendarResolutionRule)> {
            {DR_Length.day_1, ((1,1), CalendarResolutionRule.CRR_FullDetail)},
            {DR_Length.day_2, ((1,2), CalendarResolutionRule.CRR_FullDetail)},
            {DR_Length.day_5, ((1,5), CalendarResolutionRule.CRR_HalfDetail)},
            {DR_Length.week_1, ((1,7), CalendarResolutionRule.CRR_Top10_TD)},
            {DR_Length.week_2, ((2,7), CalendarResolutionRule.CRR_Top5_TD)},
            {DR_Length.week_4, ((4,7), CalendarResolutionRule.CRR_Top4_T)}
        };

        public static IDictionary<CalendarResolutionRule, string> rule_TO_gridSetup =
        new Dictionary<CalendarResolutionRule, string> {
            // Here I describe the 'RowDefinitions' of days in the top_n format. 
            // These are used to set up a grid to display events in. 
            // The constant here is just the height of the grid space for each event. 
            {CalendarResolutionRule.CRR_Top4_T, $"{Constants.CALENDAREVENTHEIGHT},{Constants.CALENDAREVENTHEIGHT},{Constants.CALENDAREVENTHEIGHT},{Constants.CALENDAREVENTHEIGHT}"},
            {CalendarResolutionRule.CRR_Top5_TD, $"{Constants.CALENDAREVENTHEIGHT},{Constants.CALENDAREVENTHEIGHT},{Constants.CALENDAREVENTHEIGHT},{Constants.CALENDAREVENTHEIGHT},{Constants.CALENDAREVENTHEIGHT}"},
            {CalendarResolutionRule.CRR_Top10_TD, $"{Constants.CALENDAREVENTHEIGHT},{Constants.CALENDAREVENTHEIGHT},{Constants.CALENDAREVENTHEIGHT},{Constants.CALENDAREVENTHEIGHT},{Constants.CALENDAREVENTHEIGHT},{Constants.CALENDAREVENTHEIGHT},{Constants.CALENDAREVENTHEIGHT},{Constants.CALENDAREVENTHEIGHT},{Constants.CALENDAREVENTHEIGHT},{Constants.CALENDAREVENTHEIGHT}"}
        };

        // Used to convert between a provided frame of the day and a time value for that time of day.
        public static IDictionary<string, TimeSpan> SchedulingTime = 
        new Dictionary<string, TimeSpan> {
            {"VEMorning", new TimeSpan(4,0,0)},
            {"EMorning", new TimeSpan(8,0,0)},
            {"LMorning", new TimeSpan(11,0,0)},
            {"EAfternoon", new TimeSpan(13,0,0)},
            {"LAfternoon", new TimeSpan(16,0,0)},
            {"EEvening", new TimeSpan(18,0,0)},
            {"LEvening", new TimeSpan(20,0,0)},
            {"Night", new TimeSpan(22,0,0)}
        };

    }

    public static class Constants { // This class contains various other constants which are required in the calendar
                                    // Comments are used to describe their purpose.

        // Describes how long an event's title can be before being truncated on on an event button.
        // This only applies to events ordered in the 'top_n' format, not with events in the 'detailed' format. 
        public const int CALENDAREVENTTITLELIMIT = 15;
        // Describes the height of the grid spaces for events in the top_n format. 
        public const string CALENDAREVENTHEIGHT = "20";

        public const int DIMMINGVALUE = 25; // The value to decrease saturation by within the DimColours() method. 

        // The value that is the 'depth' of the special range (how many days are fetched in goals/logging)
        public const int S_RANGEDEPTH = 28;
    }
}
#pragma warning restore CS8602 // Re-enabling Warnings
