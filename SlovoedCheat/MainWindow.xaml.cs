using System.Collections.Generic;
using System.IO;
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
            DictionaryOfWords.Items = new List<string>();
            DictionaryOfWords.Items.AddRange(File.ReadAllLines("russian.txt"));

            MainViewModel mainWindowViewModel = new MainViewModel();
            this.DataContext = mainWindowViewModel;
        }
    }
}
