using System.Collections.Generic;

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
				changed = true;
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
		}

		// set "CurrentAsDefaults" action
		internal void SetDefault()	// List<GameList> Glist)
		{
			if (0 == Gname.Length)
			{
				OOps("SetDefault: no Gname");
				return;
			}
			int p, Index = slim.data.gList.FindIndex(i => i.cList[0].Name == Gname);
			if (0 > Index)
			{
				CarChange(CurrentCar, Gname);
				Index = slim.data.gList.FindIndex(i => i.cList[0].Name == Gname);
			}
			if (0 > Index)
			{
				OOps($"SetDefault: {Gname} not in slim.data.gList");
				return;
			}
			p = View.Selection;
			changed |= simValues[p].Current != slim.data.gList[Index].cList[0].Vlist[p];
			slim.data.gList[Index].cList[0].Vlist[p] = simValues[p].Default = simValues[p].Current;
/*
			List<GameList> Glist = slim.data.gList;
			int p, Index = Glist.FindIndex(i => i.cList[0].Name == Gname);

			if (0 <= Index)
			{
				changed = true;
				for (p = 0; p < pCount; p++)
					Glist[Index].cList[0].Vlist[p] =			// first "car" has per-car game default values, then per-game
					simValues[p].Default = simValues[p].Current;
				for (; p < simValues.Count; p++)
					simValues[p].Default = simValues[p].Current;
			}
 */
		}

		// called in Save_Car()
		List<string> CurrentCopy()
		{
			List<string> New = new List<string> { };
			for (int i = 0; i < pCount; i++) { New.Add(string.Copy(simValues[i].Current)); }
			return New;
		}

		List<string> DefaultCopy()
		{
			List<string> New = new List<string> { };
			for (int i = 0; i < simValues.Count; i++) { New.Add(string.Copy(simValues[i].Default)); }
			return New;
		}

		// called in End() and 'CarChange' ("ChangeProperties")
		internal void Save_Car()	// update or create car; update or create game
		{
			if (null == CurrentCar || 0 == pCount || pCount > simValues.Count)	// weird state based on pCount??
				return;											// nothing to save

			var vList = DefaultCopy();							// search for game
			int gndex = GameIndex(Gname);
			if (0 > gndex)	 									// first car for this game?
			{
				changed = true;
				gndex = slim.data.gList.Count;
				slim.data.gList.Add(new GameList { cList = new List<CarL> { new CarL { Name = string.Copy(Gname), Vlist = vList } } });
			}
			else slim.Mod(gndex, 0, vList);						// game defaults may have changed

			vList = CurrentCopy();
			int cndex = slim.data.gList[gndex].cList.FindIndex(c => c.Name == CurrentCar);
			if (0 > cndex)
			{
				changed = true;
				slim.data.gList[gndex].cList.Add(new CarL { Name = string.Copy(CurrentCar), Vlist = vList });
			}
			else slim.Mod(gndex, cndex, vList);
		}

		int GameIndex(string gnew)
		{
			int gndx = -1;

			if (0 < gnew.Length)
				for (int g = 0; g < slim.data.gList.Count; g++)
					if (0 == slim.data.gList[g].cList.Count)
						slim.data.gList.RemoveAt(g--);
 					else if (gnew == slim.data.gList[g].cList[0].Name)
					{
						gndx = g;
						break;
					}

			return gndx;
		}

		void CarChange(string cname, string gnew)
		{
				int ml = 0;
				if (null !=cname && 0 < cname.Length && null != gnew)		// valid new car?
				{
					Msg = "Current Car: " + cname;
					if (0 < Gname.Length)									 // do not save first (null) CurrentCar in game
					{
						Save_Car();
						if (changed)
							Msg += $";  {CurrentCar} saved";
					}
					ml = Msg.Length;

					for (int i = 0; i < simValues.Count; i++)				// copy Current to previous
						simValues[i].Previous = simValues[i].Current;

					// indices for new car
					int gndx = GameIndex(gnew);
					int cndx = (0 <= gndx) ? slim.data.gList[gndx].cList.FindIndex(c => c.Name == cname) : -1;

					New_Car = (-1 == cndx) ? "true" : "false";						
					CurrentCar = cname;
					if (0 <= gndx)
					{														// copy matching values from GameList
						int i;

						GameList game = slim.data.gList[gndx];
						if (0 > cndx)
							for (i = 0; i < gCount; i++)
								simValues[i].Current = simValues[i].Default = game.cList[0].Vlist[i];
						else for (i = 0; i < pCount; i++)
						{
							simValues[i].Current = game.cList[cndx].Vlist[i];
							simValues[i].Default = game.cList[0].Vlist[i];
						}
					}														// else reuse current properties
				}
				else if (null == cname)		// CarID verification - should make a popup
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
				View.Dispatcher.Invoke(() => View.Slslider_Point());	// invoke from another thread
				SelectedStatus();
				View.Model.ButtonVisibility = System.Windows.Visibility.Visible;	// ready for business
		}
	}
}
