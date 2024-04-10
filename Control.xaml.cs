using System.Windows;
using System.Windows.Controls;

namespace blekenbleu
{
    /// <summary>
    /// Interaction code for Control.xaml
    /// </summary>
    public partial class Control : UserControl
    {
		public JSONio Plugin { get; }

        // need to reference XAML control from a static method
        public static StaticControl ui;

        // this gets called before simprops is initialized
        public Control() {
			ui = new StaticControl();
			InitializeComponent();
			this.DataContext = ui;						// StaticControl events change Control.xaml properties
		}

		public Control(JSONio plugin) : this()
		{
			this.Plugin = plugin;						// Control.xaml button events call JSONio methods
			dg.ItemsSource = plugin.simprops;			// DataGrid values
			ui.ButtonVisibility = Visibility.Hidden;	// Buttons should be hidden until carID and game are defined
			ui.StatusText = "Launch game (or Replay) to enable property value changes";
		}

		private byte _Select;
        internal byte Selection                            // fortunately changed only on UI thread
        {
            get { return _Select; }
            set
            {
                if (_Select != value)
                {
                    _Select = value;
                    Selected();                   		// force selected cell highlight
                }
            }
        }

		// highlights Current property value selected
		internal void Selected()	// crashes if called from other threads
		{
			if ((dg.Items.Count > Selection) && (dg.Columns.Count > 2))
			{
				//Selection the item.
				dg.CurrentCell = new DataGridCellInfo(dg.Items[Selection], dg.Columns[1]);
				dg.SelectedCells.Clear();
				dg.SelectedCells.Add(dg.CurrentCell);
			}
		}

		// highlights selected cell when plugin first displays
		private void dgSelect(object sender, RoutedEventArgs e) { Selected(); }

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
			Plugin.ment(1, "in");
		}

		private void Dec_Click(object sender, RoutedEventArgs e)
		{
			Plugin.ment(-1, "de");
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
