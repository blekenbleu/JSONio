using System.Collections.Generic;
using System.Windows.Controls;

namespace blekenbleu
{
	// manually define DataGrid columns shown
	// https://wpf-tutorial.com/datagrid-control/custom-columns/
	public class SimProp
	{
		public string Name { get; set; }
		public string Default { get; set; }
		public string Current { get; set; }
		public string Previous { get; set; }
	}

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
    }
}
