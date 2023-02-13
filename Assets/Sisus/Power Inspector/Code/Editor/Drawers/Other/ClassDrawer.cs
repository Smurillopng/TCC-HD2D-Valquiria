using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Even though this extends UnityObjectDrawer, it actually represents a class (or value) type,
	/// being able to list its exposed static fields, properties and methods.
	/// </summary>
	[Serializable]
	public class ClassDrawer : UnityObjectDrawer<ClassDrawer, Object>, IRootDrawer
	{
		private Type classType;
		private GUIContent headerSubtitle = new GUIContent();

		/// <inheritdoc/>
		public virtual bool WantsSearchBoxDisabled
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		protected sealed override float ToolbarIconsTopOffset
		{
			get
			{
				return ComponentToolbarIconsTopOffset;
			}
		}

		/// <inheritdoc/>
		protected sealed override float HeaderToolbarIconWidth
		{
			get
			{
				return ComponentHeaderToolbarIconWidth;
			}
		}

		/// <inheritdoc />
		protected sealed override float HeaderToolbarIconsRightOffset
		{
			get
			{
				return ComponentHeaderToolbarIconsRightOffset;
			}
		}

		/// <inheritdoc />
		protected sealed override float HeaderToolbarIconsOffset
		{
			get
			{
				return ComponentHeaderToolbarIconsOffset;
			}
		}

		protected override MonoScript MonoScript
		{
			get
			{
				return FileUtility.FindScriptFile(classType);
			}
		}

		/// <inheritdoc />
		protected override bool IsAsset
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc cref="IDrawer.Type" />
		public override Type Type
		{
			get
			{
				return classType;
			}
		}

		/// <inheritdoc cref="IUnityObjectDrawer.HeaderHeight" />
		public override float HeaderHeight
		{
			get
			{
				return 45f; //AssetTitlebarHeight without any buttons
				//return DrawGUI.Active.AssetTitlebarHeight(false);
			}
		}

		/// <inheritdoc cref="IUnityObjectDrawer.Expandable" />
		public override bool Foldable
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc cref="IUnityObjectDrawer.Unfolded" />
		public override bool Unfolded
		{
			get
			{
				return true;
			}

			set { throw new NotSupportedException("Type Unfolded state can't be altered"); }
		}

		/// <inheritdoc />
		protected override Color PrefixBackgroundColor
		{
			get
			{
				return inspector.Preferences.theme.ComponentHeaderBackground;
			}
		}

		/// <inheritdoc />
		protected override float HeaderToolbarIconHeight
		{
			get
			{
				return 17f;
			}
		}

		/// <inheritdoc />
		protected override bool HasReferenceIcon
		{
			get
			{
				return false;
			}
		}

		#if UNITY_2018_1_OR_NEWER
		/// <inheritdoc />
		protected override bool HasPresetIcon
		{
			get
			{
				return false;
			}
		}
		#endif

		/// <inheritdoc />
		protected override bool HasContextMenuIcon
		{
			get
			{
				return false;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="setClassType"> The type of the class that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		[NotNull]
		public static ClassDrawer Create([NotNull]Type setClassType, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			ClassDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ClassDrawer();
			}
			result.Setup(setClassType, parent, GUIContentPool.Create(StringUtils.SplitPascalCaseToWords(StringUtils.ToStringSansNamespace(setClassType))), inspector);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		protected override void Setup(Object[] setTargets, IParentDrawer setParent, GUIContent setLabel, IInspector setInspector)
		{
			throw new NotSupportedException("Please use the other Setup method.");
		}

		private void Setup([NotNull]Type setClassType, IParentDrawer setParent, GUIContent setLabel, IInspector setInspector)
		{
			classType = setClassType;
			GetHeaderSubtitle(setClassType, ref headerSubtitle);
			base.Setup(ArrayPool<Object>.ZeroSizeArray, setParent, setLabel, setInspector);
		}

		private static void GetHeaderSubtitle(Type classType, ref GUIContent subtitle)
		{
			if(classType == null)
			{
				subtitle.text = "Class";
			}
			else if(Types.MonoBehaviour.IsAssignableFrom(classType))
			{
				subtitle.text = "MonoBehaviour Class";
			}
			else if(Types.ScriptableObject.IsAssignableFrom(classType))
			{
				if(Types.EditorWindow.IsAssignableFrom(classType))
				{
					subtitle.text = "EditorWindow Class";
				}
				else
				{
					subtitle.text = "ScriptableObject Class";
				}
			}
			else if(classType.IsValueType)
			{
				if(classType.IsEnum)
				{
					subtitle.text = "Enum Class";
				}
				else
				{
					subtitle.text = "Struct Class";
				}
			}
			else if(classType.IsAbstract)
			{
				if(classType.IsInterface)
				{
					subtitle.text = "Interface Class";
				}
				else if(classType.IsStatic())
				{
					subtitle.text = "Static Class";
				}
				else
				{
					subtitle.text = "Abstract Class";
				}
			}
			else if(classType.IsClass)
			{
				subtitle.text = "Class";
			}
			else
			{
				subtitle.text = "Script";
			}
		}

		/// <inheritdoc />
		public override bool DrawPrefix(Rect position)
		{
			bool dirty = base.DrawPrefix(position);
			DrawSubtitle(position);
			return dirty;
		}

		/// <inheritdoc />
		protected override void DrawHeaderBase(Rect position)
		{
			DrawGUI.Editor.ColorRect(position, PrefixBackgroundColor);
			DrawGUI.Editor.InspectorTitlebar(position, Unfolded, label, Foldable, SelectedHeaderPart, MouseoveredHeaderPart);
		}
		
		/// <summary> Draw subtitle text below the main header text. </summary>
		/// <param name="headerRect"> The draw rect of the whole header. </param>
		private void DrawSubtitle(Rect headerRect)
		{
			var subtitleRect = headerRect;
			subtitleRect.height = DrawGUI.SingleLineHeight;
			subtitleRect.y += 22f;
			float iconOffset = 43f;
			subtitleRect.x += iconOffset;
			subtitleRect.width -= iconOffset + HeaderButtonsWidth + HeaderButtonsPadding;
			if(subtitleRect.width > 0f)
			{
				GUI.Label(subtitleRect, headerSubtitle, InspectorPreferences.Styles.SubHeader);
			}
		}

		/// <inheritdoc />
		protected override Object[] FindObjectsOfType()
		{
			if(classType.IsComponent())
			{
				#if UNITY_2023_1_OR_NEWER
				return Object.FindObjectsByType(classType, FindObjectsInactive.Include, FindObjectsSortMode.None);
				#else
				return Object.FindObjectsOfType(classType);
				#endif
			}

			if(classType.IsAssetType())
			{
				//finds EditorWindows, ScriptableObject assets and other assets
				return Resources.FindObjectsOfTypeAll(Type);
			}
			
			return ArrayPool<Object>.ZeroSizeArray;
		}

		/// <summary> Searches for the first references in scene. </summary>
		protected override void FindReferencesInScene()
		{
			var references = new List<GameObject>();
			var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
			var gameObjects = scene.GetAllGameObjects();
			for(int n = gameObjects.Length - 1; n >= 0; n--)
			{
				var gameObject = gameObjects[n];
				if(DoesGameObjectHaveReferencesToType(gameObject, classType))
				{
					references.Add(gameObject);
				}
			}

			int count = references.Count;
			if(count > 0)
			{
				InspectorUtility.ActiveInspector.State.ViewIsLocked = true;
				Platform.Active.Select(references.ToArray());
				for(int n = count - 1; n >= 0; n--)
				{
					DrawGUI.Active.PingObject(references[0]);
				}
			}
		}

		private static bool DoesGameObjectHaveReferencesToType(GameObject gameObject, Type targetType)
		{
			var components = gameObject.GetComponents<Component>();
			for(int c = components.Length - 1; c >= 0; c--)
			{
				var component = components[c];
				if(component == null)
				{
					continue;
				}

				var componentType = component.GetType();

				var fields = componentType.GetFields(ParentDrawerUtility.BindingFlagsInstance);
				for(int n = 0, count = fields.Length; n < count; n++)
				{
					var field = fields[n];
					if(field.FieldType == targetType && field.IsInspectorViewable(false, InspectorUtility.Preferences.showFields))
					{
						return true;
					}
				}

				var properties = componentType.GetProperties(ParentDrawerUtility.BindingFlagsInstance);
				for(int n = 0, count = properties.Length; n < count; n++)
				{
					var property = properties[n];
					if(property.PropertyType == targetType && property.IsInspectorViewable(InspectorUtility.Preferences.showProperties, false))
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <inheritdoc />
		protected override void NameByType()
		{
			Undo.RecordObjects(targets, "Auto-Name");

			var target = targets[0];
			if(target != null)
			{
				var typeName = target.GetType().Name;
				for(int n = targets.Length - 1; n >= 0; n--)
				{
					target = targets[n];
					EditorUtility.SetDirty(target);
					target.name = typeName;
				}
			}
		}
		
		/// <inheritdoc />
		protected override void DoGenerateMemberBuildList()
		{
			if(DebugMode)
			{
				if(debugModeDisplaySettings == null)
				{
					debugModeDisplaySettings = new DebugModeDisplaySettings()
					{
						Static = true
					};
				}

				#if DEV_MODE
				Debug.Log("Generating members using DebugModeDisplaySettings " + debugModeDisplaySettings);
				#endif

				Type.GetStaticInspectorViewables(ref linkedMemberHierarchy, null, ref memberBuildList, true, FieldVisibility.AllPublic, PropertyVisibility.AllPublic, MethodVisibility.AllPublic, debugModeDisplaySettings);
				return;
			}

			ParentDrawerUtility.GetStaticMemberBuildList(this, linkedMemberHierarchy, ref memberBuildList, FieldVisibility.AllPublic, PropertyVisibility.AllPublic, MethodVisibility.AllPublic);
		}

		/// <inheritdoc />
		protected override void DoBuildMembers()
		{
			#if DEV_MODE
			Debug.Log(ToString()+".DoBuildMembers()");
			#endif

			ParentDrawerUtility.BuildMembers(DrawerProvider, this, memberBuildList, ref members);

			if(inspector.Preferences.drawScriptReferenceFields || DebugMode)
			{
				var monoScript = MonoScript;
				if(monoScript != null)
				{
					DrawerArrayPool.InsertAt(ref members, 0, ScriptReferenceDrawer.Create(MonoScript, this, false), true);
				}
			}

			if(DebugMode && (members.Length == 0 || !(members[0] is DebugModeDisplaySettingsDrawer)))
			{
				#if DEV_MODE
				Debug.Log("InsertAt(0, DebugModeDisplaySettingsDrawer)");
				#endif

				DrawerArrayPool.InsertAt(ref members, 0, SpaceDrawer.Create(7f, this), true);
				DrawerArrayPool.InsertAt(ref members, 0, DebugModeDisplaySettingsDrawer.Create(this, debugModeDisplaySettings), true);
				DrawerArrayPool.InsertAt(ref members, 0, SpaceDrawer.Create(7f, this), true);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!members.ContainsNullMembers());
			#endif
		}

		public GUIContent[] AssetLabels
		{
			get
			{
				return ArrayPool<GUIContent>.ZeroSizeArray;
			}
		}
		public GUIContent[] AssetLabelsOnlyOnSomeTargets
		{
			get
			{
				return ArrayPool<GUIContent>.ZeroSizeArray;
			}
		}
	}
}