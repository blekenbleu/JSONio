﻿using System.ComponentModel;
using System.Windows;

namespace blekenbleu.jsonio
{
    /// <summary>
    /// define a class with Model-view-viewmodel pattern for dynamic UI
    /// </summary>
    public class StaticModel : INotifyPropertyChanged
	{
		// One event handler for all property changes
		public event PropertyChangedEventHandler PropertyChanged;
        // events to raise
        readonly PropertyChangedEventArgs Vevent = new PropertyChangedEventArgs("ButtonVisibility");
        readonly PropertyChangedEventArgs Tevent = new PropertyChangedEventArgs("StatusText");

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
