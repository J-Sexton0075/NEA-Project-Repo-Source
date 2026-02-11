using Calendar;
using Manager;
using Overview;
using MathNet.Numerics.Statistics;
using MathNet.Numerics.Distributions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.Linq;

namespace project_main;

#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.
#pragma warning disable CS8604 // Compiler Warning Suppressed --  Possible null reference argument for parameter. 

public partial class OverviewPage : UserControl, IInitialisable {

    public string CompareButtonTextBinding => "Get Comparision";
    public string ExplainDefaultText => "An explanation of your selected comparision goes here!";
    public string OVTTTextBinding => "Type:";
    public string OVTRTextBinding => "Range";

    bool isInitialised = false;

    // These are used in the combo boxes. 
    ObservableCollection<string> TypeSettings = new ObservableCollection<string> {
        "Goals-Complete",
        "Goals-Plans",
        "Plans-Complete",
        "Completable",
        "Time Insight",
    };

    string[] RangeSettings = [ 
        "Week",
        "Fortnight",
        "Month",
        "Three Months",
        "Year",
        "Two Years",
        "Max"
    ];

    Dictionary<string, string> typeDescriptions = // Provides text descriptions for each type
    // That can be updated when switching them, 
    new Dictionary<string, string> {
      {"Goals-Complete", "Goals-Complete: Compare your long term goals to the study you have completed during this time period."},
      {"Goals-Plans", "Compare you long term goals to the plans you made within this time period."},
      {"Plans-Complete", "Compare your plans in this time period with the study you've actually completed in this time."},
      {"Completable", "See an insight of the events you've set as 'completable' in this time period."},
      {"Time Insight", "See how you've been using your time recently."},
    }; 

    public OverviewPage() {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);


        // Set up combo boxes. 
        this.FindControl<ComboBox>("OVTTComboBox").ItemsSource = TypeSettings;

