using System;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer for displaying information about inspected GameObjects having
	/// Components that can't be displayed in merged multi-editing mode.
	/// </summary>
	public sealed class GameObjectBoxDrawer : BaseDrawer
	{
		private const float BoxHeight = 39f;
		private const float TotalHeight = 52f;

		private Rect splitterRect;
		private Rect boxRect;

		/// <inheritdoc />
		public override Type Type
		{
			get
			{
				return typeof(string);
			}
		}

		/// <inheritdoc />
		public override float Height
		{
			get
			{
				return TotalHeight;
			}
		}

		/// <inheritdoc />
		public override bool ReadOnly
		{
			get
			{
				return true;
			}
		}

		public static GameObjectBoxDrawer Create(IParentDrawer parent, GUIContent label)
		{
			GameObjectBoxDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new GameObjectBoxDrawer();
			}
			result.Setup(parent, label);
			result.LateSetup();
			return result;
		}

		private GameObjectBoxDrawer() { }

		/// <inheritdoc />
		public override float GetOptimalPrefixLabelWidth(int indentLevel)
		{
			return DrawGUI.MinAutoSizedPrefixLabelWidth;
		}

		/// <inheritdoc />
		protected override void GetDrawPositions(Rect position)
		{
			splitterRect = position;
			splitterRect.height = 1f;
			splitterRect.y += 3f;
			splitterRect.width = Screen.width;

			boxRect = position;
			boxRect.y += 8f;
			boxRect.height = BoxHeight;

			lastDrawPosition = position;

			localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
		}

		/// <inheritdoc />
		public override bool Draw(Rect position)
		{
			if(Event.current.type == EventType.Layout)
			{
				OnLayoutEvent(position);
			}

			GUI.DrawTexture(splitterRect, InspectorUtility.Preferences.graphics.horizontalSplitterBg);

			DrawGUI.Active.HelpBox(boxRect, label.text, MessageType.Info);
			
			return false;
		}
	}
}