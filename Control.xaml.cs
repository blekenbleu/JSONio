using System.Windows;
using System.Windows.Controls;

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
		public JSONio js { get; }
		public ViewModel Model;							// reference XAML controls
		internal byte Selection;						// changed only in JSONio.Select() on UI thread

		public Control() {								// called before simValues are initialized
			Model = new ViewModel(this);
			InitializeComponent();
			this.DataContext = Model;					// StaticControl events change Control.xaml properties
			Version.Text = "Version 1.35";
		}

		public Control(JSONio plugin) : this()
		{
			this.js = plugin;						// Control.xaml button events call JSONio methods
			dg.ItemsSource = plugin.simValues;			// DataGrid values
		}

		internal void OOpsMB()
        {
            Model.StatusText = JSONio.Msg;
            System.Windows.Forms.MessageBox.Show(JSONio.Msg, "JSONio");
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
		private void SLslider_DragCompleted(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			TBL.Text = js.FromSlider(0.5 + SL.Value);
		}

		internal void Slslider_Point()
		{
			SL.Value = js.ToSlider();	// TBL.Text set inside ToSlider()
		}

		private void Prior_Click(object sender, RoutedEventArgs e)	// handle button clicks
		{
			js.Select(false);
		}

		private void Next_Click(object sender, RoutedEventArgs e)
		{
			js.Select(true);
		}

		private void Inc_Click(object sender, RoutedEventArgs e)
		{
			js.Ment(1);
		}

		private void Dec_Click(object sender, RoutedEventArgs e)
		{
			js.Ment(-1);
		}

		private void Swap_Click(object sender, RoutedEventArgs e)
		{
			js.Swap();
		}

		private void Def_Click(object sender, RoutedEventArgs e)
		{
			js.SetDefault();
		}
	}
}
