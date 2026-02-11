using System;
using System.Collections.Generic;
using MathNet.Numerics.Statistics;

namespace Manager {
    public static class ManagerValues {
        // Stores our current management objects.

        // These sort of serve as global variables that I'll repeatedly have to access
        // Since they contain all the 'general information' about the calendar that's being ran.
        public static CalendarManager? RootManager = new CalendarManager("", new Dictionary<string, string> { });
        public static Calendar_CalendarPreferences? CurrentCalendarPreferences = new Calendar_CalendarPreferences(
            "", "", "", "", "", "", new Dictionary<string, string> { }, new Dictionary<string, string> { },
            [], new Dictionary<string, string> { }, "", ""
        );
        public static Calendar_CalendarPreferences? temporaryCalendarPreferences = new Calendar_CalendarPreferences(
            "", "", "", "", "", "", new Dictionary<string, string> { }, new Dictionary<string, string> { },
            [], new Dictionary<string, string> { }, "", ""
        );
        public static bool usingTemporary = false;
        public static string temporaryPath = "";

        public static SchedulerContainer? CurrentSchedulerContainer = new SchedulerContainer(
            [], [], [], [], [], [], []
        );

        public static string CurrentPage = "";

    }


    // Management object blueprints.
    // Most of this is just class definitions, attributes and constructors
    // There are a few methods to explain as well. 
    public class CalendarManager {
        public string filename; // Has to be public for JSON.NET to work. 
        public IDictionary<string, string> calendars;


        public CalendarManager(string f, IDictionary<string, string> c) {
            filename = f;
            calendars = c;
        }
    }

    public class Calendar_CalendarPreferences {
        public string filename;
        public string calendarName;
        public string thisPath;

        public string database;
        public string databaseInit;

        public string scheduler;

        public string dateInit;

        public IDictionary<string, string> preferenceColours;
        public IDictionary<string, string> typeColours;
        public List<IDictionary<string, string>> customTypes;
        public IDictionary<string, string> goals;

        public string streamlined;

        public Calendar_CalendarPreferences(
            string fn, string cn, string path, string db, string dbi, string di,
            IDictionary<string, string> pc, IDictionary<string, string> tc,
            List<IDictionary<string, string>> ct, IDictionary<string, string> gls,
            string st, string sch
        ) {
            filename = fn; calendarName = cn;
            thisPath = path; database = db;
            databaseInit = dbi; dateInit = di;
            preferenceColours = pc; typeColours = tc;
            customTypes = ct; goals = gls;
            streamlined = st; scheduler = sch;
        }

        public bool ReturnBoolInit() {
            return databaseInit == "true";
        }
        public bool ReturnBoolStreamlined() {
            return streamlined == "True";
        }

    }

    public class SchedulerContainer {
        // Days
        public Dictionary<string, Dictionary<string, double>> Monday;
        public Dictionary<string, Dictionary<string, double>> Tuesday;
        public Dictionary<string, Dictionary<string, double>> Wednesday;
        public Dictionary<string, Dictionary<string, double>> Thursday;
        public Dictionary<string, Dictionary<string, double>> Friday;
        public Dictionary<string, Dictionary<string, double>> Saturday;
        public Dictionary<string, Dictionary<string, double>> Sunday;

        // Constants
            // These values should be adjusted with testing.
        public const double significanceIncreaseValue = 0.5;
        public const double significanceLimit = 0.1;

        // Methods
        public double ReturnSignificanceBoundry(string eventType) {
            List<double> boundries = new List<double> { };
            /* Here I could have used a dictionary for each day (then a dictionary for the frames, and for the types)
             But I ended up going with the plan to have a dictionary for each day. The main difference is that this code here
             For going through each potential frame is longer. In hindsight I think going with the dictionary method may have been
             a slightly better idea, but this is the way I've gone for and there isn't anything neccasarily wrong with that 
             It's just a few sections of longer code. */
            foreach (Dictionary<string, double> frame in Monday.Values) {
                boundries.Add(frame[eventType]);
            }
            foreach (Dictionary<string, double> frame in Tuesday.Values) {
                boundries.Add(frame[eventType]);
            }
            foreach (Dictionary<string, double> frame in Wednesday.Values) {
                boundries.Add(frame[eventType]);
            }
            foreach (Dictionary<string, double> frame in Thursday.Values) {
                boundries.Add(frame[eventType]);
            }
            foreach (Dictionary<string, double> frame in Friday.Values) {
                boundries.Add(frame[eventType]);
            }
            foreach (Dictionary<string, double> frame in Saturday.Values) {
                boundries.Add(frame[eventType]);
            }
            foreach (Dictionary<string, double> frame in Sunday.Values) {
                boundries.Add(frame[eventType]);
            }
            double mean = Statistics.Mean(boundries); // Use of Math.NET Numerics for the first time. 
            return mean * (1 - significanceLimit);  // This provides the 'boundry' value when calculating significance. 
        }

        public void IncreaseSignificance(string day, string frame, string type, bool direction) {
            // See points above about the use of days as variables rather than keys in a dictionary. 
            double val;
            double incr;
            switch (day) {
                case "Monday":
                    val = Monday[frame][type];
                    incr = CalculateIncreaseValue(val, direction);
                    Monday[frame][type] += incr;
                    break;
                case "Tuesday":
                    val = Tuesday[frame][type];
                    incr = CalculateIncreaseValue(val, direction);
                    Tuesday[frame][type] += incr;
                    break;
                case "Wednesday":
                    val = Wednesday[frame][type];
                    incr = CalculateIncreaseValue(val, direction);
                    Wednesday[frame][type] += incr;
                    break;
                case "Thursday":
                    val = Thursday[frame][type];
                    incr = CalculateIncreaseValue(val, direction);
                    Thursday[frame][type] += incr;
                    break;
                case "Friday":
                    val = Friday[frame][type];
                    incr = CalculateIncreaseValue(val, direction);
                    Friday[frame][type] += incr;
                    break;
                case "Saturday":
                    val = Saturday[frame][type];
                    incr = CalculateIncreaseValue(val, direction);
                    Saturday[frame][type] += incr;
                    break;
                case "Sunday":
                    val = Sunday[frame][type];
                    incr = CalculateIncreaseValue(val, direction);
                    Sunday[frame][type] += incr;
                    break;
            }
        }
        private double CalculateIncreaseValue(double x, bool direct) {
            // The formula for this is a logistic function shifted upwards by 0.5 and then all multiplied by the significanceIncreaseValue
            // e here is defined as the base of the natural logarithm. 
            // σ(x) = (1/(1+e^(-x)) + 0.5) * v

            // If direct is false, the value is negated and instead the significance decreases. 
            return direct? significanceIncreaseValue * (1 / (1 + Math.Pow(Math.E, -x)) + 0.5) :
            significanceIncreaseValue * (1 / (1 + Math.Pow(Math.E, -x)) + 0.5) * -1;

        }
        // Constructor

        public SchedulerContainer(
        Dictionary<string, Dictionary<string, double>> mon,
        Dictionary<string, Dictionary<string, double>> tue,
        Dictionary<string, Dictionary<string, double>> wed,
        Dictionary<string, Dictionary<string, double>> thur,
        Dictionary<string, Dictionary<string, double>> fri,
        Dictionary<string, Dictionary<string, double>> sat,
        Dictionary<string, Dictionary<string, double>> sun
        ) {
            Monday = mon; Tuesday = tue;
            Wednesday = wed; Thursday = thur;
            Friday = fri; Saturday = sat;
            Sunday = sun;
        }

    }
}