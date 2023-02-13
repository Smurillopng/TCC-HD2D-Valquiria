#define DEBUG_NEXT_FIELD

using System;
using System.Reflection;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary> (Serializable) a graphical user interface style state graphical user interface
	/// drawer. This class cannot be inherited. </summary>
	[Serializable, DrawerForField(typeof(GUIStyleState), false, true)]
	public class GUIStyleStateDrawer : ParentFieldDrawer<GUIStyleState>
	{
		/// <summary> The property flags. </summary>
		private const BindingFlags PropertyFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

		/// <summary> True if PropertyInfos generated for member properties. </summary>
		private static bool propertyInfosGenerated;

		/// <summary> PropertyInfo for background member property. </summary>
		/// 
		private static PropertyInfo propertyBackground;
		#if UNITY_EDITOR
		/// <summary> PropertyInfo for scaledBackgrounds member property. Not accessible from player code. </summary>
		private static PropertyInfo propertyScaledBackgrounds;
		#endif

		/// <summary> PropertyInfo for textColor member property. </summary>
		private static PropertyInfo propertyTextColor;

		/// <inheritdoc/>
		public override bool DrawInSingleRow
		{
			get { return false; }
		}

		/// <summary> Creates a new instance of GUIStyleStateDrawer or returns a reusable instance from
		/// the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the created drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="setReadOnly"> True if control should be read only. </param>
		/// <returns> Ready-to-use instance of GUIStyleStateDrawer. </returns>
		public static GUIStyleStateDrawer Create(GUIStyleState value, [CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			GUIStyleStateDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new GUIStyleStateDrawer();
			}
			result.Setup(value, typeof(GUIStyleState), memberInfo, parent, label, setReadOnly);
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
				propertyBackground = Types.GUIStyleState.GetProperty("background", PropertyFlags);
				#if UNITY_EDITOR
				//scaledBackgrounds field is not accessible from player code
				propertyScaledBackgrounds = Types.GUIStyleState.GetProperty("scaledBackgrounds", PropertyFlags);
				#endif
				propertyTextColor = Types.GUIStyleState.GetProperty("textColor", PropertyFlags);
			}
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((GUIStyleState)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			GenerateMemberInfos();

			var hierarchy = MemberHierarchy;
			memberBuildList.Add(hierarchy.Get(memberInfo, propertyBackground));
			
			#if UNITY_EDITOR
			//scaledBackgrounds field is not accessible from player code
			memberBuildList.Add(hierarchy.Get(memberInfo, propertyScaledBackgrounds));
			#endif

			memberBuildList.Add(hierarchy.Get(memberInfo, propertyTextColor));
		}

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			#if UNITY_EDITOR
			Array.Resize(ref members, 3);
			#else
			Array.Resize(ref members, 2);
			#endif

			var first = Value;
			members[0] = ObjectReferenceDrawer.Create(first.background, memberBuildList[0], this, GUIContentPool.Create("Background"), false, false, false);

			int index = 1;
			#if UNITY_EDITOR
			//scaledBackgrounds field is not accessible from player code
			members[index] = ArrayDrawer.Create(first.scaledBackgrounds, memberBuildList[index], this, GUIContentPool.Create("Scaled Backgrounds"), false);
			index++;
			#endif
			members[index] = ColorDrawer.Create(first.textColor, memberBuildList[index], this, GUIContentPool.Create("Text Color"), false);
		}
	}
}