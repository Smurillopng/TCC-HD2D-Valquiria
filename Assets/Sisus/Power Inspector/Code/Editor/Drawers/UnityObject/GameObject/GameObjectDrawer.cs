#define ENABLE_SPREADING
#define SAFE_MODE
#define ENABLE_MATERIAL_DRAWER_EMBEDDING

//#define DEBUG_ADD_COMPONENT
//#define DEBUG_FINISH_ADDING_COMPONENTS
//#define DEBUG_NULL_LABEL
//#define DEBUG_SETUP_TIME
//#define DEBUG_DRAG_N_DROP
//#define DEBUG_DRAG
//#define DEBUG_SET_SELECTED_PART
//#define DEBUG_KEYBOARD_INPUT
//#define DEBUG_GUI_ENABLED
//#define DEBUG_MULTI_TARGET
#define DEBUG_ON_CLICK
//#define DEBUG_IS_PREFAB
//#define DEBUG_HIDE_ADD_COMPONENT_BUTTON
//#define DEBUG_SET_MOUSEOVERED_PART

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;
using Sisus.Attributes;
using System.Collections;
using UnityEditor;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif

namespace Sisus
{
	/// <summary>
	/// Drawer for GameObjects (including Prefabs).
	/// </summary>
	[Serializable, DrawerForGameObject(false, true)]
	public class GameObjectDrawer : ParentDrawer<Component[]>, IGameObjectDrawer, IOnProjectOrHierarchyChanged, IEnumerable<IComponentDrawer>
	{
		private const float tagAndLayerFieldHeight = 20f;
		private const float tagFieldLeftEdgeOffset = 42f; 
		private const float tagFieldTopOffset = 32f;
		private const float tagToLayerFieldOffset = 13f;
		private const float layerFieldRightEdgeOffset = 4f;
		private const float tagFieldWiderThanLayerField = 2f;

		private const float PrefabButtonsAllPaddings = PrefabButtonsLeftPadding + PrefabButtonsToOverridesDropdownSpacing + PrefabButtonsRightEdgePadding;
		private const float PrefabButtonsLeftPadding = 56f;
		private const float PrefabButtonsTopOffset = 53f;
		private const float PrefabButtonsHeight = 20f;
		private const float PrefabButtonsToOverridesDropdownSpacing = 13f;
		private const float PrefabButtonsRightEdgePadding = 5f;

		private static readonly List<Type> ReusableTypesList = new List<Type>();
		private static List<Object> ReusableObjectReferencesList = new List<Object>();
		private static readonly List<Component> GetComponentsList = new List<Component>();
		private static readonly AnyTypeEqualityComparer anyTypeEqualityComparer = new AnyTypeEqualityComparer();

		/// <summary>
		/// List of types of the move components relative to components.
		/// </summary>
		private static readonly Type[] MoveComponentsRelativeToComponentsTypes = { typeof(Component[]), typeof(Component[]), Types.Bool, Types.Bool };

		/// <summary>
		/// List of types of the move components relative to components.
		/// </summary>
		private static readonly Type[] CopyComponentToGameObjectTypes = { typeof(Component), typeof(GameObject), Types.Bool, Types.List.MakeGenericType(Types.Component) };
		
		/// <summary>
		/// Array for holding parameters when calling method MoveComponentsRelativeToComponents.
		/// </summary>
		private static readonly object[] MoveComponentsRelativeToComponentsParams = {null, null, false, false};

		/// <summary>
		/// Array for holding parameters when calling method CopyComponentToGameObjects.
		/// </summary>
		private static readonly object[] CopyComponentToGameObjectParams = {null, null, false, new List<Component>(1)};

		/// <summary>
		/// Temp list used when getting members to build.
		/// </summary>
		private static List<Component[]> memberBuildListTempHolder = new List<Component[]>(10);
		
		/// <summary>
		/// The mouseovered part.
		/// </summary>
		protected GameObjectHeaderPart mouseoveredPart = GameObjectHeaderPart.None;

		#if !DEV_MODE || !DEBUG_SET_SELECTED_PART
		/// <summary>
		/// The selected part.
		/// </summary>
		protected GameObjectHeaderPart selectedPart = GameObjectHeaderPart.None;
		#endif

		/// <summary>
		/// The target GameObjects of this drawer.
		/// </summary>
		[NotNullOrEmpty]
		protected GameObject[] targets = new GameObject[0];

		/// <summary>
		/// The header drawer.
		/// </summary>
		[SerializeField]
		private readonly GameObjectHeaderDrawer headerDrawer = new GameObjectHeaderDrawer();

		/// <summary> Identifier for the before header control. </summary>
		private int beforeHeaderControlID;
		
		/// <summary> True to components only on some objects found. </summary>
		protected bool componentsOnlyOnSomeObjectsFound;

		/// <summary> The inspector in which the IDrawer are contained. </summary>
		protected IInspector inspector;

		#if ENABLE_SPREADING
		/// <summary>
		/// The last update cached values member index.
		/// </summary>
		private int lastUpdateCachedValuesMemberIndex = -1;
		#endif

		private GUIContent[] assetLabels = new GUIContent[0];
		private GUIContent[] assetLabelsOnlyOnSomeTargets =  new GUIContent[0];

		/// <summary> True if targets are prefabs. </summary>
		protected bool isPrefab;

		/// <summary> True if is prefab instance, false if not. </summary>
		protected bool isPrefabInstance;

		/// <summary> The preview editor. </summary>
		private Editor previewEditor;
		
		#if DEV_MODE && DEBUG_SETUP_TIME
		ExecutionTimeLogger setupTimer = new ExecutionTimeLogger();
		private bool setupTimeLogged;
		#endif

		protected bool editable;
		protected GreyOut drawGreyedOut = GreyOut.None;
		protected bool onlyHeaderGrayedOut;

		protected bool nowRemovingComponent;

		protected static List<LinkedMemberInfo> debugModeInternalBuildList;
		protected IDrawer[] debugModeInternalDrawers = new IDrawer[0];
		protected float debugModeAdditionalHeight;

		private bool forceHideAddComponentMenuButton;

		/// <inheritdoc/>
		public bool WantsSearchBoxDisabled
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc cref="IDrawer.Inspector" />
		public override IInspector Inspector
		{
			get
			{
				return inspector;
			}
		}

		/// <inheritdoc cref="IDrawer.DebugMode" />
		public override bool DebugMode
		{
			get
			{
				return inspector.State.DebugMode;
			}
		}

		#if DEV_MODE && DEBUG_SET_SELECTED_PART
		private GameObjectHeaderPart _selectedPart;
		private GameObjectHeaderPart selectedPart
		{
			get
			{
				return _selectedPart;
			}

			set
			{
				if(_selectedPart != value) { Debug.Log(StringUtils.ToColorizedString("SelectedPart = ", value, " (was: ", _selectedPart, ")")); }
				_selectedPart = value;
			}
		}
		#endif

		/// <inheritdoc cref="IDrawer.ReadOnly" />
		public override bool ReadOnly
		{
			get
			{
				return !editable;
			}
		}

		/// <inheritdoc/>
		public override LinkedMemberHierarchy MemberHierarchy
		{
			get
			{
				return LinkedMemberHierarchy.Get(UnityObjects);
			}
		}
				
		/// <inheritdoc/>
		public Rect FirstReorderableDropTargetRect
		{
			get
			{
				//the spot is right below the header
				var dropRect = lastDrawPosition;
				dropRect.height = DrawGUI.SingleLineHeight;
				dropRect.y += HeaderHeight - DrawGUI.SingleLineHeight * 0.5f;
				return dropRect;
			}
		}

		/// <inheritdoc/>
		public virtual int LastCollectionMemberCountOffset
		{
			get
			{
				if(AddComponentButton != null)
				{
					if(componentsOnlyOnSomeObjectsFound)
					{
						return 3;
					}
					return 2;
				}
				if(componentsOnlyOnSomeObjectsFound)
				{
					return 2;
				}
				return 1;
			}
		}

		/// <inheritdoc/>
		public int FirstCollectionMemberIndex
		{
			get
			{
				return members.Length <= 1 ? -1 : 0;
			}
		}

		/// <inheritdoc/>
		public int LastCollectionMemberIndex
		{
			get
			{
				return members.Length <= 1 ? -1 : members.Length - LastCollectionMemberCountOffset;
			}
		}

		/// <inheritdoc/>
		public int FirstVisibleCollectionMemberIndex
		{
			get
			{
				switch(visibleMembers.Length)
				{
					case 0:
						return -1;
					case 1:
						// in almost all possible scenarios the first visible member will not be the add component button
						// (since when any search filter is given, that's the first member to get hidden), but theoretically
						//  it's possible to hide all other components, including the Transform, on a GameObject using HideFlags
						return !(visibleMembers[0] is IComponentDrawer) ? -1 : 0;
					default:
						return 0;
				}
			}
		}

		/// <inheritdoc/>
		public int LastVisibleCollectionMemberIndex
		{
			get
			{
				int count = visibleMembers.Length;
				switch(count)
				{
					case 0:
						return -1;
					case 1:
						//in almost all scenarios the first visible member will not be the add component button
						//but theoretically it's possible to hide all other components in the inspector
						return !(visibleMembers[0] is IComponentDrawer) ? -1 : 0;
					default:
						return !(visibleMembers[count - 1] is IComponentDrawer) ? count - 2 : count - 1;
				}
			}
		}
		
		/// <inheritdoc/>
		public AddComponentButtonDrawer AddComponentButton
		{
			get
			{
				int count = members.Length;
				return count == 0 ? null : members[count - 1] as AddComponentButtonDrawer;
			}
		}

		/// <inheritdoc cref="IDrawer.IsPrefab" />
		public override bool IsPrefab
		{
			get 
			{
				return isPrefab;
			}
		}

		/// <inheritdoc cref="IDrawer.IsPrefabInstance" />
		public override bool IsPrefabInstance
		{
			get
			{
				return isPrefabInstance;
			}
		}

		/// <inheritdoc/>
		public override Part MouseoveredPart
		{
			get
			{
				return (Part)mouseoveredPart;
			}
		}

		/// <inheritdoc/>
		public override Part SelectedPart
		{
			get
			{
				return (Part)selectedPart;
			}
		}

		/// <inheritdoc/>
		protected override Rect SelectionRect
		{
			get
			{
				var pos = lastDrawPosition;
				pos.height = Height - 6f; // 6f is some clickable whitespace below last member I guess. Selection Rect looks better without it.
				pos.x += 1f;
				pos.y += 1f;
				pos.width -= 2f;

				var visibleMembers = VisibleMembers;
				int count = visibleMembers.Length;

				// Remove height of the add component button from selection rect (if it's visible)
				if(count > 0 && visibleMembers[count - 1].GetType() == typeof(AddComponentButtonDrawer))
				{
					pos.height -= visibleMembers[count - 1].Height;
				}

				return pos;
			}
		}

		/// <inheritdoc/>
		public GUIContent[] AssetLabels
		{
			get
			{
				return assetLabels;
			}
		}

		/// <inheritdoc />
		public GUIContent[] AssetLabelsOnlyOnSomeTargets
		{
			get
			{
				return assetLabelsOnlyOnSomeTargets;
			}
		}

		/// <inheritdoc cref="IDrawer.UnityObject" />
		public override Object UnityObject
		{
			get
			{
				return targets[0];
			}
		}

		/// <inheritdoc cref="IDrawer.UnityObjects" />
		public override Object[] UnityObjects
		{
			get
			{
				return targets;
			}
		}

		/// <inheritdoc/>
		public GameObject GameObject
		{
			get
			{
				return targets[0];
			}
		}

		/// <inheritdoc/>
		public GameObject[] GameObjects
		{
			get
			{
				return targets;
			}
		}

		/// <inheritdoc cref="IDrawer.Selectable" />
		public override bool Selectable
		{
			get
			{
				return passedLastFilterCheck;
			}
		}

		/// <inheritdoc cref="IParentDrawer.HeaderHeight" />
		public override float HeaderHeight
		{
			get
			{
				return HeaderHeightOutsideDebugMode + debugModeAdditionalHeight;
			}
		}
		
		protected float HeaderHeightOutsideDebugMode
		{
			get
			{
				return EditorGUIDrawer.GameObjectTitlebarHeight(isPrefab, isPrefabInstance);
			}
		}

		/// <inheritdoc cref="IParentDrawer.AppendIndentLevel" />
		public override int AppendIndentLevel
		{
			get
			{
				return 0;
			}
		}

		/// <inheritdoc cref="IDrawer.Type" />
		public override Type Type
		{
			get
			{
				return Types.GameObject;
			}
		}

		/// <inheritdoc cref="IParentDrawer.Foldable" />
		public override bool Foldable
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc cref="IParentDrawer.Unfolded" />
		public override bool Unfolded
		{
			get
			{
				return true;
			}

			set
			{
				Debug.LogError("Can't set unfolded state of GameObjectDrawer to "+value+"; they are always unfolded.");
			}
		}

		/// <summary>
		/// Gets the active flag position.
		/// </summary>
		/// <value>
		/// The active flag position.
		/// </value>
		private Rect ActiveFlagPosition
		{
			get
			{
				var rect = labelLastDrawPosition;
				rect.x += 46f;
				rect.y += 10f;
				rect.width = 15f;
				rect.height = 16f;
				return rect;
			}
		}

		/// <summary>
		/// Gets the name field position.
		/// </summary>
		/// <value>
		/// The name field position.
		/// </value>
		private Rect NameFieldPosition
		{
			get
			{
				var rect = labelLastDrawPosition;
				rect.x += 66f;
				rect.y += 8f;
				rect.height = 20f;
				const float removeFromWidth = 62f + 74f;
				rect.width = DrawGUI.InspectorWidth - removeFromWidth;
				return rect;
			}
		}

		/// <summary>
		/// Gets the static flag position.
		/// </summary>
		/// <value>
		/// The static flag position.
		/// </value>
		private Rect StaticFlagPosition
		{
			get
			{
				var rect = NameFieldPosition;
				rect.x += rect.width + 3f;
				rect.y = labelLastDrawPosition.y + 10f;
				rect.width = 50f;
				rect.height = 16f;
				return rect;
			}
		}

		/// <summary>
		/// Gets the drop down arrow position.
		/// </summary>
		/// <value>
		/// The drop down arrow position.
		/// </value>
		private Rect DropDownArrowPosition
		{
			get
			{
				var rect = StaticFlagPosition;
				rect.x += rect.width + 3f;
				rect.y = labelLastDrawPosition.y + 10f;
				rect.height = 16f;
				rect.width = 10f;
				return rect;
			}
		}

		/// <summary>
		/// Gets the tag field position.
		/// </summary>
		/// <value>
		/// The tag field position.
		/// </value>
		protected Rect TagFieldPosition
		{
			get
			{
				var rect = labelLastDrawPosition;
				rect.x += tagFieldLeftEdgeOffset;
				rect.y += tagFieldTopOffset;
				const float removeWidth = tagFieldLeftEdgeOffset + tagToLayerFieldOffset + layerFieldRightEdgeOffset;
				float widthForTagAndLayerFields = lastDrawPosition.width - removeWidth;

				// widthForTagField + widthForLayerField = widthForTagAndLayerFields
				// => widthForTagField + widthForTagField - tagFieldWiderThanLayerField = widthForTagAndLayerFields
				// => widthForTagField = (widthForTagAndLayerFields + tagFieldWiderThanLayerField) / 2
				float widthForTagField = (widthForTagAndLayerFields + tagFieldWiderThanLayerField) * 0.5f;
				rect.width = widthForTagField;
				rect.height = tagAndLayerFieldHeight;
				return rect;
			}
		}

		/// <summary>
		/// Gets the layer field position.
		/// </summary>
		/// <value>
		/// The layer field position.
		/// </value>
		protected Rect LayerFieldPosition
		{
			get
			{
				var rect = TagFieldPosition;
				rect.x += rect.width + tagToLayerFieldOffset;
				rect.width -= tagFieldWiderThanLayerField;
				return rect;
			}
		}

		/// <summary> Gets the position of the open prefab button found on prefab instances. </summary>
		/// <value> The open prefab button position. </value>
		private Rect OpenPrefabButtonPosition
		{
			get
			{
				if(!isPrefabInstance)
				{
					return Rect.zero;
				}
				var rect = labelLastDrawPosition;
				rect.x += PrefabButtonsLeftPadding;
				rect.y += PrefabButtonsTopOffset;
				rect.width = OpenPrefabButtonWidth;
				rect.height = PrefabButtonsHeight;
				return rect;
			}
		}
		
		private float OpenPrefabButtonWidth
		{
			get
			{
				// y = m * x + b
				// y = 3 * x + 17
				// x = (y - 47) / 3
				return (lastDrawPosition.width - PrefabButtonsAllPaddings - 47f) / 3f;
			}
		}
		
		/// <summary> Gets the position of the select prefab button found on prefab instances. </summary>
		/// <value> The select prefab button position. </value>
		private Rect SelectPrefabButtonPosition
		{
			get
			{
				if(!isPrefabInstance)
				{
					return Rect.zero;
				}
				var rect = OpenPrefabButtonPosition;
				rect.x += rect.width + 1f;
				rect.width += 7f;
				return rect;
			}
		}
		
		/// <summary> Gets the position of the select prefab button found on prefab instances. </summary>
		/// <value> The select prefab button position. </value>
		private Rect PrefabOverridesButtonPosition
		{
			get
			{
				if(!isPrefabInstance)
				{
					return Rect.zero;
				}

				var rect = SelectPrefabButtonPosition;
				rect.x += rect.width + PrefabButtonsToOverridesDropdownSpacing;
				float selectButtonWidth = rect.width;
				rect.width = labelLastDrawPosition.width - PrefabButtonsAllPaddings - selectButtonWidth - OpenPrefabButtonWidth;
				return rect;
			}
		}
		
