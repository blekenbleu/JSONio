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

		public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string value)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(value));
        }

        private byte _Select;

        public byte Select
        {

            get { return _Select; }
            set
            {
                if (_Select != value)
                {
                    _Select = value;
                    OnPropertyChanged("Select");
                }
            }
        }

		// highlights Current property value selected
        private void Selected()	// crashes if called from other threads
        {
            if ((dg.Items.Count > Plugin.Select) && (dg.Columns.Count > 2))
            {
                //Select the item.
                dg.CurrentCell = new DataGridCellInfo(dg.Items[Plugin.Select], dg.Columns[1]);
                dg.SelectedCells.Clear();
                dg.SelectedCells.Add(dg.CurrentCell);
            }
        }

        private void dgSelect(object sender, RoutedEventArgs e) { Selected(); }

		// updates values displayed in SimHub plugin
        //public void Refresh() { dg.Items.Refresh(); }   // crashes if called from main Plugin thread

		// handle button clicks
		private void Prior_Click(object sender, RoutedEventArgs e)
        {
			Plugin.select(false);
            Selected();
		}

		private void Next_Click(object sender, RoutedEventArgs e)
        {
			Plugin.select(true);
            Selected();
		}

		private void Inc_Click(object sender, RoutedEventArgs e)
        {
			Plugin.ment(1, "in");
		}

		private void Dec_Click(object sender, RoutedEventArgs e)
        {
			Plugin.ment(-1, "de");
		}

		private void Swap_Click(object sender, RoutedEventArgs e)
        {
			Plugin.swap();
		}

		private void Def_Click(object sender, RoutedEventArgs e)
        {
			Plugin.new_defaults();
		}
    }
}
