using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sisus
{
	public class Buttons
	{
		public const float ButtonDrawOffset = 3f;
		
		private List<Button> buttons;

		public Action<Buttons> onButtonRectsChanged;

		public Button this[int index]
		{
			get
			{
				return buttons[index];
			}
		}

		public int Count
		{
			get
			{
				return buttons.Count;
			}
		}

		public Rect Bounds
		{
			get
			{
				int count = buttons.Count;
				switch(count)
				{
					case 0:
						return default(Rect);
					case 1:
						return buttons[0].Rect;
					default:
						var rect = buttons[0].Rect;
						float x = rect.x;
						float y = rect.y;
						float xMax = rect.xMax;
						float yMax = rect.yMax;

						for(int n = count - 1; n >= 1; n--)
						{
							rect = buttons[n].Rect;
							if(rect.x < x)
							{
								x = rect.x;
							}
							if(rect.y < y)
							{
								y = rect.y;
							}
							if(rect.xMax > xMax)
							{
								xMax = rect.xMax;
							}
							if(rect.yMax > yMax)
							{
								yMax = rect.yMax;
							}
						}
						return Rect.MinMaxRect(x, y, xMax, yMax);
				}
			}
		}

		public Buttons(int capacity)
		{
			buttons = new List<Button>(capacity);
		}

		public void Add(Button button)
		{
			buttons.Add(button);
			button.onRectChanged += OnButtonRectChanged;
			OnButtonRectChanged(button);
		}
		
		public void Clear()
		{
			for(int n = buttons.Count - 1; n >= 0; n--)
			{
				var button = buttons[n];
				button.onRectChanged -= OnButtonRectChanged;
				button.Dispose();
			}
			buttons.Clear();

			if(onButtonRectsChanged != null)
			{
				onButtonRectsChanged(this);
			}
		}

		public float Width()
		{
			int count = buttons.Count;
			if(count == 0)
			{
				return 0f;
			}

			float width = buttons[0].Rect.width;
			for(int n = buttons.Count - 1; n >= 1; n--)
			{
				width += buttons[n].Rect.width + ButtonDrawOffset;
			}
			return width;
		}

		private void OnButtonRectChanged(Button button)
		{
			if(onButtonRectsChanged != null)
			{
				onButtonRectsChanged(this);
			}
		}
	}
}