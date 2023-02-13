using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#else
using System.Reflection;
#endif

#if UNITY_2019_3_OR_NEWER && UI_ELEMENTS
using UnityEngine.UIElements;
#endif

namespace Sisus
{
	#if UNITY_EDITOR
	public abstract class PPropertyDrawer : UnityEditor.PropertyDrawer
	#else
	public abstract class PPropertyDrawer
	#endif
	{
		private IInspector inspector;
		private IParentDrawer parent;
		private IDrawer defaultDrawer;
		private LinkedMemberInfo memberInfo;

		public IInspector Inspector
		{
			get
			{
				if(inspector == null)
				{
					var activeInspector = InspectorUtility.ActiveInspector;
					if(activeInspector.NowDrawingPart == InspectorPart.Viewport)
					{
						inspector = activeInspector;
					}
				}
				return inspector;
			}
		}

		public virtual IDrawerProvider DrawerProvider
		{
			get
			{
				if(Inspector != null)
				{
					return inspector.DrawerProvider;
				}
				return DefaultDrawerProviders.GetForInspector(typeof(PowerInspector));
			}
		}

		public IParentDrawer Parent
		{
			get
			{
				if(parent == null && Inspector != null)
				{
					parent = inspector.State.drawers;
				}
				return parent;
			}
		}

		public IDrawer DefaultDrawer
		{
			get
			{
				if(defaultDrawer == null)
				{
					defaultDrawer = GetDefaultDrawer();
				}
				return defaultDrawer;
			}
		}

		public InspectorPreferences Preferences
		{
			get
			{
				return Inspector != null ? inspector.Preferences : null;
			}
		}
		
		#if !UNITY_EDITOR
		public FieldInfo fieldInfo
		{
			get
			{
				return MemberInfo != null ? memberInfo.FieldInfo : null;

			}
		}
		#endif

		public LinkedMemberInfo MemberInfo
		{
			get
			{
				if(memberInfo == null)
				{
					if(fieldInfo != null && Parent != null)
					{
						var parentMemberInfo = parent.MemberInfo;
						var memberHierarchy = parent.MemberHierarchy;
						if(parentMemberInfo != null)
						{
							LinkedMemberParent parentMemberType;
							if(fieldInfo.IsStatic || parentMemberInfo.IsStatic)
							{
								parentMemberType = LinkedMemberParent.Static;
							}
							else
							{
								parentMemberType = LinkedMemberParent.LinkedMemberInfo;
							}

							memberInfo = memberHierarchy.Get(parentMemberInfo, fieldInfo, parentMemberType);
						}
						else
						{
							LinkedMemberParent parentMemberType;
							if(fieldInfo.IsStatic)
							{
								parentMemberType = LinkedMemberParent.Static;
							}
							else if(parent is IUnityObjectDrawer)
							{
								parentMemberType = parent.UnityObject is Object ? LinkedMemberParent.UnityObject : LinkedMemberParent.ClassInstance;
							}
							else
							{
								parentMemberType = LinkedMemberParent.Missing;
							}
							memberInfo = memberHierarchy.Get(parentMemberInfo, fieldInfo, parentMemberType);
						}
					}
				}
				return memberInfo;
			}
		}

		public IDrawer GetDefaultDrawer()
		{
			GUIContent label;
			if(MemberInfo != null)
			{
				label = MemberInfo.GetLabel();
			}
			else if(fieldInfo != null)
			{
				label = new GUIContent(StringUtils.SplitPascalCaseToWords(fieldInfo.Name));
			}
			else
			{
				label = null;
			}

			return DrawerProvider.GetForField(MemberInfo, parent, label, parent != null ? parent.ReadOnly : false);
		}

		public PPropertyDrawer() : base()
		{
			var activeInspector = InspectorUtility.ActiveInspector;
			if(activeInspector.NowDrawingPart == InspectorPart.Viewport)
			{
				inspector = activeInspector;
			}
		}

		public PPropertyDrawer(IInspector setInspector, IParentDrawer setParentDrawer) : base()
		{
			inspector = setInspector;
			parent = setParentDrawer;
		}

		#if UNITY_EDITOR
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			DrawDefaultInspector(position);
		}
		#endif

		#if UNITY_2019_3_OR_NEWER && UI_ELEMENTS
		public virtual VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var container = new VisualElement();
			var propertyField = new PropertyField(property);
			container.Add(propertyField);
			return container;
		}
		#endif

		public bool DrawDefaultInspector(Rect position)
		{
			DefaultDrawer.Draw(position);
			return true;
		}
	}
}