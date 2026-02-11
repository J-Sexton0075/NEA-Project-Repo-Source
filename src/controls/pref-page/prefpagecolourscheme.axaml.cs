using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Immutable;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace project_main;

#pragma warning disable CS8600 // Compiler Warning Suppressed -- Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.
#pragma warning disable CS8604 // Compiler Warning Suppressed --  Possible null reference argument for parameter. 

public partial class PrefPageColourScheme : UserControl {
    
   //  public static IBrush defaultBrush = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom("#ff00ff");


    // The idea behind this was to do this a smart easy way, but that didn't work particularly well. 
 /*   public static readonly StyledProperty<IBrush?> PrimaryProperty = 
        AvaloniaProperty.Register<PrefPageColourScheme, IBrush?>(nameof(PrimaryColour), defaultValue: defaultBrush);
    public static readonly StyledProperty<IBrush?> SecondaryProperty = 
        AvaloniaProperty.Register<PrefPageColourScheme, IBrush?>(nameof(SecondaryColour), defaultValue: defaultBrush);
    public static readonly StyledProperty<IBrush?> TextProperty = 
        AvaloniaProperty.Register<PrefPageColourScheme, IBrush?>(nameof(TextColour), defaultValue: defaultBrush);
    public static readonly StyledProperty<IBrush?> AccentProperty = 
        AvaloniaProperty.Register<PrefPageColourScheme, IBrush?>(nameof(AccentColour), defaultValue: defaultBrush);

    public IBrush? PrimaryColour {
        get => GetValue(PrimaryProperty); 
        set => SetValue(PrimaryProperty, value); 
    }
    public IBrush? SecondaryColour {
        get => GetValue(SecondaryProperty); 
        set => SetValue(SecondaryProperty, value); 
    }
    public IBrush? TextColour {
        get => GetValue(TextProperty); 
        set => SetValue(TextProperty, value); 
    }
    public IBrush? AccentColour {
        get => GetValue(AccentProperty); 
        set => SetValue(AccentProperty, value); 
    }

    */

    public string? primary;
    public string? secondary;
    public string? text;
    public string? accent;

    public string SelectButtonText => "Select";


    public PrefPageColourScheme() {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);
    }

    public void UpdateColours() {
        Grid grid = this.FindControl<Grid>("Main");
        Avalonia.Controls.Shapes.Rectangle primaryRect = grid.FindControl<Avalonia.Controls.Shapes.Rectangle >("PrimaryRect");
        Avalonia.Controls.Shapes.Rectangle secondaryRect = grid.FindControl<Avalonia.Controls.Shapes.Rectangle >("SecondaryRect");
        Avalonia.Controls.Shapes.Rectangle textRect = grid.FindControl<Avalonia.Controls.Shapes.Rectangle >("TextRect");
        Avalonia.Controls.Shapes.Rectangle accentRect = grid.FindControl<Avalonia.Controls.Shapes.Rectangle >("AccentRect");

        primaryRect.Fill = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(primary);
        secondaryRect.Fill = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(secondary);
        textRect.Fill = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(text);
        accentRect.Fill = (ImmutableSolidColorBrush)new BrushConverter().ConvertFrom(accent);
    }

    public void ButtonHandler(object sender, RoutedEventArgs args) {
        this.FindAncestorOfType<PrefPage>().SetColourSchemePreset(this);
    }
}
#pragma warning restore CS8600 // Re-enabling Warnings
#pragma warning restore CS8602 // Re-enabling Warnings
#pragma warning restore CS8604 // Re-enabling Warnings
