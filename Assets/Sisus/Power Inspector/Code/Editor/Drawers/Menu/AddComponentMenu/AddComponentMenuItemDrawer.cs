using System;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus
{
	/// <summary>
	/// Drawer for an item representing a Component in an open Add Component popup menu.
	/// </summary>
	public sealed class AddComponentMenuItemDrawer : BaseDrawer
	{
		public static IDrawer activeItem;
		
		private AddComponentMenuItem item;

		private bool wasJustClicked;
		public bool nameBy;

		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				return item.type;
			}
		}

		public AddComponentMenuItem Item
		{
			get
			{
				return item;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="item"> Information about the menu item. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static AddComponentMenuItemDrawer Create(AddComponentMenuItem item)
		{
			AddComponentMenuItemDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new AddComponentMenuItemDrawer();
			}
			result.Setup(item);
			result.LateSetup();
			return result;
		}

		/// <summary>
		/// Prevents a default instance of the drawers from being created.
		/// The Create method should be used instead of this constructor.
		/// </summary>
		private AddComponentMenuItemDrawer() { }

		/// <inheritdoc/>
		protected sealed override void Setup(IParentDrawer setParent, GUIContent setLabel)
		{
			throw new NotSupportedException("Please use the other Setup method of AddComponentMenuItemDrawer.");
		}

		/// <summary>
		/// Sets up the drawer so that it is ready to be used.
		/// LateSetup should be called right after this.
		/// </summary>
		/// <param name="setItem"> Information about the menu item. </param>
		private void Setup(AddComponentMenuItem setItem)
		{
			item = setItem;
			label = GUIContentPool.Create(setItem.label);
		}

		/// <inheritdoc />
		public override object GetValue(int index)
		{
			return item.label;
		}

		/// <inheritdoc />
		public override bool SetValue(object newValue)
		{
			if(parent != null)
			{
				return parent.SetValue(newValue.ToString());
			}
			return false;
		}

		/// <inheritdoc />
		public override bool Draw(Rect position)
		{
			if(Event.current.type == EventType.Layout)
			{
				OnLayoutEvent(position);
			}

			const float previewSize = 20f;

			var buttonRect = position;
			buttonRect.x += previewSize;
			buttonRect.width -= previewSize;
			if(GUI.Button(buttonRect, label, activeItem == this ? DrawGUI.prefixLabelWhite : DrawGUI.prefixLabel))
			{
				#if DEV_MODE
				Debug.Log(GetType().Name +" - GUI.Button clicked");
				#endif
				wasJustClicked = true;
				DrawGUI.Use(Event.current);
			}

			var preview = item.Preview;

			if(item.IsGroup)
			{
				var iconRect = buttonRect;
				iconRect.width = previewSize - 3f;
				iconRect.height = previewSize - 3f;
				iconRect.x -= previewSize - 2f;
				iconRect.y += 2f;

				GUI.DrawTexture(iconRect, InspectorUtility.Preferences.theme.graphics.DirectoryIcon);

				if(preview != null)
				{
					iconRect.x += 5f;
					iconRect.y += 5f;
					iconRect.width -= 5f;
					iconRect.height -= 5f;
					GUI.DrawTexture(iconRect, preview, ScaleMode.ScaleToFit);
				}

				var arrowRect = buttonRect;
				arrowRect.x += buttonRect.width - previewSize;
				arrowRect.y += 2f;
				GUIStyle arrowStyle = "AC RightArrow";
				arrowRect.width = arrowStyle.normal.background.width;
				arrowRect.height = arrowStyle.normal.background.height;
				GUI.Label(arrowRect, GUIContent.none, arrowStyle);
			}
			else if(preview != null)
			{
				var iconRect = buttonRect;
				iconRect.width = 16f;
				iconRect.height = 16f;
				iconRect.x -= 18f;
				iconRect.y += 2f;

				GUI.DrawTexture(iconRect, preview, ScaleMode.ScaleToFit);
			}
			
			if(wasJustClicked)
			{
				wasJustClicked = false;
				GUI.changed = true;
				return true;
			}

			return false;
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			menu.Add("Add Component", ()=>wasJustClicked = true);

			menu.Add("Add Component And Name By", ()=>
			{
				wasJustClicked = true;
				nameBy = true;
			});

			#if UNITY_EDITOR
			if(GetMonoScript() != null)
			{
				menu.Add("Ping Script Asset", PingAsset);
			}
			#endif
		}

		#if UNITY_EDITOR
		[CanBeNull]
		public UnityEditor.MonoScript GetMonoScript()
		{
			var type = Type;
			return type != null ? FileUtility.FindScriptFile(type) : null;
		}

		public void PingAsset()
		{
			var script = GetMonoScript();
			if(script != null)
			{
				GUI.changed = true;
				DrawGUI.Active.PingObject(script);
			}
		}
		#endif
		
		/// <inheritdoc />
		public override void Dispose()
		{
			if(activeItem == this)
			{
				activeItem = null;
			}
			wasJustClicked = false;
			nameBy = false;
			base.Dispose();
		}

		/// <inheritdoc />
		public override void OnMouseover()
		{
			//do nothing
		}
	}
}