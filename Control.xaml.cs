using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
			Model = new StaticModel();
			InitializeComponent();
			this.DataContext = Model;						// StaticControl events change Control.xaml properties
			Version.Text = "Version 2.14";
		}

		public Control(JSONio plugin) : this()
		{
			this.Plugin = plugin;						// Control.xaml button events call JSONio methods
			dg.ItemsSource = plugin.simprops;			// DataGrid values
			Model.ButtonVisibility = Visibility.Hidden;	// Buttons should be hidden until carID and game are defined
			Model.StatusText = "Launch game (or Replay) to enable property value changes";
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

		// handle slider changes
        private void SLslider_DragCompleted(object sender, MouseButtonEventArgs e)
        {
            TBL.Text = "Gscale:  " + (Plugin.simprops[Plugin.S.Gscale].Current = (0.02 * (float)(int)(0.5 + ((Slider)sender).Value)).ToString());
        }

		internal void Slslider_Point()
		{
			SL.Value = 50 * Plugin.S.Current(Plugin.S.Gscale);
			TBL.Text = "Gscale:  " + Plugin.simprops[Plugin.S.Gscale].Current;
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