		/// <inheritdoc/>
		public override string DocumentationPageUrl
		{
			get
			{
				return PowerInspectorDocumentation.GetDrawerInfoUrl("gameobject-drawer");
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="target"> The target that the drawers represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static GameObjectDrawer Create([NotNull]GameObject target, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			GameObjectDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new GameObjectDrawer();
			}

			#if DEV_MODE && DEBUG_SETUP_TIME
			result.setupTimer.Start("GameObjectDrawer.Create");
			result.setupTimer.StartInterval("Setup");
			#endif

			result.Setup(target, parent, inspector);

			#if DEV_MODE && DEBUG_SETUP_TIME
			result.setupTimer.FinishInterval();
			result.setupTimer.StartInterval("LateSetup");
			#endif

			result.LateSetup();

			#if DEV_MODE && DEBUG_SETUP_TIME
			result.setupTimer.FinishInterval();
			result.setupTimer.FinishAndLogResults();
			#endif

			return result;
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawers represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static GameObjectDrawer Create([NotNull]GameObject[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			GameObjectDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new GameObjectDrawer();
			}

			#if DEV_MODE && DEBUG_SETUP_TIME
			result.setupTimer.Start("GameObjectDrawer.Create");
			result.setupTimer.StartInterval("Setup");
			#endif

			result.Setup(targets, parent, inspector);

			#if DEV_MODE && DEBUG_SETUP_TIME
			result.setupTimer.FinishInterval();
			result.setupTimer.StartInterval("LateSetup");
			#endif

			result.LateSetup();

			#if DEV_MODE && DEBUG_SETUP_TIME
			result.setupTimer.FinishInterval();
			result.setupTimer.FinishAndLogResults();
			#endif

			return result;
		}
		
		/// <inheritdoc/>
		protected sealed override void Setup(IParentDrawer setParent, GUIContent setLabel)
		{
			throw new NotSupportedException("Please use the other Setup method of GameObjectDrawer");
		}

		/// <inheritdoc/>
		public void Setup([NotNull]GameObject setTarget, [CanBeNull]IParentDrawer setParent, [NotNull]IInspector setInspector)
		{
			Setup(ArrayPool<GameObject>.CreateWithContent(setTarget), setParent, setInspector);
		}

		/// <inheritdoc/>
		public virtual void Setup([NotNullOrEmpty]GameObject[] setTargets, [CanBeNull]IParentDrawer setParent, [NotNull]IInspector setInspector)
		{
			inspector = setInspector;

			targets = setTargets;

			int targetCount = setTargets.Length;

			isPrefab = false;
			isPrefabInstance = false;
			
			var firstTarget = GameObject;
			if(firstTarget != null)
			{
				#if DEV_MODE && DEBUG_IS_PREFAB
				var isOpenInPrefabMode = PrefabStageUtility.GetPrefabStage(firstTarget) != null;
				#endif

				// this returns true for both "base" prefabs and prefab variants
				if(PrefabUtility.IsPartOfPrefabAsset(firstTarget))
				{
					isPrefab = true;
					//now check that the rest of the targets are prefabs too
					for(int n = targetCount - 1; n >= 1; n--)
					{
						if(!PrefabUtility.IsPartOfPrefabAsset(firstTarget))
						{
							isPrefab = false;
							break;
						}
					}

					#if DEV_MODE && DEBUG_IS_PREFAB
					Debug.Log("prefab asset. isOpenInPrefabMode = "+isOpenInPrefabMode);
					#endif
				}
				else if(PrefabUtility.IsPartOfPrefabInstance(firstTarget))
				{
					isPrefabInstance = true;
				}
				
				#if DEV_MODE && DEBUG_IS_PREFAB
				Debug.Log(StringUtils.ToColorizedString("GameObjectDrawer.Setup - isPrefabInstance=", isPrefabInstance, ", isOpenInPrefabMode=", isOpenInPrefabMode));
				#endif
			}

			Sisus.AssetLabels.Get(targets, ref assetLabels, ref assetLabelsOnlyOnSomeTargets);
			Sisus.AssetLabels.OnAssetLabelsChanged += OnAssetLabelsChanged;

			UpdateIsEditable();

			headerDrawer.SetTargets(setTargets, inspector.InspectorDrawer.Editors);

			string setName = setTargets[0].name;
			if(targetCount > 0)
			{
				for(int n = targets.Length - 1; n >= 0; n--)
				{
					if(!string.Equals(setName, setTargets[n].name))
					{
						setName = "-";
						break;
					}
				}
			}

			base.Setup(setParent, GUIContentPool.Create(setName));

			if(Platform.EditorMode)
			{
				setInspector.InspectorDrawer.Editors.GetEditorInternal(ref previewEditor, targets, Editors.GameObjectInspectorType, true);
			}
		}

		private void OnAssetLabelsChanged(Object[] labelsChangedforTargets)
		{
			if(labelsChangedforTargets.ContentsMatch(targets))
			{
				Sisus.AssetLabels.Get(targets, ref assetLabels, ref assetLabelsOnlyOnSomeTargets);
			}
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			if(DebugMode)
			{
				if(debugModeInternalBuildList == null)
				{
					debugModeInternalBuildList = new List<LinkedMemberInfo>();
					ParentDrawerUtility.GetMemberBuildList(this, MemberHierarchy, ref debugModeInternalBuildList, true);
				}
			}
			var showHiddenComponents = inspector.Preferences.ShowHiddenComponents || DebugMode;
			GenerateMemberBuildList(targets, ref memberBuildList, ref componentsOnlyOnSomeObjectsFound, showHiddenComponents);
		}

		protected virtual void GenerateMemberBuildList(GameObject[] targets, ref List<Component[]> memberBuildList, ref bool componentsOnlyOnSomeObjectsFound, bool showHiddenComponents)
		{
			int targetCount = targets.Length;

			#if DEV_MODE
			Debug.Assert(targetCount > 0);
			#endif
			
			if(targetCount == 1)
			{
				componentsOnlyOnSomeObjectsFound = false;
				targets[0].GetComponents(GetComponentsList);

				for(int c = 0, count = GetComponentsList.Count; c < count; c++)
				{
					var comp = GetComponentsList[c];

					// Add null components, they are supported (handled by MissingScriptDrawer).
					if(comp == null || (!comp.hideFlags.HasFlag(HideFlags.HideInInspector) || showHiddenComponents))
					{
						memberBuildList.Add(ArrayPool<Component>.CreateWithContent(GetComponentsList[c]));
					}
				}

				GetComponentsList.Clear();
			}
			else
			{
				var compsByTarget = new Component[targetCount][];
				for(int n = 0; n < targetCount; n++)
				{
					var targetComponents = targets[n].GetComponents<Component>();
					
					// remove hidden components, unless they should be shown
					if(!showHiddenComponents)
					{
						for(int c = targetComponents.Length - 1; c >= 0; c--)
						{
							var comp = targetComponents[c];
							if(comp != null && comp.hideFlags.HasFlag(HideFlags.HideInInspector))
							{
								targetComponents = targetComponents.RemoveAt(c);
							}
						}
					}

					compsByTarget[n] = targetComponents;
				}
			
				componentsOnlyOnSomeObjectsFound = false;
				int nthNull = 0;

				var compsOnFirst = compsByTarget[0];
				int compsOnFirstCount = compsOnFirst.Length;

				//for each component on the first target
				for(int c = 0; c < compsOnFirstCount; c++)
				{
					var compOnFirst = compsOnFirst[c];
					bool isNull = compOnFirst == null;
					Type type;
					int nthInstance;
					if(isNull)
					{
						nthNull++;
						type = null;
						nthInstance = nthNull;
					}
					else
					{
						if(compOnFirst.hideFlags.HasFlag(HideFlags.HideInInspector) && !showHiddenComponents)
						{
							continue;
						}

						type = compOnFirst.GetType();
						nthInstance = compsOnFirst.CountPrecedingInstancesWithSameType(compOnFirst) + 1;
					}

					var compsToMultiEdit = ArrayPool<Component>.Create(targetCount);
					compsToMultiEdit[0] = compOnFirst;

					bool foundOnAll = true;

					//try and find matching Components on all other targets, with matching number of preceding instances
					for(int n = 1; n < targetCount; n++)
					{
						var compsOnTarget = compsByTarget[n];

						//find the nth member with the given type inside the array and returns its index
						int found = isNull ? compsOnTarget.IndexOfNthNull(nthNull) : compsOnTarget.IndexOfNthInstanceOfType(type, nthInstance);
						if(found == -1)
						{
							foundOnAll = false;
							//if there was even one target without the a component of the same type
							//in so many numbers, then forget about multi-editing this component
							break;
						}
						compsToMultiEdit[n] = compsOnTarget[found];
					}

					#if DEV_MODE && DEBUG_MULTI_TARGET
					Debug.Log("# "+c+" "+(type == null ? "null" : type.Name)+ ": foundOnAll=" + StringUtils.ToColorizedString(foundOnAll) + ", targetCount="+targetCount+", nthInstance="+nthInstance);
					#endif

					//if each target had this component at the same index it can be multi-edited
					if(foundOnAll)
					{
						memberBuildList.Add(compsToMultiEdit);
					}
					else
					{
						componentsOnlyOnSomeObjectsFound = true;
						ArrayPool<Component>.Dispose(ref compsToMultiEdit);
					}
				}

				if(!componentsOnlyOnSomeObjectsFound)
				{
					for(int n = 0; n < targetCount; n++)
					{
						if(compsByTarget[n].Length != compsOnFirstCount)
						{
							//TO DO: ignore hidden components here
							componentsOnlyOnSomeObjectsFound = true;
						}
					}
				}
			}
		}

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			if(DebugMode)
			{
				int internalMemberCount = debugModeInternalBuildList.Count;

				DrawerArrayPool.DisposeContent(ref debugModeInternalDrawers);
				DrawerArrayPool.Resize(ref debugModeInternalDrawers, internalMemberCount);
				ParentDrawerUtility.BuildMembers(DrawerProvider, this, debugModeInternalBuildList, ref debugModeInternalDrawers);

				var whitelist = new HashSet<string> {"Layer", "Active Self", "Active In Hierarchy", "Is Static", "Is Static Batchable", "Tag" };

				for(int n = debugModeInternalDrawers.Length - 1; n >= 0; n--)
				{
					if(!whitelist.Contains(debugModeInternalDrawers[n].Name))
					{
						debugModeInternalDrawers = debugModeInternalDrawers.RemoveAt(n);
					}
				}
				
				var firstTarget = targets[0];
				
				if(targets.Length > 1)
				{
					debugModeInternalDrawers = debugModeInternalDrawers.InsertAt(0, TextDrawer.Create(LinkedMemberInfo.MixedContentString, null, this, new GUIContent("Instance ID"), true));
				}
				else
				{
					debugModeInternalDrawers = debugModeInternalDrawers.InsertAt(0, IntDrawer.Create(firstTarget.GetInstanceID(), null, this, new GUIContent("Instance ID"), true));
				}

				var assetPathAndLocalId = new AssetPathAndLocalId(firstTarget);
				if(assetPathAndLocalId.HasPath())
				{
					var path = assetPathAndLocalId.AssetPath;
					debugModeInternalDrawers = debugModeInternalDrawers.InsertAt(0, TextDrawer.Create(path, null, this, new GUIContent("Asset Path"), true));					

					var guid = AssetDatabase.AssetPathToGUID(path);
					debugModeInternalDrawers = debugModeInternalDrawers.InsertAt(0, TextDrawer.Create(guid, null, this, new GUIContent("Guid"), true));

					#if UNITY_2018_1_OR_NEWER
					var localId = assetPathAndLocalId.LocalId;
					#if UNITY_2018_2_OR_NEWER
					debugModeInternalDrawers = debugModeInternalDrawers.InsertAt(0, LongDrawer.Create(localId, null, this, new GUIContent("Local File Id"), true));
					#else
					debugModeInternalDrawers = debugModeInternalDrawers.InsertAt(0, IntDrawer.Create(localId, null, this, new GUIContent("Local Id"), true));
					#endif
					#endif
				}
				
				debugModeInternalDrawers = debugModeInternalDrawers.Add(IntDrawer.Create((int)GameObjectUtility.GetStaticEditorFlags(GameObject), null, this, new GUIContent("Static Editor Flags"), true));
				debugModeInternalDrawers = debugModeInternalDrawers.Add(EnumDrawer.Create(firstTarget.hideFlags, MemberHierarchy.Get(null, typeof(Object).GetProperty("hideFlags"), LinkedMemberParent.UnityObject, "hideFlags"), this, new GUIContent("Hide Flags"), true));

				debugModeAdditionalHeight = DrawGUI.RightPadding;
				for(int n = debugModeInternalDrawers.Length - 1; n >= 0; n--)
				{
					debugModeAdditionalHeight += debugModeInternalDrawers[n].Height;
				}
			}
			else if(debugModeAdditionalHeight > 0f)
			{
				debugModeAdditionalHeight = 0f;
				DrawerArrayPool.Dispose(ref debugModeInternalDrawers, true);
				debugModeInternalDrawers = ArrayPool<IDrawer>.ZeroSizeArray;
			}

			int count = memberBuildList.Count;
			int memberCount = count;

			bool includeAddComponentButton = ShouldIncludeAddComponentButton();
			if(includeAddComponentButton)
			{
				memberCount++;
			}
			
			if(componentsOnlyOnSomeObjectsFound)
			{
				memberCount++;
			}

			DrawerArrayPool.Resize(ref members, memberCount);

			int index;
			int buildListIndex = 0;
			for(index = 0; index < count; index++)
			{
				var components = memberBuildList[buildListIndex];
				var drawer = DrawerProvider.GetForComponents(inspector, components, this);
				if(drawer == null)
				{
					#if DEV_MODE
					Debug.LogWarning("Could not create ComponentDrawer for Components: "+StringUtils.ToString(components));
					#endif

					// if could not create ComponentDrawer for Component remove
					// the target from the build list and restart the building process
					memberBuildList.RemoveAt(index);
					DisposeMembers();
					DoBuildMembers();
					return;
				}

				members[index] = drawer;

				#if ENABLE_MATERIAL_DRAWER_EMBEDDING && UNITY_EDITOR
				if(Types.Renderer.IsAssignableFrom(drawer.Type))
				{
					#if DEV_MODE
					if(!AddressablesUtility.IsInstalled || Event.current.control) // for testing purposes
					#else
					if(!AddressablesUtility.IsInstalled)
					#endif
					{
						var renderers = ArrayPool<Component>.Cast<Renderer>(drawer.Components);
						int targetCount = renderers.Length;
						var firstRenderer = renderers[0];
						var firstRendererSharedMaterials = firstRenderer.sharedMaterials;
						int firstRendererSharedMaterialCount = firstRendererSharedMaterials.Length;

						var materialsOnAllTargets = new List<Material>(firstRendererSharedMaterialCount);

						for(int m = 0, materialCount = firstRendererSharedMaterialCount; m < materialCount; m++)
						{
							var material = firstRendererSharedMaterials[m];
							if(material == null)
							{
								continue;
							}

							bool foundOnAll = true;
							for(int r = targetCount - 1; r >= 1; r--)
							{
								var renderer = renderers[r];
								var sharedMaterials = renderer.sharedMaterials;
								if(sharedMaterials.Length <= m || sharedMaterials[m] != material)
								{
									foundOnAll = false;
									break;
								}
							}

							if(foundOnAll)
							{
								materialsOnAllTargets.Add(material);
							}
						}
					
						for(int m = 0, materialCount = materialsOnAllTargets.Count; m < materialCount; m++)
						{
							#if DEV_MODE && PI_ASSERTATIONS
							Debug.Assert(materialsOnAllTargets[m] != null);
							#endif

							var materialTargets = ArrayPool<Material>.CreateWithContent(materialsOnAllTargets[m]);
							var materialDrawer = MaterialDrawer.Create(materialTargets, this, inspector);
							if(m == 0)
							{
								materialDrawer.SetIsFirstInspectedEditor(true);
							}
							index++;
							count++;
							members = members.InsertAt(index, materialDrawer);
						}
					}
					#if DEV_MODE
					else { Debug.LogWarning("Material embedding disabled when Addressables is installed due to issues when trying to draw the embedded material header."); }
					#endif
				}
				#endif

				buildListIndex++;
			}

			if(componentsOnlyOnSomeObjectsFound)
			{
				members[index] = GameObjectBoxDrawer.Create(this, GUIContentPool.Create("Components found only on some selected objects can't be multi-edited."));
				index++;
			}

			if(includeAddComponentButton)
			{
				members[index] = AddComponentButtonDrawer.Create(this, inspector);
			}
		}

		public virtual void RebuildMaterialDrawers()
		{
			#if ENABLE_MATERIAL_DRAWER_EMBEDDING && UNITY_EDITOR
			for(int n = members.Length - 1; n >= 0; n--)
			{
				var member = members[n];

				if(member is MaterialDrawer)
				{
					members = members.RemoveAt(n);

					int removeAt = Array.IndexOf(visibleMembers, member);
					if(removeAt != -1)
					{
						visibleMembers = visibleMembers.RemoveAt(removeAt);
					}
					continue;
				}

				var type = member.Type;
				if(!Types.Renderer.IsAssignableFrom(type))
				{
					continue;
				}

				#if DEV_MODE
				if(!AddressablesUtility.IsInstalled || Event.current.control) // for testing purposes
				#else
				if(!AddressablesUtility.IsInstalled)
				#endif
				{
					var renderers = ArrayPool<Object>.Cast<Renderer>(member.UnityObjects);
					int targetCount = renderers.Length;
					var firstRenderer = renderers[0];
					var firstRendererSharedMaterials = firstRenderer.sharedMaterials;
					int firstRendererSharedMaterialCount = firstRendererSharedMaterials.Length;

					var materialsOnAllTargets = new List<Material>(firstRendererSharedMaterialCount);

					for(int m = 0, materialCount = firstRendererSharedMaterialCount; m < materialCount; m++)
					{
						var material = firstRendererSharedMaterials[m];
						if(material == null)
						{
							continue;
						}

						bool foundOnAll = true;
						for(int r = targetCount - 1; r >= 1; r--)
						{
							var renderer = renderers[r];
							var sharedMaterials = renderer.sharedMaterials;
							if(sharedMaterials.Length <= m || sharedMaterials[m] != material)
							{
								foundOnAll = false;
								break;
							}
						}

						if(foundOnAll)
						{
							materialsOnAllTargets.Add(material);
						}
					}

					int visibleMembersIndex = Array.IndexOf(visibleMembers, member);
					for(int m = 0, materialCount = materialsOnAllTargets.Count; m < materialCount; m++)
					{
						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(materialsOnAllTargets[m] != null);
						#endif

						var materialTargets = ArrayPool<Material>.CreateWithContent(materialsOnAllTargets[m]);
						var materialDrawer = MaterialDrawer.Create(materialTargets, this, inspector);

						members = members.InsertAt(n + m + 1, materialDrawer);

						if(materialDrawer.ShouldShowInInspector)
						{
							visibleMembers = visibleMembers.InsertAt(visibleMembersIndex + m + 1, materialDrawer);
						}
					}
				}
				#if DEV_MODE
				else { Debug.LogWarning("Material embedding disabled when Addressables is installed due to issues when trying to draw the embedded material header."); }
				#endif
			}
			#endif
		}

		/// <inheritdoc/>
		protected override void OnAfterMemberBuildListGenerated()
		{
			if(!TypeExtensions.IsReady)
			{
				EditorApplication.delayCall += OnAfterMemberBuildListGenerated;
				return;
			}

			forceHideAddComponentMenuButton = !AddComponentUtility.CanAddComponents(memberBuildList);

			#if DEV_MODE && DEBUG_HIDE_ADD_COMPONENT_BUTTON
			Debug.Log(ToString()+".forceHideAddComponentMenuButton = "+forceHideAddComponentMenuButton+ " with ShouldIncludeAddComponentButton()="+ ShouldIncludeAddComponentButton());
			#endif
		}

		/// <summary>
		/// Determines whether or not an Add Component button should be added when building the members of this drawer.
		/// 
		/// Add Component button is not included for non-main assets such as the rig of a model.
		/// </summary>
		/// <returns> True if should add the button, false if not. </returns>
		protected virtual bool ShouldIncludeAddComponentButton()
		{
			return !forceHideAddComponentMenuButton && AllowAddingOrRemovingComponents();
		}

		/// <summary>
		/// Determines whether or not adding or removing any components is allowed for this drawer.
		/// 
		/// Should be false for non-main assets such as the rig of a model.
		/// </summary>
		/// <returns> True if can add or remove components, false if not. </returns>
		protected virtual bool AllowAddingOrRemovingComponents()
		{
			// Don't draw Add Component button for non-main assets such as the rig of a model.
			if(isPrefab && (Preferences.prefabQuickEditing != PrefabQuickEditingSettings.Enabled || !AssetDatabase.IsMainAsset(targets[0])))
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		/// <inheritdoc cref="IParentDrawer.OnChildLayoutChanged" />
		public override void OnChildLayoutChanged()
		{
			var preferences = InspectorUtility.Preferences;
			if(preferences.autoResizePrefixLabelsInterval == PrefixAutoOptimizationInterval.OnLayoutChanged && preferences.autoResizePrefixLabels == PrefixAutoOptimization.AllTogether)
			{
				OptimizeMemberPrefixLabelWidthsInUnison();
			}
			base.OnChildLayoutChanged();
		}

		/// <summary>
		/// Optimize prefix label width to be just wide enough to display prefixes of member drawers
		/// upto a certain maximum width threshold.
		/// </summary>
		private void OptimizeMemberPrefixLabelWidthsInUnison()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(InspectorUtility.Preferences.autoResizePrefixLabels == PrefixAutoOptimization.AllTogether);
			#endif

			float optimalWidth = GetOptimalPrefixLabelWidth(0, true);
			optimalWidth = Mathf.Clamp(optimalWidth, DrawGUI.MinAutoSizedPrefixLabelWidth, DrawGUI.MaxAutoSizedPrefixLabelWidth);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(optimalWidth > 0f, ToString()+ ".OptimizeMemberPrefixLabelWidthsInUnison result " + optimalWidth + " <= zero");
			Debug.Assert(optimalWidth >= DrawGUI.MinAutoSizedPrefixLabelWidth, ToString()+ ".OptimizeMemberPrefixLabelWidthsInUnison result " + optimalWidth + " < MinAutoSizedPrefixLabelWidth "+DrawGUI.MinAutoSizedPrefixLabelWidth);
			#endif

			// Skip transform and add component menu
			int lastComponentIndex = members.Length - LastCollectionMemberCountOffset;
			for(int n = lastComponentIndex; n >= 1; n--)
			{
				if(members[n] is IComponentDrawer componentDrawer)
				{
					if(optimalWidth < componentDrawer.MinPrefixLabelWidth)
					{
						optimalWidth = componentDrawer.MinPrefixLabelWidth;
					}
					else if(optimalWidth > componentDrawer.MaxPrefixLabelWidth)
					{
						optimalWidth = componentDrawer.MaxPrefixLabelWidth;
					}
				}
			}

			// Skip transform and add component menu
			for(int n = lastComponentIndex; n >= 1; n--)
			{
				if(members[n] is IComponentDrawer componentDrawer)
				{
					componentDrawer.PrefixLabelWidth = optimalWidth;
				}
			}
		}

		/// <inheritdoc/>
		protected override Type GetMemberType(Component[] memberBuildListItem)
		{
			return memberBuildListItem[0].GetType();
		}

		/// <inheritdoc/>
		protected override object GetMemberValue(Component[] memberBuildListItem)
		{
			return memberBuildListItem[0];
		}


		/// <inheritdoc cref="IDrawer.SetValue(object)" />
		public override bool SetValue(object newValue)
		{
			bool changed = false;
			var go = newValue as GameObject;
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				if(targets[n] != go)
				{
					changed = true;
					targets[n] = go;
				}
			}
			return changed;
		}

