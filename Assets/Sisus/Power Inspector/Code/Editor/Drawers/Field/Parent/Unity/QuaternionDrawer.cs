#define DEBUG_NEXT_FIELD

using System;
using System.Reflection;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary> (Serializable) a vector 4G user interface drawer. This class cannot be
	/// inherited. </summary>
	[Serializable, DrawerForField(typeof(Quaternion), false, true)]
	public class QuaternionDrawer : ParentFieldDrawer<Quaternion>
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

		/// <summary> Creates a new instance of  </summary>
		/// <param name="value"> The value. </param>
		/// <param name="memberInfo"> Information describing the member. </param>
		/// <param name="parent"> The parent. </param>
		/// <param name="label"> The label. </param>
		/// <param name="setReadOnly"> True to set read only. </param>
		/// <returns> The  </returns>
		public static QuaternionDrawer Create(Quaternion value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			QuaternionDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new QuaternionDrawer();
			}
			result.Setup(value, typeof(Quaternion), memberInfo, parent, label, setReadOnly);
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
				fieldInfoX = Types.Quaternion.GetField("x", BindingFlags.Instance | BindingFlags.Public);
				fieldInfoY = Types.Quaternion.GetField("y", BindingFlags.Instance | BindingFlags.Public);
				fieldInfoZ = Types.Quaternion.GetField("z", BindingFlags.Instance | BindingFlags.Public);
				fieldInfoW = Types.Quaternion.GetField("w", BindingFlags.Instance | BindingFlags.Public);
			}
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((Quaternion)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			GenerateMemberInfos();

			var hierarchy = MemberHierarchy;
			memberBuildList.Add(hierarchy.Get(memberInfo, fieldInfoX));
			memberBuildList.Add(hierarchy.Get(memberInfo, fieldInfoY));
			memberBuildList.Add(hierarchy.Get(memberInfo, fieldInfoZ));
			memberBuildList.Add(hierarchy.Get(memberInfo, fieldInfoW));
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
				
				#if UNITY_2018_2_OR_NEWER
				var normalized = val.normalized;
				menu.Add("Normalize", () => Value = normalized, val == normalized);
				#endif

				var inverted = Quaternion.Inverse(val);
				menu.Add("Inverse", () => Value = inverted, val == inverted);

				var rounded = val;
				rounded.x = Mathf.Round(rounded.x);
				rounded.y = Mathf.Round(rounded.y);
				rounded.z = Mathf.Round(rounded.z);
				rounded.w = Mathf.Round(rounded.w);
				menu.Add("Round", () => Value = rounded, val == rounded);

				menu.Add("Set To.../Identity", ()=>Value = Quaternion.identity, val == Quaternion.identity);
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
					menu.Insert(copyIndex + 1, "Copy As Euler Angles", CopyEulerAnglesToClipboard);
				}

				if(Clipboard.CopiedType == Types.Vector3)
				{
					int pasteIndex = menu.IndexOf("Paste");
					if(pasteIndex != -1)
					{
						menu.Insert(pasteIndex + 1, "Paste From Euler Angles", PasteEulerAnglesFromClipboard);
					}
				}
			}
		}

		/// <summary> Copies the vector's magnitude to clipboard. </summary>
		private void CopyEulerAnglesToClipboard()
		{
			Clipboard.Copy(Value.eulerAngles);
			SendCopyToClipboardMessage("Copied{0} length", GetFieldNameForMessages());
		}

		/// <summary> Copies the vector's magnitude to clipboard. </summary>
		private void PasteEulerAnglesFromClipboard()
		{
			#if SAFE_MODE || DEV_MODE
			if(ReadOnly)
			{
				#if DEV_MODE
				Debug.LogWarning("PasteFromClipboard disabled for " + ToString()+" because ReadOnly");
				#endif
				return;
			}
			#endif
			
			// this fixes some problems like when you paste on an array field with its resize field being selected,
			// the array won't update its contents. It could theoretically also work by only changing the selection
			// if the selected object is grandchild of this field, but then that would break consistency (sometimes
			// selection changes, sometimes not). It could also work by deselecting whatever was previously selected,
			// but that would be weird when the pasting is done via a keyboard shortcut (ctrl+V).
			Select(ReasonSelectionChanged.Unknown);

			UndoHandler.RegisterUndoableAction(UnityObjects, "Paste From Clipboard");

			var setEulerAngles = Clipboard.Paste<Vector3>();
			SetValue(Quaternion.Euler(setEulerAngles));
			SendPasteFromClipboardMessage();
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