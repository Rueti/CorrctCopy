using System.Windows;
using System.Windows.Data;

namespace Correct_Copy;

public partial class Maual_Setting_Window : Window
{
    private ListCollectionView? view;
    private int optionCount;
    public Maual_Setting_Window() : this(RobocopyOptions.Create()) { }
    public Maual_Setting_Window(List<CopyOption> options)
    {
        InitializeComponent();
        view = new ListCollectionView(options);
        optionCount = options.Count;
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(CopyOption.Group)));
        OptionsList.ItemsSource = view;
        Search_Changed(this, new RoutedEventArgs());
    }

    private void Search_Changed(object sender, RoutedEventArgs e)
    {
        if (view == null) return;
        var query = SearchBox.Text.Trim().TrimStart('/');
        view.Filter = item => item is CopyOption option &&
            (option.Switch.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             option.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             option.Group.Contains(query, StringComparison.OrdinalIgnoreCase));
        CountText.Text = $"{view.Count} von {optionCount} Optionen · Suche nach Parameter, Beschreibung oder Gruppe";
    }
}
