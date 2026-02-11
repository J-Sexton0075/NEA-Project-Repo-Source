using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Manager;

namespace project_main;

#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.

public partial class TopbarStream : UserControl {

    // Data Bindings
    public string headerText => "Calendar";

    public TopbarStream() {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);
    }

    // Rather than one way, this button instead toggles between the two pages when pressed. 
    public void TBPrefStream(object sender, RoutedEventArgs args) {
        if (ManagerValues.CurrentPage == "PageCalendar") {
            this.FindAncestorOfType<MainWindow>().ChangePage(ManagerValues.CurrentPage, "PagePref");
        }
        else {
            this.FindAncestorOfType<MainWindow>().ChangePage(ManagerValues.CurrentPage, "PageCalendar");
        }
    }

    // The only other button is return that works exactly the same as with a normal topbar. 
    public void TBReturnStream(object sender, RoutedEventArgs args) {
        this.FindAncestorOfType<MainWindow>().ChangePage(ManagerValues.CurrentPage, "PageInit");
    }

    // Changes the header text based on whatever the page is changing to.
    public void TBChangeHeaderText(bool isPref) {
        this.FindControl<TextBlock>("HeaderText").Text = isPref ? "Preferences" : "Calendar";
    }
}
#pragma warning restore CS8602 // Re-enabling Warnings