using System.ComponentModel;
using System.Windows;

/*
 ; Model-View-ViewModel (MVVM)
 ; https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/?view=netdesktop-8.0
 ; https://www.c-sharpcorner.com/article/datacontext-autowire-in-wpf/
 ; https://learn.microsoft.com/en-us/archive/msdn-magazine/2009/february/patterns-wpf-apps-with-the-model-view-viewmodel-design-pattern
 ; https://scottlilly.com/c-design-patterns-mvvm-model-view-viewmodel/
 */
namespace blekenbleu.jsonio
{
    /// <summary>
    /// define a class with Model-view-viewmodel pattern for dynamic UI
    /// </summary>
    public class StaticModel : INotifyPropertyChanged
	{
        readonly Control Ctrl;				// Dispatcher.Invoke(Ctrl.Selected())
		public StaticModel(Control C)
		{
			Ctrl = C;
		}

		// One event handler for all property changes
		public event PropertyChangedEventHandler PropertyChanged;

        // events to raise
        readonly PropertyChangedEventArgs Vevent = new PropertyChangedEventArgs("ButtonVisibility");
        readonly PropertyChangedEventArgs Tevent = new PropertyChangedEventArgs("StatusText");
        readonly PropertyChangedEventArgs Sevent = new PropertyChangedEventArgs("Selected_Property");

		private Visibility _visibility;
		public Visibility ButtonVisibility	// must be public for XAML Binding
		{
			get { return _visibility; }
			set
			{
				if (_visibility != value)
                {
					_visibility = value;
					PropertyChanged?.Invoke(this, Vevent);
				}
			}
		}

		private string _selected_Property = "unKnown";
		public string Selected_Property			// must be public for XAML Binding
        {
            get { return _selected_Property; }

            set
            {
                if (value != _selected_Property)
				{
                	_selected_Property = value;
                	PropertyChanged?.Invoke(this, Sevent);
					// update xaml DataGrid from another thread
                    Ctrl.Dispatcher.Invoke(() => Ctrl.Selected());
				}
            }
        }

		private string _statusText = "Waiting for Car Change";
		public string StatusText			// must be public for XAML Binding
        {
            get { return _statusText; }

            set
            {
                if (value != _statusText)
				{
                	_statusText = value;
                	PropertyChanged?.Invoke(this, Tevent);
				}
            }
        }
	}		// public class StaticControl
}