		/// <inheritdoc cref="IDrawer.GetValue(int)" />
		public override object GetValue(int index)
		{
			return targets[index];
		}

		/// <inheritdoc cref="IDrawer.GetValues" />
		public override object[] GetValues()
		{
			return targets;
		}

		/// <inheritdoc cref="IDrawer.GetOptimalPrefixLabelWidth" />
		public override float GetOptimalPrefixLabelWidth(int indentLevel)
		{
			//UPDATE: Can't use this, since need to skip transform and addcomponentmenu items?
			//return DrawerGroup.GetOptimalPrefixLabelWidth(this, indentLevel, true);

			float prefixWidth = DrawGUI.MinAutoSizedPrefixLabelWidth;
			//skip transform and add component menu button
			for(int n = members.Length - LastCollectionMemberCountOffset; n >= 1; n--)
			{
				var memb = members[n];
				if(memb == null)
				{
					#if DEV_MODE
					Debug.LogError(ToString() + ".GetOptimalPrefixLabelWidth - null member #"+n + "/"+members.Length);
					#endif
					continue;
				}
				float width = memb.GetOptimalPrefixLabelWidth(indentLevel, false);
				if(width > prefixWidth)
				{
					prefixWidth = width;
				}
			}
			
			prefixWidth = Mathf.Clamp(prefixWidth, DrawGUI.MinPrefixLabelWidth, DrawGUI.InspectorWidth - DrawGUI.MinControlFieldWidth);

			return prefixWidth;
		}

		/// <inheritdoc cref="IDrawer.Draw" />
		public override bool Draw(Rect position)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(position.width <= 0f) { Debug.LogError(GetType().Name+ ".Draw called with position.width <= 0f: " + position); }
			#endif

			Profiler.BeginSample("GameObjectDrawer.Draw");

			if(targets.ContainsNullObjects())
			{
				#if DEV_MODE
				Debug.LogWarning(this+".Draw() - target was null. Aborting Draw method and rebuilding in");
				#endif
				
				inspector.RebuildDrawersIfTargetsChanged();
				Profiler.EndSample();
				return true;
			}

			var guiColorWas = GUI.color;

			if(drawGreyedOut != GreyOut.None)
			{
				var color = GUI.color;
				color.a = 0.5f;
				GUI.color = color;
			}

			DrawGUI.IndentLevel = 0;

			if(Event.current.type == EventType.Layout)
			{
				OnLayoutEvent(position);
			}

			bool dirty = DrawPrefix(labelLastDrawPosition);

			if(drawGreyedOut == GreyOut.HeaderOnly)
			{
				var color = GUI.color;
				color.a = 1f;
				GUI.color = color;
			}

			bool guiEnabledWas = GUI.enabled;
			if(isPrefab && !DebugMode)
			{
				switch(inspector.Preferences.prefabQuickEditing)
				{
					case PrefabQuickEditingSettings.ViewOnly:
						#if DEV_MODE && DEBUG_GUI_ENABLED
						Debug.Log(Msg("GUI.enabled = ", false, " (because prefabQuickEditing=ViewOnly)"));
						#endif
						GUI.enabled = false;
						break;
					case PrefabQuickEditingSettings.Off:
						Profiler.EndSample();
						return false;
				}
			}
			
			float headerHeight = HeaderHeight;
			position.y += headerHeight;
			position.height -= headerHeight;

			if(DrawBody(position))
			{
				dirty = true;
			}

			guiColorWas.a = 1f;
			GUI.color = guiColorWas;

			if(isPrefab)
			{
				#if DEV_MODE && DEBUG_GUI_ENABLED
				if(GUI.enabled != guiEnabledWas && isPrefab) { Debug.Log(Msg("GUI.enabled = ", guiEnabledWas, " (restoring)")); }
				#endif
				GUI.enabled = guiEnabledWas;
			}

			Profiler.EndSample();
			
			return dirty;
		}

		/// <inheritdoc/>
		protected override void OnLayoutEvent(Rect position)
		{
			base.OnLayoutEvent(position);

			#if DEV_MODE && DEBUG_SET_MOUSEOVERED_PART
			var mouseoveredPartWas = mouseoveredPart;
			#endif

			var mousePos = Cursor.LocalPosition;
			if(!PrefixLabelPosition.Contains(mousePos))
			{
				mouseoveredPart = GameObjectHeaderPart.None;
			}
			else if(ActiveFlagPosition.Contains(mousePos))
			{
				mouseoveredPart = GameObjectHeaderPart.ActiveFlag;
			}
			else if(StaticFlagPosition.Contains(mousePos))
			{
				mouseoveredPart = GameObjectHeaderPart.StaticFlag;
			}
			else if(DropDownArrowPosition.Contains(mousePos))
			{
				mouseoveredPart = GameObjectHeaderPart.DropDownArrow;
			}
			else if(NameFieldPosition.Contains(mousePos))
			{
				mouseoveredPart = GameObjectHeaderPart.NameField;
			}
			else if(TagFieldPosition.Contains(mousePos))
			{
				mouseoveredPart = GameObjectHeaderPart.TagField;
			}
			else if(LayerFieldPosition.Contains(mousePos))
			{
				mouseoveredPart = GameObjectHeaderPart.LayerField;
			}
			else if(OpenPrefabButtonPosition.Contains(mousePos))
			{
				mouseoveredPart = GameObjectHeaderPart.OpenPrefab;
			}
			else if(SelectPrefabButtonPosition.Contains(mousePos))
			{
				mouseoveredPart = GameObjectHeaderPart.SelectPrefab;
			}
			else if(PrefabOverridesButtonPosition.Contains(mousePos))
			{
				mouseoveredPart = GameObjectHeaderPart.PrefabOverrides;
			}
			else
			{
				mouseoveredPart = GameObjectHeaderPart.Base;
			}

			#if DEV_MODE && DEBUG_SET_MOUSEOVERED_PART
			if(mouseoveredPart != mouseoveredPartWas) { Debug.Log("mouseoveredPart = "+mouseoveredPart +" (was: "+mouseoveredPartWas+")"); }
			#endif
		}
		
		/// <inheritdoc/>
		public override bool DrawPrefix(Rect position)
		{
			Profiler.BeginSample("GameObjectDrawer.DrawPrefix");
			
			bool guiChangedWas = GUI.changed;
			GUI.changed = false;
			beforeHeaderControlID = KeyboardControlUtility.Info.LastControlID;

			#if DEV_MODE && DEBUG_GUI_ENABLED
			if(isPrefab) { Debug.Log(Msg(GetType().Name+" Drawing Header with GUI.enabled=", GUI.enabled, ", GuiEnabled=", GuiEnabled, ", GUI.color=", GUI.color, ", ReadOnly=", ReadOnly)); }
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(position == labelLastDrawPosition);
			#endif

			try
			{
				headerDrawer.Draw(position);
			}
			#if DEV_MODE
			catch(ArgumentNullException e)
			{
				Debug.LogError(e);
			#else
			catch
			{
			#endif
				headerDrawer.OnProjectOrHierarchyChanged(targets, inspector);
			}
			bool dirty = GUI.changed;
			GUI.changed = guiChangedWas;
			
			if(DebugMode)
			{
				var drawPos = position;
				drawPos.y += HeaderHeightOutsideDebugMode + DrawGUI.RightPadding;
				for(int n = 0, count = debugModeInternalDrawers.Length; n < count; n++)
				{
					var member = debugModeInternalDrawers[n];
					if(member.ShouldShowInInspector)
					{
						drawPos.height = member.Height;
						member.Draw(drawPos);
						drawPos.y += drawPos.height;
					}
				}
			}

			DrawGUI.LayoutSpace(position.height);

			if(inspector.State.ViewIsLocked && !Selected)
			{
				var lockedIndicatorRect = position;
				DrawGUI.DrawSelectionOrFocusedControlRect(lockedIndicatorRect, inspector.Preferences.theme.LockViewHighlight);
			}

			Profiler.EndSample();
			
			return dirty;
		}

		/// <inheritdoc/>
		public override bool DrawBody(Rect position)
		{
			Profiler.BeginSample("GameObjectDrawer.DrawBody");

			var pos = position;
			bool dirty = false;

			var colorWas = GUI.color;
			int count = visibleMembers.Length;
			for(int n = 0; n < count; n++)
			{
				IDrawer member;
				member = visibleMembers[n];

				float height = member.Height;

				if(GameObject == null)
				{
					#if DEV_MODE && DEBUG_GUI_ENABLED
					if(isPrefab) { Debug.Log(Msg(GetType().Name+" - GUI.enabled = ", false, " (because target ", null, ")")); }
					#endif
					GUI.enabled = false;
				}

				pos.height = height;

				if(member.Draw(pos))
				{
					dirty = true;
					count = visibleMembers.Length; // make sure count is up to date

					for(int check = n + 1; check < count; check++)
					{
						if(visibleMembers[check] == null)
						{
							#if DEV_MODE
							Debug.LogError("GameObject visibleMembers["+n+"] null during DrawBody!");
							#endif
					
							Profiler.EndSample();
							RebuildMemberBuildListAndMembers();
							ExitGUIUtility.ExitGUI();
						}
					}
				}

				pos.y += height;

				GUI.color = colorWas;
			}
			
			if(AddComponentButton == null && visibleMembers.Length > 0)
			{
				var linePos = pos;
				linePos.height = 1f;
				DrawGUI.DrawLine(linePos, inspector.Preferences.theme.ComponentSeparatorLine);
			}

			Profiler.EndSample();
			
			return dirty;
		}

		/// <inheritdoc cref="IDrawer.OnMiddleClick" />
		public override void OnMiddleClick(Event inputEvent)
		{
			#if DEV_MODE && DEBUG_MIDDLE_CLICK && UNITY_EDITOR
			Debug.Log(ToString()+".OnMiddleClick("+StringUtils.ToString(inputEvent)+ ") with isPrefabInstance=" + StringUtils.ToColorizedString(isPrefabInstance));
			#endif

			DrawGUI.Use(inputEvent);

			if(isPrefabInstance)
			{
				#if UNITY_2018_2_OR_NEWER
				var prefab = PrefabUtility.GetCorrespondingObjectFromSource(GameObject);
				#else
				var prefab = PrefabUtility.GetPrefabParent(GameObject);
				#endif
				DrawGUI.Active.PingObject(prefab);
				return;
			}

			DrawGUI.Active.PingObject(GameObject);
		}

		/// <inheritdoc cref="IDrawer.DrawSelectionRect" />
		public override void DrawSelectionRect()
		{
			//UPDATE: Always draw the main selection rect
			//no matter which header part is selected.
			//Easier to see what is selected,
			//easier to understand that copy-paste works
			//no matter which header part is selected etc.
			DrawGUI.DrawSelectionRect(SelectionRect, localDrawAreaOffset);

			if(selectedPart == GameObjectHeaderPart.Base)
			{
				return;
			}

			switch(selectedPart)
			{
				case GameObjectHeaderPart.DropDownArrow:
					DrawGUI.DrawRect(DropDownArrowPosition, DrawGUI.GetSelectedLineIndicatorColor(), localDrawAreaOffset);
					return;
				case GameObjectHeaderPart.SelectPrefab:
					DrawGUI.DrawControlSelectionIndicator(SelectPrefabButtonPosition, localDrawAreaOffset);
					return;
				case GameObjectHeaderPart.OpenPrefab:
					DrawGUI.DrawControlSelectionIndicator(OpenPrefabButtonPosition, localDrawAreaOffset);
					return;
				case GameObjectHeaderPart.PrefabOverrides:
					DrawGUI.DrawControlSelectionIndicator(PrefabOverridesButtonPosition, localDrawAreaOffset);
					return;
			}
		}

		/// <inheritdoc cref="IDrawer.ValueToStringForFiltering" />
		public override string ValueToStringForFiltering()
		{
			return StringUtils.ToString(GameObject);
		}
		
		private void OpenPrefab()
		{
			OpenPrefab(GameObject);
		}

		public static void OpenPrefab(GameObject target)
		{
			#if DEV_MODE
			Debug.Log("Open Prefab("+target.name+")");
			#endif

			var parameterTypes = ArrayPool<Type>.Create(3);
			parameterTypes[0] = Types.String;
			parameterTypes[1] = Types.GameObject;
			var enumType = Types.GetInternalEditorType("UnityEditor.SceneManagement.StageNavigationManager+Analytics+ChangeType");
			parameterTypes[2] = enumType;
			
			var parameters = ArrayPool<object>.Create(3);
			parameters[0] = AssetDatabase.GetAssetPath(target);
			parameters[1] = null;
			parameters[2] = Enum.Parse(enumType, "EnterViaAssetInspectorOpenButton");

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(enumType != null);
			Debug.Assert(parameterTypes != null);
			#endif

			var openPrefabMethod = typeof(PrefabStageUtility).GetMethod("OpenPrefab", BindingFlags.Static | BindingFlags.NonPublic, null, CallingConventions.Any, parameterTypes, null);
			if(openPrefabMethod == null)
			{
				throw new NullReferenceException("PrefabStageUtility.OpenPrefab method not found!");
			}
			openPrefabMethod.Invoke(null, parameters);
			ArrayPool<object>.Dispose(ref parameters);
			ArrayPool<Type>.Dispose(ref parameterTypes);
		}

		/// <inheritdoc cref="IDrawer.OnRightClick" />
		public override bool OnRightClick(Event inputEvent)
		{
			switch(mouseoveredPart)
			{
				case GameObjectHeaderPart.StaticFlag:
				case GameObjectHeaderPart.DropDownArrow:
				{
					var menu = Menu.Create();

					menu.Add("Copy Static Settings", CopyStaticFlags);
					if(Clipboard.CanPasteAs(typeof(StaticEditorFlags)))
					{
						menu.Add("Paste Static Settings", PasteStaticFlags);
					}
					else
					{
						menu.AddDisabled("Paste Static Settings");
					}
				
					menu.AddSeparator();

					menu.Add("Help", ()=>Application.OpenURL("https://docs.unity3d.com/Manual/StaticObjects.html"));

					DrawGUI.Use(inputEvent);
			
					if(Selectable && !Selected)
					{
						Select(ReasonSelectionChanged.ThisClicked);
					}

					ContextMenuUtility.Open(menu, true, Inspector, InspectorPart.Viewport, this, mouseoveredPart);
					return true;
				}
				case GameObjectHeaderPart.TagField:
				{
					var menu = Menu.Create();

					menu.Add("Copy Tag", CopyTag);
					if(CanPasteTag())
					{
						menu.Add("Paste Tag", PasteTag);
					}
					else
					{
						menu.AddDisabled("Paste Tag");
					}

					menu.AddSeparator();

					menu.Add("Select Previous With Tag", SelectPreviousWithTag);
					menu.Add("Select Next With Tag", SelectNextWithTag);

					menu.AddSeparator();
					menu.Add("Help", ()=>Application.OpenURL("https://docs.unity3d.com/Manual/Tags.html"));
					
					DrawGUI.Use(inputEvent);
			
					if(Selectable && !Selected)
					{
						Select(ReasonSelectionChanged.ThisClicked);
					}

					ContextMenuUtility.Open(menu, this, (Part)GameObjectHeaderPart.TagField);

					return true;
				}
				case GameObjectHeaderPart.LayerField:
				{
					var menu = Menu.Create();

					menu.Add("Copy Layer", CopyLayer);
					if(Clipboard.CanPasteAs(Types.Int))
					{
						menu.Add("Paste Layer", PasteLayer);
					}
					else
					{
						menu.AddDisabled("Paste Layer");
					}

					menu.AddSeparator();
					menu.Add("Help", ()=>Application.OpenURL("https://docs.unity3d.com/Manual/Layers.html"));

					DrawGUI.Use(inputEvent);
			
					if(Selectable && !Selected)
					{
						Select(ReasonSelectionChanged.ThisClicked);
					}

					ContextMenuUtility.Open(menu, this, (Part)GameObjectHeaderPart.LayerField);

					return true;
				}
				case GameObjectHeaderPart.NameField:
				{
					var menu = Menu.Create();

					menu.Add("Copy Name", CopyName);
					if(Clipboard.Content.Length > 0)
					{
						menu.Add("Paste Name", PasteName);
					}
					else
					{
						menu.AddDisabled("Paste Name");
					}

					DrawGUI.Use(inputEvent);
			
					if(Selectable && !Selected)
					{
						Select(ReasonSelectionChanged.ThisClicked);
					}

					ContextMenuUtility.Open(menu, this, (Part)GameObjectHeaderPart.NameField);

					return true;
				}
				default:
					return base.OnRightClick(inputEvent);
			}
		}

		private void CopyStaticFlags()
		{
			Clipboard.Copy(GameObjectUtility.GetStaticEditorFlags(GameObject));
			Clipboard.SendCopyToClipboardMessage("Static Settings");
		}
		
		private void PasteStaticFlags()
		{
			object pasted = (StaticEditorFlags)0;
			if(Clipboard.TryPaste<StaticEditorFlags>(ref pasted))
			{
				var flags = (StaticEditorFlags)pasted;
				for(int n = targets.Length - 1; n >= 0; n--)
				{
					GameObjectUtility.SetStaticEditorFlags(targets[n], flags); 
				}
				Clipboard.SendPasteFromClipboardMessage("Static Settings");
			}
		}

		private void CopyName()
		{
			Clipboard.Copy(GameObject.name);
			Clipboard.SendCopyToClipboardMessage("Name");
		}
		
		private void PasteName()
		{
			object pasted = "";
			if(Clipboard.TryPaste<string>(ref pasted))
			{
				string name = (string)pasted;
				for(int n = targets.Length - 1; n >= 0; n--)
				{
					var target = targets[n];

					if(!string.Equals(target.name, name))
					{
						UndoHandler.RegisterUndoableAction(target, UndoHandler.GetSetValueMenuText("Name"));

						target.name = name;

						if(!target.IsSceneObject())
						{
							Platform.Active.SetDirty(target);
						}
					}
				}
				Clipboard.SendPasteFromClipboardMessage("Name");
			}
		}
		
		private void CopyTag()
		{
			Clipboard.Copy(GameObject.tag);
			Clipboard.SendCopyToClipboardMessage("Tag");
		}
		
		private bool CanPasteTag()
		{
			if(Clipboard.CopiedType != Types.String)
			{
				#if DEV_MODE
				Debug.Log("CanPasteTag: NO (CopiedType was "+Clipboard.CopiedType+")");
				#endif
				return false;
			}
			return Tags.TagExists(Clipboard.Content);
		}

		private void PasteTag()
		{
			object pasted = "Untagged";
			if(Clipboard.TryPaste<string>(ref pasted))
			{
				string tag = (string)pasted;
				for(int n = targets.Length - 1; n >= 0; n--)
				{
					var target = targets[n];

					if(!target.CompareTag(tag))
					{
						UndoHandler.RegisterUndoableAction(target, UndoHandler.GetSetValueMenuText("Tag"));

						target.tag = tag;

						if(!target.IsSceneObject())
						{
							Platform.Active.SetDirty(target);
						}
					}
				}
				Clipboard.SendPasteFromClipboardMessage("Tag");
			}
		}

		private void CopyLayer()
		{
			Clipboard.Copy(GameObject.layer);
			Clipboard.SendCopyToClipboardMessage("Layer");
		}
		
		private void PasteLayer()
		{
			object pasted = 0;
			if(Clipboard.TryPaste<int>(ref pasted))
			{
				int layer = (int)pasted;
				for(int n = targets.Length - 1; n >= 0; n--)
				{
					var target = targets[n];

					if(target.layer != layer)
					{
						UndoHandler.RegisterUndoableAction(target, UndoHandler.GetSetValueMenuText("Layer"));

						target.layer = layer;

						if(!target.IsSceneObject())
						{
							Platform.Active.SetDirty(target);
						}
					}
				}
				Clipboard.SendPasteFromClipboardMessage("Layer");
			}
		}

		/// <inheritdoc cref="BaseDrawer.BuildContextMenu" />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			var hideFlags = GameObject.hideFlags;

			if(extendedMenu)
			{
				menu.Add("Inspect Static Members", ()=>inspector.RebuildDrawers(null, Types.GameObject));
			}

			if(hideFlags != HideFlags.None || extendedMenu)
			{
				menu.Add("Hide Flags/None", ()=>targets.ToList().ForEach((obj)=> obj.hideFlags = HideFlags.None), hideFlags == HideFlags.None);
				menu.Add("Hide Flags/Hide In Hierarchy", () => targets.ToList().ForEach((obj) => obj.hideFlags = HideFlags.HideInHierarchy), hideFlags == HideFlags.HideInHierarchy);
				menu.Add("Hide Flags/Hide In Inspector", () => targets.ToList().ForEach((obj) => obj.hideFlags = HideFlags.HideInInspector), hideFlags == HideFlags.HideInInspector);
				menu.Add("Hide Flags/Don't Save In Editor", () => targets.ToList().ForEach((obj) => obj.hideFlags = HideFlags.DontSaveInEditor), hideFlags == HideFlags.DontSaveInEditor);
				menu.Add("Hide Flags/Not Editable", () => targets.ToList().ForEach((obj) => obj.hideFlags = HideFlags.NotEditable), hideFlags == HideFlags.NotEditable);
				menu.Add("Hide Flags/Don't Save In Build", () => targets.ToList().ForEach((obj) => obj.hideFlags = HideFlags.DontSaveInBuild), hideFlags == HideFlags.DontSaveInBuild);
				menu.Add("Hide Flags/Don't Unload Unused Asset", () => targets.ToList().ForEach((obj) => obj.hideFlags = HideFlags.DontUnloadUnusedAsset), hideFlags == HideFlags.DontUnloadUnusedAsset);
				menu.Add("Hide Flags/Don't Save", () => targets.ToList().ForEach((obj) => obj.hideFlags = HideFlags.DontSave), hideFlags == HideFlags.DontSave);
				menu.Add("Hide Flags/Hide And Don't Save	", () => targets.ToList().ForEach((obj) => obj.hideFlags = HideFlags.HideAndDontSave), hideFlags == HideFlags.HideAndDontSave);
				
				menu.AddSeparator();
			}

			if(isPrefabInstance || isPrefab)
			{
				menu.Add("Open Prefab", OpenPrefab);
			}

			menu.Add("Ping", PingTargets);

			int targetIndexInSelection = -1;
			bool someTargetsAreSelected = false;
			bool someTargetsAreNotSelected = false;
			var selectedObjects = inspector.SelectedObjects;
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				targetIndexInSelection = Array.IndexOf(selectedObjects, targets[n]);
				if(targetIndexInSelection == -1)
				{
					someTargetsAreNotSelected = true;
				}
				else
				{
					someTargetsAreSelected = true;
				}
			}
			
