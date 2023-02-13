using System;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer for displaying a box with text information, a warning or an error.
	/// </summary>
	[Serializable]
	public sealed class BoxDrawer : ReadOnlyTextDrawer
	{
		private const float DefaultHeight = 39f;

		private float height = DefaultHeight;
		private MessageType messageType;

		/// <inheritdoc/>
		public override bool Selectable
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		public override float Height
		{
			get
			{
				return height + 2f; //add padding
			}
		}
		
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> Label with the text to shown in the box. </param>
		/// <param name="messageType"> Type of the message. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static BoxDrawer Create(IParentDrawer parent, GUIContent label, MessageType messageType, bool readOnly)
		{
			BoxDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new BoxDrawer();
			}
			result.Setup(label.text, null, parent, label, messageType, readOnly);
			result.LateSetup();
			return result;
		}

		/// <summary>
		/// Prevents a default instance of the drawer from being created.
		/// The Create method should be used instead of this constructor.
		/// </summary>
		private BoxDrawer() { }

		private void Setup(string setValue, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, MessageType setMessageType, bool setReadOnly)
		{
			messageType = setMessageType;
			Setup(setValue, typeof(string), setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		public override float GetOptimalPrefixLabelWidth(int indentLevel)
		{
			return DrawGUI.MinAutoSizedPrefixLabelWidth;
		}

		/// <inheritdoc/>
		public override bool Draw(Rect position)
		{
			//fill area behind with color to make it look nicer with prefix resize control
			position.height = Height + 2f;
			DrawGUI.Active.ColorRect(position, InspectorUtility.Preferences.theme.Background);

			//adjust position and height with single pixel padding
			position.y += 1f;
			position.height -= 2f;

			DrawGUI.Active.HelpBox(position, Value, messageType);

			return false;
		}

		/// <inheritdoc/>
		public override IDrawer GetNextSelectableDrawerUp(int column, IDrawer requester)
		{
			return parent.GetNextSelectableDrawerUp(column, this);
		}

		/// <inheritdoc/>
		public override IDrawer GetNextSelectableDrawerDown(int column, IDrawer requester)
		{
			return parent.GetNextSelectableDrawerDown(column, this);
		}

		/// <inheritdoc/>
		public override IDrawer GetNextSelectableDrawerLeft(bool moveToNextControlAfterReachingEnd, IDrawer requester)
		{
			return parent.GetNextSelectableDrawerLeft(moveToNextControlAfterReachingEnd, this);
		}

		/// <inheritdoc/>
		public override IDrawer GetNextSelectableDrawerRight(bool moveToNextControlAfterReachingEnd, IDrawer requester)
		{
			return parent.GetNextSelectableDrawerRight(moveToNextControlAfterReachingEnd, this);
		}

		/// <inheritdoc/>
		public override bool OnClick(Event inputEvent)
		{
			return false;
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			height = DefaultHeight;
			base.Dispose();
		}

		/// <inheritdoc/>
		protected override void OnLabelChanged()
		{
			SetCachedValueSilent(label.text);
			base.OnLabelChanged();
		}
	}
}