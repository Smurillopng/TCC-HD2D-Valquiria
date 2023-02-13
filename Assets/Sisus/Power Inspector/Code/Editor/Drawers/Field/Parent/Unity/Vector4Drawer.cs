#define DEBUG_NEXT_FIELD

using System;
using System.Reflection;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary> (Serializable) a vector 4G user interface drawer. This class cannot be
	/// inherited. </summary>
	[Serializable, DrawerForField(typeof(Vector4), false, true)]
	public class Vector4Drawer : ParentFieldDrawer<Vector4>
	{
		/// <summary> True if field infos generated. </summary>
		private static bool fieldInfosGenerated;

		/// <summary> The FieldInfo for the x member field. </summary>
		private static FieldInfo fieldInfoX;

		/// <summary> The FieldInfo for the y member field. </summary>
		private static FieldInfo fieldInfoY;

		/// <summary> The FieldInfo for the z member field. </summary>
		private static FieldInfo fieldInfoZ;

		/// <summary> The FieldInfo for the w member field. </summary>
		private static FieldInfo fieldInfoW;

		/// <inheritdoc/>
		public override bool DrawInSingleRow
		{
			get { return true; }
		}

		/// <summary> Creates a new instance of Vector4Drawer. </summary>
		/// <param name="value"> The value. </param>
		/// <param name="memberInfo"> Information describing the member. </param>
		/// <param name="parent"> The parent. </param>
		/// <param name="label"> The label. </param>
		/// <param name="setReadOnly"> True to set read only. </param>
		/// <returns> The Vector4Drawer. </returns>
		public static Vector4Drawer Create(Vector4 value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			Vector4Drawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new Vector4Drawer();
			}
			result.Setup(value, typeof(Vector4), memberInfo, parent, label, setReadOnly);
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
				fieldInfoX = Types.Vector4.GetField("x", BindingFlags.Instance | BindingFlags.Public);
				fieldInfoY = Types.Vector4.GetField("y", BindingFlags.Instance | BindingFlags.Public);
				fieldInfoZ = Types.Vector4.GetField("z", BindingFlags.Instance | BindingFlags.Public);
				fieldInfoW = Types.Vector4.GetField("w", BindingFlags.Instance | BindingFlags.Public);
			}
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((Vector4)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			GenerateMemberInfos();

			var hierarchy = MemberHierarchy;
			memberBuildList.Add(hierarchy.Get(memberInfo, Vector4Drawer.fieldInfoX));
			memberBuildList.Add(hierarchy.Get(memberInfo, Vector4Drawer.fieldInfoY));
			memberBuildList.Add(hierarchy.Get(memberInfo, Vector4Drawer.fieldInfoZ));
			memberBuildList.Add(hierarchy.Get(memberInfo, Vector4Drawer.fieldInfoW));
		}

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberBuildList.Count == 4);
			#endif

			var first = Value;
			Array.Resize(ref members, 4);
			members[0] = FloatDrawer.Create(first.x, memberBuildList[0], this, GUIContentPool.Create("X"), ReadOnly);
			members[1] = FloatDrawer.Create(first.y, memberBuildList[1], this, GUIContentPool.Create("Y"), ReadOnly);
			members[2] = FloatDrawer.Create(first.z, memberBuildList[2], this, GUIContentPool.Create("Z"), ReadOnly);
			members[3] = FloatDrawer.Create(first.w, memberBuildList[3], this, GUIContentPool.Create("W"), ReadOnly);
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			bool canChangeValue = !ReadOnly;
			var val = Value;

			if(canChangeValue)
			{
				menu.AddSeparatorIfNotRedundant();

				var normalized = val.normalized;
				menu.Add("Normalize", () => Value = normalized, val == normalized);

				var rounded = val;
				rounded.x = Mathf.Round(rounded.x);
				rounded.y = Mathf.Round(rounded.y);
				rounded.z = Mathf.Round(rounded.z);
				rounded.w = Mathf.Round(rounded.w);
				menu.Add("Round", () => Value = rounded, val == rounded);

				menu.Add("Set To.../Zero", ()=>Value = Vector4.zero, val == Vector4.zero);
				menu.Add("Set To.../One", ()=>Value = Vector4.one, val == Vector4.one);
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

		/// <summary> Copies the vector's magnitude to clipboard. </summary>
		private void CopyLengthToClipboard()
		{
			Clipboard.Copy(Value.magnitude);
			SendCopyToClipboardMessage("Copied{0} length", GetFieldNameForMessages());
		}

		/// <inheritdoc/>
		protected override bool TryToManuallyUpdateCachedValueFromMember(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			var setValue = Value;
			setValue[memberIndex] = (float)memberValue;
			Value = setValue;
			return true;
		}
	}
}