        this.FindControl<ComboBox>("OVTRComboBox").ItemsSource = RangeSettings;

    }

    public void InitialisePage() {
        isInitialised = false;
        // Clear combo boxes and the main page, as well as text. 
        ComboBox type = this.FindControl<ComboBox>("OVTTComboBox");
        ComboBox range = this.FindControl<ComboBox>("OVTRComboBox");
        
        TextBlock explainText = this.FindControl<TextBlock>("OVTExplain");
        TextBlock boxText1 = this.FindControl<TextBlock>("OVMBTText1");
        TextBlock boxText2 = this.FindControl<TextBlock>("OVMBTText2");
        
      //  DockPanel container = box.FindControl<ScrollViewer>("OVBMScroll")
      //                           .FindControl<DockPanel>("OVMBBlockContainer");

        /* this.FindControl<Grid>("OVMain")
               .FindControl<Grid>("OVMainBox")
               .FindControl<Grid>("OVMBTop")
               .FindControl<TextBlock>("OVMBTText1")
        */

        // Reset all the values
        type.SelectedIndex = 0;
        range.SelectedIndex = 0;
   //     container.Children.Clear();
        boxText1.Text = "";
        boxText2.Text = "";
        explainText.Text = ExplainDefaultText;
        ClearComparison();

        // Call to set the type description.
        isInitialised = true;
        SetTypeDescription();
    }


    public void TriggerSetTypeDescription(object sender, RoutedEventArgs args) {
        SetTypeDescription();
    }

    public void SetTypeDescription() {
        if (isInitialised) {
            TextBlock explainText = this.FindControl<TextBlock>("OVTExplain");
            
            ComboBox types = this.FindControl<ComboBox>("OVTTComboBox");

            explainText.Text = typeDescriptions[(string)types.SelectedItem];
        }
    }

    public void CompareClick(object sender, RoutedEventArgs args) {
        ClearComparison();
        UpdateComparisonSet();
    }

    public void ClearComparison() {
        this.FindControl<DockPanel>("OVMBBlockContainer").Children.Clear();
    }

    public void AddCompareBlock(string title, string colour, double stat) {
        this.FindControl<DockPanel>("OVMBBlockContainer").Children.Add(new OverviewBlock(title, colour, stat));
    }
    public void AddCompareBlock(string title, string colour, string stat) { // Used for string stats rather that double stats
         this.FindControl<DockPanel>("OVMBBlockContainer").Children.Add(new OverviewBlock(title, colour, stat));      
    }

    public void UpdateComparisonSet() {
        ComparisonSet set = Converters.compTypeName_TO_comparisonSet[this.FindControl<ComboBox>("OVTTComboBox").SelectedItem.ToString()];

        NotifyError("");
        switch (set) {
            case ComparisonSet.Goals_Complete:
                UpdateSetGoals_Complete();
                break;
            case ComparisonSet.Goals_Plans:
                UpdateSetGoals_Plans();
                break;
            case ComparisonSet.Plans_Complete:
                UpdateSetPlanned_Complete();
                break;
            case ComparisonSet.Completable:
                UpdateSetCompletable();
                break;
            case ComparisonSet.Insight:
                UpdateSetTimeInsight();
                break;
        }
    }

    public void UpdateSetGoals_Complete() {
        int goalLength = Converters.compString_TO_dateLength[
            ManagerValues.CurrentCalendarPreferences.goals["type"]
        ];
        double goalMagnitude = double.Parse(ManagerValues.CurrentCalendarPreferences.goals["magnitude"]);
        string rangeString = this.FindControl<ComboBox>("OVTRComboBox").SelectedItem.ToString();

        P_Range dateRange = RangeLoader.ReturnPastRange(Converters.rangeName_TO_rangeLength[rangeString]);
        NotifyRange(dateRange);
        if (goalLength > dateRange.fullRange.Count) {
            NotifyError("Your goal length cannot be longer than your comparison length!");    
        }
        else {
            List<(DateTime, DateTime)> timeframesInComparison = new List<(DateTime, DateTime)> { };

            // Step 1: Get all the GTimeframes which appear in the CTimeframe
            bool hasEnded = false;
            DateTime currentUpper = dateRange.rangeEnd;
            DateTime currentLower = currentUpper.AddDays(-(goalLength-1));
            while (!hasEnded) {
                timeframesInComparison.Add((currentLower, currentUpper));

                if (currentLower.AddDays(-goalLength) <= dateRange.rangeStart) {
                    hasEnded = true;
                }
                else {
                    currentUpper = currentUpper.AddDays(-goalLength);
                    currentLower = currentLower.AddDays(-goalLength); // Corrected from design. 
                }
            }  
            // Step 2: Get all the hours of study across the entire data set. 
            double hoursOfStudy = 0;
            List<Event> totalUniqueStudyEvents = OverviewOperations.GetUniqueEventsInDays(dateRange.fullRange)
                                                                   .Where(event_ => event_.eventType == "eventStudy" 
                                                                   || event_.eventType == "eventSurplus")
                                                                   .ToList();
            
            foreach (Event event_ in totalUniqueStudyEvents) {
                hoursOfStudy += event_.studyCompleted * (event_.endTime - event_.startTime).TotalHours;
            }

            // Step 3: Get the hours of study for each frame. 
            List<double> hoursOfStudyPerFrame = new List<double> { };
            int goalReached = 0;
            foreach ((DateTime, DateTime) goalframe in timeframesInComparison) {
                List<Day> frameDays = RangeLoader.GetDayObjectForDates(RangeLoader.GetDatesInRange(goalframe.Item1, goalframe.Item2));
                List<Event> localUniqueStudyEvents = OverviewOperations.GetUniqueEventsInDays(frameDays)
                                                           .Where(event_ => event_.eventType == "eventStudy" || event_.eventType == "eventSurplus")
                                                           .ToList();
                if (localUniqueStudyEvents.Count > 0) { // Ensure context dependant
                    double frameHoursOfStudy = 0;
                    foreach (Event event_ in localUniqueStudyEvents) {
                        frameHoursOfStudy += event_.studyCompleted * (event_.endTime - event_.startTime).TotalHours;
                    }
                    if (frameHoursOfStudy > goalMagnitude) { goalReached++; }
                    hoursOfStudyPerFrame.Add(frameHoursOfStudy);
                }
            }

            // Step 4: Calculate measures
            double stat_TotalCompleted;
            if (goalMagnitude == 0) { // Check to prevent a divide by zero error 
                stat_TotalCompleted = 0;
            }
            else {
                stat_TotalCompleted = hoursOfStudy / (goalMagnitude * timeframesInComparison.Count) * 100;
            }
            double stat_TimeframeConsistency = goalReached / timeframesInComparison.Count * 100;
            double stat_MeanCompleted = Statistics.Mean(hoursOfStudyPerFrame);
            double stat_StdDev = Statistics.StandardDeviation(hoursOfStudyPerFrame);
            double stat_MedianCompleted = Statistics.Median(hoursOfStudyPerFrame);
            double stat_LowerQuartile = Statistics.Percentile(hoursOfStudyPerFrame, 25);
            double stat_UpperQuartile = Statistics.Percentile(hoursOfStudyPerFrame, 75);
            double stat_IQR = stat_UpperQuartile - stat_LowerQuartile;
            double stat_ExtremeMax = Statistics.Maximum(hoursOfStudyPerFrame);

            // Step 5: Get Colour Strings
            string hex_TotalCompleted = OverviewOperations.GetColourForValues(100,50,0, stat_TotalCompleted);
            string hex_TimeframeConsistency = OverviewOperations.GetColourForValues(100,50,0, stat_TimeframeConsistency);
            string hex_MeanCompleted = OverviewOperations.GetColourForValues(goalMagnitude, goalMagnitude/2, 0, stat_MeanCompleted);
            string hex_StdDev = OverviewOperations.GetColourForValues(
                OverviewConstants.ColourConstantTable["goal_complete_std_deviation"].top,
                OverviewConstants.ColourConstantTable["goal_complete_std_deviation"].mid,
                OverviewConstants.ColourConstantTable["goal_complete_std_deviation"].low,
                stat_StdDev/stat_MeanCompleted // Note that here we use the coefficient of variation rather than the standard deviation for fairness of the test. 
            );
            string hex_MedianCompleted = OverviewOperations.GetColourForValues(goalMagnitude, goalMagnitude/2, 0, stat_MedianCompleted);
            string hex_LowerQuartile = OverviewOperations.GetColourForValues(goalMagnitude/2, goalMagnitude/4, 0, stat_LowerQuartile);
            string hex_UpperQuartile = OverviewOperations.GetColourForValues(goalMagnitude, goalMagnitude*0.75, goalMagnitude/2, stat_UpperQuartile);
            string hex_IQR = OverviewOperations.GetColourForValues(0, goalMagnitude/4, goalMagnitude/2, stat_IQR);
            string hex_ExtremeMax = OverviewOperations.GetColourForValues(goalMagnitude*1.5, goalMagnitude, 0, stat_ExtremeMax);

            // Step 6: Update Page
            AddCompareBlock("Total Proportion Completed", hex_TotalCompleted, stat_TotalCompleted);
            AddCompareBlock("Timeframe Consistency", hex_TimeframeConsistency, stat_TimeframeConsistency);
            AddCompareBlock("Mean Completed", hex_MeanCompleted, stat_MeanCompleted);
            AddCompareBlock("Standard Deviation", hex_StdDev, stat_StdDev);
            AddCompareBlock("Median Completed", hex_MedianCompleted, stat_MedianCompleted);
            AddCompareBlock("Lower Quartile", hex_LowerQuartile, stat_LowerQuartile);
            AddCompareBlock("Upper Quartile", hex_UpperQuartile, stat_UpperQuartile);
            AddCompareBlock("Interquartile Range", hex_IQR, stat_IQR);
            AddCompareBlock("Extreme Maximum", hex_ExtremeMax, stat_ExtremeMax);
        }
    }

    public void UpdateSetGoals_Plans() {
        int goalLength = Converters.compString_TO_dateLength[
            ManagerValues.CurrentCalendarPreferences.goals["type"]
        ];
        double goalMagnitude = double.Parse(ManagerValues.CurrentCalendarPreferences.goals["magnitude"]);
        string rangeString = this.FindControl<ComboBox>("OVTRComboBox").SelectedItem.ToString();

        P_Range dateRange = RangeLoader.ReturnPastRange(Converters.rangeName_TO_rangeLength[rangeString]);
        NotifyRange(dateRange);
        if (goalLength > dateRange.fullRange.Count) {
            NotifyError("Your goal length cannot be longer than your comparison length!");    
        }
        else {
            List<(DateTime, DateTime)> timeframesInComparison = new List<(DateTime, DateTime)> { };

            // Step 1: Get all the GTimeframes which appear in the CTimeframe
            bool hasEnded = false;
            DateTime currentUpper = dateRange.rangeEnd;
            DateTime currentLower = currentUpper.AddDays(-(goalLength-1));
            while (!hasEnded) {
                timeframesInComparison.Add((currentLower, currentUpper));

                if (currentLower.AddDays(-goalLength) <= dateRange.rangeStart) {
                    hasEnded = true;
                }
                else {
                    currentUpper = currentUpper.AddDays(-goalLength);
                    currentLower = currentLower.AddDays(-goalLength); 
                }
            }

            // Step 2: Get all the hours of study across the entire data set. 
            double hoursOfStudy = 0;
            List<Event> totalUniqueStudyEvents = OverviewOperations.GetUniqueEventsInDays(dateRange.fullRange)
                                                                   .Where(event_ => event_.eventType == "eventStudy")
                                                                   .ToList();

            foreach (Event event_ in totalUniqueStudyEvents) {
                hoursOfStudy += (event_.endTime - event_.startTime).TotalHours;
            }
            
            // Step 3: Get the hours of study for each frame
            List<double> hoursOfStudyPerFrame = new List<double> { };
            int goalReached = 0;
            foreach ((DateTime, DateTime) goalframe in timeframesInComparison) {
                List<Day> frameDays = RangeLoader.GetDayObjectForDates(RangeLoader.GetDatesInRange(goalframe.Item1, goalframe.Item2));
                List<Event> localUniqueStudyEvents = OverviewOperations.GetUniqueEventsInDays(frameDays)
                                                           .Where(event_ => event_.eventType == "eventStudy")
                                                           .ToList();
                if (localUniqueStudyEvents.Count > 0) { // Ensure context dependant
                    double frameHoursOfStudy = 0;
                    foreach (Event event_ in localUniqueStudyEvents) {
                        frameHoursOfStudy += (event_.endTime - event_.startTime).TotalHours;
                    }
                    if (frameHoursOfStudy > goalMagnitude) { goalReached++; }
                    hoursOfStudyPerFrame.Add(frameHoursOfStudy);
                }   
            }

            // Step 4: Calculate Measures
            double stat_TotalPlanned;
            if (goalMagnitude == 0) { // Check to prevent a divide by zero error 
                stat_TotalPlanned = 0;
            }
            else {
                stat_TotalPlanned = hoursOfStudy / (goalMagnitude * timeframesInComparison.Count) * 100;
            }
            double stat_TimeframeConsistency =  100 * goalReached / timeframesInComparison.Count;
            double stat_MeanPlanned = Statistics.Mean(hoursOfStudyPerFrame);
            double stat_StdDev = Statistics.StandardDeviation(hoursOfStudyPerFrame);
            double stat_MedianPlanned = Statistics.Median(hoursOfStudyPerFrame);
            double stat_LowerQuartile = Statistics.Percentile(hoursOfStudyPerFrame, 25);
            double stat_UpperQuartile = Statistics.Percentile(hoursOfStudyPerFrame, 75);
            double stat_IQR = stat_UpperQuartile - stat_LowerQuartile;
            double stat_ExtremeMax = Statistics.Maximum(hoursOfStudyPerFrame);

            // Step 5: Caclulate Colours
            string hex_TotalPlanned = OverviewOperations.GetColourForValues(100,50,0, stat_TotalPlanned);
            string hex_TimeframeConsistency = OverviewOperations.GetColourForValues(100,50,0, stat_TimeframeConsistency);
            string hex_MeanPlanned = OverviewOperations.GetColourForValues(goalMagnitude, goalMagnitude/2, 0, stat_MeanPlanned);
            string hex_StdDev = OverviewOperations.GetColourForValues(
                OverviewConstants.ColourConstantTable["goal_complete_std_deviation"].top,
                OverviewConstants.ColourConstantTable["goal_complete_std_deviation"].mid,
                OverviewConstants.ColourConstantTable["goal_complete_std_deviation"].low,
                stat_StdDev/stat_MeanPlanned // Note that here we use the coefficient of variation rather than the standard deviation for fairness of the test. 
            );
            string hex_MedianPlanned = OverviewOperations.GetColourForValues(goalMagnitude, goalMagnitude/2, 0, stat_MedianPlanned);
            string hex_LowerQuartile = OverviewOperations.GetColourForValues(goalMagnitude/2, goalMagnitude/4, 0, stat_LowerQuartile);
            string hex_UpperQuartile = OverviewOperations.GetColourForValues(goalMagnitude, goalMagnitude*0.75, goalMagnitude/2, stat_UpperQuartile);
            string hex_IQR = OverviewOperations.GetColourForValues(0, goalMagnitude/4, goalMagnitude/2, stat_IQR);
            string hex_ExtremeMax = OverviewOperations.GetColourForValues(goalMagnitude*1.5, goalMagnitude, 0, stat_ExtremeMax);

            // Step 6: Update Page
            AddCompareBlock("Total Proportion Planned", hex_TotalPlanned, stat_TotalPlanned);
            AddCompareBlock("Timeframe Consistency", hex_TimeframeConsistency, stat_TimeframeConsistency);
            AddCompareBlock("Mean Planned", hex_MeanPlanned, stat_MeanPlanned);
            AddCompareBlock("Standard Deviation", hex_StdDev, stat_StdDev);
            AddCompareBlock("Median Planned", hex_MedianPlanned, stat_MedianPlanned);
            AddCompareBlock("Lower Quartile", hex_LowerQuartile, stat_LowerQuartile);
            AddCompareBlock("Upper Quartile", hex_UpperQuartile, stat_UpperQuartile);
            AddCompareBlock("Interquartile Range", hex_IQR, stat_IQR);
            AddCompareBlock("Extreme Maximum", hex_ExtremeMax, stat_ExtremeMax);
        }
    }

    public void UpdateSetPlanned_Complete() {
        string rangeString = this.FindControl<ComboBox>("OVTRComboBox").SelectedItem.ToString();

        P_Range dateRange = RangeLoader.ReturnPastRange(Converters.rangeName_TO_rangeLength[rangeString]);
        NotifyRange(dateRange);
        // Step 1: Get all the surplus and study events
            List<Event> totalUniqueStudyEvents = OverviewOperations.GetUniqueEventsInDays(dateRange.fullRange)
                                                                   .Where(event_ => event_.eventType == "eventStudy" 
                                                                   || event_.eventType == "eventSurplus")
                                                                   .ToList();
        // Step 2: Get a list of the magnitudes of planned study and completed study. 
        // I can use LINQ to work out sums later.

        List<double> planned = new List<double> { };
        List<double> completed = new List<double> { };
        List<double> surplus = new List<double> { };

        foreach (Event event_ in totalUniqueStudyEvents) {
            if (event_.eventType == "eventSurplus") {
                surplus.Add( (event_.endTime - event_.startTime).TotalHours);
            }
            else {
                double plan = (event_.endTime - event_.startTime).TotalHours;
                planned.Add(plan);
                completed.Add(plan * event_.studyCompleted);
            }
        }

        // Step 3: Calculate totals
        double stat_TotalPlanned = planned.Sum();
        double stat_TotalPlanCompleted = completed.Sum();
        double stat_TotalSurplus = surplus.Sum();
        double stat_TotalComplete = stat_TotalSurplus + stat_TotalPlanCompleted;

        // Step 4: Calculate basic proportions
        double stat_BasicPropPlannedComplete = 100* stat_TotalPlanCompleted / stat_TotalPlanned;
        double stat_BasicPropSurplusInclusive = 100 * stat_TotalComplete / stat_TotalPlanned;

        // Step 5: Get list of proportions (for percentiles/mean/medians etc. )
        List<double> proportionList = new List<double> { };
        for (int i = 0; i < planned.Count; i++) {
            proportionList.Add(completed[i]/planned[i]);
        }

        // Step 6: Mean and max study across each day
        List<double> studyPerDay = new List<double> { };
        foreach (Day day in dateRange.fullRange) {
            double today = 0;
            foreach (Event dEvent in day.events) {
                if (dEvent.eventType == "eventStudy" || dEvent.eventType == "eventSurplus") {
                    today += dEvent.studyCompleted * (dEvent.endTime - dEvent.startTime).TotalHours;
                }
            }
            studyPerDay.Add(today);
        }

        // Step 7: Calculate remaining statistics
        double stat_MeanPropPlannedComplete = Statistics.Mean(proportionList);
        double stat_MedianPropPlannedComplete = Statistics.Median(proportionList);
        double stat_StdDevPlannedComplete = Statistics.StandardDeviation(proportionList);
        double stat_UpperQuartile = Statistics.Percentile(proportionList, 75);
        double stat_LowerQuartile = Statistics.Percentile(proportionList, 25);
        double stat_IQR = stat_UpperQuartile - stat_LowerQuartile;
        double stat_MeanPerDay = Statistics.Mean(studyPerDay);
        double stat_MaxOfDays = Statistics.Maximum(studyPerDay);

        // Step 8: Pearson's Chi Squared
        // The first step will be to tackle the merging sequence before I can calculate degrees of freedom.
        //  I’ll store the merged values in a list of tuples (Expected, Observed) but I should be careful,
        //  as tuples aren’t mutable so I’ll have to use variables temporarily before merging.

        List<(double, double)> observedExpected = new List<(double, double)> { };
        bool chiSquaredIsValid = true;
        for (int i = 0; i < planned.Count; i++) {
            double expected = planned[i];
            double observed = completed[i];
            int difference = 0;
            while (expected < 5) { // This here is due to a small technical thing in Pearson's Chi Squared where expected values must be above 5 for the test to be effective.
                try { 
                    difference ++;
                    expected += planned[i + difference];
                    observed += completed[i + difference];
                }
                catch {
                    chiSquaredIsValid = false;
                    expected += 6;
                }
            }
            i += difference;
            observedExpected.Add((expected, observed));
        }
        // The next step of this will be to calculate the actual chi-squared value. 
        // This will be used in the ‘Goodness of Fit’ measure. 
        double stat_GoodnessOfFit = 0;
        for (int i = 0; i<observedExpected.Count; i++) {
            stat_GoodnessOfFit += Math.Pow(observedExpected[i].Item2 - observedExpected[i].Item1, 2) / observedExpected[i].Item1;  
        }

        // Then I compare this to the predicted Chi-Squared value
        double degreesOfFreedom = observedExpected.Count(); // Usually, I’d have to subtract one here since in most cases there’s a ‘constraint’ that the sum of expected == sum of observed == sum of total
        //  but that isn’t the case here. Whether or not this is derived from a poor misuse of the distribution or is completely fine, 
        // I’m not 100% sure, but I’ll try anyway and if it doesn’t work I can rework this idea. 
        // After further thought about this decision, I've decided that I could reason that you should subtract one, but unless issues are found I think I'll keep it this way.
        // Since I'm not sure yet which way is correct. 

        ChiSquared distribution = new ChiSquared(degreesOfFreedom);

        double boundry = distribution.InverseCumulativeDistribution(0.95);
        // This value 0.95 represents a probability. I’ve decided that in this test my ‘Level of significance’ will be 5%. 
        // If my user exceeds this boundary with their goodness of fit,
        //  I’ll determine that there’s enough evidence to say that they aren’t ‘sticking to their plans’.
        //  I may adjust this value in testing to ensure it’s more appropriate for this scenario.  

        

        string stat_Special_ChiSquaredHypTestResult = "Low Consistency";
        if (stat_GoodnessOfFit < boundry * 0.80) {
            stat_Special_ChiSquaredHypTestResult = "Good Consistency";
        }
        else if (stat_GoodnessOfFit >= boundry) {
            stat_Special_ChiSquaredHypTestResult = "Bad Consistency";
        }
        stat_Special_ChiSquaredHypTestResult += $" -- Goodness Of Fit: {Math.Round(stat_GoodnessOfFit, 2)}" + $" Boundry: {Math.Round(boundry, 2)}"; 

        if (!chiSquaredIsValid) {
            stat_Special_ChiSquaredHypTestResult = "Not Valid for Chi Squared";
        }

        // Step 9: Get colours
        string hex_TotalPlanned = OverviewOperations.GetColourForValues(
            OverviewConstants.ColourConstantTable["planned_complete_total_planned"].top,
            OverviewConstants.ColourConstantTable["planned_complete_total_planned"].mid,
            OverviewConstants.ColourConstantTable["planned_complete_total_planned"].low,
            stat_TotalPlanned
        );
        string hex_TotalPlanCompleted = OverviewOperations.GetColourForValues(stat_TotalPlanned, stat_TotalPlanned/2, 0, stat_TotalPlanCompleted);
        string hex_TotalSurplus = OverviewOperations.GetColourForValues(
            OverviewConstants.ColourConstantTable["planned_complete_total_surplus"].top,
            OverviewConstants.ColourConstantTable["planned_complete_total_surplus"].mid,
            OverviewConstants.ColourConstantTable["planned_complete_total_surplus"].low,
            stat_TotalSurplus
        );
        string hex_TotalCompleted = OverviewOperations.GetColourForValues(stat_TotalPlanned, stat_TotalPlanned/2, 0, stat_TotalComplete);
        string hex_BasicPropPlannedComplete = OverviewOperations.GetColourForValues(100, 50, 0, stat_BasicPropPlannedComplete);
        string hex_BasicPropSurplusInclusive = OverviewOperations.GetColourForValues(100,50,0, stat_BasicPropSurplusInclusive);
        string hex_MeanPropPlannedComplete = OverviewOperations.GetColourForValues(100,50,0, stat_MeanPropPlannedComplete);
        string hex_MedianPropPlannedComplete = OverviewOperations.GetColourForValues(100,50,0, stat_MedianPropPlannedComplete);
        string hex_StdDevPlannedComplete = OverviewOperations.GetColourForValues(
            OverviewConstants.ColourConstantTable["planned_complete_std_devation"].top,
            OverviewConstants.ColourConstantTable["planned_complete_std_devation"].mid,
            OverviewConstants.ColourConstantTable["planned_complete_std_devation"].low, 
            stat_StdDevPlannedComplete          
        );
        string hex_MeanPerDay = OverviewOperations.GetColourForValues(
            OverviewConstants.ColourConstantTable["planned_complete_mean_day"].top,
            OverviewConstants.ColourConstantTable["planned_complete_mean_day"].mid,
            OverviewConstants.ColourConstantTable["planned_complete_mean_day"].low, 
            stat_MeanPerDay
        );
        string hex_MaxOfDays = OverviewOperations.GetColourForValues(
            OverviewConstants.ColourConstantTable["planned_complete_max_day"].top,
            OverviewConstants.ColourConstantTable["planned_complete_max_day"].mid,
            OverviewConstants.ColourConstantTable["planned_complete_max_day"].low, 
            stat_MaxOfDays
        );
        string hex_UpperQuartile = OverviewOperations.GetColourForValues(100,75,50, stat_UpperQuartile);
        string hex_LowerQuartile = OverviewOperations.GetColourForValues(50,25,0, stat_LowerQuartile);
        string hex_IQR = OverviewOperations.GetColourForValues(0, 25, 50, stat_IQR);
        string hex_ChiSquared = OverviewOperations.GetColourForValues(0, 0.8 * boundry, boundry, stat_GoodnessOfFit);

        // Step 10: Update Page
        AddCompareBlock("Total Planned", hex_TotalPlanned, stat_TotalPlanned);
        AddCompareBlock("Total Plan Completed", hex_TotalPlanCompleted, stat_TotalPlanCompleted);
        AddCompareBlock("Total Surplus", hex_TotalSurplus, stat_TotalSurplus);
        AddCompareBlock("Total Completed", hex_TotalCompleted, stat_TotalComplete);
        AddCompareBlock("Planned/Complete Proportion", hex_BasicPropPlannedComplete, stat_TotalComplete);
        AddCompareBlock("Planned/Complete Proportion (Surplus Inclusive)", hex_BasicPropSurplusInclusive, stat_BasicPropSurplusInclusive);
        AddCompareBlock("Mean Proportion Complete", hex_MeanPropPlannedComplete, stat_MeanPropPlannedComplete);
        AddCompareBlock("Median Proportion Complete", hex_MedianPropPlannedComplete, stat_MedianPropPlannedComplete);
        AddCompareBlock("Standard Deviation", hex_StdDevPlannedComplete, stat_StdDevPlannedComplete);
        AddCompareBlock("Mean Study Per Day", hex_MeanPerDay, stat_MeanPerDay);
        AddCompareBlock("Maximum Study", hex_MaxOfDays, stat_MaxOfDays);
        AddCompareBlock("Upper Quartile", hex_UpperQuartile, stat_UpperQuartile);
        AddCompareBlock("Lower Quartile", hex_LowerQuartile, stat_LowerQuartile);
        AddCompareBlock("Interquartile Range", hex_IQR, stat_IQR);
        AddCompareBlock("Chi Squared Test", hex_ChiSquared, stat_Special_ChiSquaredHypTestResult);
    }
    
    public void UpdateSetCompletable() {
        string rangeString = this.FindControl<ComboBox>("OVTRComboBox").SelectedItem.ToString();

        P_Range dateRange = RangeLoader.ReturnPastRange(Converters.rangeName_TO_rangeLength[rangeString]);
        NotifyRange(dateRange);
        List<Event> totalUniqueEventsCompletable = OverviewOperations.GetUniqueEventsInDays(dateRange.fullRange)
                                                                     .Where(event_ => event_.eventIsCompleteable == true)
                                                                     .ToList();

        double stat_CompletableEventsSet = totalUniqueEventsCompletable.Count();
        double stat_CompletableCompleted = totalUniqueEventsCompletable.Where(event_ => event_.eventIsCompleted == true)
                                                                       .ToList().Count;
        List<Event> totalPastCompletable = totalUniqueEventsCompletable.Where(event_ => event_.endTime <= DateTime.Now)
                                                                       .ToList();
        
        double stat_totalUpcomingCompletable = totalUniqueEventsCompletable.Where(event_ => event_.endTime > DateTime.Now)
                                                                           .ToList().Count;
        
        List<Event> totalPastCompleted = totalPastCompletable.Where(event_ => event_.eventIsCompleted == true)
                                                             .ToList();

        double stat_propComplete; 
        if (totalPastCompletable.Count == 0) { // Check for divide by zero
            stat_propComplete = 0;
        }
        else {
            stat_propComplete = 100 * totalPastCompleted.Count / totalPastCompletable.Count;
        }

        double stat_TotalSchool = totalUniqueEventsCompletable.Where(event_ => event_.eventType == "eventSchool")
                                                              .ToList().Count;
        double stat_TotalWork = totalUniqueEventsCompletable.Where(event_ => event_.eventType == "eventWork")
                                                              .ToList().Count;
        double stat_TotalFree = totalUniqueEventsCompletable.Where(event_ => event_.eventType == "eventFree")
                                                              .ToList().Count;    
        double stat_PropSchool;
        double stat_PropWork; 
        double stat_PropFree;
        List<string> customTypes = new List<string> { };
        IDictionary<string, (double, double)> typeTotalProps = new Dictionary<string, (double, double)> { };

        if (stat_CompletableEventsSet == 0 ) { // Divide by zero check
            stat_PropSchool = 0;
            stat_PropWork = 0;
            stat_PropFree = 0;   
            foreach (Dictionary<string, string> i in ManagerValues.CurrentCalendarPreferences.customTypes) {
                double tempTotal = totalUniqueEventsCompletable.Where(event_ => event_.eventType == i["name"])
                                                            .ToList().Count;
                double tempProp = 0;
                typeTotalProps.Add(i["name"], (tempTotal, tempProp));
            }
        }        
        else {
            stat_PropSchool = 100 * stat_TotalSchool / stat_CompletableEventsSet;
            stat_PropWork = 100 * stat_TotalWork / stat_CompletableEventsSet;
            stat_PropFree = 100 * stat_TotalFree / stat_CompletableEventsSet;
            foreach (Dictionary<string, string> i in ManagerValues.CurrentCalendarPreferences.customTypes) {
                double tempTotal = totalUniqueEventsCompletable.Where(event_ => event_.eventType == i["name"])
                                                            .ToList().Count;
                double tempProp = 100 * tempTotal / stat_CompletableEventsSet;
                typeTotalProps.Add(i["name"], (tempTotal, tempProp));
            }
        }    

        // Get Colours
            // Most values here are neutral, therefore do not need calculating                     
        string neutral = ReturnNeutralColour(); 
        string hex_CompletableCompleted = OverviewOperations.GetColourForValues(stat_CompletableEventsSet, stat_CompletableEventsSet/2, 0, stat_CompletableCompleted);
        string hex_PropComplete = OverviewOperations.GetColourForValues(100,50,0, stat_propComplete);

        // Add to page
        AddCompareBlock("Completable Events Set", neutral, stat_CompletableEventsSet);
        AddCompareBlock("Completable Events Completed", hex_CompletableCompleted, stat_CompletableCompleted);
        AddCompareBlock("Upcoming Completable Events", neutral, stat_totalUpcomingCompletable);
        AddCompareBlock("Proportion Complete", hex_PropComplete, stat_propComplete);
        AddCompareBlock("Completable Events: School", neutral, stat_TotalSchool);
        AddCompareBlock("Completable Events: Work", neutral, stat_TotalWork);
        AddCompareBlock("Completable Events: Free", neutral, stat_TotalFree);
        AddCompareBlock("Proportion: School", neutral, stat_PropSchool);
        AddCompareBlock("Proportion: Work", neutral, stat_PropWork);
        AddCompareBlock("Proportion: Free", neutral, stat_PropFree);
        foreach (KeyValuePair<string, (double, double)> i in typeTotalProps) {
            AddCompareBlock($"Completable Events: {i.Key}", neutral, i.Value.Item1);
            AddCompareBlock($"Proportion: {i.Key}", neutral, i.Value.Item2);
        }
    }
    
    public void UpdateSetTimeInsight() {
        string rangeString = this.FindControl<ComboBox>("OVTRComboBox").SelectedItem.ToString();

        P_Range dateRange = RangeLoader.ReturnPastRange(Converters.rangeName_TO_rangeLength[rangeString]); 
        NotifyRange(dateRange);
        List<Event> uniqueEvents = OverviewOperations.GetUniqueEventsInDays(dateRange.fullRange);
        
        double timeStudy = 0;
        double timeWork = 0;
        double timeSchool = 0;
        double timeFree = 0;
        IDictionary<string, double> timeCustom = new Dictionary<string, double> { };
        
        foreach (Event event_ in uniqueEvents) {
            switch (event_.eventType) {
                case "eventStudy":
                    timeStudy += (event_.endTime - event_.startTime).TotalHours;
                    break;
                case "eventWork":
                    timeWork += (event_.endTime - event_.startTime).TotalHours;
                    break;
                case "eventSchool":
                    timeSchool += (event_.endTime - event_.startTime).TotalHours;
                    break;
                case "eventFree": 
                    timeFree += (event_.endTime - event_.startTime).TotalHours;
                    break;
                case "eventSurplus":
                    break;
                default: 
                    if (timeCustom.ContainsKey(event_.eventType)) {
                        timeCustom[event_.eventType] += (event_.endTime - event_.startTime).TotalHours;
                    }
                    else {
                        timeCustom.Add(event_.eventType,  (event_.endTime - event_.startTime).TotalHours);
                    }
                    break;
            }
        }

        // Update page -- colours are based off the type. 
        AddCompareBlock("Time Spent: Study", ManagerValues.CurrentCalendarPreferences.typeColours["eventStudy"],timeStudy);
        AddCompareBlock("Time Spent: Work", ManagerValues.CurrentCalendarPreferences.typeColours["eventWork"],timeWork);
        AddCompareBlock("Time Spent: School", ManagerValues.CurrentCalendarPreferences.typeColours["eventSchool"],timeSchool);
        AddCompareBlock("Time Spent: Free", ManagerValues.CurrentCalendarPreferences.typeColours["eventFree"],timeFree);

        foreach (KeyValuePair<string, double> t in timeCustom) {
            // Get colour string.
            string colour = "";
            foreach (Dictionary<string, string> i in ManagerValues.CurrentCalendarPreferences.customTypes) {
                if (i["name"] == t.Key) {
                    colour = i["colour"];
                }
            }
            AddCompareBlock($"Time Spent: {t.Key}", colour, t.Value);
        }
        
    }

    public void NotifyError(string error) {
        TextBlock alertBox = this.FindControl<TextBlock>("OVMBTText1");
        
        alertBox.Text = error;
    }
    public void NotifyRange(P_Range range) {
        TextBlock alertBox = this.FindControl<TextBlock>("OVMBTText2");  

        alertBox.Text = "Range: " + range.rangeStart.ToString("dd/MM/yyyy") + " To " + range.rangeEnd.ToString("dd/MM/yyyy");  
    }
    public string ReturnNeutralColour() {
        return ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeSecondary"];
    }
}
#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings
#pragma warning restore CS8604 // Re-enabling Warnings
