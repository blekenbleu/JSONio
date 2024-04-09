using System;
using System.ComponentModel;
using System.Windows;

namespace blekenbleu
{
   	/// <summary>
   	/// define a class with Model-view-viewmodel pattern for dynamic UI
   	/// </summary>
	public class StaticControl : INotifyPropertyChanged
	{
		// One event handler for all property changes
		public event PropertyChangedEventHandler PropertyChanged;
		// events to raise
		PropertyChangedEventArgs Vevent = new PropertyChangedEventArgs("ButtonVisibility");
		PropertyChangedEventArgs Tevent = new PropertyChangedEventArgs("StatusText");
		protected void RaiseChange(PropertyChangedEventArgs ea)
		{
			PropertyChanged?.Invoke(this, ea);	// this probably is not helping
		}

		private Visibility _visibility;
		public Visibility ButtonVisibility	// must be public for XAML Binding
		{
			get { return _visibility; }
			set
			{
				if (_visibility != value)
                {
					_visibility = value;
					RaiseChange(Vevent);
				}
			}
		}

		private string _statusText;
		public string StatusText			// must be public for XAML Binding
        {
            get
            {
                return _statusText;
            }

            set
            {
                if (value == _statusText)
                    return;

                _statusText = value;

                PropertyChanged?.Invoke(this, Tevent);
            }
        }
	}		// public class StaticControl
}
