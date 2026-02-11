using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Calendar;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;
using Manager;

namespace project_main;

#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.
#pragma warning disable CS8604 // Compiler Warning Suppressed --  Possible null reference argument for parameter. 
#pragma warning disable CS8629 // Compiler Warning Suppressed -- Nullable value type may be null.

public partial class EventView : UserControl
{
    public ObservableCollection<string> overlappedEvents = new ObservableCollection<string> {};

    public string overlappedButtonText => "Show Selected Overlapped Event";
    public string checkbuttonText => "Complete Event";


    // Used to store linked event information and the conversion between the title of an overlapped event with an overlapped event. 
    private Event? linkedEvent;

    private IDictionary<string, Event> overlappedConverter = new Dictionary<string, Event> {};

    public EventView()
    {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);
    }

    public void InitialisePage(Event _event, List<Event>? overlaps) {
         
        // Initialise Details
        linkedEvent = _event;

        // Clear previous details. 
        overlappedEvents.Clear();
        overlappedConverter.Clear();

        // Fetch required elements.
        // There's lots of these here, but that's only because I need to edit many parts of the page based on event details. 
        TextBlock textTitle = this.FindControl<TextBlock>("EVTitleText");
        TextBlock textStartTime = this.FindControl<TextBlock>("EVStartTime");
        TextBlock textEndTime = this.FindControl<TextBlock>("EVEndTime");
        TextBlock textType = this.FindControl<TextBlock>("EVType");         
        TextBlock textPriority = this.FindControl<TextBlock>("EVPriority");
        TextBlock textOverlap = this.FindControl<TextBlock>("EVIsOverlap");
        TextBlock textComplete = this.FindControl<TextBlock>("EVIsComplete");
        TextBlock textDescription = this.FindControl<TextBlock>("EVDescBox");

        DockPanel overlappedPanel = this.FindControl<DockPanel>("EVOverlaps");
        ComboBox comboOverlapped = this.FindControl<ComboBox>("EVOverlappedEvents");
                                          
        DockPanel checkBoxPanel = this.FindControl<DockPanel>("EVComplete");
        CheckBox checkBox = this.FindControl<CheckBox>("EVCompleteEvent");

        // Decide whether or not to display the overlaps panel. If so, add all the relevant overlapped events to both the ComboBox
        // And the overlappedConveter dictionary so they can be converted between strings and the Event object. 
        if (overlaps != null && overlaps.Count != 0 ) {
            overlappedPanel.IsVisible = true;
            foreach (Event overlapped in overlaps) {
                overlappedEvents.Add(overlapped.eventTitle + " " + overlapped.eventID); // Keys here are a composite with title and ID to ensure they are unique and to also ensure 
                                                                                        // Users can identify the event. 
                overlappedConverter.Add(new KeyValuePair<string, Event>(overlapped.eventTitle + " " + overlapped.eventID, overlapped));
            }
            comboOverlapped.ItemsSource = overlappedEvents;
        }
        else {
            overlappedPanel.IsVisible = false;
        }

        // Add all required details to the page. 

        if (!_event.eventIsCompleteable) { // Disable the checkbox panel from being visible.
            checkBoxPanel.IsVisible = false;
        }
        else {
            checkBoxPanel.IsVisible = true;
        }
        // Add in neccasary text. 
        textTitle.Text = _event.eventTitle;
        textStartTime.Text = "Start Time: " + _event.startTime.ToString("dd/MM/yy HH:mm");
        textEndTime.Text = "End Time: " + _event.endTime.ToString("dd/MM/yy HH:mm");
        Converters.eventName_TO_eventTypeString.TryGetValue(_event.eventType, out string typeString);
        typeString ??= _event.eventType;
        textType.Text = "Event Type: " + typeString;
        textPriority.Text = "Event Priority: " + _event.eventPriority.ToString();
        textOverlap.Text =  (overlaps == null || overlaps.Count == 0) ? "No Overlapping Events" : "Overlapping Events!";
        textComplete.Text = _event.eventIsCompleteable ? (_event.eventIsCompleted ? "Event is Complete" : "Event is Incomplete" ) 
                                                       : (_event.eventIsStudy ? "Study Event" : "Event Not Completable");
        textDescription.Text = _event.eventDesc;
        checkBox.IsChecked = _event.eventIsCompleted;
    }

    public void OverlappedButtonOnClick(object sender, RoutedEventArgs args) {
            // Fetch combo box. Convert back to event and then change the view (reinitialise) to the new event. 
            ComboBox comboBox = this.FindControl<ComboBox>("EVOverlappedEvents");
        if (comboBox.SelectedItem != null) {
            (Parent.Parent.Parent as MainWindow).ChangeViewEventAlternate(overlappedConverter[comboBox.SelectedItem as string]);
        }
    }   
    public void CompleteCheckboxOnCLick(object sender, RoutedEventArgs args) {
        // Neccasary elements. 
        CheckBox boxClicked = args.Source as CheckBox;
        int checkedValue = (bool)boxClicked.IsChecked ? 1 : 0;
        
        // Connect and access the database. Update the value instantaneously when clicked. 
        
        using (SqliteConnection connection = new SqliteConnection($"Data Source={ManagerValues.CurrentCalendarPreferences.database}")) {
            connection.Open();
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = 
            @$"
                UPDATE table_events
                SET IsComplete = {checkedValue}
                WHERE EventId = {linkedEvent.eventID}
            "; // Used string interpolation rather than inserting parametrs via command.Paramters as I find it more reliable.
            command.ExecuteNonQuery();
            connection.Close();

            // Refresh range. 
            (Parent.Parent as Grid).FindControl<CalendarPage>("PageCalendarUC").UpdateCalendarPage(null!, null!);
        }
    }

    public void EditOnClick(object sender, RoutedEventArgs args) {
        (Parent.Parent.Parent as MainWindow).EnableEditEvent(linkedEvent!);
    }
    
    public void ExitOnClick(object sender, RoutedEventArgs args) {
        // Simply just disables this view page. 
        (Parent.Parent.Parent as MainWindow).DisableViewEvent();
    }
}
#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings
#pragma warning restore CS8604 // Re-enabling Warnings
#pragma warning restore CS8629 // Re-enabling Warnings