using Microsoft.Win32; //for File - Picker, old Style
using System.IO; //for assigning Filename from File Picker
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WinForms = System.Windows.Forms;



namespace Correct_Copy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        string Sourcepath, Sourcefile, Destinationfolder;



        public MainWindow()
        {
            InitializeComponent();
        }


        private void Select_Button_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select File",
                Filter = "All Files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Multiselect = false
            };

            bool? result = dlg.ShowDialog(); // only when FilePicker - Dialogue could be opened
            if (result == true)
            {
                Sourcepath = dlg.FileName;
                Sourcefile = System.IO.Path.GetFileName(dlg.FileName);
                DestinationButton.IsEnabled = true;
            }
        }



        private void Destination_Button_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new WinForms.FolderBrowserDialog
            {
                Description = "Ordner auswählen",
                UseDescriptionForTitle = true
            };

            if (dlg.ShowDialog() == WinForms.DialogResult.OK)
            {
                Destinationfolder = dlg.SelectedPath;
                
            }
        }
    }
}