using System.ComponentModel;

namespace blekenbleu.jsonio
{
	// programatically define DataGrid columns
	// https://wpf-tutorial.com/datagrid-control/custom-columns/
	public class Values : INotifyPropertyChanged	// https://stackoverflow.com/questions/26871641/how-to-refresh-a-window-in-c-wpf
	{
		private string _Default = "default", _Current = "current", _Previous = "previous";

		public event PropertyChangedEventHandler PropertyChanged;
		private readonly PropertyChangedEventArgs Cevent = new PropertyChangedEventArgs("Current");
		private readonly PropertyChangedEventArgs Devent = new PropertyChangedEventArgs("Default");
		private readonly PropertyChangedEventArgs Pevent = new PropertyChangedEventArgs("Previous");

		public string Name { get; set; }	// should not change
		public string Current
		{
			get { return _Current; }
			set
			{
				if (string.Compare(_Current, value) != 0)
				{
					_Current = value;
					PropertyChanged?.Invoke(this, Cevent);
				}
			}
		}

		public string Default
		{
			get { return _Default; }
			set
			{
				if (string.Compare(_Default, value) != 0)
				{
					_Default = value;
					PropertyChanged?.Invoke(this, Devent);
				}
			}
		}

		public string Previous
		{
			get { return _Previous; }
			set
			{
				if (string.Compare(_Previous, value) != 0)
				{
					_Previous = value;
					PropertyChanged?.Invoke(this, Pevent);
				}
			}
		}
	}	// class Values
}
