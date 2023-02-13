using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public delegate void OnHeaderPartClicked(IUnityObjectDrawer containingDrawer, Rect clickedRect, Event inputEvent);
	public delegate Rect CalculatePosition(Rect headerRect);

	/// <summary>
	/// Class responsible for drawing a part of the header of Drawer.
	/// </summary>
	public class HeaderPartDrawer
	{
		private static readonly Pool<HeaderPartDrawer> Pool = new Pool<HeaderPartDrawer>(6);

		private static int NthCustomHeaderButton = -1;

		private HeaderPart part;
		private Rect rect;
		private OnHeaderPartClicked onClicked;
		private OnHeaderPartClicked onRightClicked;
		private Texture texture;
		private bool drawSelectionRect;
		private bool drawMouseoverRect;
		private bool selectable;
		private readonly GUIContent label = new GUIContent();
		private Color? guiColor;
		private CalculatePosition overrideCalculatePosition;
		private bool rectIsValid = false;

		[CanBeNull]
		public CalculatePosition OverrideCalculatePosition
		{
			get
			{
				return overrideCalculatePosition;
			}
		}

		public HeaderPart Part
		{
			get
			{
				return part;
			}
		}

		public Rect Rect
		{
			get
			{
				return rect;
			}

			set
			{
				rect = value;
				rectIsValid = true;
			}
		}

		public bool RectIsValid
		{
			get
			{
				return rectIsValid;
			}
		}

		public Texture Texture
		{
			get
			{
				return texture;
			}

			set
			{
				texture = value;
				label.image = value;
			}
		}

		public bool DrawMouseoverRect
		{
			get
			{
				return drawMouseoverRect;
			}
		}

		public bool Selectable
		{
			get
			{
				return selectable;
			}
		}

		public void SetGUIColor(Color color)
		{
			guiColor = color;
		}

		public static HeaderPartDrawer Create([NotNull]Texture icon, [NotNull]OnHeaderPartClicked onPartClicked, string tooltip = "", bool selectable = true)
		{
			Debug.Assert(icon != null);
			Debug.Assert(onPartClicked != null);

			HeaderPartDrawer result;
			if(!Pool.TryGet(out result))
			{
				result = new HeaderPartDrawer();
			}

			NthCustomHeaderButton++;
			if(NthCustomHeaderButton >= 10)
			{
				NthCustomHeaderButton = 0;
			}
			var headerPart = (HeaderPart)(NthCustomHeaderButton + ((int)HeaderPart.CustomHeaderButton1));
			result.Setup(headerPart, true, true, icon, tooltip, onPartClicked, onPartClicked, selectable);
			return result;
		}

		public static HeaderPartDrawer Create(HeaderPart headerPart, bool drawSelectionRect, bool drawMouseoverRect, string tooltip, OnHeaderPartClicked onPartClicked, OnHeaderPartClicked onPartRightClicked = null, bool selectable = true, CalculatePosition overrideCalculatePosition = null)
		{
			HeaderPartDrawer result;
			if(!Pool.TryGet(out result))
			{
				result = new HeaderPartDrawer();
			}

			result.Setup(headerPart, drawSelectionRect, drawMouseoverRect, tooltip, onPartClicked, onPartRightClicked, selectable, overrideCalculatePosition);
			return result;
		}

		public static HeaderPartDrawer Create(HeaderPart headerPart, bool drawSelectionRect, bool drawMouseoverRect, [CanBeNull]Texture drawTexture, string tooltip, OnHeaderPartClicked onPartClicked, OnHeaderPartClicked onPartRightClicked = null, bool selectable = true)
		{
			HeaderPartDrawer result;
			if(!Pool.TryGet(out result))
			{
				result = new HeaderPartDrawer();
			}
			result.Setup(headerPart, drawSelectionRect, drawMouseoverRect, drawTexture, tooltip, onPartClicked, onPartRightClicked, selectable);
			return result;
		}

		private void Setup(HeaderPart headerPart, bool drawsSelectionRect, bool drawsMouseoverRect, string tooltip, [CanBeNull]OnHeaderPartClicked onPartClicked, [CanBeNull]OnHeaderPartClicked onPartRightClicked, bool isSelectable, CalculatePosition setOverrideCalculatePosition)
		{
			part = headerPart;
			rect = default(Rect);
			drawSelectionRect = drawsSelectionRect;
			drawMouseoverRect = drawsMouseoverRect;
			onClicked = onPartClicked;
			onRightClicked = onPartRightClicked;
			selectable = isSelectable;
			overrideCalculatePosition = setOverrideCalculatePosition;

			label.image = null;
			label.tooltip = tooltip == null ? "" : tooltip;
		}
		
		private void Setup(HeaderPart headerPart, bool drawsSelectionRect, bool drawsMouseoverRect, [CanBeNull]Texture drawTexture, string tooltip, [CanBeNull]OnHeaderPartClicked onPartClicked, [CanBeNull]OnHeaderPartClicked onPartRightClicked, bool isSelectable)
		{
			part = headerPart;
			rect = default(Rect);
			drawSelectionRect = drawsSelectionRect;
			drawMouseoverRect = drawsMouseoverRect;
			texture = drawTexture;
			onClicked = onPartClicked;
			onRightClicked = onPartRightClicked;
			selectable = isSelectable;
			
			label.image = drawTexture;
			label.tooltip = tooltip == null ? "" : tooltip;
		}

		public bool MouseIsOver()
		{
			return rect.Contains(Cursor.LocalPosition);
		}

		public bool OnClicked(IUnityObjectDrawer containingDrawer, Event inputEvent)
		{
			#if DEV_MODE
			Debug.Log("HeaderPart."+part+ ".OnClick with onClicked="+StringUtils.ToString(onClicked));
			#endif

			if(onClicked != null)
			{
				onClicked(containingDrawer, rect, inputEvent);
				return true;
			}
			return false;
		}

		public bool OnRightClicked(IUnityObjectDrawer containingDrawer, Event inputEvent)
		{
			if(onRightClicked != null)
			{
				onRightClicked(containingDrawer, rect, inputEvent);
				return true;
			}
			return false;
		}

		public void Draw()
		{
			if(texture != null)
			{
				#if UNITY_2019_3_OR_NEWER
				if(MouseIsOver())
				{
					if(DrawGUI.IsProSkin)
					{
						DrawGUI.Active.ColorRect(rect, new Color(1f, 1f, 1f, 0.1f));
					}
					else
					{
						DrawGUI.Active.ColorRect(rect, new Color(1f, 1f, 1f, 0.6f));
					}
				}
				#endif

				var guiColorWas = GUI.color;
				if(guiColor.HasValue)
				{
					GUI.color = guiColor.Value;
				}

				GUI.Label(rect, label, InspectorPreferences.Styles.Centered);

				GUI.color = guiColorWas;
			}
			else if(label.tooltip.Length > 0)
			{
				GUI.Label(rect, label, InspectorPreferences.Styles.Centered);
			}
		}

		public void DrawSelectionRect()
		{
			if(drawSelectionRect)
			{
				DrawGUI.DrawControlSelectionIndicator(rect);
			}
		}
		
		public void Dispose()
		{
			onClicked = null;
			onRightClicked = null;
			texture = null;
			guiColor = null;
			overrideCalculatePosition = null;
			rectIsValid = false;
			//rect = default(Rect);

			var disposing = this;
			Pool.Dispose(ref disposing);
		}

		public static implicit operator HeaderPart(HeaderPartDrawer info)
		{
			return info == null ? HeaderPart.None : info.part;
		}

		public override string ToString()
		{
			return part.ToString();
		}
	}
}