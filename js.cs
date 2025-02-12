using System.Collections.Generic;
using System.Windows;

namespace blekenbleu.jsonio
{
	public partial class JSONio
	{
		internal void SetSlider()
		{
			if (0 > slider)
				return;

			View.Model.Slider_Property = simValues[slider].Name;
			/* slider View.SL.Maximum = 100; scale property to it, based on Steps[slider]
			 ; Steps	   Guestimated range
			 ; 1  (0.01)	0 - 2
			 ; 10 (0.10)	0 - 10
			 ; 100 (1)		0 - 100
			 ; 1000 (10)	0 - 1000
			 */
			if (0 != Steps[slider] % 10)
			{
				Slider_factor[0] = 0.02;	// slider to value
				Slider_factor[1] = 50;	// value to slider
			} else if (0 != Steps[slider] % 100) {
				Slider_factor[0] = 0.1;	// slider to value
				Slider_factor[1] = 10;	// value to slider
			} else {
				Slider_factor[0] = 1;	// slider to value
				Slider_factor[1] = 1;	// value to slider
			}
		}

		internal string FromSlider(double value)
		{
			simValues[slider].Current = (Slider_factor[0] * (int)value).ToString();
			return simValues[slider].Name + ":  " + simValues[slider].Current;
		}

		internal double ToSlider()
		{
			if(0 > slider)
				return 0;
			View.TBL.Text = simValues[slider].Name + ":  " + simValues[slider].Current;
			return Slider_factor[1] * System.Convert.ToDouble(simValues[slider].Current);
		}

		// check whether current properties differ from JSON
		internal void Changed()
		{
			if (-1 == gndx || -1 == cndx)
				return;

			// this should be unnecessary if slim.Reconcile() works..
			if (gCount != slim.data.gList[gndx].cList[0].Vlist.Count
			 || pCount != slim.data.gList[gndx].cList[cndx].Vlist.Count)
				changed = true;
			else for (int p = 0; p < gCount; p++)
				// default change?
				if (simValues[p].Default != slim.data.gList[gndx].cList[0].Vlist[p]
						// current per-car change?
			 	 || (p < pCount && simValues[p].Current != slim.data.gList[gndx].cList[cndx].Vlist[p]))
				{
					changed = true;
					break;
				}

			View.Model.ChangedVisibility = changed ? Visibility.Visible : Visibility.Hidden;
		}

		/// <summary>
		/// Helper functions used in Init() AddAction()s and Control.xaml.cs button Clicks
		/// </summary>
		/// <param name="sign"></param> should be 1 or -1
		/// <param name="prefix"></param> should be "in" or "de"
		public void Ment(int sign)
		{
			if (0 == Gname.Length || 0 == CurrentCar.Length)
				return;
			int step = Steps[View.Selection];
			int iv = (int)(0.004 + 100 * float.Parse(simValues[View.Selection].Current));

			iv += sign * step;
			if (0 <= iv)
			{
				if (0 != step % 100)
					simValues[View.Selection].Current = $"{(float)(0.01 * iv)}";
				else simValues[View.Selection].Current = $"{(int)(0.004 + 0.01 * iv)}";
				Changed();
				if (slider == View.Selection)
					View.Slslider_Point();
			}
		}

		private void SelectedStatus()
		{
			if (null == View)
				return;
			View.Model.Selected_Property = simValues[View.Selection].Name;
			View.Model.StatusText = Gname + " " + CurrentCar + ":\t" + View.Model.Selected_Property;
		}

		/// <summary>
		/// Select next or prior property; exception if invoked on other than UI thread
		/// </summary>
		/// <param name="next"></param> false for prior
		public void Select(bool next)
		{
			if (0 == Gname.Length || 0 == CurrentCar.Length)
				return;

			if (next)
			{
				if (++View.Selection >= simValues.Count)
					View.Selection = 0;
			}
			else if (0 < View.Selection)	// prior
				View.Selection--;
			else View.Selection = (byte)(simValues.Count - 1);
			SelectedStatus();
		}

		public void Swap()
		{
			string temp;
			for (int i = 0; i < simValues.Count; i++)
			{
				temp = simValues[i].Previous;
				simValues[i].Previous = simValues[i].Current;
				simValues[i].Current = temp;
			}
			Changed();
		}

