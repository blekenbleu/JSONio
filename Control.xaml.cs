﻿using System.Windows;
using System.Windows.Controls;

namespace blekenbleu.jsonio
{
	/// <summary>
	/// Interaction code for Control.xaml
	/// </summary>
	public partial class Control : UserControl
	{
		public JSONio Plugin { get; }

		// need to reference XAML control from a static method
		public static StaticModel Model;

		// this gets called before simprops is initialized
		public Control() {
			Model = new StaticModel(this);
			InitializeComponent();
			//	https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/how-to-specify-the-binding-source?view=netframeworkdesktop-4.8
			//  https://www.codeproject.com/articles/126249/mvvm-pattern-in-wpf-a-simple-tutorial-for-absolute
			this.DataContext = Model;						// StaticControl events change Control.xaml properties
			// alternatively, DataContext in XAML	https://dev.to/mileswatson/a-beginners-guide-to-mvvm-using-c-wpf-241b
			Version.Text = "Version 1.21";
		}

		public Control(JSONio plugin) : this()
		{
			this.Plugin = plugin;						// Control.xaml button events call JSONio methods
			dg.ItemsSource = plugin.simValues;			// DataGrid values
			Model.ButtonVisibility = Visibility.Hidden;	// Buttons should be hidden until carID and game are defined
			Model.StatusText = "Launch game (or Replay) to enable property value changes";
		}

		internal byte Selection;						// changed only in JSONio.Select() on UI thread

		// highlights Current property value selected
		internal void Selected()	// crashes if called from other threads
		{
			if ((dg.Items.Count > Selection) && (dg.Columns.Count > 2))
			{
				//Select the item.
				dg.CurrentCell = new DataGridCellInfo(dg.Items[Selection], dg.Columns[1]);
				dg.SelectedCells.Clear();
				dg.SelectedCells.Add(dg.CurrentCell);
			}
		}

		// highlights selected cell when plugin first displays
		private void DgSelect(object sender, RoutedEventArgs e) { Selected(); }

		// handle button clicks
		private void Prior_Click(object sender, RoutedEventArgs e)
		{
			Plugin.Select(false);
		}

		private void Next_Click(object sender, RoutedEventArgs e)
		{
			Plugin.Select(true);
		}

		private void Inc_Click(object sender, RoutedEventArgs e)
		{
			Plugin.Ment(1);
		}

		private void Dec_Click(object sender, RoutedEventArgs e)
		{
			Plugin.Ment(-1);
		}

		private void Swap_Click(object sender, RoutedEventArgs e)
		{
			Plugin.Swap();
		}

		private void Def_Click(object sender, RoutedEventArgs e)
		{
			Plugin.New_defaults();
		}
	}
}
