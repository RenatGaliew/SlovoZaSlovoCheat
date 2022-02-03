using System.Windows;

namespace SlovoedCheat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent(); 
            MainViewModel mainWindowViewModel = new MainViewModel();
            this.DataContext = mainWindowViewModel;
        }
    }
}
