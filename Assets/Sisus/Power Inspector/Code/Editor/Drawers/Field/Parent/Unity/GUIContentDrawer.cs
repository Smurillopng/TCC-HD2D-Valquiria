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
	[Serializable, DrawerForField(typeof(GUIContent), false, true)]
	public class GUIContentDrawer : ParentFieldDrawer<GUIContent>
	{
		/// <summary> True if PropertyInfos generated for member properties. </summary>
		private static bool propertyInfosGenerated;

		/// <summary> The PropertyInfo for the text member. </summary>
		private static PropertyInfo propertyInfoText;

		/// <summary> The PropertyInfo for the image member. </summary>
		private static PropertyInfo propertyInfoImage;

		/// <summary> The PropertyInfo for the tooltip member. </summary>
		private static PropertyInfo propertyInfoTooltip;

		/// <inheritdoc/>
		public override bool DrawInSingleRow
		{
			get
			{
				return false;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		[NotNull]
		public static GUIContentDrawer Create(GUIContent value, [CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, GUIContent label, bool readOnly)
		{
			GUIContentDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new GUIContentDrawer();
			}
			result.Setup(value, typeof(GUIContent), memberInfo, parent, label, readOnly);
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
				propertyInfoText = Types.GUIContent.GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
				propertyInfoImage = Types.GUIContent.GetProperty("image", BindingFlags.Instance | BindingFlags.Public);
				propertyInfoTooltip = Types.GUIContent.GetProperty("tooltip", BindingFlags.Instance | BindingFlags.Public);
			}
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((GUIContent)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			GenerateMemberInfos();

			var hierarchy = MemberHierarchy;
			if(hierarchy == null)
			{
				memberBuildList.Add(null);
				memberBuildList.Add(null);
				memberBuildList.Add(null);
				return;
			}
			memberBuildList.Add(hierarchy.Get(memberInfo, propertyInfoText, "text"));
			memberBuildList.Add(hierarchy.Get(memberInfo, propertyInfoImage, "image"));
			memberBuildList.Add(hierarchy.Get(memberInfo, propertyInfoTooltip, "tooltip"));
		}

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			if(DebugMode)
			{
				base.DoBuildMembers();
				return;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberBuildList.Count == 3);
			#endif

			var first = Value;
			Array.Resize(ref members, 3);
			var readOnly = ReadOnly;
			members[0] = TextDrawer.Create(first.text, memberBuildList[0], this, GUIContentPool.Create("Text"), readOnly, false);
			members[1] = ObjectReferenceDrawer.Create(first.image, memberBuildList[1], this, GUIContentPool.Create("Image"), false, false, readOnly);
			members[2] = TextDrawer.Create(first.tooltip, memberBuildList[2], this, GUIContentPool.Create("Tooltip"), readOnly, false);
		}

		/// <inheritdoc/>
		protected override bool ValuesAreEqual(GUIContent a, GUIContent b)
		{
			if(ReferenceEquals(a, b))
			{
				return true;
			}

			if(ReferenceEquals(a, null) || ReferenceEquals(b, null))
			{
				return false;
			}

			if(!string.Equals(a.text, b.text))
			{
				return false;
			}

			if(!string.Equals(a.tooltip, b.tooltip))
			{
				return false;
			}

			if(a.image != b.image)
			{
				return false;
			}

			return true;
		}

		/// <inheritdoc/>
		protected override bool TryToManuallyUpdateCachedValueFromMember(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			var setValue = Value;
			switch(memberIndex)
			{
				case 0:
					setValue.text = (string)memberValue;
					break;
				case 1:
					setValue.image = (Texture)memberValue;
					break;
				case 2:
					setValue.tooltip = (string)memberValue;
					break;
				default:
					return false;
			}
			DoSetValue(setValue, false, false);
			return true;
		}
	}
}