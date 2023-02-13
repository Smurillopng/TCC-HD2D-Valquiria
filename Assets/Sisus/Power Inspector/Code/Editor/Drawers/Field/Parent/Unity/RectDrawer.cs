#define DEBUG_NEXT_FIELD

using System;
using System.Reflection;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary> (Serializable) a rectangle graphical user interface drawer. This class cannot
	/// be inherited. </summary>
	[Serializable, DrawerForField(typeof(Rect), false, true)]
	public class RectDrawer : ParentFieldDrawer<Rect>
	{
		/// <summary> True if PropertyInfos generated for member properties. </summary>
		private static bool propertyInfosGenerated;

		/// <summary> The PropertyInfo for the x member property. </summary>
		private static PropertyInfo propertyInfoX;
		/// <summary> The PropertyInfo for the y member property. </summary>
		private static PropertyInfo propertyInfoY;
		/// <summary> The PropertyInfo for the width member property. </summary>
		private static PropertyInfo propertyInfoWidth;
		/// <summary> The PropertyInfo for the height member property. </summary>
		private static PropertyInfo propertyInfoHeight;

		/// <inheritdoc/>
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
		[NotNull]
		public static RectDrawer Create(Rect value, [CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, GUIContent label, bool readOnly)
		{
			RectDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new RectDrawer();
			}
			result.Setup(value, typeof(Rect), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <summary> Generates PropertyInfos for members. This should be called at least once before
		/// DoGenerateMemberBuildList is called. </summary>
		private static void GenerateMemberInfos()
		{
			if(!propertyInfosGenerated)
			{
				propertyInfosGenerated = true;
				propertyInfoX = Types.Rect.GetProperty("x", BindingFlags.Instance | BindingFlags.Public);
				propertyInfoY = Types.Rect.GetProperty("y", BindingFlags.Instance | BindingFlags.Public);
				propertyInfoWidth = Types.Rect.GetProperty("width", BindingFlags.Instance | BindingFlags.Public);
				propertyInfoHeight = Types.Rect.GetProperty("height", BindingFlags.Instance | BindingFlags.Public);
			}
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((Rect)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			GenerateMemberInfos();

			var hierarchy = MemberHierarchy;
			memberBuildList.Add(hierarchy.Get(memberInfo, propertyInfoX));
			memberBuildList.Add(hierarchy.Get(memberInfo, propertyInfoY));
			memberBuildList.Add(hierarchy.Get(memberInfo, propertyInfoWidth));
			memberBuildList.Add(hierarchy.Get(memberInfo, propertyInfoHeight));
		}

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberBuildList.Count == 4);
			#endif

			var first = Value;
			Array.Resize(ref members, 4);
			var readOnly = ReadOnly;
			members[0] = FloatDrawer.Create(first.x, memberBuildList[0], this, GUIContentPool.Create("X"), readOnly);
			members[1] = FloatDrawer.Create(first.y, memberBuildList[1], this, GUIContentPool.Create("Y"), readOnly);
			members[2] = FloatDrawer.Create(first.width, memberBuildList[2], this, GUIContentPool.Create("W"), readOnly);
			members[3] = FloatDrawer.Create(first.height, memberBuildList[3], this, GUIContentPool.Create("H"), readOnly);
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
				menu.AddSeparatorIfNotRedundant();

				var val = Value;
				
				var rounded = val;
				rounded.x = Mathf.Round(rounded.x);
				rounded.y = Mathf.Round(rounded.y);
				rounded.width = Mathf.Round(rounded.width);
				rounded.height = Mathf.Round(rounded.height);
				if(val == rounded)
				{
					menu.AddDisabled("Round");
				}
				else
				{
					menu.Add("Round", () => Value = rounded);
				}
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		/// <inheritdoc/>
		protected override bool TryToManuallyUpdateCachedValueFromMember(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			var setValue = Value;
			switch(memberIndex)
			{
				case 0:
					setValue.x = (float)memberValue;
					break;
				case 1:
					setValue.y = (float)memberValue;
					break;
				case 2:
					setValue.width = (float)memberValue;
					break;
				case 3:
					setValue.height = (float)memberValue;
					break;
				default:
					return false;
			}
			DoSetValue(setValue, false, false);
			return true;
		}
	}
}