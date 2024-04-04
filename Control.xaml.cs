using System.Collections.Generic;
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

		public List<SimProp> simprops;

        public SettingsControl()
        {
            InitializeComponent();

			simprops = new List<SimProp>();
			dg.ItemsSource = simprops;
        }

        public SettingsControl(JSONio plugin) : this()
        {
            this.Plugin = plugin;
        }

		// handle button clicks
		private void Prior_Click(object sender, RoutedEventArgs e)
        {
			Plugin.select(false);
		}

		private void Next_Click(object sender, RoutedEventArgs e)
        {
			Plugin.select(true);
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
