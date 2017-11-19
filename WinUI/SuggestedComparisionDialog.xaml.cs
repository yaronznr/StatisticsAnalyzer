using StatisticsAnalyzerCore;
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

namespace WinUI
{
    /// <summary>
    /// Interaction logic for SuggestedComparisionDialog.xaml
    /// </summary>
    public partial class SuggestedComparisionDialog : Window
    {
        private IEnumerable<Comparision> _suggestedComparisions;

        public Comparision SelectedComparision { get; private set; }

        public SuggestedComparisionDialog(IEnumerable<Comparision> suggestedComparisions)
        {
            InitializeComponent();
            listView.ItemsSource = suggestedComparisions.Select(comp => comp.ComparisionText);
            _suggestedComparisions = suggestedComparisions;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void listView_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            btnOk.IsEnabled = true;
            SelectedComparision = _suggestedComparisions.Where(comp => comp.ComparisionText == listView.SelectedItem.ToString()).Single();
        }
    }
}
