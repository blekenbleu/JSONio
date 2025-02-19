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
		public JSONio JS { get; }
		public ViewModel Model;							// reference XAML controls
		internal byte Selection;						// changes only in JSONio.Select() on UI thread
		internal static string version = "1.57";

		public Control() {								// called before simValues are initialized
			Model = new ViewModel(this);
			InitializeComponent();
			this.DataContext = Model;					// StaticControl events change Control.xaml binds
		}

		public Control(JSONio plugin) : this()
		{
			this.JS = plugin;							// Control.xaml button events call JSONio methods
			dg.ItemsSource = plugin.simValues;			// DataGrid values
		}

		private void Hyperlink_RequestNavigate(object sender,
									System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
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

		// xaml DataGrid:  Loaded="DgSelect"
		private void DgSelect(object sender, RoutedEventArgs e)
		{
			Selected();
		}

		// handle slider changes
		private void Slider_DragCompleted(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			JS.FromSlider(0.5 + SL.Value);
		}

		private void Slider_Click(object sender, RoutedEventArgs e)	// handle button clicks
		{
			JS.SelectSlider();
		}

		private void Prior_Click(object sender, RoutedEventArgs e)	// handle button clicks
		{
			JS.Select(false);
		}

		private void Next_Click(object sender, RoutedEventArgs e)
		{
			JS.Select(true);
		}

		private void Inc_Click(object sender, RoutedEventArgs e)
		{
			JS.Ment(1);
		}

		private void Dec_Click(object sender, RoutedEventArgs e)
		{
			JS.Ment(-1);
		}

		private void Swap_Click(object sender, RoutedEventArgs e)
		{
			JS.Swap();
		}

		private void Def_Click(object sender, RoutedEventArgs e)
		{
			JS.SetDefault();
		}
	}
}
