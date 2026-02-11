using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace project_main;

public partial class GoalsPageHeaderBlock : UserControl {

    public string LSEHT1 => "Study Session:";
    public string LSEHT2 => "Complete";
    public string LSEHT3 => "Incomplete";
    public string LSEHT4 => "%";
    public string LSEHT5 => "% Complete";
    public string LSEHT6 => "Save";
    public string LSEHT7 => "Delete";

    public GoalsPageHeaderBlock() {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);
    }    
}

