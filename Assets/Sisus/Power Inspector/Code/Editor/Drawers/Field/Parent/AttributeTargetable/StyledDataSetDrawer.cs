#define DEBUG_DRAW_IN_SINGLE_ROW

using System;
using UnityEngine;
using Sisus.Attributes;
using JetBrains.Annotations;

namespace Sisus
{
	/// <summary>
	/// Like DataSetDrawer except draws members inside a GUIStyle container.
	/// GUIStyle to use can be configured using the UseDrawerAttribute, with the first parameter
	/// specifying the name of a GUIStyle found in the active theme of the inspector's preferences.
	/// Can also be configured to be drawn without the header.
	/// </summary>
	/// <example>
	/// <code>
	/// [UseDrawer(typeof(StyledDataSetDrawer), "VCS_StickyNote")]
	/// public MyClass dataSet = new MyClass();
	/// </code>
	/// </example>
	[Serializable]
	public sealed class StyledDataSetDrawer : ParentFieldDrawer<object>
	{
		private bool drawHeader;
		private GUIStyle style;
		private bool drawInSingleRow;
		private int appendIndentLevel;

		/// <inheritdoc/>
		public override bool Foldable
		{
			get
			{
				return base.Foldable && drawHeader;
			}
		}

		/// <inheritdoc />
		public override bool Selectable
		{
			get
			{
				return drawHeader && base.Selectable;
			}
		}

		/// <inheritdoc />
		public override float Height
		{
			get
			{
				if(!drawHeader && !DrawInSingleRow)
				{
					return base.Height - HeaderHeight;
				}
				return base.Height;
			}
		}

		/// <summary>
		/// Gets the append indent level.
		/// </summary>
		/// <value>
		/// The append indent level.
		/// </value>
		public override int AppendIndentLevel
		{
			get
			{
				return appendIndentLevel;
			}
		}

		/// <inheritdoc/>
		public override bool DrawInSingleRow
		{
			get
			{
				return drawInSingleRow || base.DrawInSingleRow;
			}
		}

		public void SetDrawInSingleRow(bool setDrawInSingleRow)
		{
			if(setDrawInSingleRow != drawInSingleRow)
			{
				drawInSingleRow = setDrawInSingleRow;
				GUI.changed = true;
				if(setDrawInSingleRow && !Unfolded)
				{
					SetUnfolded(true, false);
				}
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="style"> GUIStyle inside which to draw members. </param>
		/// <param name="appendIndentLevel"> How many levels of indentation to add when drawing members. </param>
		/// <param name="drawHeader"> Should the prefix label header be drawn for the dataset. </param>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static StyledDataSetDrawer Create(GUIStyle style, int appendIndentLevel, bool drawHeader, object value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			StyledDataSetDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new StyledDataSetDrawer();
			}
			result.Setup(style, appendIndentLevel, drawHeader, value, DrawerUtility.GetType(memberInfo, value), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public void SetupInterface(object attribute, object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			var useDrawer = (IDrawerSetupDataProvider)attribute;
			var parameters = useDrawer.GetSetupParameters();
			var setStyle = Inspector.Preferences.GetStyle((string)parameters[0]);
			int parameterCount = parameters.Length;
			var setIndent = parameterCount > 1 ? (int)parameters[1] : 1;
			var setDrawHeader = parameterCount > 2 ? (bool)parameters[2] : true;
			Setup(setStyle, setIndent, setDrawHeader, setValue, setValueType, setMemberInfo, setParent, setDrawHeader ? setLabel : GUIContent.none, setReadOnly);
		}

		/// <inheritdoc cref="IFieldDrawer.SetupInterface"/>
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			var parameterProvider = setMemberInfo.GetAttribute<IDrawerSetupDataProvider>();
			var parameters = parameterProvider.GetSetupParameters();
			var setStyle = Inspector.Preferences.GetStyle((string)parameters[0]);

			if(setStyle == null)
			{
				Debug.LogWarning("StyledDataSetDrawer.SetupInterface - failed to find style \"" + ((string)parameters[0]) + "\"");
				setStyle = GUI.skin.label;
			}

			int parameterCount = parameters.Length;
			var setIndent = parameterCount > 1 ? (int)parameters[1] : 1;
			var setDrawHeader = parameterCount > 2 ? (bool)parameters[2] : true;
			Setup(setStyle, setIndent, setDrawHeader, setValue, setValueType, setMemberInfo, setParent, setDrawHeader ? setLabel : GUIContent.none, setReadOnly);
		}

		/// <summary>
		/// Sets up the drawer so that it is ready to be used.
		/// LateSetup should be called right after this.
		/// </summary>
		/// <param name="setStyle"> GUIStyle inside which to draw members. </param>
		/// <param name="indentLevel"> How many levels of indentation to add when drawing members. </param>
		/// <param name="setDrawHeader"> Should the prefix label header be drawn for the dataset. </param>
		/// <param name="setValue"> The initial cached value of the drawers. </param>
		/// <param name="setValueType"> Type constraint for the value. </param>
		/// <param name="setMemberInfo"> LinkedMemberInfo for the field or property that these drawers represent. </param>
		/// <param name="setParent"> Drawer whose member these Drawer are. Can be null. </param>
		/// <param name="setLabel"> The label (name) of the field. Can be null. </param>
		/// <param name="setReadOnly"> True if Drawer should be read only. </param>
		private void Setup([NotNull]GUIStyle setStyle, int indentLevel, bool setDrawHeader, object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			style = new GUIStyle(setStyle);
			style.fixedHeight = 0f;
			style.fixedWidth = 0f;
			style.stretchHeight = true;
			style.stretchWidth = true;

			appendIndentLevel = indentLevel;
			drawHeader = setDrawHeader;
			
			if(setValueType == null)
			{
				#if DEV_MODE
				Debug.LogWarning(GetType().Name+".Setup called with null setValueType");
				#endif
				setValueType = DrawerUtility.GetType(setMemberInfo, setValue);
			}

			// This is an important step, because parent is referenced by DebugMode
			parent = setParent;
			drawInSingleRow = DrawerUtility.CanDrawInSingleRow(setValueType, DebugMode);

			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);

			#if DEV_MODE && DEBUG_DRAW_IN_SINGLE_ROW
			if(drawInSingleRow) {  Debug.Log(Msg(ToString(setLabel, setMemberInfo)+".Setup with drawInSingleRow=", drawInSingleRow, ", type=", setValueType, ", DebugMode=", DebugMode)); }
			#endif
		}