		// set "CurrentAsDefaults" action
		internal void SetDefault()	// List<GameList> Glist)
		{
			if (0 == Gname.Length)
			{
				OOps("SetDefault: no Gname");
				return;
			}
			int p = View.Selection;
			string Current = simValues[p].Current;
			if (0 > gndx)
				Save_Car();
			simValues[p].Default = Current;
			Changed();
		}

		List<string> DefaultCopy()		// called in Save_Car()
		{
			int i;
			List<string> New = new List<string> { };
			for (i = 0; i < gCount; i++)
				New.Add(string.Copy(simValues[i].Default));
			return New;
		}

		List<string> CurrentCopy()		// called in Save_Car()
		{
			int i;
			List<string> New = new List<string> { };
			for (i = 0; i < pCount; i++)
				New.Add(string.Copy(simValues[i].Current));
			return New;
		}

		internal bool Save_Car()
		{
			if (null == CurrentCar || 0 == pCount)
				return false;			// nothing to save

			if (0 > GameIndex(Gname))	// first car for this game?
			{
				write = true;
				gndx = slim.data.gList.Count;
				slim.data.gList.Add(new GameList
				{ cList = new List<CarL>
					{ new CarL { Name = string.Copy(Gname), Vlist = DefaultCopy() } } });
			}

			if (0 > (cndx = slim.data.gList[gndx].cList.FindIndex(c => c.Name == CurrentCar)))
			{
				write = true;
				cndx = 1;
				slim.data.gList[gndx].cList.Add(new CarL
					{ Name = string.Copy(CurrentCar), Vlist = CurrentCopy() });
			}
			changed = false;		// Save_Car() updated per-car slim.data
			Changed();				// may still be per-game changes
			return write;
		}

		int GameIndex(string gnew)
		{
			if (1 > gnew.Length)
				return gndx;

			for (int g = 0; g < slim.data.gList.Count; g++)
				if (0 == slim.data.gList[g].cList.Count
				 || null == slim.data.gList[g].cList[0].Name)
					slim.data.gList.RemoveAt(g--);
				else if (gnew == slim.data.gList[g].cList[0].Name)
					gndx = g;
			return gndx;
		}

		void CarChange(string cname, string gnew)
		{
			int ml = 0;
			if (null !=cname && 0 < cname.Length && null != gnew && 0 < gnew.Length)	// valid?
			{
				Msg = "Current Car: " + cname;
				if (0 < Gname.Length && Save_Car())	 // do not save first (null) CurrentCar in game
					Msg += $";  {CurrentCar} saved";
				ml = Msg.Length;

				for (int i = 0; i < simValues.Count; i++)				// copy Current to previous
					simValues[i].Previous = simValues[i].Current;

				// indices for new car
				cndx = (0 <= GameIndex(gnew)) ?
								slim.data.gList[gndx].cList.FindIndex(c => c.Name == cname) : -1;

				New_Car = (-1 == cndx) ? "true" : "false";
				if (0 <= gndx)
				{														// matching GameList
					int i;

					GameList game = slim.data.gList[gndx];
					if (-1 < cndx)										// else leave current
					{													// existing car
						for (i = 0; i < pCount; i++)
							simValues[i].Current = game.cList[cndx].Vlist[i];
						if (null == CurrentCar)							// first in this game?
						{
							int vcount = game.cList[0].Vlist.Count;
							int count = pCount > vcount ? vcount : pCount;
                            for (i = 0; i < count; i++)
								simValues[i].Default = game.cList[0].Vlist[i];
							count = gCount > vcount ? vcount : gCount;
							for(; i < count; i++)
								simValues[i].Current = simValues[i].Default = game.cList[0].Vlist[i];
						}
					}
				}													// else reuse current properties
				CurrentCar = cname;
			}
			else if (null == cname)
				Msg = "null CarID";
			else if (0 == cname.Length)
				Msg = "empty CarID";

			if (null == gnew)
				Msg += ", null CurrentGame Name, ";
			else if (0 == gnew.Length)
				Msg += ", empty CurrentGame Name, ";
			else Gname = gnew;

			if (ml < Msg.Length)
				OOps(null);
			else Msg = "";
			View.Dispatcher.Invoke(() => View.Slslider_Point());	// invoke on another thread
			SelectedStatus();
			View.Model.ButtonVisibility = System.Windows.Visibility.Visible;	// ready
		}	// CarChange()
	}		// public partial class JSONio
}
