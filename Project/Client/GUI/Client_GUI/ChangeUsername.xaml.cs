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

namespace Client_GUI
{
    /// <summary>
    /// Interaction logic for ChangeUsername.xaml
    /// </summary>
    public partial class ChangeUsername : Window
    {
        public ChangeUsername()
        {
            InitializeComponent();

            // Set focus to text box
            txtUsername.Text = Properties.Settings.Default.Username;
            txtUsername.Focus();
            txtUsername.SelectAll();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement proper serverside username change functionality
            Properties.Settings.Default.Username = txtUsername.Text;
            Properties.Settings.Default.Save();
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
