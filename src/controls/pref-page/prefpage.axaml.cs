using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Manager;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Avalonia.Interactivity;
using Setup;
using Microsoft.Data.Sqlite;
using Avalonia.VisualTree;

namespace project_main;

#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8601 // Compiler Warning Supressed --  Possible null reference assignment.
#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.
#pragma warning disable CS8604 // Compiler Warning Suppressed --  Possible null reference argument for parameter. 
#pragma warning disable CS8629 // Compiler Warning Suppressed -- Nullable value type may be null.

public partial class PrefPage : UserControl, IInitialisable {

    // Bindings
    public string TypeText => "Event Types: ";
    public string PreviewText => "Preview";
    public string StreamlinedModeText => "Streamlined Mode";

    public string SaveButtonText => "Save Changes";
    public string DeleteButtonText => "Delete Calendar";
    public string ClearButtonText => "Clear Changes";

    public string HexTextWatermark => "#HexCode";
    public string TypeTextWatermark => "Custom Type Name";

    public string SubmitCustomColourButtonText => "Select Custom Scheme";

    public string CustomTypeButtonText => "Add Custom Type";

    public string PMCCPrimaryText => "Primary";
    public string PMCCSecondaryText => "Secondary";
    public string PMCCTextText => "Text";
    public string PMCCAccentText => "Accent";
    public string PMCCAlertTopText => "Alert Scale -- Top";
    public string PMCCAlertMidText => "Alert Scale -- Mid";
    public string PMCCAlertLowText => "Alert Scale -- Low";


    // Other Variables
    bool preferencesEdited = false;
    int deletePressed = 12; // Records how many times the delete button has been pressed. Resets upon saving/clearing changes.

    public PrefPage() {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);

