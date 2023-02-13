//#define DEBUG_DRAW_IN_SINGLE_ROW

using System;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer for generic non-primitive class or struct.
	/// inherited. </summary>
	[Serializable]
	public sealed class DataSetDrawer : ParentFieldDrawer<object>
	{
		private bool drawInSingleRow;

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
				Inspector.InspectorDrawer.Repaint();
				if(setDrawInSingleRow && !Unfolded)
				{
					SetUnfolded(true, false);
				}
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static DataSetDrawer Create(object value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			DataSetDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new DataSetDrawer();
			}
			result.Setup(value, DrawerUtility.GetType(memberInfo, value), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void Setup(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{	
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberBuildState == MemberBuildState.Unstarted);
			#endif

			if(setValue == null)
			{
				setValueType = DrawerUtility.GetType(setMemberInfo, setValue);
			}

			// This is an important step, because parent is referenced by DebugMode
			parent = setParent;

			drawInSingleRow = (setMemberInfo != null && setMemberInfo.GetAttribute<Attributes.DrawInSingleRowAttribute>() != null) || (setValueType != null && setValueType.GetCustomAttributes(typeof(Attributes.DrawInSingleRowAttribute), false).Length > 0) || DrawerUtility.CanDrawInSingleRow(setValueType, DebugMode);

			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Value != null || DrawInSingleRow);
			Debug.Assert(Value != null || CanBeNull);
			#endif


			#if DEV_MODE && DEBUG_DRAW_IN_SINGLE_ROW
			if(drawInSingleRow) {  Debug.Log(Msg(ToString(setLabel, setMemberInfo)+".Setup with drawInSingleRow=", drawInSingleRow, ", setValueType=", setValueType, ", DebugMode=", DebugMode, ", setMemberInfo.Type="+(setMemberInfo == null ? "n/a" : StringUtils.ToString(setMemberInfo.Type)) +", setValue.GetType()=", StringUtils.TypeToString(setValue))); }
			#endif
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			if(CanBeNull && Value == null)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString() + ".DoBuildMembers called with Value=null and CanBeNull=true. Will leave memberBuildList empty.");
				#endif

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(memberBuildList.Count == 0);
				#endif

				return;
			}

			ParentDrawerUtility.GetMemberBuildList(this, MemberHierarchy, ref memberBuildList, DebugMode);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(DrawInSingleRow || members.Length < 5, Msg(ToString(), " built ", Members.Length, " members but DrawInSingleRow was ", DrawInSingleRow, ": ", StringUtils.ToString(members)));
			#endif
		}

		/// <inheritdoc/>
		public override bool DrawBodySingleRow(Rect position)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(position.width > 0f, ToString()+".DrawBodySingleRow called with position "+position);
			#endif

			DrawDragBarIfReorderable();

			if(Value == null && !Foldable && VisibleMembers.Length == 0)
			{
				bool guiWasEnabled = GUI.enabled;
				GUI.enabled = false;
				GUI.Label(position, "null");
				GUI.enabled = guiWasEnabled;
				return false;
			}

			return ParentDrawerUtility.DrawBodySingleRow(this, memberRects);
		}
	}
}