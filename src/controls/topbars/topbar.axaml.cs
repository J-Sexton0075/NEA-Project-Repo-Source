using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Manager;

namespace project_main;

#pragma warning disable CS8602 // Compiler Warning Suppressed --  Dereference of a possibly null reference.

public partial class Topbar : UserControl {
    // Data Bindings    
    public string calendarButtonText => "Calendar";
    public string overviewButtonText => "Overview & Insights";
    public string goalsButtonText => "Goals & Logging";
    public string prefButtonText => "Preferences";

    public Topbar() {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);
    }

    // Each of the buttons have their own method. 

    public void TBCalendarButton(object sender, RoutedEventArgs args) {
        if (ManagerValues.CurrentPage != "PageCalendar") {
            this.FindAncestorOfType<MainWindow>().ChangePage(ManagerValues.CurrentPage, "PageCalendar");
        }
    }

    public void TBOverviewButton(object sender, RoutedEventArgs args) {
        if (ManagerValues.CurrentPage != "PageOverview") {
            this.FindAncestorOfType<MainWindow>().ChangePage(ManagerValues.CurrentPage, "PageOverview");
        }
    }

    public void TBGoalsButton(object sender, RoutedEventArgs args) {
        if (ManagerValues.CurrentPage != "PageGoals") {
            this.FindAncestorOfType<MainWindow>().ChangePage(ManagerValues.CurrentPage, "PageGoals");
        }
    }

    public void TBPrefButton(object sender, RoutedEventArgs args) {
        if (ManagerValues.CurrentPage != "PagePref") {
            this.FindAncestorOfType<MainWindow>().ChangePage(ManagerValues.CurrentPage, "PagePref");
        }
    }

    public void TBReturnButton(object sender, RoutedEventArgs args) {
        this.FindAncestorOfType<MainWindow>().ChangePage(ManagerValues.CurrentPage, "PageInit");
    } // Note the access of the page here is the same but validation is different because this can never already be PageInit    
}
#pragma warning restore CS8602 // Re-enabling Warnings