        SetColourSchemes(); // Sets the preset colour schemes for the user to choose from. 
    }
    
    public void InitialisePage() { 
        // Sets up the page. Also has a couple commented out old test things that I used when trying to make sure everything was saved/set up correctly for this page. 
        deletePressed = 12;
        ClearValues();
        ActivateTemporary();
       // TestMethod(); 
        ColourSchemeSetRefresh();
        LoadEventTypes();
        LoadStreamlined(); 
 //       bool test = false; // Old testing code
  //      test = CheckIfHexCode("#abcdfe");
    }

    public void DeleteButtonPressed(object sender, RoutedEventArgs args) {
        deletePressed --;
        NotifyError("Are you sure you want to delete the calendar? This cannot be undone."); // Warns the user, they will have to press the button 12 times for the calendar to actually be deleted. 
        SetDeleteButtonContent();
        if (deletePressed == 0) {
            // Calendar can be deleted now
            // Start with temporary file, then delete everything else one by one. 
            string name = ManagerValues.CurrentCalendarPreferences.calendarName;
            string pathTemp = ManagerValues.temporaryCalendarPreferences.thisPath;
            string pathMain = ManagerValues.CurrentCalendarPreferences.thisPath;
            string pathDB = ManagerValues.CurrentCalendarPreferences.database;
            string pathScheduler = ManagerValues.CurrentCalendarPreferences.scheduler;
            
            // Update the general manager. Both the object and the file.
            ManagerValues.RootManager.calendars.Remove(name);
            string json = JsonConvert.SerializeObject(ManagerValues.RootManager);
            File.WriteAllText(SetupMain.managerPath, json);


            SqliteConnection.ClearAllPools(); // This is required to make sure everything related to the database is correctly closed since there still can be various open connections.
            // Delete the files. 
            File.Delete(pathDB);
            File.Delete(pathScheduler);
            File.Delete(pathTemp);
            File.Delete(pathMain);

    
            // Return to initial page. 
            this.FindAncestorOfType<MainWindow>().ChangePage("PagePref", "PageInit");
        }
    }

    public void SetDeleteButtonContent() { // This method is just used to change the content of the delete button. It has no additional purpose. 
        Button button = this.FindControl<Button>("PMO2DeleteButton");
        if (deletePressed == 12) {
            button.Content = DeleteButtonText;
        }
        else {
            button.Content = $"Click the button {deletePressed} times.";
        }
    }


    public void SaveChanges(object sender, RoutedEventArgs args) {
        // Commits the changes to the files in the order:
        // -- Temporary File --> Main Object --> Main File
        if (ManagerValues.usingTemporary){
            NotifyError("Settings Saved!");
            string path = ManagerValues.CurrentCalendarPreferences.thisPath;
            string temporaryPath = ManagerValues.temporaryCalendarPreferences.thisPath;
            // Save to temporary
            ManagerValues.temporaryCalendarPreferences.thisPath = path; // Reset the file path.
            // Write to file
            string savedJson = JsonConvert.SerializeObject(ManagerValues.temporaryCalendarPreferences);
            File.WriteAllText(temporaryPath, savedJson);
            // Copy to current.
            ManagerValues.CurrentCalendarPreferences = ManagerValues.temporaryCalendarPreferences;
            File.WriteAllText(path, savedJson);

            // Reset Values
            File.Delete(temporaryPath); // Deletes the temporary file since it's currently not needed. A new one will be created in a second. 
            ActivateTemporary(); // This is the only thing needed to correctly update the calendar. Attempting to call more than this can cause major issues such as 
            // The incorrect file being deleted. 
            
        }
        else {
            NotifyError("Cannot save settings while temporary values are not in use.");
        }
    } 

    public void ClearChanges(object sender, RoutedEventArgs args) {
        // Resets values to default. Luckily, this can pretty easily be done just with a sequence of the other methods I have and requires nothing extra
        // Other than resetting the deletePressed count. 
        NotifyError("Changes Cleared.");
        deletePressed = 12;
        DeactivateTemporary();
        ClearValues();
        ActivateTemporary();
        ColourSchemeSetRefresh();
        LoadEventTypes();
        LoadStreamlined();
    }

    public void VisualStreamlinedUpdate(bool isOn) {
        // Updates the streamlined topbar to the appropriate state (on/off)
        this.FindAncestorOfType<MainWindow>().EnableDisableTopbar(isOn);
    }

    public void ChangeStreamlined(object sender, RoutedEventArgs args) {
        // Called when the switch is changed. Will just get the value from the passed in sender, update the file and then update the page visually. 
        if (ManagerValues.usingTemporary) {
            preferencesEdited = true;
            ManagerValues.temporaryCalendarPreferences.streamlined = (sender as ToggleSwitch).IsChecked.ToString();
            VisualStreamlinedUpdate((bool)(sender as ToggleSwitch).IsChecked);
        }
        else {
            (sender as ToggleSwitch).IsChecked = false;
            NotifyError("Cannot change streamlined while temporary values are not in use.");
        }
    }

    public void DeleteEventType(PrefPageEventType toDelete, string typeName) {
        // Similar to other methods for deleting little visual 'blocks', this uses the same technique to delete them, however I also need to remove it from the file.
        if (ManagerValues.usingTemporary) {
            // Delete from file.
            Dictionary<string, string> willDelete = new Dictionary<string, string> { };
            preferencesEdited = true;
            foreach (Dictionary<string, string> type in ManagerValues.temporaryCalendarPreferences.customTypes) {
                if (type["name"] == typeName) {
                    willDelete = type;
                }
            }
            ManagerValues.temporaryCalendarPreferences.customTypes.Remove(willDelete);

            // Delete visually  
            this.FindControl<DockPanel>("PMTTypePanel").Children.Remove(toDelete);

            // Refresh
            LoadEventTypes();
        }
        else {
            NotifyError("Cannot delete event types if temporary values are not in use.");
        }
    }

    public void AddCustomType(object sender, RoutedEventArgs args) {
        // Adds a small custom type element to the list of custom types, and also will have to add this to the file.
        // There's quite a lot of validation for this one since custom types must have a unique, non empty name, that cannot be in the same format as the internal code for preset event types
        // And a valid hex code must be passed in as the colour. 
        if (ManagerValues.usingTemporary) {
            // Get Elements
            string name = this.FindControl<TextBox>("PMONTTypeName").Text;
            string hex = this.FindControl<TextBox>("PMONTTypeHex").Text;          
            // Validate -- There's quite a few stages of validation for this, which I'll process sequentially. 
            if (CheckIfHexCode(hex)) {
                if (name != "") {
                    if (name.Length < 5 || name[0..5].ToLower() != "event") {// e0 v1 e2 n3 t4
                        List<string> customTypeNames = new List<string> { };
                        foreach (Dictionary<string, string> s in ManagerValues.temporaryCalendarPreferences.customTypes) {
                            customTypeNames.Add(s["name"]);
                        }
                        if (!customTypeNames.Contains(name)) {
                            // Validated, can add the new type.
                            preferencesEdited = true;
                            IDictionary<string, string> newCustomType = new Dictionary<string, string> {
                                {"name", name},
                                {"colour", hex},
                            };

                            // Add to the settings.
                            ManagerValues.temporaryCalendarPreferences.customTypes.Add(newCustomType);

                            // Refresh Page
                            LoadEventTypes();
                            this.FindControl<TextBox>("PMONTTypeName").Text = "";
                            this.FindControl<TextBox>("PMONTTypeHex").Text = "";
                        }
                        else {
                            NotifyError("A custom type of this name already exists.");
                        }
                    }
                    else {
                        NotifyError("Custom type name cannot be prepended with the string 'event'.");
                    }
                }
                else {
                    NotifyError("Custom type name cannot be blank.");
                }
            }
            else {
                NotifyError("Invalid hex code for custom type.");
            }
        }
        else {
            NotifyError("Cannot add a custom type if temporary values are not in use.");
        }

    }

    public void CustomTypePreviewUpdated(object sender, RoutedEventArgs args) {
        // This method is called when a hex code for custom types is inputted. Since you cannot view the colour without adding a custom type, 
        // I thought it'd be helpful to let users see this beforehand, so if there is a valid hex code inputted this will preview the colour. 
        if (ManagerValues.usingTemporary) {
            string text = (sender as TextBox).Text;
            if (CheckIfHexCode(text)) {
                this.FindControl<TextBlock>("PMONTPreview").Background = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(text);
            }
            else { // Defaults to the secondary colour if there's not a valid input.
                this.FindControl<TextBlock>("PMONTPreview").Background = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeSecondary"]);
            }
        }
        else {
            NotifyError("Cannot preview custom type colour if temporary values are not in use.");
        }
    }

    public void SetColourSchemeType(string type, string colour) {
        // Sets the colour of an event when it has been changed. This is not the method to do this visually, but rather to set this in the settings file. 
        // The method to do this visually is in prefpagecolourscheme.axaml.cs
        // Hence the need to validate the values again. 
        if (ManagerValues.usingTemporary) {
            NotifyError("");
            // This is double validated, once to set the colour within prefpageeventtype and another to notify the user of errors.
            if (CheckIfHexCode(colour)) { // Value must be a valid hex code for a colour. 
                preferencesEdited = true;
                // Valid
                if (type == "eventSchool" || type == "eventStudy" || type == "eventWork" || type == "eventFree") {
                    // Preset type, so just change the colour in the settings.
                    ManagerValues.temporaryCalendarPreferences.typeColours[type] = colour;
                }
                else {
                    // Custom type, requires a bit more effort to locate. 
                    List<IDictionary<string, string>> customTypes = ManagerValues.temporaryCalendarPreferences.customTypes;
                    foreach (IDictionary<string, string> customType in customTypes) {
                        if (customType["name"] == type) { // Linear search on the custom types until I find the right one. 
                                                        // Potentially a better way to do this perhaps with LINQ but this works well enough
                            customType["colour"] = colour;
                        }
                    }
                    
                }
            }
            else {
                NotifyError("Invalid hex code in updating event type.");
            }
        }
        else {
            NotifyError("Cannot set type colours if temporary values are not in use.");
        }
    }

    public void ColourSchemeSetRefresh() { // Contains all the methods that need to be ran after setting the colour scheme. 
        RefreshCustomColours(); // Have updated this method to be after ActivateTemporary and to take temporary values into account.
        this.FindAncestorOfType<MainWindow>().UpdateColoursFromSettings(); // Updates the colour scheme using UserResources based on the just-updated settings.
    }

    public void SetColourSchemeCustom(object sender, RoutedEventArgs args) {
        // Sets the colour scheme to a custom scheme defined fully by the user. 
        NotifyError("");
        if (ManagerValues.usingTemporary) {
            preferencesEdited = true;
            // Get elements
        
            string inputPrimary = this.FindControl<TextBox>("PMCCHexPrimary").Text;
            string inputSecondary = this.FindControl<TextBox>("PMCCHexSecondary").Text;
            string inputText = this.FindControl<TextBox>("PMCCHexText").Text;
            string inputAccent = this.FindControl<TextBox>("PMCCHexAccent").Text;
            string inputAlertTop = this.FindControl<TextBox>("PMCCHexAlertTop").Text;
            string inputAlertMid = this.FindControl<TextBox>("PMCCHexAlertMid").Text;
            string inputAlertLow = this.FindControl<TextBox>("PMCCHexAlertLow").Text;

            // Validate inputs. Ensures they are all valid hex codes. 
            if (
                CheckIfHexCode(inputPrimary) &&
                CheckIfHexCode(inputSecondary) &&
                CheckIfHexCode(inputText) &&
                CheckIfHexCode(inputAccent) &&
                CheckIfHexCode(inputAlertTop) &&
                CheckIfHexCode(inputAlertMid) &&
                CheckIfHexCode(inputAlertLow)
            ) {
                // Set values
                ManagerValues.temporaryCalendarPreferences.preferenceColours["schemePrimary"] = inputPrimary;
                ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeSecondary"] = inputSecondary;
                ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeText"] = inputText;
                ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeAccent"] = inputAccent;
                ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeAlertTop"] = inputAlertTop;
                ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeAlertMid"] = inputAlertMid;
                ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeAlertLow"] = inputAlertLow;

                // Refresh as neccasary. 
                ColourSchemeSetRefresh();
            }
            else {
                NotifyError("Input contains invalid hex code.");
            }

        }
        else {
            NotifyError("Cannot set colour scheme if temporary values are not in use.");
        }
    }

    public static bool CheckIfHexCode(string input) { // Checks if an inputted value is a hex code.
        return Regex.IsMatch(input, "^#([0-9a-fA-F]{6})$");
    }

    public void SetColourSchemePreset(PrefPageColourScheme scheme) {
        // Simpler method to set a colour scheme, when selected from one of the eight custom colour scheme options. 
        if (ManagerValues.usingTemporary) {
            preferencesEdited = true;
            // Update Settings
            ManagerValues.temporaryCalendarPreferences.preferenceColours["schemePrimary"] = scheme.primary;
            ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeSecondary"] = scheme.secondary;
            ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeText"] = scheme.text;
            ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeAccent"] = scheme.accent;

            // Call necessary refresh methods
            ColourSchemeSetRefresh();
        }
        else {
            NotifyError("Cannot set colour scheme if temporary values are not in use.");
        }
    }

    public void LoadStreamlined() {
        // Loads the current streamlined value upon initialisation or resetting of the page. 
        if (ManagerValues.usingTemporary) {
            this.FindControl<ToggleSwitch>("PMO1StreamlinedSwitch").IsChecked = 
            ManagerValues.temporaryCalendarPreferences.ReturnBoolStreamlined();
            VisualStreamlinedUpdate(ManagerValues.temporaryCalendarPreferences.ReturnBoolStreamlined());
            
        }
        else {
            NotifyError("Cannot load streamlined value if temporary values are not in use.");
        }
    }
    

    // IGNORE
    /*public void TestMethod() { // Used to temporarily set a value to test something. Not a permanent method 
        ManagerValues.temporaryCalendarPreferences.customTypes.Add(
            new Dictionary<string, string> {
                {"name", "CustomEventType"},
                {"colour", "#57e190"}
            }
        );
    } */

    // Back to code. 


    // Clears the values from the page, used upon initialisation.
    public void ClearValues() {
        // Clear neccasary values 
        this.FindControl<DockPanel>("PMTTypePanel").Children.Clear(); 

        this.FindControl<TextBlock>("PMOErrorBlock").Text = "";
        this.FindControl<TextBox>("PMONTTypeName").Text = "";
        this.FindControl<TextBox>("PMONTTypeHex").Text = "";
        this.FindControl<TextBlock>("PMONTPreview").Background = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(ManagerValues.CurrentCalendarPreferences.preferenceColours["schemeSecondary"]);
    }

    public void LoadEventTypes() {
        // Loads the event types that are saved in settings, and the default ones, with their correct colours. 
        // Does this by adding another control to a panel for each of the elements. 
        if (ManagerValues.usingTemporary) {
            DockPanel panel = this.FindControl<DockPanel>("PMTTypePanel");
            // Clear original contents
            panel.Children.Clear();

            // Add default event types. 
            panel.Children.Add(
                new PrefPageEventType("Study", ManagerValues.temporaryCalendarPreferences.typeColours["eventStudy"], false, "eventStudy")
            );
            panel.Children.Add(
                new PrefPageEventType("Work", ManagerValues.temporaryCalendarPreferences.typeColours["eventWork"], false, "eventWork")
            );
            panel.Children.Add(
                new PrefPageEventType("School/Homework", ManagerValues.temporaryCalendarPreferences.typeColours["eventSchool"], false, "eventSchool")
            );
            panel.Children.Add(
                new PrefPageEventType("Free Time", ManagerValues.temporaryCalendarPreferences.typeColours["eventFree"], false, "eventFree")
            );

            // Add custom event types 
            foreach (Dictionary<string, string> custom in ManagerValues.temporaryCalendarPreferences.customTypes) {
                panel.Children.Add(new PrefPageEventType(custom["name"], custom["colour"], true, custom["name"]));
            }

        }
        else {
            NotifyError("Cannot load event types if temporary values are not in use.");
        }
    }

    public bool AttemptClose() {
        // Attempts to close the page. Only allows this to happen if preferences are not edited, else it stops this from happening. 
        if (preferencesEdited) {
            // There are edits in place. You are unable to close the page right now. 
            NotifyError("You cannot close the page with unsaved changes.");
            return false;
        }
        else {
            // Deactivate temporary file. 
            DeactivateTemporary();
            return true;
        }
    }

    public void NotifyError(string error) { // Basic method for showing a text-based message to the user, usually used for error handling. 
        TextBlock errorBlock = this.FindControl<TextBlock>("PMOErrorBlock");
        errorBlock.Text = error;
    }

    public void ActivateTemporary() {
        // Enables the temporary file system. This also resets changes in the temporary file since it copies it over from the main calendar preferences.
        ManagerValues.usingTemporary = true;

        // Create a copy of the preferences file.
        string tempPath = ManagerValues.CurrentCalendarPreferences.thisPath + ".TEMPFILE.json";
        File.Copy(ManagerValues.CurrentCalendarPreferences.thisPath, tempPath);

        string json = File.ReadAllText(tempPath);
        ManagerValues.temporaryCalendarPreferences = JsonConvert.DeserializeObject<Calendar_CalendarPreferences>(json);
        ManagerValues.temporaryCalendarPreferences.thisPath = tempPath;
        // This variable is used to track whether the page has been edited or not. 
        // If an edit occurs this is set to true, and the user may not leave the page without saving / clearing / cancelling the leave.
        preferencesEdited = false;
    }

    public void DeactivateTemporary() {
        // Disables the temporary file, and deletes it if it still exists. 
        ManagerValues.usingTemporary = false;
    
        // Delete File
        if (File.Exists(ManagerValues.temporaryCalendarPreferences.thisPath)) {
            File.Delete(ManagerValues.temporaryCalendarPreferences.thisPath);
        }
        ManagerValues.temporaryCalendarPreferences = null;
    }

    public void RefreshCustomColours() {
        // Refreshes the custom colours in the grid for editing colours,  shown to the user after they're either changed to a cutstom or preset scheme.
        if (ManagerValues.usingTemporary) {
            // There's plenty of elements to fetch here. 

            TextBox primaryBox = this.FindControl<TextBox>("PMCCHexPrimary");
            Avalonia.Controls.Shapes.Rectangle primaryPreview = this.FindControl<Avalonia.Controls.Shapes.Rectangle>("PMCCPreviewPrimary");

            TextBox secondaryBox = this.FindControl<TextBox>("PMCCHexSecondary");
            Avalonia.Controls.Shapes.Rectangle secondaryPreview = this.FindControl<Avalonia.Controls.Shapes.Rectangle>("PMCCPreviewSecondary");

            TextBox textBox = this.FindControl<TextBox>("PMCCHexText");
            Avalonia.Controls.Shapes.Rectangle textPreview = this.FindControl<Avalonia.Controls.Shapes.Rectangle>("PMCCPreviewText");

            TextBox accentBox = this.FindControl<TextBox>("PMCCHexAccent");
            Avalonia.Controls.Shapes.Rectangle accentPreview = this.FindControl<Avalonia.Controls.Shapes.Rectangle>("PMCCPreviewAccent");

            TextBox alerttopBox = this.FindControl<TextBox>("PMCCHexAlertTop");
            Avalonia.Controls.Shapes.Rectangle alerttopPreview = this.FindControl<Avalonia.Controls.Shapes.Rectangle>("PMCCPreviewAlertTop");

            TextBox alertmidBox = this.FindControl<TextBox>("PMCCHexAlertMid");
            Avalonia.Controls.Shapes.Rectangle alertmidPreview = this.FindControl<Avalonia.Controls.Shapes.Rectangle>("PMCCPreviewAlertMid");

            TextBox alertlowBox = this.FindControl<TextBox>("PMCCHexAlertLow");
            Avalonia.Controls.Shapes.Rectangle alertlowPreview = this.FindControl<Avalonia.Controls.Shapes.Rectangle>("PMCCPreviewAlertLow");

            // Now set them to the current things in settings.
            primaryBox.Text = ManagerValues.temporaryCalendarPreferences.preferenceColours["schemePrimary"];
            primaryPreview.Fill = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(ManagerValues.temporaryCalendarPreferences.preferenceColours["schemePrimary"]);
            secondaryBox.Text = ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeSecondary"];
            secondaryPreview.Fill = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeSecondary"]);
            textBox.Text = ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeText"];
            textPreview.Fill = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeText"]);
            accentBox.Text = ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeAccent"];
            accentPreview.Fill = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeAccent"]);
            alerttopBox.Text = ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeAlertTop"];
            alerttopPreview.Fill = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeAlertTop"]);
            alertmidBox.Text = ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeAlertMid"];
            alertmidPreview.Fill = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeAlertMid"]);
            alertlowBox.Text = ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeAlertLow"];
            alertlowPreview.Fill = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(ManagerValues.temporaryCalendarPreferences.preferenceColours["schemeAlertLow"]);
        }
        else {
            NotifyError("You cannot update colours when temporary values are not in use!");
        }
    }

    public void SetColourSchemes() {
        // Preset schemes are saved in this method. You can edit the strings here to edit the preset schemes, but must call UpdateColours() for each scheme afterwards. 
        // This was originally going to be done with Avalonia properities, but I struggled to get that to work. This method is still quite neat, easy to adjust if required and
        // The idea is that p1 is the same theme as default, although this is not set-in-code so could be changed.
 
        PrefPageColourScheme s1 = this.FindControl<PrefPageColourScheme>("PMCS1");
        PrefPageColourScheme s2 = this.FindControl<PrefPageColourScheme>("PMCS2");
        PrefPageColourScheme s3 = this.FindControl<PrefPageColourScheme>("PMCS3");
        PrefPageColourScheme s4 = this.FindControl<PrefPageColourScheme>("PMCS4");
        PrefPageColourScheme s5 = this.FindControl<PrefPageColourScheme>("PMCS5");
        PrefPageColourScheme s6 = this.FindControl<PrefPageColourScheme>("PMCS6");
        PrefPageColourScheme s7 = this.FindControl<PrefPageColourScheme>("PMCS7");
        PrefPageColourScheme s8 = this.FindControl<PrefPageColourScheme>("PMCS8");

        // Scheme 1 ---  Default
        s1.primary = "#f4f8f7";
        s1.secondary = "#70a49d";
        s1.text = "#192622";
        s1.accent = "#19fcd7";
        s1.UpdateColours();

        // Scheme 2 --- Default, Dark Version
        s2.primary = "#131c1a";
        s2.secondary = "#4e7a75";
        s2.text = "#f0f5f3";
        s2.accent = "#118774";
        s2.UpdateColours();

        // Scheme 3 --- Blue Toned, Muted
        s3.primary = "#12243a";
        s3.secondary = "#37414c";
        s3.text = "#99b0bb";
        s3.accent = "#8ca6c3";
        s3.UpdateColours();

        // Scheme 4 --- Blue Toned, Bright
        s4.primary = "#e6ebf0";
        s4.secondary = "#6797cb";
        s4.text = "#2e3b42";
        s4.accent = "#122b48";
        s4.UpdateColours();

        // Scheme 5 --- Dark with red, Dark Focused
        s5.primary = "#161616";
        s5.secondary = "#440909";
        s5.text = "#a23030";
        s5.accent = "#b36f45";
        s5.UpdateColours();

        // Scheme 6 --- Light with red. 
        s6.primary = "#f2f2f2";
        s6.secondary = "#f6bbbb";
        s6.text = "#491b1b";
        s6.accent = "#b9754b";
        s6.UpdateColours();

        // Scheme 7 --- Green, Muted/Dark
        s7.primary = "#11150c";
        s7.secondary = "#393a34";
        s7.text = "#ede0cd";
        s7.accent = "#697665";
        s7.UpdateColours();

        // Scheme 8 --- Purple, Light Styled
        s8.primary = "#fff2fd";
        s8.secondary = "#874c99";
        s8.text = "#430053";
        s8.accent = "#bf75d1";
        s8.UpdateColours();
    }
}
#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8601 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings
#pragma warning restore CS8604 // Re-enabling Warnings
#pragma warning restore CS8629 // Re-enabling Warnings