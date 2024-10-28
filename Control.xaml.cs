using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

/* XAML DataContext:  Binding source
 ;	https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/how-to-specify-the-binding-source?view=netframeworkdesktop-4.8
 ;	https://www.codeproject.com/articles/126249/mvvm-pattern-in-wpf-a-simple-tutorial-for-absolute
 ;	alternatively, DataContext in XAML	https://dev.to/mileswatson/a-beginners-guide-to-mvvm-using-c-wpf-241b
 */

namespace blekenbleu.jsonio
{
	/// <summary>
	/// Interaction code for Control.xaml
	/// </summary>
	public partial class Control : UserControl
	{
		public JSONio Plugin { get; }
		public static StaticModel Model;				// must reference XAML controls from statics
		internal byte Selection;						// changed only in JSONio.Select() on UI thread

		public Control() {								// called before simValues are initialized
			Model = new StaticModel(this);
			InitializeComponent();
			this.DataContext = Model;					// StaticControl events change Control.xaml properties
			Version.Text = "Version 2.27";
		}

		public Control(JSONio plugin) : this()
		{
			this.Plugin = plugin;						// Control.xaml button events call JSONio methods
			plugin.SetSlider();							// whenever SimHub gets around to calling 
			dg.ItemsSource = plugin.simValues;			// DataGrid values
		}

		// highlights selected property cell
		internal void Selected()						// crashes if called from other threads
		{
			if ((dg.Items.Count > Selection) && (dg.Columns.Count > 2))
			{
				//Select the item.
				dg.CurrentCell = new DataGridCellInfo(dg.Items[Selection], dg.Columns[1]);
				dg.SelectedCells.Clear();
				dg.SelectedCells.Add(dg.CurrentCell);
			}
		}

		private void DgSelect(object sender, RoutedEventArgs e)
		{
			Selected();
		}

		// handle slider changes
		private void SLslider_DragCompleted(object sender, MouseButtonEventArgs e)
		{
			TBL.Text = Plugin.FromSlider(0.5 + SL.Value);
		}

		internal void Slslider_Point()
		{
			SL.Value = Plugin.ToSlider();	// TBL.Text set inside ToSlider()
		}

		private void Prior_Click(object sender, RoutedEventArgs e)	// handle button clicks
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
