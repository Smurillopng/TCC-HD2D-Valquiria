#define DEBUG_NEXT_FIELD

using System;
using System.Reflection;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary> Drawer for Vector2 controls. </summary>
	[Serializable, DrawerForField(typeof(Vector2), false, true)]
	public class Vector2Drawer : ParentFieldDrawer<Vector2>
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
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static Vector2Drawer Create(Vector2 value, [CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, GUIContent label, bool readOnly)
		{
			Vector2Drawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new Vector2Drawer();
			}
			result.Setup(value, typeof(Vector2), memberInfo, parent, label, readOnly);
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
				fieldInfoX = Types.Vector2.GetField("x", BindingFlags.Instance | BindingFlags.Public);
				fieldInfoY = Types.Vector2.GetField("y", BindingFlags.Instance | BindingFlags.Public);
			}
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((Vector2)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			GenerateMemberInfos();

			var hierarchy = MemberHierarchy;
			memberBuildList.Add(hierarchy.Get(memberInfo, Vector2Drawer.fieldInfoX));
			memberBuildList.Add(hierarchy.Get(memberInfo, Vector2Drawer.fieldInfoY));

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
			members[0] = FloatDrawer.Create(first.x, memberBuildList[0], this, GUIContentPool.Create("X"), ReadOnly);
			members[1] = FloatDrawer.Create(first.y, memberBuildList[1], this, GUIContentPool.Create("Y"), ReadOnly);
		}

		/// <inheritdoc/>
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
				menu.Add("Round", () => Value = rounded, val == rounded);

				menu.Add("Set To.../Zero", ()=>Value = Vector2.zero, val == Vector2.zero);
				menu.Add("Set To.../One", ()=>Value = Vector2.one, val == Vector2.one);
				menu.Add("Set To.../Up", ()=>Value = Vector2.up, val == Vector2.up);
				menu.Add("Set To.../Down", ()=>Value = Vector2.down, val == Vector2.down);
				menu.Add("Set To.../Left", ()=>Value = Vector2.left, val == Vector2.left);
				menu.Add("Set To.../Right", ()=>Value = Vector2.right, val == Vector2.right);
			}

			var transform = Transform;
			if(transform != null)
			{
				bool worldSpaceEqualsLocalSpace = false;
				if(transform.parent != null)
				{
					var pointTransformed = transform.parent.TransformPoint(val);
					worldSpaceEqualsLocalSpace = (Vector2)pointTransformed == val;
				}

				menu.AddSeparatorIfNotRedundant();

				if(canChangeValue)
				{
					menu.Add("Position/Convert/Local To World Point", LocalToWorldSpace, worldSpaceEqualsLocalSpace);
					menu.Add("Position/Convert/World To Local Point", WorldToLocalSpace, worldSpaceEqualsLocalSpace);
				}

				menu.Add("Position/Copy As World Point", CopyToClipboardAsWorldPoint);

				if(canChangeValue)
				{
					menu.Add("Position/Paste As Local Point", PasteFromClipboardAsLocalPoint);
				}
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

		/// <summary> Converts value from local to world space. </summary>
		private void LocalToWorldSpace()
		{
			var transform = Transform;
			if(transform.parent != null)
			{
				Value = transform.parent.TransformPoint(Value);
			}
		}

		/// <summary> Converts value from local to world space. </summary>
		private void WorldToLocalSpace()
		{
			var transform = Transform;
			if(transform.parent != null)
			{
				Value = transform.parent.InverseTransformPoint(Value);
			}
		}

		/// <summary> Copies to clipboard as world point. </summary>
		private void CopyToClipboardAsWorldPoint()
		{
			var transform = Transform;
			Clipboard.Copy(transform.parent != null ? (Vector2)transform.parent.TransformPoint(Value) : Value);
			Clipboard.SendCopyToClipboardMessage("Copied{0} as world point", GetFieldNameForMessages());
		}

		/// <summary> Pastes from clipboard as local point. </summary>
		private void PasteFromClipboardAsLocalPoint()
		{
			var world = (Vector2)Clipboard.Paste(Type);
			var transform = Transform;
			var local = transform.parent != null ? (Vector2)transform.parent.InverseTransformPoint(world) : world;
			Value = local;

			Clipboard.SendPasteFromClipboardMessage("Pasted{0} as local point", GetFieldNameForMessages());
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