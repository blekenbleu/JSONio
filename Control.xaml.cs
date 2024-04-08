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

		// Raise the event
		PropertyChangedEventArgs SCevent = new PropertyChangedEventArgs("Select");
		protected void SelectChange()
		{
			PropertyChanged?.Invoke(this, SCevent); // this probably is not helping
			Selected();							 // this enables dashboard changes to be seen
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
					SelectChange();
				}
			}
		}

		// highlights Current property value selected
		private void Selected()	// crashes if called from other threads
		{
			if ((dg.Items.Count > Select) && (dg.Columns.Count > 2))
			{
				//Select the item.
				dg.CurrentCell = new DataGridCellInfo(dg.Items[Select], dg.Columns[1]);
				dg.SelectedCells.Clear();
				dg.SelectedCells.Add(dg.CurrentCell);
			}
		}

		// highlights selected cell when plugin first displays
		private void dgSelect(object sender, RoutedEventArgs e) { Selected(); }

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
