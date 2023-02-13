#define DEBUG_NEXT_FIELD

using System;
using System.Reflection;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary> (Serializable) a vector 3G user interface drawer. This class cannot be
	/// inherited. </summary>
	[Serializable, DrawerForField(typeof(Vector3), false, true)]
	public class Vector3Drawer : ParentFieldDrawer<Vector3>
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

		/// <summary> Creates a new instance of Vector3Drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the created drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> Ready-to-use instance of RectDrawer. </returns>
		public static Vector3Drawer Create(Vector3 value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			Vector3Drawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new Vector3Drawer();
			}
			result.Setup(value, typeof(Vector3), memberInfo, parent, label, readOnly);
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
				fieldInfoX = Types.Vector3.GetField("x", BindingFlags.Instance | BindingFlags.Public);
				fieldInfoY = Types.Vector3.GetField("y", BindingFlags.Instance | BindingFlags.Public);
				fieldInfoZ = Types.Vector3.GetField("z", BindingFlags.Instance | BindingFlags.Public);
			}
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((Vector3)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}
		
		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			GenerateMemberInfos();

			var hierarchy = MemberHierarchy;
			memberBuildList.Add(hierarchy.Get(memberInfo, fieldInfoX));
			memberBuildList.Add(hierarchy.Get(memberInfo, fieldInfoY));
			memberBuildList.Add(hierarchy.Get(memberInfo, fieldInfoZ));
			
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

			var first = Value;
			Array.Resize(ref members, 3);
			members[0] = FloatDrawer.Create(first.x, memberBuildList[0], this, GUIContentPool.Create("X"), ReadOnly);
			members[1] = FloatDrawer.Create(first.y, memberBuildList[1], this, GUIContentPool.Create("Y"), ReadOnly);
			members[2] = FloatDrawer.Create(first.z, memberBuildList[2], this, GUIContentPool.Create("Z"), ReadOnly);
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
				menu.Add("Normalize", ()=>Value = normalized, val == normalized);

				var rounded = val;
				rounded.x = Mathf.Round(rounded.x);
				rounded.y = Mathf.Round(rounded.y);
				rounded.z = Mathf.Round(rounded.z);
				menu.Add("Round", ()=>Value = rounded, val == rounded);

				menu.Add("Set To.../Zero", ()=>Value = Vector3.zero, val.IsZero());
				menu.Add("Set To.../One", ()=>Value = Vector3.one, val == Vector3.one);
				menu.Add("Set To.../Up", ()=>Value = Vector3.up, val == Vector3.up);
				menu.Add("Set To.../Down", ()=>Value = Vector3.down, val == Vector3.down);
				menu.Add("Set To.../Left", ()=>Value = Vector3.left, val == Vector3.left);
				menu.Add("Set To.../Right", ()=>Value = Vector3.right, val == Vector3.right);
				menu.Add("Set To.../Forward", ()=>Value = Vector3.forward, val == Vector3.forward);
				menu.Add("Set To.../Back", ()=>Value = Vector3.back, val == Vector3.back);
			}

			var transform = Transform;
			if(transform != null)
			{
				bool worldSpaceEqualsLocalSpace = false;
				if(transform.parent != null)
				{
					var pointTransformed = transform.parent.TransformPoint(val);
					worldSpaceEqualsLocalSpace = pointTransformed == val;
				}

				menu.AddSeparatorIfNotRedundant();

				if(canChangeValue)
				{
					//TO DO: If any Vector3 could be viewed in World Space, then these would no longer be needed I think
					//since it can all be done with a combination of changing to world space view, copying and pasting
					menu.Add("Position/Local To World Point", LocalToWorldSpace, worldSpaceEqualsLocalSpace);
					menu.Add("Position/World To Local Point", WorldToLocalSpace, worldSpaceEqualsLocalSpace);
				}

				menu.Add("Position/Copy As World Point", CopyToClipboardAsWorldPoint);

				if(canChangeValue)
				{
					menu.Add("Position/Paste As World Point", PasteFromClipboardAsWorldPoint);
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

		/// <summary> Local to world space. </summary>
		private void LocalToWorldSpace()
		{
			var transform = Transform;
			if(transform.parent != null)
			{
				Value = transform.parent.TransformPoint(Value);
			}
		}

		/// <summary> World to local space. </summary>
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
			Clipboard.Copy(transform.parent != null ? transform.parent.TransformPoint(Value) : Value);
			Clipboard.SendCopyToClipboardMessage("Copied{0} as world point", GetFieldNameForMessages());
		}

		/// <summary> Pastes from clipboard as world point. </summary>
		private void PasteFromClipboardAsWorldPoint()
		{
			var world = (Vector3)Clipboard.Paste(Type);
			var transform = Transform;
			var local = transform.parent != null ? transform.parent.InverseTransformPoint(world) : world;
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