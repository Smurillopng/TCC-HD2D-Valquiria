#define DEBUG_NEXT_FIELD

using System;
using System.Reflection;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary> Drawer for Size3D controls. </summary>
	[Serializable, DrawerForField(typeof(Size3D), false, true)]
	public sealed class Size3DDrawer : ParentFieldDrawer<Size3D>
	{
		/// <summary> True if FieldInfos generated for member fields. </summary>
		private static bool fieldInfosGenerated;

		/// <summary> The FieldInfo for the height member field. </summary>
		private static FieldInfo fieldInfoHeight;

		/// <summary> The FieldInfo for the width member field. </summary>
		private static FieldInfo fieldInfoWidth;

		/// <summary> The FieldInfo for the depth member field. </summary>
		private static FieldInfo fieldInfoDepth;

		/// <inheritdoc/>
		public override bool DrawInSingleRow
		{
			get
			{
				return true;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the class member that the created drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="setReadOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static Size3DDrawer Create(Size3D value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			Size3DDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new Size3DDrawer();
			}
			result.Setup(value, DrawerUtility.GetType(memberInfo, value), memberInfo, parent, label, setReadOnly);
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
				fieldInfoHeight = typeof(Size3D).GetField("height", BindingFlags.Instance | BindingFlags.Public);
				fieldInfoWidth = typeof(Size3D).GetField("width", BindingFlags.Instance | BindingFlags.Public);
				fieldInfoDepth = typeof(Size3D).GetField("depth", BindingFlags.Instance | BindingFlags.Public);
			}
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((Size3D)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			GenerateMemberInfos();

			var hierarchy = MemberHierarchy;
			memberBuildList.Add(hierarchy.Get(memberInfo, fieldInfoHeight));
			memberBuildList.Add(hierarchy.Get(memberInfo, fieldInfoWidth));
			memberBuildList.Add(hierarchy.Get(memberInfo, fieldInfoDepth));
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberBuildList.Count == 3);
			#endif
		}

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberBuildList.Count == 3);
			#endif

			var first = Value;
			Array.Resize(ref members, 3);
			members[0] = IntDrawer.Create(first.height, memberBuildList[0], this, GUIContentPool.Create("H"), ReadOnly);
			members[1] = IntDrawer.Create(first.width, memberBuildList[1], this, GUIContentPool.Create("W"), ReadOnly);
			members[2] = IntDrawer.Create(first.depth, memberBuildList[2], this, GUIContentPool.Create("D"), ReadOnly);
		}

		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			base.BuildContextMenu(ref menu, extendedMenu);

			if(memberInfo == null || !memberInfo.MixedContent)
			{
				int copyIndex = menu.IndexOf("Copy");
				if(copyIndex != -1)
				{
					menu.Insert(copyIndex + 1, "Copy Count", CopyLengthToClipboard);
				}
			}
		}

		/// <summary> Copies the length to clipboard. </summary>
		private void CopyLengthToClipboard()
		{
			Clipboard.Copy(Value.Count);
			SendCopyToClipboardMessage("Copied{0} count", GetFieldNameForMessages());
		}

		protected override bool DoSetValue(Size3D setValue, bool applyToField, bool updateMembers)
		{
			Size3D.Positive(ref setValue);
			return base.DoSetValue(setValue, applyToField, updateMembers);
		}

		/// <inheritdoc/>
		protected override bool TryToManuallyUpdateCachedValueFromMember(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			var setSize = Value;

			switch(memberIndex)
			{
				case 0:
					Size3D.SetHeight(ref setSize, (int)memberValue);
					break;
				case 1:
					Size3D.SetWidth(ref setSize, (int)memberValue);
					break;
				case 2:
					Size3D.SetDepth(ref setSize, (int)memberValue);
					break;
				default:
					Debug.LogError(StringUtils.Concat("memberIndex ", memberIndex, " was not between 0 and 2"));
					return false;
			}

			Value = setSize;
			return true;
		}
	}
}