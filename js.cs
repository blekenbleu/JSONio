using System;
using System.Collections.Generic;
using System.Windows;

namespace blekenbleu.jsonio
{
	public partial class JSONio
	{
		// check whether current properties differ from JSON
		bool Changed()
		{
			bool changed = false;
			
			if ((0 > gndx || 0 > cndx) && SaveCar())
				return changed;
			// this should be unnecessary if slim.Reconcile() works..
			if (gCount != slim.data.gList[gndx].cList[0].Vlist.Count
			 || pCount != slim.data.gList[gndx].cList[cndx].Vlist.Count)
				changed = true;
			else for (int p = 0; p < gCount; p++)
				if (simValues[p].Default != slim.data.gList[gndx].cList[0].Vlist[p]			// per-game default change?
		 		 || p < pCount && simValues[p].Current != slim.data.gList[gndx].cList[cndx].Vlist[p]) // per-car change?
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

			View.Model.SliderProperty = simValues[slider].Name;
			/* slider View.SL.Maximum = 100; scale property to it, based on Steps[slider]
			 ; Steps	   Guestimated range
			 ; 1  (0.01)	0 - 2
			 ; 10 (0.10)	0 - 10
			 ; 100 (1)		0 - 100
			 ; 1000 (10)	0 - 1000
			 */
			if (100 < Convert.ToInt32(simValues[slider].Default))
			{
				SliderFactor[0] = 10;	// slider to value
				SliderFactor[1] = 0.1;	// value to slider
			}
			else if (0 != Steps[slider] % 10)
			{
				SliderFactor[0] = 0.02;	// slider to value
				SliderFactor[1] = 50;	// value to slider
			} else if (0 != Steps[slider] % 100) {
				SliderFactor[0] = 0.1;	// slider to value
				SliderFactor[1] = 10;	// value to slider
			} else {
				SliderFactor[0] = 1;	// slider to value
				SliderFactor[1] = 1;	// value to slider
			}
		}

		List<string> DefaultCopy()		// called in SaveCar()
		{
			int i;
			List<string> New = new List<string> { };
			for (i = 0; i < gCount; i++)
				New.Add(string.Copy(simValues[i].Default));
			return New;
		}

		List<string> CurrentCopy()		// called in SaveCar()
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

		internal bool SaveCar()
		{
			if (null == CurrentCar || 0 == pCount)
				return false;			// nothing to save

			if (0 > GameIndex(Gname))	// first car for this game?
			{
				write = true;
				gndx = slim.data.gList.Count;
				slim.data.gList.Add(new GameList
				{ cList = new List<CarL>
					{ new CarL { Name = string.Copy(Gname),
								 Vlist = DefaultCopy()
								}
					}
				});
			}

			if (0 > (cndx = slim.data.gList[gndx].cList.FindIndex(c => c.Name == CurrentCar)))
			{
				write = true;
				cndx = slim.data.gList[gndx].cList.Count;
				slim.data.gList[gndx].cList.Add(new CarL
					{ Name = string.Copy(CurrentCar),
					  Vlist = CurrentCopy()
					});
			}
			Changed();				// may still be per-game changes
			return write;
		}	// SaveCar()

		// Control.xaml methods -------------------------------------------------
		internal string FromSlider(double value)
		{
			simValues[slider].Current = (SliderFactor[0] * (int)value).ToString();
			Changed();
			return simValues[slider].Name + ":  " + simValues[slider].Current;
		}

		internal double ToSlider()
		{
			if(0 > slider)
				return 0;
			View.Model.SliderProperty = simValues[slider].Name + ":  " + simValues[slider].Current;
			return SliderFactor[1] * System.Convert.ToDouble(simValues[slider].Current);
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
					View.SliderPoint();
			}
		}

		private void SelectedStatus()		// also in CarChange()
		{
			if (null == View)
				return;
			View.Model.SelectedProperty = simValues[View.Selection].Name;
			View.Model.StatusText = Gname + " " + CurrentCar + ":\t" + View.Model.SelectedProperty;
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
			View.SliderPoint();
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
			simValues[p].Default = simValues[p].Current;	// End() sorts per-game changes
			Changed();
		}

		// set "SelectedAsSlider" action
		internal void SelectSlider()	// List<GameList> Glist)
		{
			slider = View.Selection;
			SetSlider();
			View.SliderPoint();
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
				if (0 < Gname.Length && SaveCar())				// do not save first instance
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
					NewCar = "true";
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
						NewCar = "false";
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
			View.Dispatcher.Invoke(() => View.SliderPoint());	// invoke on another thread
			SelectedStatus();
			View.Model.ButtonVisibility = System.Windows.Visibility.Visible;	// ready
		}	// CarChange()
	}		// public partial class JSONio
}
