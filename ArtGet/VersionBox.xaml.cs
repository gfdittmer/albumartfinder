using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ArtGet
{
    /// <summary>
    /// Interaction logic for VersionBox.xaml
    /// </summary>
    public partial class VersionBox : Window
    {
        public VersionBox()
        {
            InitializeComponent();
        }

        public Version netVersion;

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            labelVersion.Content = "Current .NET version: "+netVersion.Major+"."+netVersion.Minor;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.microsoft.com/en-us/download/details.aspx?id=17851");
            this.Close();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
