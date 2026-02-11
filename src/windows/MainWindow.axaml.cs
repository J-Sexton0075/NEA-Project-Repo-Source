using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Calendar;
using Manager;
using Setup;

namespace project_main;

#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Compiler Warning Suppressed -- Dereference of a possibly null reference.
#pragma warning disable CS8604 // Compiler Warning Supressed  -- Possible null reference argument.

public partial class MainWindow : Window {
    // Class definition is not my code, it was implemented by default. 


    // Data Bindings
    public string InitalPageInitialVis => "True";

    public string CalendarPageInitialVis => "False";
    public string CalendarTopbarVis => "False";
    public string CalendarTopbarStreamVis => "False";

    public string EventViewPageInitialVis => "False";
    public string CreatePageInitialVis => "False";
    public string EventEditPageInitialVis => "False";

    public string GoalsPageInitialVis => "False";
    public string GoalsTopbarVis => "False";
    
    // Note that goals and overview pages do not have streamlined topbars. 
    public string OverviewPageInitialVis => "False";
    public string OverviewTopbarVis => "False";

    public string PrefPageInitialVis => "False";
    public string PrefTopbarVis => "False";
    public string PrefTopbarStreamVis => "False";

    public MainWindow() {
        DataContext = this; 
        SetupMain.InitialiseApplication();
        InitializeComponent(); // This function is not my code. It was implemented by default. 
                               // For reference, this is (almost) exactly the same in practice as AvaloniaXamlLoader.Load(this);
                               // (Although functionally it differs if I was making a cross-platform application. )
    }


