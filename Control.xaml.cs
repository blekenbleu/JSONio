using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace blekenbleu
{
    /// <summary>
    /// Interaction code for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        public JSONio Plugin { get; }

        // this gets called before simprops is initialized
        public SettingsControl() { InitializeComponent(); }

        public SettingsControl(JSONio plugin) : this()
        {
            this.Plugin = plugin;
            dg.ItemsSource = plugin.simprops;
        }

		// highlights Current property value selected
        private void Select()	// crashes if called from other threads
        {
            if ((dg.Items.Count > Plugin.Select) && (dg.Columns.Count > 2))
            {
                //Select the item.
                dg.CurrentCell = new DataGridCellInfo(dg.Items[Plugin.Select], dg.Columns[1]);
                dg.SelectedCells.Clear();
                dg.SelectedCells.Add(dg.CurrentCell);
            }
        }

        private void dgSelect(object sender, RoutedEventArgs e) { Select(); }

		// updates values displayed in SimHub plugin
        //public void Refresh() { dg.Items.Refresh(); }   // crashes if called from main Plugin thread

		// handle button clicks
		private void Prior_Click(object sender, RoutedEventArgs e)
        {
			Plugin.select(false);
            Select();
		}

		private void Next_Click(object sender, RoutedEventArgs e)
        {
			Plugin.select(true);
            Select();
		}

		private void Inc_Click(object sender, RoutedEventArgs e)
        {
			Plugin.ment(1, "in");
//          Select();
		}

		private void Dec_Click(object sender, RoutedEventArgs e)
        {
			Plugin.ment(-1, "de");
//          Select();
		}

		private void Swap_Click(object sender, RoutedEventArgs e)
        {
			Plugin.swap();
//          Select();
		}

		private void Def_Click(object sender, RoutedEventArgs e)
        {
			Plugin.new_defaults();
//          Select();
		}
    }
}
