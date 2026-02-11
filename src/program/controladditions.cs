using Avalonia.Interactivity;

namespace project_main;

public interface IInitialisable {
    public void InitialisePage();
}


// Unsure if I want either of these to have anything yet but I've implemented them JIC. 
public interface ICalendarDay {

}

public interface ICalendarEvent {
    public void OnPress(object sender, RoutedEventArgs args);
}