    // This method is my code. It is used to change the resources to the current colour scheme. 
    public void UpdateColoursFromSettings() {
        string primaryStr;
        string secondaryStr;
        string textStr;
        string accentStr;
        if (!ManagerValues.usingTemporary) {
            primaryStr = ManagerValues.CurrentCalendarPreferences.preferenceColours["schemePrimary"];
            secondaryStr = ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeSecondary"];
            textStr = ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeText"];
            accentStr = ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeAccent"];
        }
        else {
            primaryStr = ManagerValues.temporaryCalendarPreferences.preferenceColours["schemePrimary"];
            secondaryStr = ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeSecondary"];
            textStr = ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeText"];
            accentStr = ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeAccent"];
        }

        // Old unnecessary code that breaks stuff. 
     /*   bool foundPrimary = TryGetResource("ThemePrimary",null, out object? f1);
        bool foundSecondary = TryGetResource("ThemeSecondary", ActualThemeVariant, out object? f2);
        bool foundText = TryGetResource("ThemeText", ActualThemeVariant, out object? f3);
        bool foundAccent = TryGetResource("ThemeAccent", ActualThemeVariant, out object? f4); */

        Resources["ThemePrimary"] =  (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(primaryStr); 
        Resources["ThemeSecondary"] =  (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(secondaryStr);
        Resources["ThemeText"] =  (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(textStr);
        Resources["ThemeAccent"] =  (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(accentStr);
    }


    // This method is my code.
    public void ChangePage(string from, string to) {
        if (from != "PagePref" || (from == "PagePref" && (this.FindControl<DockPanel>(from).FindControl<Grid>("PagePrefGrid").FindControl<UserControl>("PagePrefUC") as PrefPage).AttemptClose())) {

            this.FindControl<DockPanel>(from).IsVisible = false;
            DockPanel makeToVisibile = this.FindControl<DockPanel>(to);
            makeToVisibile.IsVisible = true;

            ManagerValues.CurrentPage = to;

            string fromNorm = from + "Topbar";
            string toNorm = to + "Topbar";
            string fromStream = from + "TopbarStream";
            string toStream = to + "TopbarStream";

            // Unload old topbar (if required)
            if (from != "PageInit") {
                this.FindControl<DockPanel>(fromNorm).IsVisible = false;
                if (from == "PageCalendar" || from == "PagePref") {
                    this.FindControl<DockPanel>(fromStream).IsVisible = false;
                }  
            
            }
            // Load new topbar (if required)
            if (to != "PageInit") {
                if ((to == "PageCalendar" || to == "PagePref") && ManagerValues.CurrentCalendarPreferences.ReturnBoolStreamlined()) {
                    this.FindControl<DockPanel>(toStream).IsVisible = true;
                    this.FindControl<TopbarStream>(toStream + "UC").TBChangeHeaderText(to == "PagePref");
                }
                else {
                    this.FindControl<DockPanel>(toNorm).IsVisible = true;
                }

                // Call initialiser 
                (makeToVisibile.FindControl<Grid>(to + "Grid")
                            .FindControl<UserControl>(to + "UC") as IInitialisable)
                            .InitialisePage();
            }
            // The suffixes Grid and UC are appended to the name of the control to find the specific parts. 
            // This naming scheme is consistent across the namings of the controls in this window and must remain
            // To ensure that the correct controls can be found no matter what page is being initialised. 

            else { // PageInit has different access to the other pages. 
                (makeToVisibile.FindControl<UserControl>(to + "UC") as IInitialisable).InitialisePage();
            }   
        }
    }
    // Note that this may cause issues because it only *shows* the controls rather than initialises them
    // But this can be worked-around as long as I'm aware of this. 
    
    // These methods are in place so I can view and un-view events. 
    public void ChangeViewEvent(Event _event, List<Event>? overlaps) {
        this.FindControl<DockPanel>("PageCalendar").IsVisible = false;
        DockPanel viewEvent = this.FindControl<DockPanel>("PageEventView");
        viewEvent.IsVisible = true;
        (viewEvent.FindControl<UserControl>("PageEventViewUC") as EventView).InitialisePage(_event, overlaps);
    }

    public void DisableViewEvent() {
        this.FindControl<DockPanel>("PageEventView").IsVisible = false;
        this.FindControl<DockPanel>("PageCalendar").IsVisible = true;
    }
    
    public void ChangeViewEventAlternate(Event newEvent) {
        (this.FindControl<DockPanel>("PageEventView")
            .FindControl<UserControl>("PageEventViewUC") as EventView).InitialisePage(newEvent, null); 
    }

    public void EnableCreateEvent() {
        this.FindControl<DockPanel>("PageCalendar").IsVisible = false;
        DockPanel createEvent = this.FindControl<DockPanel>("PageEventCreate");
        createEvent.IsVisible = true;
        (createEvent.FindControl<UserControl>("PageEventCreateUC") as EventCreate).InitialisePage();
    }
    public void DisableCreateEvent() {
        this.FindControl<DockPanel>("PageEventCreate").IsVisible = false;
        this.FindControl<DockPanel>("PageCalendar").IsVisible = true;
    }

    public void EnableEditEvent(Event linked) {
        this.FindControl<DockPanel>("PageCalendar").IsVisible = false;
        this.FindControl<DockPanel>("PageEventView").IsVisible = false;
        DockPanel createEvent = this.FindControl<DockPanel>("PageEventEdit");
        createEvent.IsVisible = true;
        (createEvent.FindControl<UserControl>("PageEventEditUC") as EventEdit).InitialisePage(linked);      
    }
    
    public void DisableEditEvent() {
        this.FindControl<DockPanel>("PageEventEdit").IsVisible = false;
        this.FindControl<DockPanel>("PageCalendar").IsVisible = true;
    }

    // This method is in placed to change the topbar outside of loading/unloading the page
    // Used only in the preferences page. 
    public void EnableDisableTopbar(bool stream) {
        // PagePrefTopbar & PagePrefTopbarStream are the control names
        DockPanel topbar = this.FindControl<DockPanel>("PagePrefTopbar");
        DockPanel topbarStreamlined = this.FindControl<DockPanel>("PagePrefTopbarStream");
        topbar.IsVisible = !stream;
        topbarStreamlined.IsVisible = stream;
    }
}
#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings
#pragma warning restore CS8604 // Re-enabling Warnings
