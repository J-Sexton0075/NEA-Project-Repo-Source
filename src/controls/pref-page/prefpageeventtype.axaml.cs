using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;

namespace project_main;

#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.
#pragma warning disable CS8604 // Compiler Warning Suppressed --  Possible null reference argument for parameter.

public partial class PrefPageEventType : UserControl {
    
    private string typeCodeName;

    // Bindings
    public string ColourWatermark => "#HexCode";
    public string ButtonTextSave => "Save";
    public string ButtonTextDelete => "Delete";

    public PrefPageEventType(string name, string colour, bool isCustom, string t) {
        typeCodeName = t;
        
        // Avalonia Stuff
        DataContext = this;
        AvaloniaXamlLoader.Load(this);

        // Get Elements
        Grid grid = this.FindControl<Grid>("Main");
        TextBlock nameText = this.FindControl<TextBlock>("PPETEventName");
        TextBox colourBox = this.FindControl<TextBox>("PPETEventColour");
        Button deleteButton = this.FindControl<Button>("PPETDelete");

        // Set appropriate visibilities and text, colours and buttons
        grid.Background = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(colour);
        nameText.Text = name;
        colourBox.Text = colour;

        deleteButton.IsVisible = isCustom;
    }

    public void ButtonClickSave(object sender, RoutedEventArgs args) {
        string hex = this.FindControl<TextBox>("PPETEventColour").Text;
        if (PrefPage.CheckIfHexCode(hex)) { // Validates, if this is invalid, nothing happens, 
        // other than the method being called (which due to a second validation will just notify the user of an error)
            // Update Visually
            this.FindControl<Grid>("Main").Background = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(hex);
        }
        // Update Settings
        this.FindAncestorOfType<PrefPage>().SetColourSchemeType(typeCodeName, hex);
    }   
    
    public void ButtonClickDelete(object sender, RoutedEventArgs args) {
        this.FindAncestorOfType<PrefPage>().DeleteEventType(this, typeCodeName);
    }
}
#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings
#pragma warning restore CS8604 // Re-enabling Warnings