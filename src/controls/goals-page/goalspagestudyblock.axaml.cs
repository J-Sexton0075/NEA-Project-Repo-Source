using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Calendar;
using Microsoft.Data.Sqlite;
using Manager;
using Avalonia.VisualTree;

namespace project_main;

#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.

public partial class GoalsPageStudyBlock : UserControl  {
    public Event linkedEvent;

    // Bindings
    public bool GPSBClearDefaultVis => false;
    public string SaveButton => "Save";
    public string DeleteButton => "Delete";

    public GoalsPageStudyBlock(Event link) {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);

        linkedEvent = link;

        // Get UI elements
        TextBlock text = this.FindControl<TextBlock>("GPSBText");
        CheckBox cbComplete = this.FindControl<CheckBox>("GPSBCheckComplete");
        CheckBox cbIncomplete = this.FindControl<CheckBox>("GPSBCheckIncomplete");
        CheckBox cbPartial = this.FindControl<CheckBox>("GPSBCheckPartial");
        TextBox percentageBox = this.FindControl<TextBox>("PartialPercentage");
        Button save = this.FindControl<Button>("GPSBSave");
        Button clearSurplus = this.FindControl<Button>("GPSBClearSurplus");
        
        // Generate and add the correct text to the page. 
        text.Text = linkedEvent.eventTitle + "- " + (linkedEvent.endTime - linkedEvent.startTime).TotalHours + " Hours - " +  linkedEvent.startTime.ToString("dd/MM/yyyy");

        // Change visibility of the elements based on whether its a surplus event or not. 
        bool isSurplus = linkedEvent.eventType == "eventSurplus";
        cbComplete.IsVisible = !isSurplus;
        cbIncomplete.IsVisible = !isSurplus;
        cbPartial.IsVisible = !isSurplus;
        percentageBox.IsVisible = !isSurplus;
        save.IsVisible = !isSurplus;

        clearSurplus.IsVisible = isSurplus;

        // Change status of the boxes. Make the correct elements active. 
        if (linkedEvent.studyCompleted == 1) {
            cbComplete.IsChecked = true;
            percentageBox.Text = "100";   
            percentageBox.IsEnabled = false;         
        }   
        else if (linkedEvent.studyCompleted == 0) {
            cbIncomplete.IsChecked = true;
            percentageBox.Text = "0";
            percentageBox.IsEnabled = false;
        }
        else {
            cbPartial.IsChecked = true;
            percentageBox.Text = (linkedEvent.studyCompleted * 100).ToString();
        }     
    }

    public void CheckedHandler (object sender, RoutedEventArgs args) {
        // Handles the check boxes changing. 
        CheckBox cbComplete = this.FindControl<CheckBox>("GPSBCheckComplete");
        CheckBox cbIncomplete = this.FindControl<CheckBox>("GPSBCheckIncomplete");
        CheckBox cbPartial = this.FindControl<CheckBox>("GPSBCheckPartial");
        TextBox percentageBox = this.FindControl<TextBox>("PartialPercentage");


        // Disable other check boxes.
        string name = (sender as CheckBox).Name;
        if (name == "GPSBCheckComplete") {
            cbComplete.IsChecked = true;
            cbIncomplete.IsChecked = false;
            cbPartial.IsChecked = false;
            percentageBox.Text = "100";   
            percentageBox.IsEnabled = false;   
        }
        else if (name == "GPSBCheckIncomplete") {
            cbComplete.IsChecked = false;
            cbIncomplete.IsChecked = true;
            cbPartial.IsChecked = false;
            percentageBox.Text = "0";
            percentageBox.IsEnabled = false;
        }
        else {
            cbIncomplete.IsChecked = false;
            cbComplete.IsChecked = false;
            cbPartial.IsChecked = true;
            percentageBox.Text = (linkedEvent.studyCompleted * 100).ToString();
            percentageBox.IsEnabled = true; 
        }
    }

    public void SaveHandler(object sender, RoutedEventArgs args) {
        // Save changes of event to database.
        // Since the buttons update the check box, all I need to do is take the value in the text box, validate it and then divide by 100. 
        TextBox textBox =  this.FindControl<TextBox>("PartialPercentage");

        bool isValid = double.TryParse(textBox.Text, out double val); // Validates for numeric value. 
        if (!isValid) {
            CallNotifyError("The percentage complete must be numeric");
        }
        else if (val > 100 || val < 0) { // Validate magnitude.
            CallNotifyError("The percentage complete must be between 0 and 100");
        }   
        else { // Is valid, save and commit to database. 
            linkedEvent.studyCompleted = val / 100; 

            // Commit to database to save the event. 
            using (SqliteConnection connection = new SqliteConnection($"Data Source={ManagerValues.CurrentCalendarPreferences.database}")) {
                connection.Open();
                
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = 
                @"
                    UPDATE table_events
                    SET StudyComplete = @studyComplete
                    WHERE EventID = @eventID;
                ";

                command.Parameters.AddWithValue("@studyComplete", linkedEvent.studyCompleted);
                command.Parameters.AddWithValue("@eventID", linkedEvent.eventID);

                command.ExecuteNonQuery();

                connection.Close();
            }
            CallUpdatePage();
        }
    }

    public void DeleteHandler (object sender, RoutedEventArgs args) {
        // Clear the linked surplus event. Additional validation to ensure it's surplus???
        if (linkedEvent.eventType == "eventSurplus") {
            // Delete from the database. 
            using (SqliteConnection connection = new SqliteConnection($"Data Source={ManagerValues.CurrentCalendarPreferences.database}")) {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = 
                @"
                    DELETE FROM table_events
                    WHERE EventID = @eventID
                ";
                command.Parameters.AddWithValue("@eventID", linkedEvent.eventID);
                command.ExecuteNonQuery();

                connection.Close();
            }
            CallDeleteEvent(this);

        }
        else {
            CallNotifyError("Attempted to delete a non-surplus event");
        }
    }


    // These methods are just for accessing and calling other methods on the main Goals Page. 
    public void CallNotifyError(string text) {
        // Calls notify error in the goals page
        this.FindAncestorOfType<GoalsPage>().NotifyError_Logging(text);
    }

    public void CallUpdatePage() {
        this.FindAncestorOfType<GoalsPage>().InitialiseLogging();
    }

    public void CallDeleteEvent(Control toDelete) {
        this.FindAncestorOfType<GoalsPage>().RemoveSurplus(toDelete);
    }   
}
#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings