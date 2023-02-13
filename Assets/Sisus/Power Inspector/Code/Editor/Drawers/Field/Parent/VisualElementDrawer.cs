#if UNITY_2019_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sisus
{
	/// <summary> (Serializable) a rectangle graphical user interface drawer. This class cannot
	/// be inherited. </summary>
	[Serializable, DrawerForField(typeof(VisualElement), true, true)]
	public class VisualElementDrawer : ParentFieldDrawer<VisualElement>
	{
		/// <summary> True if PropertyInfos generated for member properties. </summary>
		private static bool fieldInfosGenerated;

		private static PropertyInfo nameField;
		//private static FieldInfo typeNameField;
		private static PropertyInfo positionField;
		private static PropertyInfo rotationField;
		private static PropertyInfo scaleField;
		//private static PropertyInfo childrenProperty;
		private static FieldInfo childrenField;

		/// <inheritdoc/>
		protected override bool RebuildDrawersIfValueChanged
		{
			get
			{
				return RebuildingMembersAllowed;
			}
		}

		/// <inheritdoc/>
		public override bool DrawInSingleRow
		{
			get
			{
				return Value == null;
			}
		}

		/// <inheritdoc/>
		protected override bool CanBeNull
		{
			get
			{
				return true;
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
		public static VisualElementDrawer Create(VisualElement value, [CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, GUIContent label, bool readOnly)
		{
			VisualElementDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new VisualElementDrawer();
			}
			result.Setup(value, typeof(VisualElement), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <summary> Generates PropertyInfos for members. This should be called at least once before
		/// DoGenerateMemberBuildList is called. </summary>
		private static void GenerateMemberInfos()
		{
			if(!fieldInfosGenerated)
			{
				fieldInfosGenerated = true;
				var type = typeof(VisualElement);
				var transformType = typeof(ITransform);
				//var interfaceMap = type.GetInterfaceMap(typeof(ITransform));
				//interfaceMap.Get

				//nameField = type.GetField("m_Name", BindingFlags.Instance | BindingFlags.NonPublic);
				//typeNameField = type.GetField("m_TypeName", BindingFlags.Instance | BindingFlags.NonPublic);
				//positionField = type.GetField("m_Position", BindingFlags.Instance | BindingFlags.NonPublic);
				//rotationField = type.GetField("m_Rotation", BindingFlags.Instance | BindingFlags.NonPublic);
				//scaleField = type.GetField("m_Scale", BindingFlags.Instance | BindingFlags.NonPublic);
				nameField = type.GetProperty("name", BindingFlags.Instance | BindingFlags.Public);
				positionField = transformType.GetProperty("position", BindingFlags.Instance | BindingFlags.Public);
				rotationField = transformType.GetProperty("rotation", BindingFlags.Instance | BindingFlags.Public);
				scaleField = transformType.GetProperty("scale", BindingFlags.Instance | BindingFlags.Public);
				//childrenProperty = type.GetProperty("Children", BindingFlags.Instance | BindingFlags.Public);
				childrenField = type.GetField("m_Children", BindingFlags.Instance | BindingFlags.NonPublic);

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(nameField != null);
				Debug.Assert(positionField != null);
				Debug.Assert(rotationField != null);
				Debug.Assert(scaleField != null);
				Debug.Assert(childrenField != null);
				#endif
			}
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((VisualElement)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
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
				memberBuildList.Add(null);
				memberBuildList.Add(null);
				return;
			}

			memberBuildList.Add(hierarchy.Get(memberInfo, nameField, "name"));
			memberBuildList.Add(hierarchy.Get(memberInfo, positionField, "position"));
			memberBuildList.Add(hierarchy.Get(memberInfo, rotationField, "rotation"));
			memberBuildList.Add(hierarchy.Get(memberInfo, scaleField, "scale"));
			memberBuildList.Add(hierarchy.Get(memberInfo, childrenField, "m_Children"));
		}

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			var first = Value;
			if(first == null)
			{
				Array.Resize(ref members, 0);
				return;
			}

			Array.Resize(ref members, 5);
			var readOnly = ReadOnly;
			members[0] = TextDrawer.Create(first.name, memberBuildList[0], this, GUIContentPool.Create("Name"), readOnly, false);
			members[1] = Vector3Drawer.Create((Vector3)positionField.GetValue(first), memberBuildList[1], this, GUIContentPool.Create("Position"), readOnly);
			members[2] = QuaternionDrawer.Create((Quaternion)rotationField.GetValue(first), memberBuildList[2], this, GUIContentPool.Create("Rotation"), readOnly);
			members[3] = Vector3Drawer.Create((Vector3)scaleField.GetValue(first), memberBuildList[3], this, GUIContentPool.Create("Scale"), readOnly);
			var listValue = (List<VisualElement>)childrenField.GetValue(first);
			if(listValue == null)
			{
				listValue = new List<VisualElement>();
			}
			members[4] = ListDrawer<VisualElement>.Create(listValue, memberBuildList[4], this, GUIContentPool.Create("Children"), true); // temp always read-only to avoid issues
		}

		/// <inheritdoc/>
		protected override bool TryToManuallyUpdateCachedValueFromMember(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			var setValue = Value;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setValue != null);
			#endif

			switch(memberIndex)
			{
				case 0:
					setValue.name = (string)memberValue;
					break;
				case 1:
					var transform = setValue as ITransform;
					if(transform != null)
					{
						transform.position = (Vector3)memberValue;
					}
					break;
				case 2:
					transform = setValue as ITransform;
					if(transform != null)
					{
						transform.rotation = (Quaternion)memberValue;
					}
					break;
				case 3:
					transform = setValue as ITransform;
					if(transform != null)
					{
						transform.scale = (Vector3)memberValue;
					}
					break;
				case 4:
					//childrenField.SetValue(setValue, (List<VisualElement>)memberValue);
					break;
				default:
					return false;
			}
			DoSetValue(setValue, false, false);
			return true;
		}

		public override bool DrawBody(Rect position)
		{
			if(Value == null)
			{
				bool guiWasEnabled = GUI.enabled;
				GUI.enabled = false;
				GUI.Label(position, "null");
				GUI.enabled = guiWasEnabled;
				return false;
			}
			return base.DrawBody(position);
		}
	}
}
#endif