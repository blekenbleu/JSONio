﻿using System.ComponentModel;
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
    public class ViewModel : INotifyPropertyChanged
	{
        readonly Control Ctrl;				// Ctrl.Dispatcher.Invoke(Ctrl.Selected())
		public ViewModel(Control C)
		{
			Ctrl = C;
		}

		// One event handler for all property changes
		public event PropertyChangedEventHandler PropertyChanged;

        // events to raise
        readonly PropertyChangedEventArgs Bevent = new PropertyChangedEventArgs("ButtonVisibility");
        readonly PropertyChangedEventArgs Cevent = new PropertyChangedEventArgs("ChangedVisibility");
        readonly PropertyChangedEventArgs Nevent = new PropertyChangedEventArgs("Slider_Property");
        readonly PropertyChangedEventArgs Sevent = new PropertyChangedEventArgs("Selected_Property");
        readonly PropertyChangedEventArgs SVevent = new PropertyChangedEventArgs("SliderVisibility");
        readonly PropertyChangedEventArgs Tevent = new PropertyChangedEventArgs("StatusText");

		private Visibility _bvis = Visibility.Hidden;	// until carID and game are defined
		public Visibility ButtonVisibility				// must be public for XAML Binding
		{
			get { return _bvis; }
			set
			{
				if (_bvis != value)
                {
					_bvis = value;
					PropertyChanged?.Invoke(this, Bevent);
				}
			}
		}

		private Visibility _cvis = Visibility.Hidden;	// until carID and game are defined
		public Visibility ChangedVisibility				// must be public for XAML Binding
		{
			get { return _cvis; }
			set
			{
				if (_cvis != value)
                {
					_cvis = value;
					PropertyChanged?.Invoke(this, Cevent);
				}
			}
		}

		private Visibility _svis = Visibility.Hidden;
		public Visibility SliderVisibility		// must be public for XAML Binding
		{
			get { return _svis; }
			set
			{
				if (_svis != value)
                {
					_svis = value;
					PropertyChanged?.Invoke(this, SVevent);
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

		private string _slider_Property = "";
		public string Slider_Property			// must be public for XAML Binding
        {
            get { return _slider_Property; }

            set
            {
                if (value != _slider_Property)
				{
                	_slider_Property = value;
                	PropertyChanged?.Invoke(this, Nevent);
					SliderVisibility = Visibility.Visible;
				}
            }
        }

		static internal readonly string statusText = "To enable:  launch game or Replay";
		private string _statusText = statusText;
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
