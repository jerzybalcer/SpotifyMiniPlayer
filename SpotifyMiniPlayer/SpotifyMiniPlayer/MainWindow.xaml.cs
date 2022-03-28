using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpotifyMiniPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void MainContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void PreviousBtn_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("previous");
        }

        private void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("play");
        }
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("next");
        }
    }
}
