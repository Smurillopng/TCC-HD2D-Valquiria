#if UNITY_2017_2_OR_NEWER // Vector3Int did not exist in versions ealier than 2017.2

#define DEBUG_NEXT_FIELD

using System;
using System.Reflection;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary> Drawer for Vector2Int controls. </summary>
	[Serializable, DrawerForField(typeof(Vector3Int), false, true)]
	public class Vector3IntDrawer : ParentFieldDrawer<Vector3Int>
	{
		/// <summary> True if FieldInfos generated for member fields. </summary>
		private static bool fieldInfosGenerated;

		/// <summary> The FieldInfo for the x member field. </summary>
		private static FieldInfo fieldInfoX;

		/// <summary> The FieldInfo for the y member field. </summary>
		private static FieldInfo fieldInfoY;

		/// <summary> The FieldInfo for the z member field. </summary>
		private static FieldInfo fieldInfoZ;

		/// <inheritdoc/>
		public override bool DrawInSingleRow
		{
			get { return true; }
		}

		/// <summary> Creates a new Vector3IntDrawer. </summary>
		/// <param name="value"> The value. </param>
		/// <param name="memberInfo"> Information describing the member. </param>
		/// <param name="parent"> The parent. </param>
		/// <param name="label"> The label. </param>
		/// <param name="setReadOnly"> True to set read only. </param>
		/// <returns> The Vector3IntDrawer. </returns>
		public static Vector3IntDrawer Create(Vector3Int value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			Vector3IntDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new Vector3IntDrawer();
			}
			result.Setup(value, typeof(Vector3Int), memberInfo, parent, label, setReadOnly);
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
				fieldInfoX = Types.Vector3Int.GetField("m_X", BindingFlags.Instance | BindingFlags.NonPublic);
				fieldInfoY = Types.Vector3Int.GetField("m_Y", BindingFlags.Instance | BindingFlags.NonPublic);
				fieldInfoZ = Types.Vector3Int.GetField("m_Z", BindingFlags.Instance | BindingFlags.NonPublic);
			}
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((Vector3Int)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			GenerateMemberInfos();

			var hierarchy = MemberHierarchy;
			memberBuildList.Add(hierarchy.Get(memberInfo, Vector3IntDrawer.fieldInfoX));
			memberBuildList.Add(hierarchy.Get(memberInfo, Vector3IntDrawer.fieldInfoY));
			memberBuildList.Add(hierarchy.Get(memberInfo, Vector3IntDrawer.fieldInfoZ));
			
			#if DEV_MODE
			Debug.Assert(memberBuildList.Count == 3);
			#endif
		}

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberBuildList.Count == 3);
			#endif

			Array.Resize(ref members, 3);
			var first = Value;
			members[0] = IntDrawer.Create(first.x, memberBuildList[0], this, GUIContentPool.Create("X"), ReadOnly);
			members[1] = IntDrawer.Create(first.y, memberBuildList[1], this, GUIContentPool.Create("Y"), ReadOnly);
			members[2] = IntDrawer.Create(first.z, memberBuildList[2], this, GUIContentPool.Create("Z"), ReadOnly);
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

				menu.Add("Set To.../Zero", ()=>Value = Vector3Int.zero, val == Vector3Int.zero);
				menu.Add("Set To.../One", ()=>Value = Vector3Int.one, val == Vector3Int.one);
				menu.Add("Set To.../Up", ()=>Value = Vector3Int.up, val == Vector3Int.up);
				menu.Add("Set To.../Down", ()=>Value = Vector3Int.down, val == Vector3Int.down);
				menu.Add("Set To.../Left", ()=>Value = Vector3Int.left, val == Vector3Int.left);
				menu.Add("Set To.../Right", ()=>Value = Vector3Int.right, val == Vector3Int.right);
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
		/// <returns> A Vector3Int. </returns>
		private Vector3Int ValueNormalized()
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