			if(someTargetsAreSelected)
			{
				menu.Add("Deselect", ()=>
				{
					if(UserSettings.MergedMultiEditMode || selectedObjects.Length == 1)
					{
						inspector.Select(null as Object);
					}
					else
					{
						inspector.Select(inspector.SelectedObjects.RemoveAt(targetIndexInSelection));
					}
				});
			}

			if(someTargetsAreNotSelected)
			{
				menu.Add("Select", SelectTargets);
			}

			menu.AddSeparator();

			bool canMoveToBack = false;
			bool canMoveToFront = false;
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var transform = targets[n].transform;
				var transformParent = transform.parent;
				if(transformParent != null)
				{
					int childCount = transformParent.childCount;
					if(childCount >= 2)
					{
						if(transformParent.GetChild(0) != transform)
						{
							canMoveToBack = true;
						}

						if(transformParent.GetChild(childCount - 1) != transform)
						{
							canMoveToFront = true;
						}
					}
				}
			}

			bool canChangeValues = !ReadOnly;

			if(canChangeValues)
			{
				menu.Add("Auto-Name", NameByMainComponent);
				menu.AddSeparator();
			}

			if(canMoveToBack || canMoveToFront)
			{
				bool isUiElement = targets[0].transform is RectTransform;

				menu.Add(isUiElement ? "Move To Back" : "Set As First Sibling", MoveToBack, !canMoveToBack);
				menu.Add(isUiElement ? "Move To Front" : "Set As Last Sibling", MoveToFront, !canMoveToFront);

				menu.AddSeparator();
			}

			if(canChangeValues)
			{
				menu.Add("Reset", Reset);
				menu.Add("Reset Component Values", ResetComponentValues);
				menu.AddSeparator();
			}
			
			AddCopyPasteMenuItems(ref menu);
			menu.AddSeparator();

			menu.Add("Collapse All", ()=> SetUnfolded(false, true));
			menu.Add("Expand All", ()=> SetUnfolded(true, true));
		}

		private void AddCopyPasteMenuItems(ref Menu menu)
		{
			menu.Add("Copy", CopyToClipboard);
			
			bool multipleTargets = targets.Length >= 2;
			if(isPrefab)
			{
				menu.Add(multipleTargets ? "Copy Asset Paths" : "Copy Asset Path", CopyAssetPathClipboard);
			}
			else
			{
				menu.Add(multipleTargets ? "Copy Hierarchy Paths" : "Copy Hierarchy Path", CopyHierarchyPathToClipboard);
			}

			if(!ReadOnly)
			{
				menu.Add("Paste", PasteFromClipboard);

				if(AllowAddingOrRemovingComponents() && Clipboard.HasObjectReference() && Clipboard.CopiedType == Types.GameObject)
				{
					menu.Add("Paste Components", PasteComponentsFromClipboard);
				}
			}
		}

		/// <inheritdoc/>
		public override bool CanPasteFromClipboard()
		{
			return !ReadOnly && Clipboard.HasObjectReference();
		}

		#if DEV_MODE
		/// <inheritdoc/>
		protected override void AddDevModeDebuggingEntriesToRightClickMenu(ref Menu menu)
		{
			base.AddDevModeDebuggingEntriesToRightClickMenu(ref menu);

			menu.Add("Debugging/Update Visible Members", UpdateVisibleMembers);
			menu.Add("Debugging/List Members", ()=>{Debug.Log("Members: "+StringUtils.ToString(Members)+ "\nVisibleMembers: " + StringUtils.ToString(Members));});
			menu.Add("Debugging/Validate Members", ValidateMembers);
		}
		#endif

		private void NameByMainComponent()
		{
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var target = targets[n];
				var component = target.GetMainComponent();
				ComponentDrawerUtility.NameByType(target, component);
			}
		}

		private void CopyAssetPathClipboard()
		{
			int count = targets.Length;
			if(count >= 2)
			{
				var copy = StringArrayPool.Create(count);
				for(int n = count - 1; n >= 0; n--)
				{
					copy[n] = AssetDatabase.GetAssetPath(targets[n]);
				}
				Clipboard.Copy(copy);
			}
			else
			{
				Clipboard.Copy(AssetDatabase.GetAssetPath(GameObject));
			}
		}

		private void CopyHierarchyPathToClipboard()
		{
			int count = targets.Length;
			if(count >= 2)
			{
				var copy = StringArrayPool.Create(count);
				for(int n = count - 1; n >= 0; n--)
				{
					copy[n] = targets[n].transform.GetHierarchyPath();
				}
				Clipboard.Copy(copy);
			}
			else
			{
				Clipboard.Copy(GameObject.transform.GetHierarchyPath());
			}
		}

		/// <summary>
		/// Make all GameObject targets be the first child of their parent GameObjects (if any).
		/// In the context of UI elements, this means that they are rendered first, which is where
		/// the "Move To Back" comes from.
		/// </summary>
		private void MoveToBack()
		{
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var t = targets[n].transform;
				if(t.parent != null)
				{
					t.SetAsFirstSibling();
				}
			}
		}

		/// <summary>
		/// Make all GameObject targets be the last child of their parent GameObjects (if any).
		/// In the context of UI elements, this means that they are rendered last, which is where
		/// the "Move To Front" comes from.
		/// </summary>
		private void MoveToFront()
		{
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var t = targets[n].transform;
				if(t.parent != null)
				{
					t.SetAsLastSibling();
				}
			}
		}

		/// <inheritdoc cref="IDrawer.CopyToClipboard" />
		public override void CopyToClipboard()
		{
			if(targets.Length > 1)
			{
				Clipboard.TryCopy(targets, Types.GameObject);
				SendCopyToClipboardMessage();
				DeepCopyUsingMenuItem();
				return;
			}

			CopyToClipboard(0);
		}

		/// <inheritdoc/>
		public override void CopyToClipboard(int index)
		{
			Clipboard.TryCopy(targets[index], Types.GameObject);
			SendCopyToClipboardMessage();
			DeepCopyUsingMenuItem();
		}

		/// <summary>
		/// Gives focus to the hierarcy window and then invokes the Edit/Copy menu item
		/// to copy the GameObject internally. This makes it possible to use the Edit/Paste
		/// menu item to paste a new instance of the GameObject, and even if the loaded
		/// scene is changed in the editor.
		/// 
		/// This method can only be called in the editor.
		/// </summary>
		private void DeepCopyUsingMenuItem()
		{
			bool selectionMatchesTargets = true;
			var selection = Selection.gameObjects;
			for(int n = selection.Length - 1; n >= 0; n--)
			{
				if(Array.IndexOf(targets, selection[n]) == -1)
				{
					selectionMatchesTargets = false;
					break;
				}
			}

			var manager = Manager;
			var focusedControl = manager.FocusedDrawer;

			if(selectionMatchesTargets)
			{
				DrawGUI.ExecuteMenuItem("Window/General/Hierarchy");
				DrawGUI.ExecuteMenuItem("Edit/Copy");
			}
			else
			{
				bool viewWasLocked = inspector.State.ViewIsLocked;

				inspector.State.ViewIsLocked = true;

				inspector.Select(targets);
				DrawGUI.ExecuteMenuItem("Window/General/Hierarchy");
				DrawGUI.ExecuteMenuItem("Edit/Copy");

				inspector.Select(selection);
				inspector.State.ViewIsLocked = viewWasLocked;
			}

			//refocus the inspector drawer window
			//since it can lose focus when executing menu items
			inspector.InspectorDrawer.FocusWindow();
			
			if(focusedControl != null)
			{
				inspector.Select(focusedControl, ReasonSelectionChanged.Initialization);
			}
			else
			{
				manager.Select(inspector, InspectorPart.Viewport, ReasonSelectionChanged.Initialization);
			}
		}

		/// <inheritdoc/>
		protected override void DoReset()
		{
			UndoHandler.RegisterUndoableAction(GameObject, "Reset GameObject");

			GameObject.layer = LayerMask.NameToLayer("Default");
			GameObject.tag = "Untagged";
			GameObject.isStatic = false;
			GameObject.SetActive(true);

			UpdateIsEditable();

			if(OnValueChanged != null)
			{
				OnValueChanged(this, GameObject);
			}
		}

		/// <inheritdoc/>
		public virtual void OnProjectOrHierarchyChanged(OnChangedEventSubject changed, ref bool hasNullReferences)
		{
			if(nowRemovingComponent)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+ " ignoring OnProjectOrHierarchyChanged event because nowRemovingComponent was true.");
				#endif
				return;
			}

			if(changed == OnChangedEventSubject.Hierarchy)
			{
				if(isPrefab)
				{
					return;
				}
			}
			else if(changed == OnChangedEventSubject.Project)
			{
				if(!isPrefab)
				{
					return;
				}
			}

			for(int n = targets.Length - 1; n >= 0; n--)
			{
				Object target = targets[n];
				if(target == null)
				{
					if(UnityObjectExtensions.TryToFixNull(ref target))
					{
						#if DEV_MODE
						Debug.LogWarning(ToString()+".OnProjectOrHierarchyChanged fixed targets["+n+"] (\""+target.name+"\") being null.");
						#endif
						continue;
					}
					
					#if DEV_MODE
					Debug.Log(ToString()+".OnProjectOrHierarchyChanged targets["+n+"] was null and could not be fixed.");
					#endif

					hasNullReferences = true;
				}
			}

			// Handle MaterialDrawers in members.
			for(int n = members.Length - 1; n >= 0; n--)
			{
				var memberToHandle = members[n] as IOnProjectOrHierarchyChanged;
				if(memberToHandle != null)
				{
					memberToHandle.OnProjectOrHierarchyChanged(changed, ref hasNullReferences);
				}
			}
			
			headerDrawer.OnProjectOrHierarchyChanged(targets, inspector);

			UpdateIsEditable();
			UpdateLabelText();

			HandlePossibleVisibleComponentsChanged();
		}
		
		private void HandlePossibleVisibleComponentsChanged()
		{
			var showHiddenComponents = inspector.Preferences.ShowHiddenComponents || DebugMode;
			GenerateMemberBuildList(targets, ref memberBuildListTempHolder, ref componentsOnlyOnSomeObjectsFound, showHiddenComponents);

			int countWas = memberBuildList.Count;
			int countIs = memberBuildListTempHolder.Count;
			bool visibleComponentsChanged;
			if(countIs != countWas)
			{
				visibleComponentsChanged = true;
			}
			else
			{
				visibleComponentsChanged = false;
				for(int n = countIs - 1; n >= 0; n--)
				{
					if(!memberBuildList[n].ContentsMatch(memberBuildListTempHolder[n]))
					{
						visibleComponentsChanged = true;
						break;
					}
				}
			}

			if(visibleComponentsChanged)
			{
				assumeVisibleMembersChangedDuringNextUpdateVisibleMembers = true;

				var swap = memberBuildList;
				memberBuildList = memberBuildListTempHolder;
				memberBuildListTempHolder = swap;
				memberBuildListTempHolder.Clear();
				RebuildMembers();
			}
			else
			{
				memberBuildListTempHolder.Clear();
			}
		}

		private void UpdateIsEditable()
		{
			editable = true;
			drawGreyedOut = GreyOut.None;

			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var gameObject = targets[n];
				if(gameObject != null)
				{
					if(gameObject.hideFlags.HasFlag(HideFlags.NotEditable))
					{
						editable = false;
						drawGreyedOut = GreyOut.None;
						return;
					}

					if(isPrefab)
					{
						if(PrefabUtility.IsPartOfImmutablePrefab(gameObject))
						{
							editable = false;
							drawGreyedOut = GreyOut.None;
							return;
						}

						if(inspector.Preferences.prefabQuickEditing != PrefabQuickEditingSettings.Enabled)
						{
							editable = false;
							drawGreyedOut = GreyOut.None;
							return;
						}

						if(!gameObject.ActiveInPrefabHierarchy())
						{
							drawGreyedOut = inspector.Preferences.drawInactivateGreyedOut;
						}
					}
					else if(!gameObject.activeInHierarchy)
					{
						drawGreyedOut = inspector.Preferences.drawInactivateGreyedOut;
					}
				}
				#if DEV_MODE
				else { Debug.Log(Msg(ToString()+".UpdateGrayedOut called with GameObject ", null, ". inactive=", inactive)); }
				#endif
			}
		}

		/// <summary>
		/// Resets the component values.
		/// </summary>
		private void ResetComponentValues()
		{
			for(int n = members.Length - LastCollectionMemberCountOffset; n >= 0; n--)
			{
				members[n].Reset(false);
			}
		}

		/// <summary>
		/// Selects GameObject targets.
		/// </summary>
		private void SelectTargets()
		{
			if(targets.Length == 1)
			{
				Inspector.SelectAndShow(GameObject, ReasonSelectionChanged.OtherClicked);
				return;
			}
			
			if(inspector.SelectedObjects.ContentsMatch(targets))
			{
				return;
			}

			inspector.Select(targets);
		}

		/// <summary>
		/// Override all components from GameObject copied to the clipboard.
		/// </summary>
		private void PasteComponentsFromClipboard()
		{
			//TO DO: Add support for multiple copied targets? at least if matches number of currently selected targets?
			var copied = Clipboard.PasteObjectReference(Types.GameObject) as GameObject;
			if(copied != null)
			{
				for(int t = targets.Length - 1; t >= 0; t--)
				{
					if(targets[t] == copied)
					{
						inspector.Message("Can't paste components because copied GameObject \""+copied.name+ "\" is one of the targets.", copied, MessageType.Warning);
						return;
					}
				}
			
				//Destroy existing components on all targets
				for(int t = targets.Length - 1; t >= 0; t--)
				{
					DestroyComponents(targets[t]);
				}

				//get components that should be copied over
				copied.GetComponents(GetComponentsList);
				var copyComponents = GetComponentsList;
				int componentsToCopyCount = GetComponentsList.Count;

				for(int t = targets.Length - 1; t >= 0; t--)
				{
					var target = targets[t];
					
					//Copy over new ones
					CopyComponents(copyComponents);
					
					Clipboard.SendPasteFromClipboardMessage(StringUtils.Concat("Pasted ", componentsToCopyCount, " components{0}."), target.name);
					
				}
				copyComponents.Clear();
			}
		}
		
		/// <summary> Destroys all components on the target except for the Transform component (which is mandatory). </summary>
		/// <param name="target"> Target GameObject whose Components to destroy. </param>
		private static void DestroyComponents(GameObject target)
		{
			target.GetComponents(GetComponentsList);
			for(int n = GetComponentsList.Count - 1; n >= 0; n--)
			{
				var comp = GetComponentsList[n];
				if(comp != null && !(comp is Transform))
				{
					SmartDestroy(comp);
				}
			}
		}

		/// <summary>
		/// Smart destroy.
		/// </summary>
		/// <param name="target">
		/// Target for the. </param>
		private static void SmartDestroy(Object target)
		{
			if(!Application.isPlaying)
			{
				Object.DestroyImmediate(target);
				return;
			}

			//TO DO: also remove components that rely on this component!

			Object.Destroy(target);
		}
		
		private void CopyComponents(List<Component> copyComponents)
		{
			int count = copyComponents.Count;
			for(int n = 0; n < count; n++)
			{
				var component = copyComponents[n];
				if(component != null && !(component is Transform))
				{
					ReusableTypesList.Add(component.GetType());
				}
			}

			var added = AddComponents(ReusableTypesList, false);
			int targetCount = targets.Length;
			int sourceIndex = 0;
			for(int n = 0; n < count; n++)
			{
				var copyComponent = copyComponents[n];
				if(copyComponent == null)
				{
					continue;
				}

				string copiedData = PrettySerializer.SerializeUnityObject(copyComponent, ref ReusableObjectReferencesList);

				var copyTransform = copyComponent as Transform;
				if(copyTransform != null)
				{
					for(int t = targetCount - 1; t >= 0; t--)
					{
						var targetTransform = targets[t].transform;
						PrettySerializer.DeserializeUnityObject(copiedData, targetTransform, ref ReusableObjectReferencesList);
						OnValidateHandler.CallForTarget(targetTransform);
					}
				}
				else
				{
					var targetComponentDrawer = added[sourceIndex];
					var targetComponents = (Component[])targetComponentDrawer.GetValues();
					for(int c = targetComponents.Length - 1; c >= 0; c--)
					{
						var targetComponent = targetComponents[c];
						PrettySerializer.DeserializeUnityObject(copiedData, targetComponent, ref ReusableObjectReferencesList);
						OnValidateHandler.CallForTarget(targetComponent);
					}
					sourceIndex++;
				}

				ReusableObjectReferencesList.Clear();
			}
			ReusableTypesList.Clear();
		}

		/// <inheritdoc />
		public override string GetFieldNameForMessages()
		{
			return targets.Length == 1 ? targets[0].name : "";
		}

		/// <summary>
		/// Ping targets.
		/// </summary>
		private void PingTargets()
		{
			GUI.changed = true;
			DrawGUI.Ping(targets);
		}

		/// <inheritdoc/>
		protected override void DoPasteFromClipboard()
		{
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				#if DEV_MODE
				bool success = Clipboard.TryPasteUnityObject(targets[n]);
				Debug.Log("Clipboard.TryPaste(" + Type.Name + ", ref " + (targets[n] == null ? "null" : targets[n].name) + "): "+StringUtils.ToColorizedString(success));
				#else
				Clipboard.TryPasteUnityObject(targets[n]);
				#endif
			}
		}

		/// <inheritdoc cref="IDrawer.OnMouseover" />
		public override void OnMouseover()
		{
			#if DEV_MODE && DEBUG_SET_MOUSEOVERED_PART
			var debugColor = Color.red;
			debugColor.a = 0.5f;
			switch(mouseoveredPart)
			{
				//case GameObjectHeaderPart.None:
				//	DrawGUI.DrawMouseoverEffect(PrefixLabelPosition, Color.red);
				//	break;
				case GameObjectHeaderPart.ActiveFlag:
					DrawGUI.DrawMouseoverEffect(ActiveFlagPosition, debugColor);
					break;
				case GameObjectHeaderPart.StaticFlag:
					DrawGUI.DrawMouseoverEffect(StaticFlagPosition, debugColor);
					break;
				case GameObjectHeaderPart.DropDownArrow:
					DrawGUI.DrawMouseoverEffect(DropDownArrowPosition, debugColor);
					break;
				case GameObjectHeaderPart.NameField:
					DrawGUI.DrawMouseoverEffect(NameFieldPosition, debugColor);
					break;
				case GameObjectHeaderPart.TagField:
					DrawGUI.DrawMouseoverEffect(TagFieldPosition, debugColor);
					break;
				case GameObjectHeaderPart.LayerField:
					DrawGUI.DrawMouseoverEffect(LayerFieldPosition, debugColor);
					break;
				case GameObjectHeaderPart.OpenPrefab:
					DrawGUI.DrawMouseoverEffect(OpenPrefabButtonPosition, debugColor);
					break;
				case GameObjectHeaderPart.SelectPrefab:
					DrawGUI.DrawMouseoverEffect(SelectPrefabButtonPosition, debugColor);
					break;
				case GameObjectHeaderPart.PrefabOverrides:
					DrawGUI.DrawMouseoverEffect(PrefabOverridesButtonPosition, debugColor);
					break;
			}
			#endif

			switch(mouseoveredPart)
			{
				case GameObjectHeaderPart.StaticFlag:
				case GameObjectHeaderPart.ActiveFlag:
					DrawGUI.Active.SetCursor(MouseCursor.Link);
					return;
				case GameObjectHeaderPart.Base:
					if(inspector.Preferences.enableTutorialTooltips)
					{
						GUI.Label(labelLastDrawPosition, new GUIContent("", isPrefabInstance || isPrefab ? "Middle Mouse : Ping Prefab In Assets" : "Middle Mouse : Ping In Hierarchy"));
					}
					return;
			}
		}

		/// <inheritdoc/>
		public virtual void OnMemberDragNDrop(MouseDownInfo mouseDownInfo, Object[] draggedObjects)
		{
			var reordering = mouseDownInfo.Reordering;

			var dropTargetInfo = reordering.MouseoveredDropTarget;
			int dropIndex = reordering.MouseoveredDropTarget.MemberIndex;
			
			#if DEV_MODE && DEBUG_DRAG_N_DROP
			Debug.LogError(ToString()+ ".OnMemberDragNDrop(" + mouseDownInfo +", "+ StringUtils.ToString(draggedObjects) + "), dropIndex=" + dropIndex + ", ActiveInspector="+InspectorUtility.ActiveInspector+", Event="+StringUtils.ToString(Event.current) + ", reorderingControl=" + StringUtils.ToString(reordering.Control));
			#endif

			int count = draggedObjects.Length;
			if(count == 0 && reordering.Drawer == null)
			{
				return;
			}

			if(dropIndex <= 0)
			{
				return;
			}

			var reorderingControl = reordering.Drawer;

			Component[] draggedComponents;
			if(reorderingControl == null)
			{
				var dropTargetInspector = dropTargetInfo.Inspector;
				var viewWasLocked = dropTargetInspector.State.ViewIsLocked;
				dropTargetInspector.State.ViewIsLocked = true;

				bool wereMonoScripts = false;
				for(int n = 0; n < count; n++)
				{
					var monoScript = draggedObjects[n] as MonoScript;
					if(monoScript != null)
					{
						wereMonoScripts = true;

						var componentType = monoScript.GetClass();
						if(componentType == null)
						{
							continue;
						}

						int addComponentIndex = dropIndex;
						OnNextLayout(()=> AddComponent(componentType, true, addComponentIndex));
						dropIndex++;
						#if DEV_MODE && DEBUG_DRAG_N_DROP_DETAILED
						Debug.Log("wereMonoScripts = "+StringUtils.True+ ", addComponentIndex = " + addComponentIndex);
						#endif
					}
				}

				dropTargetInspector.State.ViewIsLocked = viewWasLocked;

				if(wereMonoScripts)
				{
					DrawGUI.Active.AcceptDrag();
					DrawGUI.ClearDragAndDropObjectReferences();
					mouseDownInfo.Clear();
					return;
				}

				#if DEV_MODE && DEBUG_DRAG_N_DROP_DETAILED
				Debug.Log("wereMonoScripts = "+StringUtils.False);
				#endif

				//try to cast dragged objects into components
				//if failed, then stop
				if(!ArrayPool<Object>.TryCast(draggedObjects, false, out draggedComponents))
				{
					return;
				}
			}
			else
			{
				draggedComponents = (Component[])reorderingControl.GetValues();
			}

			//TO DO: Support multi-select
			var draggedComponent = draggedComponents[0];

			var sourceParent = reordering.Parent;

			//handle MissingComponent
			if(draggedComponent == null)
			{
				if(this != sourceParent)
				{
					Debug.LogWarning("Moving Missing Components between GameObjects is not supported");
					return;
				}

				var hierarchy = LinkedMemberHierarchy.Get(UnityObjects);
				var serializedObject = hierarchy.SerializedObject;
				if(serializedObject != null)
				{
					var property = serializedObject.FindProperty("m_Component");

					int sourceIndex = reordering.MemberIndex;

					count = property.arraySize;
					#if DEV_MODE && DEBUG_DRAG_N_DROP_DETAILED
					Debug.Log("Moving array element from " + sourceIndex + " to " + dropIndex + " (count="+ count + ")");
					#endif
					if(sourceIndex < count && dropIndex < count)
					{
						//fix for property.MoveArrayElement moving the MissingComponent two steps upwards
						if(dropIndex < sourceIndex)
						{
							dropIndex++;
							#if DEV_MODE && DEBUG_DRAG_N_DROP_DETAILED
							Debug.Log("dropIndex++, now: "+ dropIndex);
							#endif
						}

						property.MoveArrayElement(sourceIndex, dropIndex);
						serializedObject.ApplyModifiedProperties();
					}
				}
				return;
			}

			#if DEV_MODE && DEBUG_DRAG_N_DROP_DETAILED
			Debug.Log("visibleMembers["+(dropIndex - 1)+"]: "+ visibleMembers[dropIndex]);
			#endif

			var dropUnder = (IComponentDrawer)visibleMembers[dropIndex - 1];
			
			#if DEV_MODE && DEBUG_DRAG_N_DROP
			Debug.Log(StringUtils.ToColorizedString("draggedComponent=", draggedComponent, ", dropUnder.Component=", dropUnder.Component, ", dropUnder=", dropUnder.ToString()));
			#endif

			if(dropUnder.Component != draggedComponent)
			{
				#if DEV_MODE && DEBUG_DRAG_N_DROP_DETAILED
				Debug.Log("sourceParent now: "+StringUtils.ToString(sourceParent));
				#endif

				DrawGUI.Active.AcceptDrag();
				DrawGUI.ClearDragAndDropObjectReferences();

				#if DEV_MODE && DEBUG_DRAG_N_DROP
				Debug.Log(StringUtils.ToColorizedString("Drag N Dropped component ", draggedComponent.GetType(), " below "+ dropUnder.Component.GetType(), " within ", (sourceParent == this ? "single" : "multiple"), " GameObjectDrawer"));
				#endif

				// TO DO: handle case where there are multiple targets
				var sourceGameObject = draggedComponent.gameObject;
				var sourceComponents = sourceGameObject.GetComponents<Component>();
				int from = Array.IndexOf(sourceComponents, draggedComponent);
				int to = dropIndex;
				if(from > 0)
				{
					// handle case where Components are dragged between different GameObjects
					var targetGameObject = GameObject;
					if(targetGameObject != sourceGameObject)
					{
						#if DEV_MODE && DEBUG_DRAG_N_DROP
						Debug.Log("Moving component " + draggedComponent.GetType().Name + " from GameObject \"" + sourceGameObject.name + "\" to \"" + targetGameObject.name + "\" under " + dropUnder.Component.GetType().Name + " ...");
						#endif

						var newComponents = MoveComponentsUnderComponents(draggedComponents, dropUnder.Components);
						RebuildMemberBuildListAndMembers();
						reordering.Parent.RebuildMemberBuildListAndMembers();
						var dropTargetInspector = dropTargetInfo.Inspector;
						dropTargetInspector.ScrollToShow(newComponents[0]);
						return;
					}

					int diff = to - from;
					if(diff >= 2)
					{
						#if DEV_MODE && DEBUG_DRAG_N_DROP
						Debug.Log("Moving component "+draggedComponent.GetType().Name+" down "+(diff-1)+" steps from "+from+" to "+to+" under "+dropUnder.Component.GetType().Name+"...");
						#endif

						MoveComponentsUnderComponents(draggedComponents, dropUnder.Components);

						if(sourceParent == this)
						{
							SetMembers(members.Shift(from, to));
						}
						else
						{
							#if DEV_MODE && DEBUG_DRAG_N_DROP_DETAILED
							Debug.Log("RemoveAt(" + from + ") from members: " + StringUtils.ToString(members));
							#endif

							try
							{
								sourceParent.SetMembers(sourceParent.Members.RemoveAt(from));
							}
							catch(NullReferenceException)
							{
								if(sourceParent == null)
								{
									Debug.LogError("sourceParent was null. reordering.Control="+StringUtils.ToString(reordering.Drawer)+", reordering.Control.Parent="+(reordering.Drawer == null ? "n/a" : StringUtils.ToString(reordering.Drawer.Parent)));
								}
								else if(sourceParent.Members == null)
								{
									Debug.LogError("sourceParent.Members was null");
								}
							}

							#if DEV_MODE && DEBUG_DRAG_N_DROP_DETAILED
							Debug.Log("InsertAt("+to+ ", "+StringUtils.ToString(reorderingControl) +") for members: "+StringUtils.ToString(members));
							#endif

							SetMembers(members.InsertAt(to, reorderingControl));
						}

						//also update same target on any other open inspector instances
						var instances = inspector.Manager.ActiveInstances;
						for(int n = instances.Count - 1; n >= 0; n--)
						{
							var instance = instances[n];
							if(instance != inspector)
							{
								var drawers = instance.State.drawers;
								for(int i = drawers.Length - 1; i >= 0; i--)
								{
									var refresh = drawers[i] as GameObjectDrawer;
									if(refresh != this && refresh != null && refresh.targets.ContentsMatch(targets))
									{
										refresh.RebuildMemberBuildList();
										refresh.RebuildMembers();
									}
								}
							}
						}
					}
					else if(diff <= -1)
					{
						#if DEV_MODE && DEBUG_DRAG_N_DROP
						Debug.Log("Moving component " + draggedComponent.GetType().Name + " up " + (-diff)+ " steps from " + from + " to " + to + " under " + dropUnder.Component.GetType().Name + " ...");
						#endif

						members = members.Shift(from, to);

						MoveComponentsUnderComponents(draggedComponents, dropUnder.Components);
						UpdateVisibleMembers();
						inspector.ScrollToShow(draggedComponent);
					}
					#if DEV_MODE && DEBUG_DRAG_N_DROP
					else
					{
						Debug.Log("Drag n drop from "+from+" to "+to+ ": component order not changed...");
					}
					#endif
				}
				#if DEV_MODE && DEBUG_DRAG_N_DROP
				else if(from == 0)
				{
					Debug.LogError("Can't move Transform via drag n drop");
				}
				else
				{
					Debug.LogError("from: "+from+", to: "+to);
				}
				#endif
			}
		}

		/// <inheritdoc/>
		public void OnMemberReorderingEnded(IReorderable reordering) { }

		/// <inheritdoc/>
		public int GetDropTargetIndexAtPoint(Vector2 point)
		{
			int index = ReorderableParentDrawerUtility.GetDropTargetIndexAtPoint(this, point, true);
			if(index == 0 && Types.Transform.IsAssignableFrom(visibleMembers[0].Type))
			{
				return -1;
			}
			return index;
		}

		/// <inheritdoc/>
		public Component[] MoveComponentsUnderComponents([NotNull]Component[] componentsToMove, [NotNull]Component[] moveUnderneath)
		{
			#if !UNITY_EDITOR
			Debug.LogWarning("MoveComponentsUnderComponents not supported in builds.");
			return ArrayPool<Component>.ZeroSizeArray;
			#else
			bool sameGameObjects = true;
			int count = componentsToMove.Length;
			if(moveUnderneath.Length != count)
			{
				sameGameObjects = false;
			}
			else
			{
				for(int n = count - 1; n >= 0; n--)
				{
					if(componentsToMove[n].gameObject != moveUnderneath[n].gameObject)
					{
						sameGameObjects = false;
						break;
					}
				}
			}

			if(!sameGameObjects)
			{
				var list = (List<Component>)CopyComponentToGameObjectParams[3];
				list.Clear();

				for(int n = 0; n < count; n++)
				{
					var targetGameObject = moveUnderneath[n].gameObject;
					var componentToMove = componentsToMove[n];
					if(componentToMove.gameObject != targetGameObject)
					{
						CopyComponentToGameObjectParams[0] = componentToMove;
						CopyComponentToGameObjectParams[1] = targetGameObject;
						
						var copyMethod = typeof(UnityEditorInternal.ComponentUtility).GetMethod("CopyComponentToGameObject", BindingFlags.Static | BindingFlags.NonPublic, null, CopyComponentToGameObjectTypes, null);
						if(copyMethod == null)
						{
							Debug.LogError("MoveComponentsUnderComponents - method ComponentUtility.CopyComponentToGameObject not found!");
							return componentsToMove;
						}
						copyMethod.Invoke(null, CopyComponentToGameObjectParams);
						CopyComponentToGameObjectParams[0] = null;

						Platform.Active.Destroy(componentToMove);
					}
					else
					{
						list.Add(componentToMove);

						MoveComponentsRelativeToComponentsParams[0] = ArrayPool<Component>.CreateWithContent(componentToMove);
						MoveComponentsRelativeToComponentsParams[1] = ArrayPool<Component>.CreateWithContent(moveUnderneath[n]);

						var moveMethod = typeof(UnityEditorInternal.ComponentUtility).GetMethod("MoveComponentsRelativeToComponents", BindingFlags.Static | BindingFlags.NonPublic, null, MoveComponentsRelativeToComponentsTypes, null);
						if(moveMethod == null)
						{
							Debug.LogError("MoveComponentsUnderComponents - method ComponentUtility.MoveComponentsRelativeToComponents not found!");
							return componentsToMove;
						}
						moveMethod.Invoke(null, MoveComponentsRelativeToComponentsParams);

						var dispose = (Component[])MoveComponentsRelativeToComponentsParams[0];
						ArrayPool<Component>.Dispose(ref dispose);
						dispose = (Component[])MoveComponentsRelativeToComponentsParams[1];
						ArrayPool<Component>.Dispose(ref dispose);
						MoveComponentsRelativeToComponentsParams[0] = null;
						MoveComponentsRelativeToComponentsParams[1] = null;
					}
				}

				var newComponents = list.ToArray();
				list.Clear();
				return newComponents;
			}

			MoveComponentsRelativeToComponentsParams[0] = componentsToMove;
			MoveComponentsRelativeToComponentsParams[1] = moveUnderneath;
			var method = typeof(UnityEditorInternal.ComponentUtility).GetMethod("MoveComponentsRelativeToComponents", BindingFlags.Static | BindingFlags.NonPublic, null, MoveComponentsRelativeToComponentsTypes, null);
			if(method == null)
			{
				Debug.LogError("MoveComponentsUnderComponents - method ComponentUtility.MoveComponentsRelativeToComponents not found!");
				return componentsToMove;
			}
			method.Invoke(null, MoveComponentsRelativeToComponentsParams);
			MoveComponentsRelativeToComponentsParams[0] = null;
			MoveComponentsRelativeToComponentsParams[1] = null;

			return componentsToMove;
			#endif
		}

		#if DEV_MODE
		/// <inheritdoc/>
		public virtual void ValidateMembers()
		{
			int count = members.Length;
			for(int a = 0; a < count; a++)
			{
				var testA = members[a];

				Debug.Assert(!testA.Inactive, ToString() + " Member #" + a + " was inactive: " + members[a]);

				var valueA = testA.GetValue();
				for(int b = a + 1; b < count; b++)
				{
					var testB = members[b];

					Debug.Assert(testA != testB, ToString()+" Members #"+a+" and #"+b+" are the same: "+members[a]);
					
					var valueB = testB.GetValue();

					Debug.Assert(valueA != valueB || valueA == null, ToString() + " Members #" + a + " and #" + b + " have the same value: " + StringUtils.ToString(valueA));
				}
			}
		}
		#endif

		/// <inheritdoc/>
		public virtual bool MemberIsReorderable(IReorderable member)
		{
			#if  UNITY_EDITOR
			//Component reordering requires ComponentUtility which is Editor-only
			var comp = member as IComponentDrawer;
			if(comp != null)
			{
				var values = member.GetValues();
				if(values.Length > 0)
				{
					var obj = values[0] as Object;

					//allow null target to support reordering of MissingComponent
					return obj == null || SubjectIsReorderable(obj);
				}

				//allow null target to support reordering of MissingComponent
				return true;
			}
			#endif
			return false;
		}

		/// <inheritdoc/>
		public virtual bool SubjectIsReorderable(Object member)
		{
			#if  UNITY_EDITOR
			//Component reordering requires ComponentUtility which is Editor-only
			//and MonoScripts are editor-only too
			var type = member.GetType();
			
			//dragging Components on GameObject inspector can be used for reordering components
			//and moving Components between GameObjects
			if(type.IsComponent())
			{
				return type != Types.Transform && Type != Types.RectTransform;
			}

			//dragging MonoScripts on GameObject inspector can be used for adding components
			if(type == Types.MonoScript)
			{
				return true;
			}
			#endif
			
			return false;
		}

		/// <inheritdoc/>
		public void DeleteMember(IDrawer delete)
		{
			var focusedControl = InspectorUtility.ActiveManager.FocusedDrawer;
			var selectedIndexPath = focusedControl == null ? null : focusedControl.GenerateMemberIndexPath(this);
			int indexPathLastElement = selectedIndexPath == null ? -1 : selectedIndexPath.Length - 1;
			int selectMemberAtIndex = indexPathLastElement == - 1 ? - 1 : selectedIndexPath[indexPathLastElement];

			assumeVisibleMembersChangedDuringNextUpdateVisibleMembers = true;

			nowRemovingComponent = true;

			RemoveComponent((IComponentDrawer)delete);

			RebuildMemberBuildListAndMembers();

			if(selectedIndexPath != null)
			{
				if(indexPathLastElement != -1)
				{
					selectedIndexPath[indexPathLastElement] = selectMemberAtIndex;
				}
				SelectMemberAtIndexPath(selectedIndexPath, ReasonSelectionChanged.Dispose);
			}

			nowRemovingComponent = false;
		}

		/// <inheritdoc/>
		public void OnMemberReorderingStarted(IReorderable reordering) { }

		/// <inheritdoc cref="IDrawer.OnMouseoverDuringDrag" />
		public override void OnMouseoverDuringDrag(MouseDownInfo mouseDownInfo, Object[] dragAndDropObjectReferences) { }
		
		/// <inheritdoc/>
		public virtual void OnSubjectOverDropTarget(MouseDownInfo mouseDownInfo, Object[] draggedObjects)
		{
			var reordering = mouseDownInfo.Reordering;
			var dropTarget = reordering.MouseoveredDropTarget;
			int dropIndex = dropTarget.MemberIndex;
			
			#if DEV_MODE && DEBUG_DRAG
			Debug.Log("GameObject.OnReorderableOverDropTarget with dropIndex=" + StringUtils.ToColorizedString(dropIndex) + ", sourceIndex=" + reordering.MemberIndex + ", reordering.Control=" + StringUtils.ToString(reordering.Control)+ ", reordering.Parent="+ StringUtils.ToString(reordering.Parent)+", CursorPos="+Cursor.LocalPosition+"\ndraggedObjects="+StringUtils.ToString(draggedObjects));
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!draggedObjects.ContainsNullObjects());
			Debug.Assert(mouseDownInfo.MouseButtonIsDown, "GameObjectDrawer.OnSubjectOverDropTarget with !mouseDownInfo.MouseButtonIsDown");
			#endif

			if(dropIndex < 0)
			{
				return;
			}

			if(dropIndex >= visibleMembers.Length)
			{
				#if DEV_MODE
				Debug.LogError("OnSubjectOverDropTarget called with dropIndex "+dropIndex+" >= visibleMembers.Length");
				#endif
				mouseDownInfo.Clear();
				return;
			}

			int sourceIndex = reordering.MemberIndex;

			if(sourceIndex == -1)
			{
				if(draggedObjects.Length == 0)
				{
					return;
				}

				var firstDragged = draggedObjects[0];

				if(!(firstDragged is Component))
				{
					//allow dragging MonoScripts from the Project view
					if(!(firstDragged is MonoScript))
					{
						return;
					}
				}
				// allow dragging Components from other Inspectors
				else
				{
					// but don't allow dragging Transform components
					if(firstDragged is Transform)
					{
						return;
					}

					// don't allow dragging Components above themselves
					if(visibleMembers[dropIndex].UnityObject == firstDragged)
					{
						return;
					}

					// and don't allow dragging Components below themselves
					if(dropIndex > 0 && visibleMembers[dropIndex - 1].UnityObject == firstDragged)
					{
						return;
					}
				}
			}
			// if source and target GameObjects are the same...
			else if(reordering.Parent.UnityObjects.ContentsMatch(dropTarget.Parent.UnityObjects))
			{
				// don't allow dragging Components above or below themselves
				if(dropIndex == sourceIndex || dropIndex == sourceIndex + 1)
				{
					#if DEV_MODE
					Debug.LogWarning("OnSubjectOverDropTarget - Ignoring dragging above or below self...");
					#endif
					return;
				}
			}
			
			int count = visibleMembers.Length;
			Rect reorderDropRect;
			if(count == 0)
			{
				reorderDropRect = FirstReorderableDropTargetRect;
			}
			else if(dropIndex < count)
			{
				var memb = visibleMembers[dropIndex];
				reorderDropRect = memb.Bounds;
				reorderDropRect.height = DrawGUI.SingleLineHeight;
				reorderDropRect.y -= DrawGUI.SingleLineHeight * 0.5f;
			}
			else
			{
				var memb = visibleMembers[count - 1];
				reorderDropRect = memb.Bounds;
				reorderDropRect.height = DrawGUI.SingleLineHeight;
				reorderDropRect.y -= DrawGUI.SingleLineHeight * 0.5f;
			}

			reorderDropRect.y += Mathf.RoundToInt(reorderDropRect.height * 0.5f) - 1f;
			reorderDropRect.height = 3f;
			DrawGUI.Active.DragAndDropVisualMode = DragAndDropVisualMode.Move;
			GUI.DrawTexture(reorderDropRect, InspectorUtility.Preferences.graphics.ReorderDropTargetBg, ScaleMode.StretchToFill);
		}

		/// <inheritdoc/>
		public void OnMemberDrag(MouseDownInfo mouseDownInfo, Object[] draggedObjects) { }

		/// <inheritdoc/>
		public int VisibleComponentMemberCount()
		{
			int count = visibleMembers.Length;
			if(count > 0)
			{
				int lastIndex = count - 1;

				//if last member is not a component (probably the Add Component menu button), don't count it
				if(!(visibleMembers[lastIndex] is IComponentDrawer))
				{
					return lastIndex;
				}
			}
			return count;
		}

		/// <inheritdoc cref="IDrawer.OnKeyboardInputGiven" />
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			#if DEV_MODE && DEBUG_KEYBOARD_INPUT
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".OnKeyboardInputGiven( ", StringUtils.ToString(inputEvent), " with selectedPart=", selectedPart, ", KeyboardControl=", KeyboardControlUtility.KeyboardControl, ", EditingTextField=", DrawGUI.EditingTextField));
			#endif
			

			if(selectedPart == GameObjectHeaderPart.NameField && DrawGUI.EditingTextField)
			{
				if(keys.DetectTextFieldReservedInput(inputEvent, TextFieldType.TextRow))
				{
					#if DEV_MODE && DEBUG_KEYBOARD_INPUT
					Debug.Log("Detected text field reserved input!");
					#endif
					return false;
				}
			}

			switch(inputEvent.keyCode)
			{
				case KeyCode.F2:
					if(!ReadOnly)
					{
						if(selectedPart == GameObjectHeaderPart.NameField)
						{
							DrawGUI.EditingTextField = !DrawGUI.EditingTextField;
						}
						else
						{
							SelectSubControl(GameObjectHeaderPart.NameField, ReasonSelectionChanged.KeyPressShortcut);
							DrawGUI.EditingTextField = true;
						}
					}
					return true;
				case KeyCode.Return:
				case KeyCode.Space:
				case KeyCode.KeypadEnter:
					switch(selectedPart)
					{
						case GameObjectHeaderPart.None:
							if(!ReadOnly)
							{
								GUI.changed = true;
								DrawGUI.Use(inputEvent);

								var go = GameObject;
								bool setActive = !go.activeSelf;
								UndoHandler.RegisterUndoableAction(go, setActive ? "Set Active" : "Set Inactive");
								go.SetActive(setActive);
								UpdateIsEditable();

								if(OnValueChanged != null)
								{
									OnValueChanged(this, GameObject);
								}
							}
							return true;
						case GameObjectHeaderPart.SelectPrefab:
							GUI.changed = true;
							DrawGUI.Use(inputEvent);
							#if UNITY_2018_2_OR_NEWER
							var prefab = PrefabUtility.GetCorrespondingObjectFromSource(GameObject);
							#else
							var prefab = PrefabUtility.GetPrefabParent(GameObject);
							#endif
							DrawGUI.Ping(prefab);
							inspector.Select(prefab);
							return true;
						case GameObjectHeaderPart.NameField:
							bool set = !DrawGUI.EditingTextField;
							OnNextLayout(()=> OnNextLayout(()=> OnNextLayout(()=> DrawGUI.EditingTextField = set)));
							return true;
					}
					return false;
				case KeyCode.Backspace:
					if(ReadOnly || inputEvent.modifiers != EventModifiers.None)
					{
						return false;
					}
					
					switch(selectedPart)
					{
						case GameObjectHeaderPart.LayerField:
							var defaultLayer = 0;

							if(GameObject.layer != defaultLayer)
							{
								GUI.changed = true;
								DrawGUI.Use(inputEvent);

								Undo.RecordObject(GameObject, "Reset Layer");
							
								GameObject.layer = defaultLayer;
								if(OnValueChanged != null)
								{
									OnValueChanged(this, GameObject);
								}
								return true;
							}
							return false;
						case GameObjectHeaderPart.TagField:
							if(!GameObject.CompareTag("Untagged"))
							{
								GUI.changed = true;
								DrawGUI.Use(inputEvent);

								Undo.RecordObject(GameObject, "Reset Tag");

								GameObject.tag = "Untagged";
								if(OnValueChanged != null)
								{
									OnValueChanged(this, GameObject);
								}
								return true;
							}
							return false;
					}
					GUI.changed = true;
					DrawGUI.Use(inputEvent);
					return false;
			}
			
			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc/>
		protected override void SelectNextFieldLeft(bool moveToNextControlAfterReachingEnd, bool additive = false)
		{
			switch(selectedPart)
			{
				case GameObjectHeaderPart.None:
					Select(ReasonSelectionChanged.SelectPrevControl);
					SelectSubControl(LastHeaderPart(), ReasonSelectionChanged.SelectPrevControl);
					return;
				case GameObjectHeaderPart.TagField:
					if(moveToNextControlAfterReachingEnd)
					{
						SelectSubControl(GameObjectHeaderPart.DropDownArrow, ReasonSelectionChanged.SelectPrevControl);
					}
					else
					{
						SelectSubControl(GameObjectHeaderPart.TagField, ReasonSelectionChanged.SelectControlLeft);
					}
					return;
				case GameObjectHeaderPart.OpenPrefab:
					if(moveToNextControlAfterReachingEnd)
					{
						SelectSubControl(GameObjectHeaderPart.LayerField, ReasonSelectionChanged.SelectPrevControl);
					}
					return;
				case GameObjectHeaderPart.Base:
					if(moveToNextControlAfterReachingEnd)
					{
						var result = GetNextSelectableDrawerLeft(true, this);
						if(result == this || result == null)
						{
							if(Inspector.Toolbar != null)
							{
								Manager.Select(inspector, InspectorPart.Toolbar, ReasonSelectionChanged.SelectPrevControl);
								return;
							}
						}
						Select(ReasonSelectionChanged.SelectPrevControl);
						return;
					}
					SelectSubControl(GameObjectHeaderPart.Base, ReasonSelectionChanged.SelectControlLeft);
					return;
				default:
					SelectSubControl((GameObjectHeaderPart)selectedPart.PreviousEnumValue(), ReasonSelectionChanged.SelectPrevControl);
					return;
			}
		}

		/// <inheritdoc/>
		protected override void SelectNextFieldRight(bool moveToNextControlAfterReachingEnd, bool additive = false)
		{
			switch(selectedPart)
			{
				case GameObjectHeaderPart.DropDownArrow:
					if(moveToNextControlAfterReachingEnd)
					{
						SelectSubControl(GameObjectHeaderPart.TagField, ReasonSelectionChanged.SelectNextControl);
					}
					else
					{
						SelectSubControl(GameObjectHeaderPart.DropDownArrow, ReasonSelectionChanged.SelectControlRight);
					}
					return;
				case GameObjectHeaderPart.LayerField:
					if(moveToNextControlAfterReachingEnd)
					{
						if(isPrefabInstance)
						{
							SelectSubControl(GameObjectHeaderPart.OpenPrefab, ReasonSelectionChanged.SelectNextControl);
							return;
						}
						base.SelectNextFieldRight(true, additive);
					}
					else
					{
						SelectSubControl(GameObjectHeaderPart.LayerField, ReasonSelectionChanged.SelectControlRight);
					}
					return;
				case GameObjectHeaderPart.PrefabOverrides:
					if(moveToNextControlAfterReachingEnd)
					{
						base.SelectNextFieldRight(true, additive);
						return;
					}
					else
					{
						SelectSubControl(selectedPart, ReasonSelectionChanged.SelectControlRight);
					}
					return;
				default:
					SelectSubControl((GameObjectHeaderPart)selectedPart.NextEnumValue(), ReasonSelectionChanged.SelectNextControl);
					return;
			}
		}

		/// <inheritdoc />
		protected override void SelectNextFieldUp(int column, bool additive = false)
		{
			switch(selectedPart)
			{
				case GameObjectHeaderPart.OpenPrefab:
				case GameObjectHeaderPart.SelectPrefab:
					SelectSubControl(GameObjectHeaderPart.TagField, ReasonSelectionChanged.SelectControlUp);
					return;
				case GameObjectHeaderPart.PrefabOverrides:
					SelectSubControl(GameObjectHeaderPart.LayerField, ReasonSelectionChanged.SelectControlUp);
					return;
				case GameObjectHeaderPart.TagField:
					SelectSubControl(GameObjectHeaderPart.ActiveFlag, ReasonSelectionChanged.SelectControlUp);
					return;
				case GameObjectHeaderPart.LayerField:
					SelectSubControl(GameObjectHeaderPart.StaticFlag, ReasonSelectionChanged.SelectControlUp);
					return;
				default:
					var select = GetNextSelectableDrawerUp(column, this);
					if(select == this || select == null)
					{
						if(Inspector.Toolbar != null)
						{
							Manager.Select(inspector, InspectorPart.Toolbar, ReasonSelectionChanged.SelectControlUp);
							return;
						}
					}
					OnNextLayout(()=>inspector.Select(select, ReasonSelectionChanged.SelectControlUp));
					return;
			}
		}

		/// <inheritdoc />
		protected override void SelectNextFieldDown(int column, bool additive = false)
		{
			if(isPrefabInstance)
			{
				switch(selectedPart)
				{
					case GameObjectHeaderPart.OpenPrefab:
					case GameObjectHeaderPart.PrefabOverrides:
					case GameObjectHeaderPart.SelectPrefab:
						base.SelectNextFieldDown(column, additive);
						return;
					case GameObjectHeaderPart.TagField:
						SelectSubControl(GameObjectHeaderPart.OpenPrefab, ReasonSelectionChanged.SelectControlDown);
						return;
					case GameObjectHeaderPart.LayerField:
						SelectSubControl(GameObjectHeaderPart.PrefabOverrides, ReasonSelectionChanged.SelectControlDown);
						return;
				}
			}

			switch(selectedPart)
			{
				case GameObjectHeaderPart.ActiveFlag:
				case GameObjectHeaderPart.NameField:
					SelectSubControl(GameObjectHeaderPart.TagField, ReasonSelectionChanged.SelectControlDown);
					return;
				case GameObjectHeaderPart.StaticFlag:
				case GameObjectHeaderPart.DropDownArrow:
					SelectSubControl(GameObjectHeaderPart.LayerField, ReasonSelectionChanged.SelectControlDown);
					return;
				default:
					base.SelectNextFieldDown(column, additive);
					return;
			}
		}

		/// <inheritdoc cref="IDrawer.Duplicate" />
		public override void Duplicate()
		{
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var source = targets[n];
				var clone = Object.Instantiate(source);
				clone.transform.parent = source.transform.parent;
			}
		}

		/// <summary>
		/// Select given part of the header.
		/// </summary>
		/// <param name="select"> The header part to select. </param>
		/// <param name="reason"> Reason why header part is being selected. </param>
		private void SelectSubControl(GameObjectHeaderPart select, ReasonSelectionChanged reason)
		{
			#if DEV_MODE && DEBUG_SET_SELECTED_PART
			Debug.Log(StringUtils.ToColorizedString("SelectSubControl(", StringUtils.ToColorizedString(select), ") with Event=", StringUtils.ToString(Event.current), ", EditingTextField=", DrawGUI.EditingTextField, ", beforeHeaderControlID=", beforeHeaderControlID));
			#endif

			selectedPart = select;
			OnNextLayout(()=>SelectSubControlStep(select, reason));
		}

		/// <summary>
		/// Handles effects of the part of the header being selected, such as setting KeyboardControl to right value.
		/// </summary>
		/// <param name="select">
		/// The select. </param>
		/// <param name="reason"> Reason why header part is being selected. </param>
		/// <param name="repeatTimes">
		/// (Optional) List of times of the repeats. </param>
		private void SelectSubControlStep(GameObjectHeaderPart select, ReasonSelectionChanged reason, int repeatTimes = 3)
		{
			if(repeatTimes > 0)
			{
				OnNextLayout(()=>SelectSubControlStep(select, reason, repeatTimes - 1));
			}
			
			GUI.changed = true;
			int baseID = beforeHeaderControlID + 3;
			
			switch(select)
			{
				//these controls aren't selectable in Unity by default
				//so we must handle them manually
				case GameObjectHeaderPart.None:
				case GameObjectHeaderPart.OpenPrefab:
				case GameObjectHeaderPart.PrefabOverrides:
				case GameObjectHeaderPart.SelectPrefab:
				case GameObjectHeaderPart.Base: //ADDING THIS CAUSED ISSES WHEN NAME FIELD WAS CLICKED!
					if(KeyboardControlUtility.JustClickedControl == 0)
					{
						KeyboardControlUtility.KeyboardControl = 0;
					}
					return;
				case GameObjectHeaderPart.ActiveFlag:
					if(reason != ReasonSelectionChanged.ControlClicked && reason != ReasonSelectionChanged.SelectPrevControl)
					{
						KeyboardControlUtility.SetKeyboardControl(baseID + 1, 3);
					}
					return;
				case GameObjectHeaderPart.NameField:
					if(reason != ReasonSelectionChanged.ControlClicked)
					{
						KeyboardControlUtility.SetKeyboardControl(baseID + 2, 3);
						//KeyboardControlUtility.KeyboardControl = baseID + 2;
						//TextFieldUtility.SyncEditingTextField(); //new test
						DrawGUI.EditingTextField = false; //new new test
					}
					else
					{
						DrawGUI.EditingTextField = true;
					}
					return;
				case GameObjectHeaderPart.StaticFlag:
					if(reason != ReasonSelectionChanged.ControlClicked && reason != ReasonSelectionChanged.SelectNextControl && reason != ReasonSelectionChanged.SelectPrevControl)
					{
						KeyboardControlUtility.SetKeyboardControl(baseID + 3, 3);
					}
					return;
				case GameObjectHeaderPart.DropDownArrow:
					if(reason != ReasonSelectionChanged.ControlClicked)
					{
						KeyboardControlUtility.SetKeyboardControl(baseID + 4, 3);
					}
					return;
				case GameObjectHeaderPart.TagField:
					if(reason != ReasonSelectionChanged.ControlClicked)
					{
						KeyboardControlUtility.SetKeyboardControl(baseID + 5, 3);
					}
					return;
				case GameObjectHeaderPart.LayerField:
					if(reason != ReasonSelectionChanged.ControlClicked)
					{
						KeyboardControlUtility.SetKeyboardControl(baseID + 6, 3);
					}
					return;
				default:
					#if DEV_MODE
					Debug.LogError("Unknown GameObjectHeaderPart: "+select.ToString()+". ReasonSelectionChanged="+reason);
					#endif
					return;
			}
		}
		
		/// <inheritdoc cref="IDrawer.OnClick" />
		public override bool OnClick(Event inputEvent)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(mouseoveredPart != GameObjectHeaderPart.None);
			#endif
			
			#if DEV_MODE && DEBUG_ON_CLICK
			Debug.Log(ToString() + ".OnClick with mouseoveredPart="+ mouseoveredPart+", KeyboardControl = " + KeyboardControlUtility.KeyboardControl + ", beforeHeaderControlID="+ beforeHeaderControlID+", offset="+(KeyboardControlUtility.KeyboardControl - beforeHeaderControlID));
			#endif

			SelectSubControl(mouseoveredPart, ReasonSelectionChanged.ControlClicked);

			// NOTE: This breaks the Open Prefab button, so disabled for prefabs
			if(mouseoveredPart == GameObjectHeaderPart.Base && !IsPrefab)
			{
				DrawGUI.Active.DragAndDropObjectReferences = targets;
			}

			return base.OnClick(inputEvent);
		}

		/// <inheritdoc/>
		protected override void OnSelectedInternal(ReasonSelectionChanged reason, IDrawer previous, bool isMultiSelection)
		{
			#if DEV_MODE && DEBUG_SELECT
			Debug.Log(StringUtils.ToColorizedString("OnSelectedInternal(", reason, ").KeyboardControl=", KeyboardControlUtility.KeyboardControl, ", beforeHeaderControlID=", beforeHeaderControlID, ", offset=", (KeyboardControlUtility.KeyboardControl - beforeHeaderControlID)));
			#endif

			// needed for selection rect to be drawn in the right place
			inspector.OnNextLayout(inspector.RefreshView);

			switch(reason)
			{
				case ReasonSelectionChanged.PrefixClicked:
					break;
				case ReasonSelectionChanged.SelectPrevControl:
					SelectSubControl(LastHeaderPart(), reason);
					break;
			}

			base.OnSelectedInternal(reason, previous, isMultiSelection);
		}

		private GameObjectHeaderPart LastHeaderPart()
		{
			return isPrefabInstance ? GameObjectHeaderPart.PrefabOverrides : GameObjectHeaderPart.LayerField;
		}
		
		/// <inheritdoc/>
		protected override void OnDeselectedInternal(ReasonSelectionChanged reason, IDrawer previous)
		{
			selectedPart = GameObjectHeaderPart.None;
		}

		/// <inheritdoc/>
		public void AddItemsToOpeningViewMenu(ref Menu menu)
		{
			#if DEV_MODE
			Debug.Log(ToString() + ".GameObject.AddItemsToOpeningViewMenu");
			#endif

			if(menu.Contains("Component Unfolding/Unrestricted"))
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+".AddItemsToOpeningViewMenu aborting because menu already contained item \"Component Unfolding/Unrestricted\". This is normal in stacked multi-edit mode.");
				#endif
				return;
			}

			var preferences = inspector.Preferences;

			menu.AddSeparatorIfNotRedundant();
			
			menu.Add("Component Unfolding/Unrestricted", "Any number of components can be shown expanded simultaneously.", DisableEditComponentsOneAtATime, !UserSettings.EditComponentsOneAtATime);
			menu.Add("Component Unfolding/One At A Time", "Only one component at a time can be shown expanded.", EnableEditComponentsOneAtATime, UserSettings.EditComponentsOneAtATime);

			menu.Add("Hidden Components/Show", "Show Components that have been hidden using HideFlags though code.", EnableShowHiddenComponents, preferences.ShowHiddenComponents);
			menu.Add("Hidden Components/Hide", "Hide Components that have been hidden using HideFlags though code.", DisableShowHiddenComponents, !preferences.ShowHiddenComponents);

			menu.Add("Categorized Components/Off", DisableCategorizedComponents, !preferences.EnableCategorizedComponents);
			menu.Add("Categorized Components/On", EnableCategorizedComponents, preferences.EnableCategorizedComponents);

			if(isPrefab)
			{
				menu.AddSeparator();
				menu.Add("Prefab Quick Editing/Off", SetPrefabQuickEditingOff, preferences.prefabQuickEditing == PrefabQuickEditingSettings.Off);
				menu.Add("Prefab Quick Editing/View Only", SetPrefabQuickEditingToViewOnly, preferences.prefabQuickEditing == PrefabQuickEditingSettings.ViewOnly);
				menu.Add("Prefab Quick Editing/Enabled", SetPrefabQuickEditingToEnabled, preferences.prefabQuickEditing == PrefabQuickEditingSettings.Enabled);
			}

			bool addScriptFieldItem = false;

			menu.Add("Help/Drawer/Game Object", PowerInspectorDocumentation.ShowDrawerInfo, "gameobject-gui-drawers");
			menu.Add("Help/Features/Create Script Wizard", PowerInspectorDocumentation.ShowFeature, "create-script-wizard");
			

			for(int n = 0, count = LastCollectionMemberIndex; n < count; n++)
			{
				var member = members[n] as IComponentDrawer;
				if(member != null)
				{
					if(!addScriptFieldItem && member.GetValue() is MonoBehaviour)
					{
						addScriptFieldItem = true;
					}
				
					member.AddItemsToOpeningViewMenu(ref menu);
				}
			}

			ViewMenuUtility.AddFieldVisibilityItems(ref menu, inspector, addScriptFieldItem);
			ViewMenuUtility.AddPreviewAreaItems(ref menu);
		}

		private void EnableCategorizedComponents()
		{
			inspector.Preferences.EnableCategorizedComponents = true;
		}

		private void DisableCategorizedComponents()
		{
			inspector.Preferences.EnableCategorizedComponents = false;
		}

		private void SetPrefabQuickEditingOff()
		{
			SetPrefabQuickEditing(PrefabQuickEditingSettings.Off);
		}

		private void SetPrefabQuickEditingToViewOnly()
		{
			SetPrefabQuickEditing(PrefabQuickEditingSettings.ViewOnly);
		}

		private void SetPrefabQuickEditingToEnabled()
		{
			SetPrefabQuickEditing(PrefabQuickEditingSettings.Enabled);
		}

		private void SetPrefabQuickEditing(PrefabQuickEditingSettings setQuickEditing)
		{
			inspector.Preferences.prefabQuickEditing = setQuickEditing;
			UpdateIsEditable();
			GUI.changed = true;
		}

		private void EnableEditComponentsOneAtATime()
		{
			if(!UserSettings.EditComponentsOneAtATime)
			{
				UserSettings.EditComponentsOneAtATime = true;
				// rebuild drawers instead of calling FoldAllComponents
				// so that Components get collapsed without recording the changes
				inspector.ForceRebuildDrawers();
			}
		}

		private void DisableEditComponentsOneAtATime()
		{
			if(UserSettings.EditComponentsOneAtATime)
			{
				UserSettings.EditComponentsOneAtATime = false;
				// rebuild drawers instead of calling UnfoldAllComponents
				// so that Components get unfolded without recording the changes
				inspector.ForceRebuildDrawers();
			}
		}

		private void EnableShowHiddenComponents()
		{
			if(!Inspector.Preferences.ShowHiddenComponents)
			{
				Inspector.Preferences.ShowHiddenComponents = true;
				//rebuild drawers in case there are hidden Components on current GameObject
				inspector.ForceRebuildDrawers();
			}
		}

		private void DisableShowHiddenComponents()
		{
			if(Inspector.Preferences.ShowHiddenComponents)
			{
				Inspector.Preferences.ShowHiddenComponents = false;
				//rebuild drawers in case there are hidden Components on current GameObject
				inspector.ForceRebuildDrawers();
			}
		}


		#if ENABLE_SPREADING
		/// <inheritdoc cref="IDrawer.UpdateCachedValuesFromFieldsRecursively" />
		public override void UpdateCachedValuesFromFieldsRecursively()
		{
			lastUpdateCachedValuesMemberIndex++;

			int count = updateCachedValuesFor.Count;
			if(lastUpdateCachedValuesMemberIndex >= count)
			{
				//how did this happen?
				if(count == 0)
				{
					#if DEV_MODE
					Debug.LogError("UpdateCachedValuesFromFieldsRecursively was called for "+ToString()+ " but updateCachedValuesFor.Count was zero");
					#endif
					return;
				}
				lastUpdateCachedValuesMemberIndex = 0;
			}
			
			var updateMember = updateCachedValuesFor[lastUpdateCachedValuesMemberIndex];
			if(updateMember == null)
			{
				#if DEV_MODE
				Debug.LogWarning("UpdateCachedValuesFromFieldsRecursively() NullReferenceException " + ToString()+" @ updateCachedValuesFor[" + lastUpdateCachedValuesMemberIndex + "]");
				#endif
				UpdateCachedValuesForList();
				return;
			}

			try
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(updateMember.GetType() != typeof(MissingScriptDrawer));
				#endif
				updateMember.UpdateCachedValuesFromFieldsRecursively();
			}
			catch(NullReferenceException)
			{
				#if DEV_MODE
				Debug.LogWarning("UpdateCachedValuesFromFieldsRecursively() NullReferenceException " + ToString()+" @ updateCachedValuesFor[" + lastUpdateCachedValuesMemberIndex + "]");
				#endif
			}
			catch(ArgumentOutOfRangeException)
			{
				#if DEV_MODE
				Debug.LogWarning("UpdateCachedValuesFromFieldsRecursively() ArgumentOutOfRangeException " + ToString()+ " @ updateCachedValuesFor[" + lastUpdateCachedValuesMemberIndex + "]");
				#endif
			}
		}
		#endif

		/// <inheritdoc/>
		public virtual IEnumerator<IComponentDrawer> ForEachComponent()
		{
			int lastIndex = members.Length - LastCollectionMemberCountOffset;

			#if DEV_MODE
			Debug.Log("ForEachComponent: lastIndex="+ lastIndex+", LastCollectionMemberCountOffset="+LastCollectionMemberCountOffset+ ", members="+StringUtils.ToString(members));
			#endif

			for(int n = 0; n <= lastIndex; n++)
			{
				var componentDrawer = members[n] as IComponentDrawer;
				if(componentDrawer != null)
				{
					yield return (IComponentDrawer)members[n];
				}
			}
		}

		/// <summary>
		/// Select previous target with same tag as this target.
		/// </summary>
		private void SelectPreviousWithTag()
		{
			var target = targets[0];
			var all = GameObject.FindGameObjectsWithTag(target.tag);
			Array.Sort(all, SortGameObjectsByHierarchyOrder.Instance);
			int count = all.Length;
			if(count > 0)
			{
				for(int n = count - 1; n >= 0; n--)
				{
					if(all[n] == target)
					{
						if(n > 0)
						{
							inspector.SelectAndShow(all[n-1], ReasonSelectionChanged.SelectPrevOfType);
							return;
						}
						inspector.SelectAndShow(all[count-1], ReasonSelectionChanged.SelectPrevOfType);
						return;
					}
				}
				inspector.SelectAndShow(all[0], ReasonSelectionChanged.SelectPrevOfType);
			}
		}

		/// <summary>
		/// Select next target with same tag as this target.
		/// </summary>
		private void SelectNextWithTag()
		{
			var target = targets[0];
			var all = GameObject.FindGameObjectsWithTag(target.tag);
			Array.Sort(all, SortGameObjectsByHierarchyOrder.Instance);
			int lastIndex = all.Length - 1;
			if(lastIndex > -1)
			{
				for(int n = lastIndex; n >= 0; n--)
				{
					if(all[n] == target)
					{
						if(n < lastIndex)
						{
							inspector.SelectAndShow(all[n+1], ReasonSelectionChanged.SelectNextOfType);
							return;
						}
						inspector.SelectAndShow(all[0], ReasonSelectionChanged.SelectNextOfType);
						return;
					}
				}
				inspector.SelectAndShow(all[0], ReasonSelectionChanged.SelectNextOfType);
			}
		}

		/// <inheritdoc cref="IDrawer.SelectPreviousComponent" />
		public override void SelectPreviousComponent()
		{
			var gameObject = GameObject;
			var transform = gameObject.transform;
			var previousTransform = gameObject.transform.PreviousVisibleInInspector(false);

			// select last visible Component inp previous GameObject (if any)
			var components = previousTransform.GetComponents<Component>();
			for(int n = components.Length - 1; n >= 0; n--)
			{
				var selectComponent = components[n];

				// simply skipping missing components at least for now
				if(selectComponent == null)
				{
					continue;
				}

				// skip hidden components, unless they are shown in the inspector
				if(!selectComponent.hideFlags.HasFlag(HideFlags.HideInInspector) || inspector.State.DebugMode || inspector.Preferences.ShowHiddenComponents)
				{
					#if DEV_MODE
					Debug.Log("Selecting last component "+selectComponent.GetType().Name +" on previous GameObject " + previousTransform.name+ ". It has hideFlags="+ selectComponent.hideFlags, selectComponent);
					#endif
					inspector.SelectAndShow(selectComponent, ReasonSelectionChanged.SelectPrevComponent);
					return;
				}
				#if DEV_MODE
				Debug.Log("SelectPreviousComponent skipping component "+components[n].GetType().Name+" because it's hidden");
				#endif
			}

			// if previous GameObject had no visible Components, then select GameObject itself
			inspector.SelectAndShow(previousTransform.gameObject, ReasonSelectionChanged.SelectPrevComponent);
		}
		
		/// <inheritdoc cref="IDrawer.SelectNextComponent" />
		public override void SelectNextComponent()
		{
			if(visibleMembers.Length > 0 && visibleMembers[0].GetType() != typeof(AddComponentButtonDrawer))
			{
				visibleMembers[0].Select(ReasonSelectionChanged.SelectNextComponent);
				return;
			}

			var go = GameObject;
			var nextTransform = go.transform.NextVisibleInInspector(false);
			var selectGameObject = nextTransform.gameObject;
			if(selectGameObject != go)
			{
				KeyboardControlUtility.KeyboardControl = 0;
				inspector.SelectAndShow(selectGameObject, ReasonSelectionChanged.SelectNextComponent);
			}
		}

		/// <inheritdoc cref="IDrawer.SelectPreviousOfType" />
		public override void SelectPreviousOfType()
		{
			var go = GameObject;
			var previousTransform = go.transform.PreviousVisibleInInspector(false);
			var selectGameObject = previousTransform.gameObject;
			if(selectGameObject != go)
			{
				KeyboardControlUtility.KeyboardControl = 0;
				inspector.SelectAndShow(selectGameObject, ReasonSelectionChanged.SelectPrevComponent);
			}
		}

		/// <inheritdoc cref="IDrawer.SelectNextOfType" />
		public override void SelectNextOfType()
		{
			KeyboardControlUtility.KeyboardControl = 0;

			var go = GameObject;
			var nextTransform = go.transform.NextVisibleInInspector(false);
			var selectGameObject = nextTransform.gameObject;
			if(selectGameObject != go)
			{
				KeyboardControlUtility.KeyboardControl = 0;
				inspector.SelectAndShow(selectGameObject, ReasonSelectionChanged.SelectNextComponent);
			}
		}

		/// <inheritdoc cref="IParentDrawer.SetUnfolded(bool, bool)" />
		public override void SetUnfolded(bool setUnfolded, bool setChildrenAlso)
		{
			if(!setChildrenAlso)
			{
				return;
			}

			for(int n = members.Length - 1; n >= 0; n--)
			{
				var memb = members[n];
				if(memb != null)
				{
					memb.ApplyInChildren((member) =>
					{
						var memberParent = member as IParentDrawer;
						if(memberParent != null)
						{
							memberParent.SetUnfolded(setUnfolded);
						}
					});
				}
			}
		}

		/// <inheritdoc cref="IDrawer.AddPreviewWrappers" />
		public override void AddPreviewWrappers(ref List<IPreviewableWrapper> previews)
		{
			if(previewEditor != null)
			{
				Previews.GetPreviews(previewEditor, targets, ref previews);
			}
			
			for(int n = 0; n <= LastVisibleCollectionMemberIndex; n++)
			{
				var member = visibleMembers[n];
				member.AddPreviewWrappers(ref previews);
			}
		}
		
		/// <inheritdoc cref="IDrawer.Dispose" />
		public override void Dispose()
		{
			forceHideAddComponentMenuButton = false;

			if(debugModeAdditionalHeight > 0f)
			{
				debugModeAdditionalHeight = 0f;
				DrawerArrayPool.Dispose(ref debugModeInternalDrawers, true);
				debugModeInternalDrawers = ArrayPool<IDrawer>.ZeroSizeArray;
			}

			componentsOnlyOnSomeObjectsFound = false;

			nowRemovingComponent = false;

			selectedPart = GameObjectHeaderPart.None;

			if(assetLabels != null)
			{
				GUIContentArrayPool.Dispose(ref assetLabels);
			}

			Sisus.AssetLabels.OnAssetLabelsChanged -= OnAssetLabelsChanged;

			if(!ReferenceEquals(previewEditor, null))
			{
				Editors.Dispose(ref previewEditor);
			}

			headerDrawer.ResetState();

			base.Dispose();

			inspector = null;
		}

		/// <inheritdoc/>
		public virtual void AddComponentMember(int memberIndex, IComponentDrawer componentDrawer)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			int assertCount = members.Length + 1;
			#endif

			var setMembers = members;
						
			DrawerArrayPool.InsertAt(ref setMembers, memberIndex, componentDrawer, false);
			SetMembers(setMembers);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(members.Length == assertCount);
			Debug.Assert(setMembers.Length == assertCount);
			#endif
		}

		/// <summary>
		/// Updates the label text.
		/// </summary>
		private void UpdateLabelText()
		{
			string setName = targets[0].name;
			for(int n = targets.Length - 1; n >= 1; n--)
			{
				if(!string.Equals(setName, targets[n].name))
				{
					setName = "-";
					break;
				}
			}
			label.text = setName;
		}

		public IEnumerator<IComponentDrawer> GetEnumerator()
		{
			return ForEachComponent();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ForEachComponent();
		}

		/// <inheritdoc/>
		[NotNull]
		public IComponentDrawer[] AddComponents(List<Type> types, bool scrollToShow = true)
		{
			bool viewWasLocked;

			HandleAutoName(types[0]);

			StartAddingComponents(out viewWasLocked);

			int count = types.Count;
			var results = ArrayPool<IComponentDrawer>.Create(count);
			bool createdShownInInspector = false;
			for(int n = 0; n < count; n++)
			{
				var type = types[n];

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(type != null);
				Debug.Assert(type.IsComponent());
				#endif

				var created = DoAddComponent(type, scrollToShow, -1);
				if(created == null)
				{
					#if DEV_MODE
					Debug.LogWarning("AddComponentInternal returned null for type: " + StringUtils.ToString(type));
					#endif
					results = results.RemoveAt(n);
					continue;
				}
				if(created.ShouldShowInInspector)
				{
					createdShownInInspector = true;
				}
				results[n] = created;
			}
			
			FinishAddingComponents(createdShownInInspector, viewWasLocked);

			for(int n = results.Length - 1; n >= 0; n--)
			{
				var addedDrawer = results[n];
				if(!addedDrawer.Inactive)
				{
					var addedComponents = addedDrawer.Components;
					for(int c = addedComponents.Length - 1; c >= 0; c--)
					{
						ComponentModifiedCallbackUtility.OnComponentAdded(addedComponents[c]);
					}
				}
			}

			// Drawer selected for GameObject targets can be affected by components found on said GameObjects.
			// If drawer for this gameobject changed as a result of components being added, rebuild instructions
			var newDrawerType = DrawerProvider.GetDrawerTypeForGameObjects(targets);
			if(newDrawerType != GetType())
			{
				// delay rebuilding by a frame, so that can first return created IComponentDrawer during this frame.
				inspector.OnNextLayout(inspector.ForceRebuildDrawers);
			}

			return results;
		}

		/// <inheritdoc/>
		public IComponentDrawer AddComponent([NotNull]Type type, bool scrollToShow, int index = -1)
		{
			#if DEV_MODE
			Debug.Log("AddComponent("+type.Name+")");
			#endif

			bool includedAddComponentButton = ShouldIncludeAddComponentButton();

			HandleAutoName(type);

			HashSet<AnyType> dependencies = null;
			if(CollectDependenciesForAddComponent(type, ref dependencies))
			{
				int dependencyCount = dependencies.Count;
				int hasRequiredCount = 0;
				foreach(var requiresOneOfComponents in dependencies)
				{
					bool hasOneOfRequiredComponents = false;
					foreach(var requiredComponent in requiresOneOfComponents.types)
					{
						foreach(var componentDrawer in this)
						{
							if(requiredComponent.IsAssignableFrom(componentDrawer.Type))
							{
								hasRequiredCount++;
								hasOneOfRequiredComponents = true;
								break;
							}
						}
						if(hasOneOfRequiredComponents)
						{
							break;
						}
					}

					if(!hasOneOfRequiredComponents)
					{
						if(requiresOneOfComponents.Count == 1)
						{
							hasRequiredCount++;
							AddComponent(requiresOneOfComponents[0], false, index);
						}
						else
						{
							var menu = Menu.Create();
							foreach(var requiredComponent in requiresOneOfComponents.types)
							{
								var addComponent = requiredComponent;
								menu.Add(addComponent.Name, ()=>
								{
									var addedDependent = AddComponent(addComponent, false, -1);

									if(addedDependent != null)
									{
										hasRequiredCount++;
										if(hasRequiredCount == dependencyCount)
										{
											AddComponent(type, false, index);
										}
									}
								});
							}
							menu.OpenAt(AddComponentButton.Bounds);
						}
					}
				}

				if(hasRequiredCount < dependencyCount)
				{
					return null;
				}
			}		

			bool viewWasLocked;
			StartAddingComponents(out viewWasLocked);
			var created = DoAddComponent(type, scrollToShow, index);
			if(created == null)
			{
				#if DEV_MODE
				Debug.LogWarning("AddComponentInternal returned null for type: " + StringUtils.ToString(type));
				#endif
				return null;
			}

			FinishAddingComponents(created.ShouldShowInInspector, viewWasLocked);

			var addedComponents = created.Components;
			for(int c = addedComponents.Length - 1; c >= 0; c--)
			{
				ComponentModifiedCallbackUtility.OnComponentAdded(addedComponents[c]);
			}

			// Drawer selected for GameObject targets can be affected by components found on said GameObjects.
			// If drawer for this gameobject changed as a result of components being added, rebuild instructions
			if(DrawerProvider.GetDrawerTypeForGameObjects(targets) != GetType())
			{
				// delay rebuilding by a frame, so that can first return created IComponentDrawer during this frame.
				inspector.OnNextLayout(inspector.ForceRebuildDrawers);
			}
			// when a component with the OnlyComponent attribute is added to a GameObject, need to remove the add component button
			else if(includedAddComponentButton != ShouldIncludeAddComponentButton())
			{
				// delay rebuilding by a frame, so that can first return created IComponentDrawer during this frame.
				inspector.OnNextLayout(RebuildMembers);
			}

			return created;
		}

		private void HandleAutoName(Type type)
		{
			if(inspector.Preferences.autoNameByAddedComponentIfHasDefaultName)
			{
				// HierarchyFolder has its own auto-naming logic
				if(!string.Equals(type.Name, "HierarchyFolder", StringComparison.OrdinalIgnoreCase))
				{
					return;
				}

				for(int t = targets.Length - 1; t >= 0; t--)
				{
					var gameObject = targets[t];
					string name = gameObject.name;
					int length = name.Length;
					if(length <= 15 && name.StartsWith("GameObject", StringComparison.Ordinal) && (length == 10 || name.Substring(10).StartsWith(" (", StringComparison.Ordinal)))
					{
						gameObject.name = StringUtils.SplitPascalCaseToWords(type.Name);
					}
				}
			}
		}

		private void StartAddingComponents(out bool viewWasLocked)
		{
			var state = inspector.State;

			//lock the state so that nothing else (like OnHierarchyChanged events)
			//will alter the drawers while we do our thing
			viewWasLocked = state.ViewIsLocked;
			state.ViewIsLocked = true;
		}

		private void FinishAddingComponents(bool createdShownInInspector, bool viewWasLocked)
		{
			var state = inspector.State;

			state.ViewIsLocked = viewWasLocked;

			// Rebuild member build list because components have changed, in case RebuildMembers gets called at some point.
			RebuildMemberBuildList();

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberBuildState == MemberBuildState.MembersBuilt);
			#endif

			#if DEV_MODE && DEBUG_FINISH_ADDING_COMPONENTS
			Debug.Log("FinishAddingComponents - Members now: " + StringUtils.ToString(members));
			#endif
			
			if(createdShownInInspector)
			{
				UpdateVisibleMembers();
				inspector.RefreshView();
			}

			#if DEV_MODE && DEBUG_FINISH_ADDING_COMPONENTS
			Debug.Log("FinishAddingComponents - VisibleMembers now: " + StringUtils.ToString(visibleMembers));
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			ValidateMembers();
			#endif

			if(OnValueChanged != null)
			{
				OnValueChanged(this, GameObject);
			}
		}

		/// <summary>
		/// Add component of type in targets of GameObjectDrawer at given index.
		/// </summary>
		/// <param name="gameObjectDrawer"> GameObjectDrawer that should contain the component. </param>
		/// <param name="type"> Type of component to add. </param>
		/// <param name="scrollToShow"> True to scroll the inspector view to show the added component. </param>
		/// <param name="index"> Member index inside GameObjectDrawer for the component drawer to add. </param>
		/// <returns> Drawer for added component, or null if was no able to create drawer. </returns>
		[CanBeNull]
		private IComponentDrawer DoAddComponent([NotNull]Type type, bool scrollToShow, int index)
		{
			if(!type.IsComponent())
			{
				DrawGUI.Active.DisplayDialog("Can't Add Script", "Can't add script " + StringUtils.ToStringSansNamespace(type) + ". The script needs to derive from MonoBehaviour.", "Ok");
				return null;
			}

			#if DEV_MODE && DEBUG_ADD_COMPONENT
			Debug.Log(ToString()+ ".AddComponent(" + (type == null ? "null" : type.Name) + ", "+index+") with members.Length="+members.Length);
			#endif
			
			int count = targets.Length;
			var comps = ArrayPool<Component>.Create(count);

			try
			{
				for(int t = count - 1; t >= 0; t--)
				{
					var gameObject = targets[t];

					#if DEV_MODE && PI_ASSERTATIONS
					int compCount = gameObject.GetComponents<Component>().Length;
					#endif

					comps[t] = Platform.Active.AddComponent(gameObject, type);

					#if DEV_MODE && PI_ASSERTATIONS
					if(gameObject.GetComponents<Component>().Length != compCount + 1)
					{ Debug.LogError("targets[" + t + "] " + gameObject.name + " component count was"+ compCount +" and after AddComponent is "+gameObject.GetComponents<Component>().Length); }
					#endif
				}
			}
			// Catch ArgumentException: Can't add script behaviour InterfaceDemoComponentB because it is an editor script. To attach a script it needs to be outside the 'Editor' folder.
			catch(ArgumentException e)
			{
				Debug.LogError(e);
				return null;
			}
			
			var created = CreateDrawerForComponents(comps);

			int memberCount = members.Length;

			
			bool addingAsLastComponent = true;
			Component[] existingComponentsAtIndex = null;
			int lastComponentIndex = memberCount - LastCollectionMemberCountOffset;
			if(index != -1 && index < lastComponentIndex)
			{
				// Find component(s) below which components should be added
				// Usually found at index - 1, but should skip all non-component drawers
				for(int n = index - 1; n >= 0; n--)
				{
					var componentDrawer = members[n] as IComponentDrawer;
					if(componentDrawer != null)
					{
						existingComponentsAtIndex = componentDrawer.Components;
						break;
					}
				}
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(inspector.State.ViewIsLocked);
			#endif

			if(index == -1)
			{
				index = LastCollectionMemberIndex + 1;
			}

			AddComponentMember(index, created);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(members.Length >= memberCount);
			#endif

			if(scrollToShow)
			{
				inspector.ScrollToShow(created);
			}
			
			#if DEV_MODE && DEBUG_ADD_COMPONENT
			Debug.Log(StringUtils.ToColorizedString(ToString()+".DoAddComponent done, members now: ", StringUtils.ToString(members), " with index=", index, ", existingComponentsAtIndex=", existingComponentsAtIndex, ", created=", created, ", addingAsLastComponent=", addingAsLastComponent));
			#endif

			if(!addingAsLastComponent)
			{
				MoveComponentsUnderComponents(created.GetValues() as Component[], existingComponentsAtIndex);
			}
			
			//new test: always unfold created components?
			if(created.Foldable)
			{
				created.SetUnfolded(true, false, false);
			}

			return created;
		}

		/// <summary>
		/// Creates IComponentDrawer for components.
		/// 
		/// All components should be of the same type and exists on the targets of this GameObjectDrawer.
		/// 
		/// The created drawer is not automatically added as a child of this GameObjectDrawer.
		/// </summary>
		/// <param name="components"> Components for which to create the drawer. </param>
		/// <returns> Drawer that implments IComponentDrawer </returns>
		[NotNull]
		protected virtual IComponentDrawer CreateDrawerForComponents([NotNull]Component[] components)
		{
			return DrawerProvider.GetForComponents(inspector, components, this);
		}

		/// <inheritdoc/>
		public IComponentDrawer ReplaceComponent(IComponentDrawer replace, Type replacementType, bool scrollToShow = true, bool checkForDependencies = false)
		{
			#if DEV_MODE
			Debug.Log("ReplaceComponent("+replace+", "+replacementType+")");
			#endif

			var index = RemoveComponent(replace, checkForDependencies);
			return AddComponent(replacementType, scrollToShow, index);
		}

		/// <inheritdoc/>
		public int RemoveComponent([NotNull]IComponentDrawer remove)
		{
			return RemoveComponent(remove, true);
		}

		/// <summary>
		/// Remove component targets of given component drawer member.	
		/// </summary>
		/// <param name="remove"> Drawer of components that should be removed. </param>
		/// <param name="checkForDependencies">
		/// True if should check for components that depend on the components that are being removed.
		/// If this is true and any dependent components are found, then user will be prompted to decide what to do.
		/// 
		/// If false we try to remove the components without checking for any dependencies.
		/// </param>
		/// <returns> If a direct member of the GameObject drawer was removed, returns index of said member. If no direct member was removed, returns -1. </returns>
		protected virtual int RemoveComponent([NotNull]IComponentDrawer remove, bool checkForDependencies)
		{
			#if SAFE_MODE || DEV_MODE
			if(remove == null)
			{
				#if DEV_MODE
				Debug.LogError("RemoveComponent() - target was null!");
				#endif
				return -1;
			}
			#endif
			
			var state = inspector.State;

			var viewWasLocked = state.ViewIsLocked;
			state.ViewIsLocked = true;

			var parentDrawer = remove.Parent;
			bool thisIsParent = this == parentDrawer;
			if(parentDrawer == null)
			{
				#if DEV_MODE
				Debug.LogError("RemoveComponent(" + remove + ") - Component had no parent; can't remove the component. remove.Inactive=" + remove.Inactive);
				#endif
				return -1;
			}

			var parentMembers = parentDrawer.Members;
			int indexInParentMembers = Array.IndexOf(parentMembers, remove);
			int indexInThisMembers;
			int removeMemberAtIndex;
			if(thisIsParent)
			{
				indexInThisMembers = indexInParentMembers;
				removeMemberAtIndex = indexInParentMembers;
			}
			else
			{
				indexInThisMembers = Array.IndexOf(members, parentDrawer);
				if(parentMembers.Length == 1)
				{
					removeMemberAtIndex = indexInThisMembers;
				}
				else
				{
					removeMemberAtIndex = -1;
				}
			}

			//check if other Components on the GameObject require the remove target
			if(checkForDependencies)
			{
				var dependencies = new List<IComponentDrawer>(1) { remove };
				if(CollectDependenciesForRemoveComponent(ref dependencies))
				{
					int dependentCount = dependencies.Count;
					string msg;
					string removeButtonText;
					if(dependentCount == 2)
					{
						msg = "Component "+dependencies[1].Type.Name+" requires "+remove.Type.Name+". Remove both components?";
						removeButtonText = "Remove Both";
					}
					else
					{
						msg = "Components ";
						msg += dependencies[1].Type.Name;
						int lastIndex = dependentCount - 1;
						for(int n = 2; n < dependentCount; n++)
						{
							msg += n != lastIndex ? ", " : " and ";
							msg += dependencies[n].Type.Name;
						}
						msg += " require "+remove.Type.Name+". Remove all "+dependentCount+" components?";

						removeButtonText = "Remove All";
                    }

					if(DrawGUI.Active.DisplayDialog("Dependencies Detected!", msg, removeButtonText, "Cancel"))
					{
						for(int n = dependentCount - 1; n >= 0; n--)
						{
							//TO DO: It's not enough to collect the dependencies of the remove target
							//we also should collect dependencies of the components that require the remove target!
							//and the dependencies of those too!
							//so basically need to convert the above to a while loop,
							//where every loop that new dependencies have been found,
							//need to continue adding their dependincies too to the list (if not already there)
							RemoveComponent(dependencies[n], false);
						}
						return RemoveComponent(remove, false);
					}
					return -1;
				}
			}

			#if SAFE_MODE
			if(indexInParentMembers == -1)
			{
				Debug.LogError("RemoveComponent - Failed to find index of member " + remove + " in parent " + parentDrawer + " members: "+StringUtils.ToString(parentMembers));
				return -1;
			}
			#endif

			bool isNull = remove.UnityObject == null;
			int nthNull = 1;
			if(isNull)
			{
				for(int n = 0; n < indexInParentMembers; n++)
				{
					if(parentMembers[n].UnityObject == null)
					{
						nthNull++;
					}
				}
			}

			remove.Dispose();

			// Save reference to components that should be removed before moving on to removing member drawers,
			// to make sure that the references aren't lost.
			var removeComponents = remove.UnityObjects;

			// Remove drawer of removed component in members and visible members of parent.
			var visibleMembers = parentDrawer.VisibleMembers;
			int visibleIndex = Array.IndexOf(visibleMembers, remove);
			DrawerArrayPool.RemoveAt(ref parentMembers, indexInParentMembers, false, false);
			if(visibleIndex != -1)
			{
				DrawerArrayPool.RemoveAt(ref visibleMembers, visibleIndex, false, false);
			}
			parentDrawer.SetMembers(parentMembers, visibleMembers);

			// Remove parent of removed component drawer in GameObjectDrawer if parent was left with zero members.
			// This behaviour makes sense for CategorizedComponentsDrawer.
			// TO DO: Don't do this at this level, so that this behaviour can be customized at the GameObjectDrawer level.
			// Instead call a method like OnComponentRemoved in the GameObjectDrawer.
			if(!thisIsParent && removeMemberAtIndex != -1)
			{
				parentDrawer.Dispose();

				visibleIndex = Array.IndexOf(visibleMembers, remove);
				DrawerArrayPool.RemoveAt(ref members, removeMemberAtIndex, false, false);
				if(visibleIndex != -1)
				{
					DrawerArrayPool.RemoveAt(ref visibleMembers, visibleIndex, false, false);
				}
				parentDrawer.SetMembers(parentMembers, visibleMembers);
			}

			if(!isNull)
			{
				for(int n = removeComponents.Length - 1; n >= 0; n--)
				{
					var component = removeComponents[n];
					if(component != null)
					{
						Platform.Active.Destroy(component);
					}
				}
			}
			else
			{
				for(int n = removeComponents.Length - 1; n >= 0; n--)
				{
					if(targets.Length < n)
					{
						#if DEV_MODE
						Debug.LogWarning("objs.Length ("+ targets.Length+") < "+n+")");
						#endif
						continue;
					}
					var gameObject = targets[n];
					if(gameObject == null)
					{
						#if DEV_MODE
						Debug.LogWarning("gameObject == null");
						#endif
						continue;
					}

					var components = gameObject.GetComponents<Component>();
					int componentCount = components.Length;
					int nullCounter = 0;
					for(int i = 0; i < componentCount; i++)
					{
						var component = components[i];
						if(component == null)
						{
							nullCounter++;
							#if !UNITY_2019_1_OR_NEWER
							if(nullCounter == nthNull)
							{
								Undo.RegisterCompleteObjectUndo(gameObject, "Remove Missing Scripts");
								var serializedObject = new SerializedObject(gameObject);
								var property = serializedObject.FindProperty("m_Component");
								#if DEV_MODE
								Debug.Log("Deleting array element " + i + " / " + property.arraySize);
								#endif
								property.DeleteArrayElementAtIndex(i);
								serializedObject.ApplyModifiedProperties();

								EditorUtility.SetDirty(gameObject);
								UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
								break;
							}
							#endif
						}
					}

					#if UNITY_2019_1_OR_NEWER
					if(nullCounter <= 1 || DrawGUI.Editor.DisplayDialog("Remove All With Missing Scripts?", "The GameObject \""+gameObject.name+ "\" contains multiple MonoBehaviours with missing scripts. Would you like to remove all of them?", "Remove All", "Cancel"))
					{
						#if DEV_MODE
						Debug.Log("RemoveMonoBehavioursWithMissingScript: " + gameObject.name);
						#endif

						Undo.RegisterCompleteObjectUndo(gameObject, "Remove Missing Scripts");

						GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
						EditorUtility.SetDirty(gameObject);
						UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
					}
					#endif
				}
			}

			state.ViewIsLocked = viewWasLocked;

			GUI.changed = true;
			if(OnValueChanged != null)
			{
				OnValueChanged(this, GameObject);
			}
			return removeMemberAtIndex;
		}

		/// <summary>
		/// Finds RequireComponent references to contents of dependencies List inside component drawers in members, and adds any found results to the end of the dependencies list.
		/// </summary>
		/// <param name="dependencies"> List of dependencies found for drawers. </param>
		/// <param name="members"> Drawers to check. </param>
		/// <returns> True if any dependencies were found, false if not. </returns>
		private bool CollectDependenciesForRemoveComponent([NotNull]ref List<IComponentDrawer> dependencies)
		{
			bool found = false;

			for(int d = dependencies.Count - 1; d >= 0; d--)
			{
				var test = dependencies[d];
				var type = test.Type;
				
				int instanceCount = 0;
				foreach(var componentDrawer in this)
				{
					if(componentDrawer.Type == type)
					{
						instanceCount++;
					}
				}

				// If there were multiples instance of this type of Component
				// no need to check if any members require it (it's not possible
				// to require more than one instance of a type)
				if(instanceCount > 1)
				{
					#if DEV_MODE
					Debug.Log("CollectDependenciesForRemoveComponent "+ type.Name+" instanceCount > 1");
					#endif
					continue;
				}
				
				foreach(var componentDrawer in this)
				{
					if(dependencies.Contains(componentDrawer))
					{
						#if DEV_MODE
						Debug.Log("CollectDependenciesForRemoveComponent dependencies.Contains(" + componentDrawer + ")");
						#endif
						continue;
					}
					
					var attributes = componentDrawer.Type.GetCustomAttributes(true);

					#if DEV_MODE
					Debug.Log("CollectDependenciesForRemoveComponent "+ componentDrawer.Type.Name+".GetCustomAttributes: " + StringUtils.ToString(attributes));
					#endif

					for(int r = attributes.Length - 1; r >= 0; r--)
					{
						var requireComponent = attributes[r] as RequireComponent;
						if(requireComponent != null)
						{
							#if DEV_MODE
							Debug.Log("CollectDependenciesForRemoveComponent found RequireComponent with type: "+ StringUtils.ToStringSansNamespace(type));
							#endif

							if((requireComponent.m_Type0 != null && requireComponent.m_Type0.IsAssignableFrom(type)) || (requireComponent.m_Type1 != null && requireComponent.m_Type1.IsAssignableFrom(type)) || (requireComponent.m_Type2 != null && requireComponent.m_Type2.IsAssignableFrom(type)))
							{
								#if DEV_MODE
								Debug.Log("CollectDependenciesForRemoveComponent found dependency for "+ componentDrawer);
								#endif

								dependencies.Add(componentDrawer);
								CollectDependenciesForRemoveComponent(ref dependencies);
								found = true;
							}
							continue;
						}

						var requireComponents = attributes[r] as IRequireComponents;
						if(requireComponents != null)
						{
							#if DEV_MODE
							Debug.Log("CollectDependenciesForRemoveComponent found IRequireComponents with types: " + StringUtils.ToString(requireComponents.RequiredComponents));
							#endif

							var types = requireComponents.RequiredComponents;
							for(int t = types.Length - 1; t >= 0; t--)
							{
								if(types[t].IsAssignableFrom(type))
								{
									dependencies.Add(componentDrawer);
									CollectDependenciesForRemoveComponent(ref dependencies);
									found = true;
								}
							}
						}
					}					
				}
			}

			return found;
		}

		/// <summary>
		/// Finds RequireComponent and IRequireComponents attributes in checkTypes, and if there are any abstract classes or "require any"
		/// type collection, then adds them to requiredOneOfComponents.
		/// </summary>
		/// <param name="checkTypes"> List of types to check for require component attributes. </param>
		/// <param name="requiredComponents"> List into which found "require any" type results should be added. </param>
		/// <returns> True if any dependencies were found, false if not. </returns>
		private bool CollectDependenciesForAddComponent(Type type, [CanBeNull]ref HashSet<AnyType> requiredComponents)
		{
			bool dependenciesFound = false;

			var attributes = type.GetCustomAttributes(true);
			for(int r = attributes.Length - 1; r >= 0; r--)
			{
				var requireComponent = attributes[r] as RequireComponent;
				if(requireComponent != null)
				{
					var requiredType = requireComponent.m_Type0;
					if(requiredType != null)
					{
						if(requiredComponents == null)
						{
							requiredComponents = new HashSet<AnyType>(anyTypeEqualityComparer);
						}

						if(requiredType.IsAbstract || requiredType.IsBaseComponentType())
						{
							var requireAny = requiredType.GetExtendingComponentTypesNotThreadSafe(false);

							#if DEV_MODE
							Debug.Log("CollectDependenciesForAddComponent(" + type.Name+"): "+StringUtils.ToString(requireAny)+ " via RequireComponent type0");
							#endif

							requiredComponents.Add(new AnyType(requireAny.ToArray()));
							dependenciesFound = true;
						}
						else
						{
							requiredComponents.Add(new AnyType(requiredType));
						}
					}

					requiredType = requireComponent.m_Type1;
					if(requiredType != null)
					{
						if(requiredComponents == null)
						{
							requiredComponents = new HashSet<AnyType>(anyTypeEqualityComparer);
						}

						if(requiredType.IsAbstract || requiredType.IsBaseComponentType())
						{
							var requireAny = TypeExtensions.GetExtendingComponentTypesNotThreadSafe(requiredType, false);

							#if DEV_MODE
							Debug.Log("CollectDependenciesForAddComponent(" + type.Name+"): "+StringUtils.ToString(requireAny) + " via RequireComponent type1");
							#endif

							requiredComponents.Add(new AnyType(requireAny.ToArray()));
							dependenciesFound = true;
						}
						else
						{
							requiredComponents.Add(new AnyType(requiredType));
						}
					}

					requiredType = requireComponent.m_Type2;
					if(requiredType != null)
					{
						if(requiredComponents == null)
						{
							requiredComponents = new HashSet<AnyType>(anyTypeEqualityComparer);
						}

						if(requiredType.IsAbstract || requiredType.IsBaseComponentType())
						{
							var requireAny = TypeExtensions.GetExtendingComponentTypes(requiredType, false);

							#if DEV_MODE
							Debug.Log("CollectDependenciesForAddComponent(" + type.Name+"): "+StringUtils.ToString(requireAny) + " via RequireComponent type2");
							#endif

							requiredComponents.Add(new AnyType(requireAny.ToArray()));
							dependenciesFound = true;
						}
						else
						{
							requiredComponents.Add(new AnyType(requiredType));
						}
					}
					continue;
				}

				var requireComponents = attributes[r] as IRequireComponents;
				if(requireComponents != null)
				{
					var types = requireComponents.RequiredComponents;

					if(requireComponents.AllRequired)
					{
						for(int t = types.Length - 1; t >= 0; t--)
						{
							if(requiredComponents == null)
							{
								requiredComponents = new HashSet<AnyType>(anyTypeEqualityComparer);
							}

							dependenciesFound = true;

							var requiredType = types[t];
							if(requiredType != null && (requiredType.IsAbstract || requiredType.IsBaseComponentType()))
							{
								var requireAny = TypeExtensions.GetExtendingComponentTypes(requiredType, false);
								#if DEV_MODE
								Debug.Log("CollectDependenciesForAddComponent(" + type.Name+"): "+StringUtils.ToString(requireAny) + " via " + attributes[r].GetType().Name + " AllRequired types[" + t+"] "+ requiredType.Name);
								#endif

								requiredComponents.Add(new AnyType(requireAny.ToArray()));
							}
							else
							{
								requiredComponents.Add(new AnyType(requiredType));
							}
						}
					}
					else
					{
						if(requiredComponents == null)
						{
							requiredComponents = new HashSet<AnyType>(anyTypeEqualityComparer);
						}
						
						if(types.Length == 1)
						{
							var test = types[0];
							if(test != null && (test.IsAbstract || test.IsBaseComponentType()))
							{
								types = test.GetExtendingComponentTypes(false).ToArray();
							}
						}

						#if DEV_MODE
						Debug.Log("CollectDependenciesForAddComponent(" + type.Name+"): "+StringUtils.ToString(types) + " via "+ attributes[r].GetType().Name + " with AnyRequired");
						#endif

						requiredComponents.Add(new AnyType(types));
						dependenciesFound = true;
					}
				}
			}

			return dependenciesFound;
		}
	}
}