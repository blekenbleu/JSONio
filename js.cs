using System.Collections.Generic;
using System.Windows;

namespace blekenbleu.jsonio
{
	public partial class JSONio
	{
		// check whether current properties differ from JSON
		internal bool Changed()
		{
			bool changed = false;

			// this should be unnecessary if slim.Reconcile() works..
			if (-1 < gndx && -1 < cndx
			 && (gCount != slim.data.gList[gndx].cList[0].Vlist.Count
			  || pCount != slim.data.gList[gndx].cList[cndx].Vlist.Count))
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
			return changed;
		}

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

		int GameIndex(string gnew)
		{
			if (1 > gnew.Length)
				return gndx;										// should be unlikely

			for (int g = 0; g < slim.data.gList.Count; g++)
				if (0 == slim.data.gList[g].cList.Count
				 || null == slim.data.gList[g].cList[0].Name)
					slim.data.gList.RemoveAt(g--);					// Reconcile() failure
				else if (gnew == slim.data.gList[g].cList[0].Name)
					gndx = g;
			return gndx;
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
			Changed();				// may still be per-game changes
			return write;
		}	// Save_Car()

		// Control.xaml methods -------------------------------------------------
		internal string FromSlider(double value)
		{
			simValues[slider].Current = (Slider_factor[0] * (int)value).ToString();
			Changed();
			return simValues[slider].Name + ":  " + simValues[slider].Current;
		}

		internal double ToSlider()
		{
			if(0 > slider)
				return 0;
			View.TBL.Text = simValues[slider].Name + ":  " + simValues[slider].Current;
			return Slider_factor[1] * System.Convert.ToDouble(simValues[slider].Current);
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

		private void SelectedStatus()		// also in CarChange()
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
			if (Current != simValues[p].Default)
			{
				simValues[p].Default = Current;
				write = true;
			}
			Changed();
		}

/*--------------------------------------------------------------
 ;      invoked for CarId changes, based on this `NCalcScripts/JSONio.ini` entry:
 ;          [ExportEvent]
 ;          name='CarChange'
 ;          trigger=changed(200, [DataCorePlugin.GameData.CarId])
 ;--------------------------------------------------------------- */
		void CarChange(string cname, string gnew)
		{
			int ml = 0;

			if (0 == simValues.Count)
				return;


			if (null !=cname && 0 < cname.Length && null != gnew && 0 < gnew.Length)	// valid?
			{
				GameList game = null;
				int i, count = 0, vcount = 0;

				Msg = "Current Car: " + cname;
				if (0 < Gname.Length && Save_Car())				// do not save first instance
					Msg += $";  {CurrentCar} saved";
				ml = Msg.Length;

				for (i = 0; i < simValues.Count; i++)			// copy Current to previous
					simValues[i].Previous = simValues[i].Current;

				// indices for new car
				if (0 <= GameIndex(gnew))						// sets gndx
				{
					game = slim.data.gList[gndx];
					cndx = game.cList.FindIndex(c => c.Name == cname);
					vcount = game.cList[0].Vlist.Count;
					count = gCount > vcount ? vcount : gCount;
				}
				else cndx = -1;

				if (0 > cndx)
				{
					New_Car = "true";
					if (0 <= gndx)									// set at line 132
					{												// not a new game
						if (gnew != Settings.game)
						{											// different game
							count = pCount > vcount ? vcount : pCount;
							for (i = 0; i < count; i++)				// per-car defaults
								simValues[i].Default = game.cList[0].Vlist[i];
						}
						for (i = pCount; i < count; i++)			// per-game defaults
							simValues[i].Default = game.cList[0].Vlist[i];	// perhaps altered since .ini
					}
				}
				else
				{													// existing car
						New_Car = "false";
						if (cname != Settings.carid)				// previous car?
							for (i = 0; i < pCount; i++)
								simValues[i].Current = game.cList[cndx].Vlist[i];
						if (null == CurrentCar)						// first in this game instance?
						{											// restore game defaults
							count = pCount > vcount ? vcount : pCount;
							for (i = 0; i < count; i++)
								simValues[i].Default = game.cList[0].Vlist[i];
							count = gCount > vcount ? vcount : gCount;
							for(i = pCount; i < count; i++)
								simValues[i].Current = simValues[i].Default = game.cList[0].Vlist[i];
						}
				}
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
