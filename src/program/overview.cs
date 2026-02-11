using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Calendar;
using Manager;

#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.

namespace Overview {
    public static class OverviewOperations {

        public static List<Event> GetUniqueEventsInDays(List<Day> days) {
            List<Event> uniqueEvents = new List<Event> {};
            foreach (Day day in days) {
                foreach (Event event_ in day.events) {
                    if (!uniqueEvents.Select(x => x.eventID)
                                     .ToList()
                                     .Contains(event_.eventID)) 
                    {
                        uniqueEvents.Add(event_);    
                    }
                }        
            }
            
            return uniqueEvents;
        }


        public static string GetColourForValues(double u, double m, double l, double t) {
            if (u >= l) {
                // The higher, the better.
                return GetColourForValuesLookForUpper(u,m,l,t);
            }
            else {
                // The lower, the better
                return GetColourForValuesLookForLower(u,m,l,t);
            }         
        }

        private static string GetColourForValuesLookForUpper(double upperbound, double midref, double lowerbound, double testValue) {
            // In this case we want a higher value. 

            // Exact and beyond values
                    // Beyond values being when the testValue is beyond either of the two bounds. 
            if (testValue >= upperbound) { return ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertTop"]; }
            else if (testValue <= lowerbound) { return ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertLow"]; }
            else if (testValue == midref) { return ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertMid"]; }
            else if (testValue > lowerbound && testValue < midref) {
                // Get upper colour bounds -- For this we are using the middle reference.
                int upperRed = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertMid"][1..3], NumberStyles.AllowHexSpecifier);
                int upperGreen = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertMid"][3..5], NumberStyles.AllowHexSpecifier);
                int upperBlue = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertMid"][5..], NumberStyles.AllowHexSpecifier);

                // Get lower colour bounds -- Using the lower reference here.
                int lowerRed = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertLow"][1..3], NumberStyles.AllowHexSpecifier);
                int lowerGreen = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertLow"][3..5], NumberStyles.AllowHexSpecifier);
                int lowerBlue = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertLow"][5..], NumberStyles.AllowHexSpecifier);

                // Linear interpolation
                double proportion = (testValue - lowerbound) / (midref - lowerbound);

                int finalRed = (int)(lowerRed + (upperRed - lowerRed) * proportion);
                int finalGreen = (int)(lowerGreen + (upperGreen - lowerGreen) * proportion);
                int finalBlue = (int)(lowerBlue + (upperBlue - lowerBlue) * proportion);

                return "#" + finalRed.ToString("x2") + finalGreen.ToString("x2") + finalBlue.ToString("x2");
            }
            else {
                // Get upper colour bounds --Here we are using the upper reference. 
                int upperRed = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertTop"][1..3], NumberStyles.AllowHexSpecifier);
                int upperGreen = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertTop"][3..5], NumberStyles.AllowHexSpecifier);
                int upperBlue = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertTop"][5..], NumberStyles.AllowHexSpecifier);

                // Get lower colour bounds -- Using the middle reference
                int lowerRed = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertMid"][1..3], NumberStyles.AllowHexSpecifier);
                int lowerGreen = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertMid"][3..5], NumberStyles.AllowHexSpecifier);
                int lowerBlue = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertMid"][5..], NumberStyles.AllowHexSpecifier);

                // Linear interpolation
                double proportion = (testValue - midref) / (upperbound - midref);

                int finalRed = (int)(lowerRed + (upperRed - lowerRed) * proportion);
                int finalGreen = (int)(lowerGreen + (upperGreen - lowerGreen) * proportion);
                int finalBlue = (int)(lowerBlue + (upperBlue - lowerBlue) * proportion);

                return "#" + finalRed.ToString("x2") + finalGreen.ToString("x2") + finalBlue.ToString("x2");
            }
        }

        private static string GetColourForValuesLookForLower(double upperbound, double midref, double lowerbound, double testValue) {
            // Would prefer a lower value.
            // Exact and beyond values
                    // Beyond values being when the testValue is beyond either of the two bounds. 
            if (testValue <= upperbound) { return ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertTop"]; }
            else if (testValue >= lowerbound) { return ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertLow"]; }
            else if (testValue == midref) { return ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertMid"]; }
            else if (testValue < lowerbound && testValue > midref) { // Developer note -- Here I've just gone through and swapped everything related to lowerbound with midref.
                // Get upper colour bounds -- For this we are using the middle reference.
                int lowerRed = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertMid"][1..3], NumberStyles.AllowHexSpecifier);
                int lowerGreen = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertMid"][3..5], NumberStyles.AllowHexSpecifier);
                int lowerBlue = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertMid"][5..], NumberStyles.AllowHexSpecifier);

                // Get lower colour bounds -- Using the lower reference here.
                int upperRed = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertLow"][1..3], NumberStyles.AllowHexSpecifier);
                int upperGreen = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertLow"][3..5], NumberStyles.AllowHexSpecifier);
                int upperBlue = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertLow"][5..], NumberStyles.AllowHexSpecifier);

                // Linear interpolation
                double proportion = (testValue - midref) / (lowerbound - midref);

                int finalRed = (int)(lowerRed + (upperRed - lowerRed) * proportion);
                int finalGreen = (int)(lowerGreen + (upperGreen - lowerGreen) * proportion);
                int finalBlue = (int)(lowerBlue + (upperBlue - lowerBlue) * proportion);

                return "#" + finalRed.ToString("x2") + finalGreen.ToString("x2") + finalBlue.ToString("x2");
            }
            else {
                // Get upper colour bounds --Here we are using the upper reference. 
                int lowerRed = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertTop"][1..3], NumberStyles.AllowHexSpecifier);
                int lowerGreen = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertTop"][3..5], NumberStyles.AllowHexSpecifier);
                int lowerBlue = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertTop"][5..], NumberStyles.AllowHexSpecifier);

                // Get lower colour bounds -- Using the middle reference
                int upperRed = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertMid"][1..3], NumberStyles.AllowHexSpecifier);
                int upperGreen = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertMid"][3..5], NumberStyles.AllowHexSpecifier);
                int upperBlue = int.Parse(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAlertMid"][5..], NumberStyles.AllowHexSpecifier);

                // Linear interpolation
                double proportion = (testValue - upperbound) / (midref - upperbound);

                int finalRed = (int)(lowerRed + (upperRed - lowerRed) * proportion);
                int finalGreen = (int)(lowerGreen + (upperGreen - lowerGreen) * proportion);
                int finalBlue = (int)(lowerBlue + (upperBlue - lowerBlue) * proportion);

                return "#" + finalRed.ToString("x2") + finalGreen.ToString("x2") + finalBlue.ToString("x2");
            }
       }

    }
    public static class OverviewConstants {
        public static IDictionary<string, ColourConstant> ColourConstantTable = 
        new  Dictionary<string, ColourConstant> { // Values of this can be adjusted. Currently placeholder.
            {"goal_complete_std_deviation", new ColourConstant( // The order of this may cause issues.
                2, 5, 10
            )},
            {"goal_planned_std_deviation", new ColourConstant(
                2, 5, 10
            )},
            {"planned_complete_total_planned", new ColourConstant(
                10, 5, 0
            )},
            {"planned_complete_total_surplus", new ColourConstant(
                3, 1, 0
            )},
            {"planned_complete_std_devation", new ColourConstant(
                2, 5, 10
            )},
            {"planned_complete_mean_day", new ColourConstant(
                5, 3, 0
            )},
            {"planned_complete_max_day", new ColourConstant(
                8, 4, 0
            )}
        };

        public static IDictionary<string, string> DescriptionTable = 
        new Dictionary<string, string> { // Has descriptions for all of the possible comparison types.
            {"Total Proportion Completed", "(%) The total proportion of your goal you've completed."},
            {"Timeframe Consistency", "(%), Your consistency across the timeframe."},
            {"Mean Completed", "The mean is a measure of the average value of the data."},
            {"Standard Deviation", "Standard deviation is a measure of how spread out data is."},
            {"Median Completed", "The median is a measure of the most middle (average) value of the data."},
            {"Lower Quartile", "The lower quartile is the 25% percentile. 25% of the data falls below this point."},
            {"Upper Quartile", "The upper quartile is the 75% percentile. 25% of the data falls above this point."},
            {"Interquartile Range", "The difference between the two quartile values."},
            {"Extreme Maximum", "The most maximum value across the data set."},
            {"Total Proportion Planned", "(%) The total proportion of your goal you've planned."},
            {"Mean Planned", "(Hours) The mean average amount of study across all goals passed within this timeframe."},
            {"Median Planned", "The median is a measure of the most middle (average) value of the data."},
            {"Total Planned", "(Hours) The total amount of study you've planned."},
            {"Total Plan Completed", "(Hours) The amount of planned study you've completed."},
            {"Total Surplus", "(Hours) The total surplus study you've done in addition to your plans."},
            {"Total Completed", "(Hours) The total unplanned and planned study you've completed."},
            {"Planned/Complete Proportion", "(%) The proportion of your planned study you've completed."},
            {"Planned/Complete Proportion (Surplus Inclusive)", "(%) The proportion of your planned study you've completed, considering the surplus."},
            {"Mean Proportion Complete", "(%) The mean average proportion of study you've completed per day across the timeframe."},
            {"Median Proportion Complete", "(%) The median average proportion of study you've completed per day across the timeframe."},
            {"Mean Study Per Day", "(Hours) The mean amount of study you've completed per day across the timeframe."},
            {"Maximum Study", "(Hours) The most study you've completed on a day in the timeframe."},
            {"Chi Squared Test", "Pearson's Chi Squared Test is a statistical test used to determine if there's a link between an observed and expected set of data. It is used to help determine if you're sticking to your plans well enough."},
            {"Completable Events Set", "The number of completeable events you've set in this timeframe."},
            {"Completable Events Completed", "The number of completable events you've completed in this timeframe."},
            {"Upcoming Completable Events", "The number of upcoming completable events on your to-do."},
            {"Proportion Complete", "(%) The proportion of completable events you've completed in this timeframe."},
            {"Completable Events: School", "The number of completable events typed 'School/Homework'."},
            {"Completable Events: Work", "The number of completable events typed 'Work'."},
            {"Completable Events: Free", "The number of completable events typed 'Free Time'"},
            {"Proportion: School", "(%) The proportion of completable events typed 'School/Homework'."},
            {"Proportion: Work", "(%) The proportion of completable events typed 'Work'."},
            {"Proportion: Free", "(%) The proportion of completable events typed 'Free Time'."},
            {"Time Spent: Study", "(Hours) The time spent in 'Study' events this timeframe."},
            {"Time Spent: School", "(Hours) The time spent in 'School/Homework' events this timeframe."},
            {"Time Spent: Work", "(Hours) The time spent in 'Work' events this timeframe."},
            {"Time Spent: Free", "(Hours) The time spent in 'Free Time' events this timeframe."},
        };
    }

    public struct ColourConstant {
        public double top;
        public double mid; 
        public double low;

        public ColourConstant(double t, double m, double l) {
            top = t;
            mid = m;
            low = l;
        }
    }
}
#pragma warning restore CS8602 // Re-enabling Warnings
