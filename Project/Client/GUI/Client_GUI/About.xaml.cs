using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Reflection;

namespace Client_GUI
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();

            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Major.ToString() + "." + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString();
            string build = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var buildDate = Assembly.GetExecutingAssembly().GetLinkerTime();
            lblVersion.Content = "[v" + version + "]";
            lblBuild.Content = "Build [v" + build + "]";
            lblBuildDate.Content = "Built on " + buildDate.Day + "-" + buildDate.Month + "-" + buildDate.Year + " " + buildDate.Hour + ":" + buildDate.Minute;
            
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void lblEmail_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lblEmail.Content = "=^-^=";
        }

    }
}
