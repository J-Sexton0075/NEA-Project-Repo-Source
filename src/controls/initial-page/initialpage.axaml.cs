using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Creation;

namespace project_main;

#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.

// Class definition was copied from MainWindow. 
public partial class InitialPage : UserControl, IInitialisable {

    private bool hasCalendars = true;

    public string Message => "Select a calendar to load:";
    public string TextBoxVisibility => "False";
    public string Button1Text => "Load Calendar";
    public string Button2Text => "New Calendar";

    public ObservableCollection<string>? CalendarList { get; set; }


    private bool routeSwitch = false;


    public InitialPage() {
        DataContext = this; // Allows for data binding
        GetAllCalendars();
        AvaloniaXamlLoader.Load(this); // Loads the xaml so you can actually see it.
    }

    public void InitialisePage() {
        // I would nullify the objects here to prevent potential conflicting issues but this causes a crash when I try to access them later
        // As the objects no longer exist. 

        // Get all calendars again (refresh the collection)
        GetAllCalendars();
        // Update visuals
        this.FindControl<ComboBox>("CalendarSelectionBox").ItemsSource = CalendarList;

        // Clear text box
        this.FindControl<TextBox>("NewCalendarInput").Text = "";
        
    }




    // Gets all the user's calendars from the 
    void GetAllCalendars() {
        if (Manager.ManagerValues.RootManager != null) {

            if (Manager.ManagerValues.RootManager.calendars.Count != 0) {
                CalendarList = []; // Simplest definition of a collection. Much easier than the whole new ObservableCollection<string> {} alternative.
                foreach (string calendar in Manager.ManagerValues.RootManager.calendars.Keys) {
                    CalendarList.Add(calendar);
                }
            }
            else { // The user has no calendars and must create one
                hasCalendars = false;
                CalendarList = ["You have no calendars!"];
            }

        }
        else {
            // This shouldn't ever be thrown, but if it is I can pick it up and fix 
            // It. 
            throw new System.Exception("Initialisation Error: Rootmanager is not initialised!");
        }
    }

    void LoadCalendarClick(object sender, RoutedEventArgs args) {
        Button? myButton = this.FindControl<Button>("LoadCalendarButton");
        ComboBox? myComboBox = this.FindControl<ComboBox>("CalendarSelectionBox");
        // Validate that the user has calendars.
        if (hasCalendars == false) {
            myButton.Content = "You have no calendars to load!";
        }
        else {
            // Get selected calendar and load this. 
            string? myCalendarName;
            
            try {
                myCalendarName = myComboBox.SelectedItem.ToString();
            }
            catch (System.NullReferenceException) {
                myCalendarName = null;
            }   
            
            
            if (myCalendarName == null) {
                myButton.Content = "Please select a calendar to load!";
            }
            else {
                CalendarCreator.LoadCalendar(myCalendarName);
                (Parent.Parent.Parent as MainWindow).UpdateColoursFromSettings();
                // Change the page to PageCalendar. 
                (Parent.Parent.Parent as MainWindow).ChangePage("PageInit", "PageCalendar");
                /* Check MainWindow.axaml to see the structure in xaml. 
                    It's like this:
                    >Main Window 
                    >> Grid -- Which, by the way is used to overlap the UI elements
                    >>> DockPanel 
                    >>>> PageControl
                    >>> DockPanel
                    Where each DockPanel is a page. 
                    So Parent.Parent.Parent refers to 
                    This (InitialPage) . 
                    Parent (DockPanel) .
                    Parent (Grid) .
                    Parent (MainWindow)

                    And from there I can access the method.
                */
            }
        }

    }

    void NewCalendarClick(object sender, RoutedEventArgs args) {
        TextBox? myBox = this.FindControl<TextBox>("NewCalendarInput");
        Button? myButton = this.FindControl<Button>("NewCalendarButton");
        if (!routeSwitch) {
            routeSwitch = true;
            if (myBox != null) {
                myBox.IsVisible = true;
            }
            if (myButton != null) {
                myButton.Content = "Input Calendar Name";
            }
        } // routeSwitch manages the switch between the button opening the textbox to input the name and actually inputting 
          // The name of the calendar (and then creating it). 
          // Above you see the swap of the UI elements
          // Below you see initialisation of the calendar. 
        
        else {
            if (myButton != null && myBox != null && Manager.ManagerValues.RootManager != null) {
                string inputtedName = myBox.Text ?? "";
                if (inputtedName == "") {
               /*     || inputtedName.Any(char.IsWhiteSpace)
                    || Manager.ManagerValues.RootManager.calendars.ContainsKey(inputtedName)
                    || inputtedName.Length > 25) {*/
                    /* These are the four rules for validation :
                        Name isn't  blank
                        Name contains no whitespace characters
                        Name isn't the name of another calendar
                        Name isn't more than 25 characters. 
                    */
                    // Validation Failed
                    myButton.Content = "Invalid Name (Name is empty)";
                }
                else if (inputtedName.Any(char.IsWhiteSpace)) {
                    myButton.Content = "Invalid Name (Contains whitespace)";
                }
                else if (Manager.ManagerValues.RootManager.calendars.ContainsKey(inputtedName)) {
                    myButton.Content = "Invalid Name (Already exists)";
                }
                else if (inputtedName.Length > 25) {
                    myButton.Content = "Invalid Name (Too long)";
                }
                else {
                    myButton.Content = Button2Text;
                    myBox.IsVisible = false;
                    routeSwitch = false;
                    // Calls two functions. The InitialiseCalendar function which makes the core Json file
                    // And the LoadCalendar function which both initialises the scheduler and the database
                    // Before loading it.
                    CalendarCreator.InitialiseCalendar(inputtedName);
                    CalendarCreator.LoadCalendar(inputtedName);

                    // See explanation above for this one. 
                    this.FindAncestorOfType<MainWindow>().ChangePage("PageInit", "PageCalendar");
                }

            }
        }
    }
}
#pragma warning restore CS8602 // Re-enabling Warnings