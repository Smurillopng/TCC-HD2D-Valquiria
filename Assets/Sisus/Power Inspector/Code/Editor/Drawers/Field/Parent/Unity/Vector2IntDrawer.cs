#define DEBUG_NEXT_FIELD

#if UNITY_2017_2_OR_NEWER // Vector2Int did not exist in versions ealier than 2017.2

using System;
using System.Reflection;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary> Drawer for Vector2Int controls. </summary>
	[Serializable, DrawerForField(typeof(Vector2Int), false, true)]
	public class Vector2IntDrawer : ParentFieldDrawer<Vector2Int>
	{
		/// <summary> True if FieldInfos generated for member fields. </summary>
		private static bool fieldInfosGenerated;

		/// <summary> The FieldInfo for the x member field. </summary>
		private static FieldInfo fieldInfoX;

		/// <summary> The FieldInfo for the y member field. </summary>
		private static FieldInfo fieldInfoY;

		/// <inheritdoc/>
		public override bool DrawInSingleRow
		{
			get { return true; }
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the class member that the created drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="setReadOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static Vector2IntDrawer Create(Vector2Int value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			Vector2IntDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new Vector2IntDrawer();
			}
			result.Setup(value, typeof(Vector2Int), memberInfo, parent, label, setReadOnly);
			result.LateSetup();
			return result;
		}

		/// <summary> Generates FieldInfos for members. This should be called at least once before
		/// DoGenerateMemberBuildList is called. </summary>
		private static void GenerateMemberInfos()
		{
			if(!fieldInfosGenerated)
			{
				fieldInfosGenerated = true;
				fieldInfoX = Types.Vector2Int.GetField("m_X", BindingFlags.Instance | BindingFlags.NonPublic);
				fieldInfoY = Types.Vector2Int.GetField("m_Y", BindingFlags.Instance | BindingFlags.NonPublic);
			}
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((Vector2Int)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			GenerateMemberInfos();

			var hierarchy = MemberHierarchy;
			memberBuildList.Add(hierarchy.Get(memberInfo, Vector2IntDrawer.fieldInfoX));
			memberBuildList.Add(hierarchy.Get(memberInfo, Vector2IntDrawer.fieldInfoY));
			
			#if DEV_MODE
			Debug.Assert(memberBuildList.Count == 2);
			#endif
		}

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberBuildList.Count == 2);
			#endif

			var first = Value;
			Array.Resize(ref members, 2);
			members[0] = IntDrawer.Create(first.x, memberBuildList[0], this, GUIContentPool.Create("X"), ReadOnly);
			members[1] = IntDrawer.Create(first.y, memberBuildList[1], this, GUIContentPool.Create("Y"), ReadOnly);
		}

		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			if(!ReadOnly)
			{
				menu.AddSeparatorIfNotRedundant();

				var val = Value;

				var normalized = ValueNormalized();
				menu.Add("Normalize", ()=>Value = normalized, val == normalized);

				menu.Add("Set To.../Zero", ()=>Value = Vector2Int.zero, val == Vector2Int.zero);
				menu.Add("Set To.../One", ()=>Value = Vector2Int.one, val == Vector2Int.one);
				menu.Add("Set To.../Up", ()=>Value = Vector2Int.up, val == Vector2Int.up);
				menu.Add("Set To.../Down", ()=>Value = Vector2Int.down, val == Vector2Int.down);
				menu.Add("Set To.../Left", ()=>Value = Vector2Int.left, val == Vector2Int.left);
				menu.Add("Set To.../Right", ()=>Value = Vector2Int.right, val == Vector2Int.right);
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			if(memberInfo == null || !memberInfo.MixedContent)
			{
				int copyIndex = menu.IndexOf("Copy");
				if(copyIndex != -1)
				{
					menu.Insert(copyIndex + 1, "Copy Length", CopyLengthToClipboard);
				}
			}
		}

		/// <summary> Copies the length to clipboard. </summary>
		private void CopyLengthToClipboard()
		{
			Clipboard.Copy(Value.magnitude);
			SendCopyToClipboardMessage("Copied{0} length", GetFieldNameForMessages());
		}

		/// <summary> Value normalized. </summary>
		/// <returns> A Vector2Int. </returns>
		private Vector2Int ValueNormalized()
		{
			var val = Value;
			if(val.x > 0)
			{
				val.x = 1;
			}
			else if(val.x < 0)
			{
				val.x = -1;
			}
			if(val.y > 0)
			{
				val.y = 1;
			}
			else if(val.y < 0)
			{
				val.y = -1;
			}
			return val;
		}

		/// <inheritdoc/>
		protected override bool TryToManuallyUpdateCachedValueFromMember(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			var setValue = Value;
			setValue[memberIndex] = (int)memberValue;
			Value = setValue;
			return true;
		}
	}
}
#endif