		/// <inheritdoc/>
		public override void OnAfterMembersBuilt()
		{
			if(!drawHeader && !Unfolded)
			{
				SetUnfoldedInstantly(true);
			}
			base.OnAfterMembersBuilt();
		}

		/// <inheritdoc/>
		protected override void Setup(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new InvalidOperationException("Please use the other Setup method.");
		}

		/// <inheritdoc/>
		protected override void GetDrawPositions(Rect position)
		{
			if(!drawHeader && !DrawInSingleRow)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(position.width > 0f, ToString()+".GetDrawPositions position width <= 0f: "+position);
				#endif

				lastDrawPosition = position;
				lastDrawPosition.height = 0f; //Height - HeaderHeight;

				labelLastDrawPosition = lastDrawPosition;

				bodyLastDrawPosition = lastDrawPosition;
				bodyLastDrawPosition.y += lastDrawPosition.height;
				bodyLastDrawPosition.height = DrawGUI.SingleLineHeight;

				DrawGUI.AddMarginsAndIndentation(ref labelLastDrawPosition);
				const float FoldoutArrowSize = 12f;
				labelLastDrawPosition.x -= FoldoutArrowSize;
				labelLastDrawPosition.width += FoldoutArrowSize;

				localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
			}
			else
			{
				base.GetDrawPositions(position);
			}
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			ParentDrawerUtility.GetMemberBuildList(this, MemberHierarchy, ref memberBuildList, DebugMode);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(DrawInSingleRow || members.Length < 5, Msg(ToString(), " built ", Members.Length, " members but DrawInSingleRow was ", DrawInSingleRow, ": ", StringUtils.ToString(members)));
			#endif
		}

		/// <inheritdoc/>
		public override bool Draw(Rect position)
		{
			if(Event.current.type == EventType.Repaint)
			{
				style.Draw(position, false, false, false, Selected);
			}

			// Also temporarily set all background colors in theme to match this color.
			var theme = Inspector.Preferences.theme;
			GUIThemeColors.BackgroundColors previousBackgroundColors;
			var transparentColor = new Color(0f, 0f, 0f, 0f);
			theme.SetBackgroundColors(transparentColor, out previousBackgroundColors);

			bool dirty;
			try
			{
				dirty = base.Draw(position);
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogError(e);
			#else
			catch(Exception)
			{
			#endif
				theme.RestoreBackgroundColors(previousBackgroundColors);
				throw;
			}

			theme.RestoreBackgroundColors(previousBackgroundColors);
			return dirty;
		}

		/// <inheritdoc/>
		public override bool DrawPrefix(Rect position)
		{
			if(!drawHeader)
			{
				return false;
			}
			return base.DrawPrefix(position);
		}
	}
}