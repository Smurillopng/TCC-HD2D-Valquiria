#define DEBUG_NEXT_FIELD

using System;
using System.Reflection;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(RectOffset), false, true)]
	public class RectOffsetDrawer : ParentFieldDrawer<RectOffset>
	{
		/// <summary> True if PropertyInfos generated for member properties. </summary>
		private static bool fieldInfosGenerated;

		/// <summary> The PropertyInfo for the "left" member property. </summary>
		private static PropertyInfo propertyInfoLeft;
		/// <summary> The PropertyInfo for the "right" member property. </summary>
		private static PropertyInfo propertyInfoRight;
		/// <summary> The PropertyInfo for the "top" member property. </summary>
		private static PropertyInfo propertyInfoTop;
		/// <summary> The PropertyInfo for the "bottom" member property. </summary>
		private static PropertyInfo propertyInfoBottom;

		/// <inheritdoc />
		public override bool DrawInSingleRow
		{
			get { return true; }
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static RectOffsetDrawer Create(RectOffset value, [CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, GUIContent label, bool readOnly)
		{
			RectOffsetDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new RectOffsetDrawer();
			}
			result.Setup(value, typeof(RectOffset), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((RectOffset)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override void DoGenerateMemberBuildList()
		{
			if(!fieldInfosGenerated)
			{
				fieldInfosGenerated = true;
				propertyInfoLeft = Types.RectOffset.GetProperty("left", BindingFlags.Instance | BindingFlags.Public);
				propertyInfoRight = Types.RectOffset.GetProperty("right", BindingFlags.Instance | BindingFlags.Public);
				propertyInfoTop = Types.RectOffset.GetProperty("top", BindingFlags.Instance | BindingFlags.Public);
				propertyInfoBottom = Types.RectOffset.GetProperty("bottom", BindingFlags.Instance | BindingFlags.Public);
			}

			var hierarchy = MemberHierarchy;
			memberBuildList.Add(hierarchy.Get(memberInfo, propertyInfoLeft));
			memberBuildList.Add(hierarchy.Get(memberInfo, propertyInfoRight));
			memberBuildList.Add(hierarchy.Get(memberInfo, propertyInfoTop));
			memberBuildList.Add(hierarchy.Get(memberInfo, propertyInfoBottom));
		}

		/// <inheritdoc />
		protected override void DoBuildMembers()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberBuildList.Count == 4);
			#endif
			
			var first = Value;
			Array.Resize(ref members, 4);
			members[0] = IntDrawer.Create(first.left, memberBuildList[0], this, GUIContentPool.Create("X"), false);
			members[1] = IntDrawer.Create(first.right, memberBuildList[1], this, GUIContentPool.Create("Y"), false);
			members[2] = IntDrawer.Create(first.top, memberBuildList[2], this, GUIContentPool.Create("W"), false);
			members[3] = IntDrawer.Create(first.bottom, memberBuildList[3], this, GUIContentPool.Create("H"), false);
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			if(!ReadOnly)
			{
				var val = Value;

				menu.AddSeparatorIfNotRedundant();

				menu.Add("Set To.../Zero", ()=>Value = new RectOffset(0, 0, 0 , 0), val == new RectOffset(0, 0, 0, 0));
				menu.Add("Set To.../One", () => Value = new RectOffset(1, 1, 1, 1), val == new RectOffset(1, 1, 1, 1));
				menu.Add("Set To.../Two", () => Value = new RectOffset(2, 2, 2, 2), val == new RectOffset(2, 2, 2, 2));
				menu.Add("Set To.../Three", () => Value = new RectOffset(3, 3, 3, 3), val == new RectOffset(3, 3, 3, 3));
				menu.Add("Set To.../Four", () => Value = new RectOffset(4, 4, 4, 4), val == new RectOffset(4, 4, 4, 4));
				menu.Add("Set To.../Five", () => Value = new RectOffset(5, 5, 5, 5), val == new RectOffset(5, 5, 5, 5));

				menu.Add("Inverse", ()=>Value = new RectOffset(-val.left, -val.right, -val.top, -val.bottom), val != new RectOffset(0, 0, 0, 0));
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}
	}
}