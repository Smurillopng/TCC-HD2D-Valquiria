#define SAFE_MODE
//#define PI_CREATE_ASSET_MENUS

//#define DEBUG_ON_VALIDATE
//#define DEBUG_GET

using System;
using System.IO;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sisus.Attributes;
using Sisus.Newtonsoft.Json;
using Sisus.Newtonsoft.Json.Converters;
using Sisus.CreateScriptWizard;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using UnityEditor;
#if UNITY_2018_1_OR_NEWER && UNITY_EDITOR
using UnityEditor.Presets;
#endif

namespace Sisus
{
	#if PI_CREATE_ASSET_MENUS
	[CreateAssetMenu]
	#endif
	public class InspectorPreferences : ScriptableObject
	{
		private static InspectorPreferences fallbackPreferences;
		public static InspectorStyles Styles;
		public static JsonSerializerSettings jsonSerializerSettings;

		[Header("Data Visibility")]
		[PTooltip("Serialized Only: Only show fields that are serialized by Unity or explicitly exposed with.",
				"All Public: All public fields are shown, even if not serialized by Unity, unless explicitly hidden with Attributes like HideInInspector or NonSerialized.",
				"All Public And Private: All fields are shown, including private ones, unless explicitly hidden with Attributes like HideInInspector or NonSerialized.")]
		public FieldVisibility showFields = FieldVisibility.SerializedOnly;
		[Tooltip("Attribute-Exposed Only: Only show properties explicitly exposed with Attributes like ShowInInspector.\n\nAuto-Generated Public: All public auto-generated properties such as shown, unless explicitly hidden with attributes like HideInInspector. Other properties are not shown unless explicitly exposed with attributes like ShowInInspector.\n\nAll Public: All public properties are shown, unless explicitly hidden with attributes like HideInInspector or NonSerialized.")]
		public PropertyVisibility showProperties = PropertyVisibility.AttributeExposedOnly;
		[Tooltip("Attribute-Exposed Only: Only show methods explicitly exposed with attributes like ShowInInspector.\n\nContext Menu: All methods with the ContextMenu attribute are shown, unless hidden with attributes like HideInInspector. This includes static methods.\nOther methods are not shown unless explicitly exposed with attributes like ShowInInspector.\n\nAll Public: All public methods are shown, unless explicitly hidden using attributes like HideInInspector.\nNon-public methods are only shown if exposed using attributes such as ShowInInspector.")]
		public MethodVisibility showMethods = MethodVisibility.AttributeExposedOnly;
		[ShowInInspector, Tooltip("If true, Components that have been hidden using HideFlags are still shown in the Inspector (though they will be grayed-out to indicate that they are hidden).\nIf false, these Components will be not be shown in the Inspector.")]
		private bool showHiddenComponents;
		[Tooltip("If true, a reference to the MonoScript asset of MonoBehaviours and ScriptableObjects will be shown as their first field.")]
		public bool drawScriptReferenceFields;

		[Tooltip("List of properties that should never be shown in Power Inspector, even in Debug Mode+.")]
		public PropertyReference[] propertyBlacklist = new PropertyReference[0];

		[Header("Data Visualization")]
		[Tooltip("The order in which the Inspector should list fields, properties and methods.\nThe default order is:\n1. Fields\n2. Properties\n3. Methods")]
		public Member[] MemberDisplayOrder = { Member.Field, Member.Property, Member.Method };
		[Tooltip("If true all properties with a get and set accessor will be visualized just like fields are in Debug Mode+.\n\nIf false all properties, except for auto-properties, will default to using a special drawer in Debug Mode+ where the user needs to specifically press a button to get or set the value.\nThis is the safer mode to use, because it is possible that side effects occur when invoking the get or set methods of a property.")]
		public bool simplePropertiesInDebugMode = true;
		[Tooltip("If true, a dedicated tooltip icon will be placed next to members that have a tooltip as a clear indicator that one exists.\nThe tooltip text will then only be shown when this hint icon is mouseovered.")]
		public bool enableTooltipIcons = true;
		[Tooltip("Tooltips will be generated and shown in the Inspector View from the XML documentation comments of class members that have them.Tooltips can still also be added using Tooltip attributes.")]
		public bool enableTooltipsFromXmlComments = true;
		public TextFieldHeightDeterminant textFieldHeight = TextFieldHeightDeterminant.WordWrapping;

		[Tooltip("When Component Unfolding: One At A Time mode is active and the inspected target changes, should the first component start out unfolded?")]
		public bool inOneAtATimeModeAutoUnfoldFirst = false;

		public GreyOut drawInactivateGreyedOut = GreyOut.HeaderOnly;
		public GreyOut drawDisabledGreyedOut = GreyOut.HeaderOnly;

		public bool drawObjectFieldEyedropper = true;

		[Tooltip("Determines whether or not mouseover effects should be enabled for certain GUI elements.")]
		public MouseoverEffectSettings mouseoverEffects = new MouseoverEffectSettings
		{
			unityObjectHeader = false,
			unityObjectHeaderTint = true,

			headerButton = true,

			prefixLabel = true,
			prefixLabelTint = true
		};
		
		[Header("Miscellaneous")]
		[Tooltip("Unity completely ignores files with the extension \".tmp\", not even listing them in the Project Window.\nFiles with another supernumerary extension like \".disabled\" will be listed in the Project Window as text assets, but won't get compiled as scripts.")]
		public string disabledScriptExtension = ".disabled";

		[Tooltip("Display extra tooltips that can help in learning all the features in Power Inspector.")]
		public bool enableTutorialTooltips = true;

		[Header("Prefix Column Resizing")]
		[Tooltip("What kind of prefix column resizer control should be drawn for Unity Object drawers that use an Editor when drawing their body?")]
		public PrefixResizerPositioning prefixResizerForEditors = PrefixResizerPositioning.AlwaysTopOnly;
		[Tooltip("What kind of prefix column resizer control should be drawn for Unity Object drawers that do not use an Editor when drawing their body?")]
		public PrefixResizerPositioning prefixResizerForEditorless = PrefixResizerPositioning.AlwaysVertical;
		[Tooltip("Should prefix column widths be automatically optimized, and if so, should it be done separately for each Component, or for all Components of a GameObject in unison?")]
		public PrefixAutoOptimization autoResizePrefixLabels = PrefixAutoOptimization.AllSeparately;
		[Tooltip("When should prefix column widths be automatically optimized?")]
		public PrefixAutoOptimizationInterval autoResizePrefixLabelsInterval = PrefixAutoOptimizationInterval.OnLayoutChanged;
		[Range(0, 500)]
		[PTooltip("Maximum width for the prefix label column when optimal width is automatically determined.",
			"Value is absolute value in pixels.")]
		public int minAutoSizedPrefixColumnWidth = 35;
		[Range(0f, 1f), FormerlySerializedAs("maxAutoSizedPrefixLabelWidth")]
		[PTooltip("Maximum width for the prefix label column when optimal width is automatically determined.",
			"Value is between 0 and 1 and is relative to the full width of the inspector window.")]
		public float maxAutoSizedPrefixColumnWidth = 0.55f;
		
		[Header("Header Toolbar")]
		public bool drawDebugModeIcon = true;
		[FormerlySerializedAs("drawExecuteMethodIcon")]
		public bool drawQuickInvokeIcon = true;
		public bool drawReferenceIcon = true;

		[Header("Prefabs")]
		#if !UNITY_2018_3_OR_NEWER
		[SerializeField, HideInInspector] //Don't even show this setting if using older Unity versions where Prefab Mode hasn't yet been introduced.
		#endif
		public PrefabQuickEditingSettings prefabQuickEditing = PrefabQuickEditingSettings.Enabled;


		[Header("Menus")]
		public bool popupMenusScrollToActiveItem = true;
		[FormerlySerializedAs("disableMenuItems")]
		public MenuItems disabledMenuItems = MenuItems.None;
		
		[Header("Animations")]
		[Range(0f, 144f)]
		public float foldingAnimationSpeed = 6f;
		public bool animateInitialUnfolding = false;
		[SerializeField, HideInInspector]
		public float reorderingAnimationSpeed = 7f;
		[SerializeField, HideInInspector]
		public int reorderingEaseType = 0;		

		[Header("Custom Editors")]
		[PTooltip("Never: Always use built-in field focusing logic for keyboard inputs inside drawers using a Custom Editor.",
		"",
		"Dynamic (Default): Use Power Inspector's custom field selection logic for keyboard inputs inside drawers drawn using a Custom Editor, except when a drawer has specifically been flagged as preferring the built-in field focusing system, then use built-in field focusing logic instead.",
		"",
		"Always (Experimental): Always use Power Inspector's custom field focusing logic inside drawers drawn using a Custom Editor.",
		"",
		"Note: this is still an experimental feature, and you might run into issues when using it. For example, you might be unable to move focus to some controls using keyboard inputs.",
		"As a workaround for this issue, you can still move focus to controls by clicking them with the mouse instead.")]
		public OverrideFieldFocusing overrideCustomEditorFieldFocusing = OverrideFieldFocusing.Dynamic;

		[Header("Compatibility")]
		[SerializeField, Tooltip("Compatibility with some plugins can be improved by using Editors instead of Power Inspector's own drawers for drawing Unity Object targets - at the cost of losing some features."), FormerlySerializedAs("useEditorsOverGUIInstructions")]
		private UseEditorsOverDrawers useEditorsOverDrawers = UseEditorsOverDrawers.BasedOnPluginPreferences;

		[Tooltip("Specify whether or not some enhancements of Power Inspector should be injected to affect the default inspector experience as well."), SerializeField]
		public DefaultInspectorEnhancements defaultInspector = new DefaultInspectorEnhancements();

		[Header("Input")]
		public bool changeFoldedStateOnFirstClick = false;
		[HideInInspector, NonSerialized]
		public bool doubleClickPrefixToReset = false;
		public KeyConfigs keyConfigs = new KeyConfigs();

		[Header("Create Script Wizard")]
		public string defaultScriptPath = "";
		public string defaultEditorScriptPath = "Editor";
		public string defaultNamespace = "";

		[FormerlySerializedAs("newScriptWindow")]
		public NewScriptWindowSettings createScriptWizard = new NewScriptWindowSettings();

		[Header("Add Component Menu")]
		public string defaultAddComponentMenuName = "";
		public bool autoNameByAddedComponentIfHasDefaultName = true;
		public AddComponentMenuConfig addComponentMenuConfig = new AddComponentMenuConfig();

		[Header("Themes")]
		public GUIThemeColorsList themes = new GUIThemeColorsList();
		
		[Header("Messages")]
		public MessageDisplayMethod messageDisplayMethod = MessageDisplayMethod.Notification | MessageDisplayMethod.Console;
		[Range(0f, 10f)]
		public float displayDurationPerWord = 0.1f;
		[Range(0f, 10f)]
		public float minDisplayDuration = 0.3f;
		[Range(0f, 10f)]
		public float maxDisplayDuration = 4f;
		[Tooltip("Warn when about to invoke a method in edit mode using Quick Invoke Menu?")]
		public bool warnAboutInvokingInEditMode = true;
		[Tooltip("Always start IEnumerator type methods as coroutines, or ask first?")]
		public bool askAboutStartingCoroutines = false;

		[NonSerialized, HideInInspector]
		public Action<InspectorPreferences> onSettingsChanged;

		[SerializeField, HideInInspector]
		private int currentVersion;

		[NonSerialized, HideInInspector]
		private bool onValidateInProgress;
		[NonSerialized, HideInInspector]
		private bool isFirstOnValidate = true;
		[NonSerialized, HideInInspector]
		private bool setupDone;

		public bool SetupDone
		{
			get
			{
				return setupDone;
			}
		}

		[Header("Categorized Components")]
		public ComponentCategory[] componentCategories = new ComponentCategory[]
		{
			new ComponentCategory("", typeof(Transform), typeof(RectTransform))
		};

		public string defaultComponentCategory = "Miscellaneous";

		[SerializeField, HideInInspector]
		private bool categorizedComponents = false;
		[SerializeField, HideInInspector]
		private bool generateFromAddComponentMenu = true;

		[PHeader("Categorized Components")]
		[PTooltip("Should components in the inspector view be categorized based on their categories in the Add Component menu?")]
		[ShowInInspector]
		public bool EnableCategorizedComponents
 		{
			get
			{
				return categorizedComponents;
			}

			set
			{
				if(value == categorizedComponents)
				{
					return;
				}

				Set(ref categorizedComponents, value, "Enable Categorized Components: {0}");
				RebuildInspectorDrawers();
			}
		}

		[ShowInInspector]
		public bool GenerateFromAddComponentMenu
		{
			get
			{
				return generateFromAddComponentMenu;
			}

			set
			{
				if(generateFromAddComponentMenu != value)
				{
					generateFromAddComponentMenu = value;
					ComponentCategories.Rebuild(componentCategories, generateFromAddComponentMenu);
				}
			}
		}

		[PHeader("Categorized Components")]
		[ShowInInspector]
		private void RebuildComponentCategories()
		{
			ComponentCategories.Rebuild(componentCategories, generateFromAddComponentMenu);
			RebuildInspectorDrawers();
		}

		[Header("Transform Drawer")]
		public bool tintXYZLabels = true;

		#if DEV_MODE // TO DO: implement this
		[/*PHeader("Color Drawer"), */HideInInspector]
		public KeyValuePair<string, Color>[] setValueColors = new KeyValuePair<string, Color>[]
		{
			new KeyValuePair<string, Color>("Clear", Color.clear),
			new KeyValuePair<string, Color>("White", Color.white),
			new KeyValuePair<string, Color>("Black", Color.black),
			new KeyValuePair<string, Color>("Gray", Color.gray),
			new KeyValuePair<string, Color>("Red", Color.red),
			new KeyValuePair<string, Color>("Yellow", Color.yellow),
			new KeyValuePair<string, Color>("Blue", Color.blue),
			new KeyValuePair<string, Color>("Green", Color.green),
			new KeyValuePair<string, Color>("Cyan", Color.cyan),
			new KeyValuePair<string, Color>("Magenta", Color.magenta)
		};
		#endif

		[PHeader("Transform Drawer")]
		[ShowInInspector]
		public GUIContent PositionLabel
		{
			get
			{
				return labels.Position;
			}

			set
			{
				labels.Position = value;
			}
		}

		[ShowInInspector]
		public GUIContent RotationLabel
		{
			get
			{
				return labels.Rotation;
			}

			set
			{
				labels.Rotation = value;
			}
		}

		[ShowInInspector]
		public GUIContent ScaleLabel
		{
			get
			{
				return labels.Scale;
			}

			set
			{
				labels.Scale = value;
			}
		}

		[ShowInInspector]
		public GUIContent XLabel
		{
			get
			{
				return labels.X;
			}

			set
			{
				labels.X = value;
			}
		}

		[ShowInInspector]
		public GUIContent YLabel
		{
			get
			{
				return labels.Y;
			}

			set
			{
				labels.Y = value;
			}
		}

		[ShowInInspector]
		public GUIContent ZLabel
		{
			get
			{
				return labels.Z;
			}

			set
			{
				labels.Z = value;
			}
		}
		
		public GUIThemeColors theme
		{
			get
			{
				try
				{
					return themes.Active;
				}
				#if DEV_MODE
				catch(NullReferenceException e) //temp fix for issue where PowerInspectorWindow would throw exceptions when OnLostFocus was called during assembly reload process
				{
					Debug.LogError("InspectorPreferences.themes.Active NullReferenceException with isCompiling="+Platform.IsCompiling+".\n"+e);
				#else
				catch(NullReferenceException)
				{
				#endif
					if(Event.current != null)
					{
						GUIUtility.ExitGUI();
					}
					return null;
				}
			}
		}
		
		public Color PrefixMouseoveredTextColor
		{
			get
			{
				if(mouseoverEffects.prefixLabelTint)
				{
					return theme.PrefixMouseoveredText;
				}
				return theme.PrefixIdleText;
			}
		}

		public Color PrefixSelectedAndMouseoveredTextColor
		{
			get
			{
				return Color.Lerp(theme.PrefixSelectedText, PrefixMouseoveredTextColor, 0.5f);
			}
		}

		[CanBeNull]
		public GUIStyle GetStyle([NotNullOrEmpty]string styleName)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!string.IsNullOrEmpty(styleName), "InspectorPreferences.GetStyle called with null or empty style name.");
			#endif

			var fromTheme = theme;
			if(fromTheme == null)
			{
				#if DEV_MODE
				Debug.LogError("InspectorPreferences.GetStyle(" + StringUtils.ToString(styleName) + ") called but theme was null. IsCompiling=" + Platform.IsCompiling);
				#endif
				return null;
			}

			if(fromTheme.guiSkin == null)
			{
				#if DEV_MODE
				Debug.LogError("InspectorPreferences.GetStyle(" + StringUtils.ToString(styleName) + ") called but theme.guiSkin was null. IsCompiling=" + Platform.IsCompiling);
				#endif
				return null;
			}

			return fromTheme.guiSkin.FindStyle(styleName);
		}

		public GUISkin GUISkin
		{
			get
			{
				return theme.guiSkin;
			}
		}

		public FieldVisibility ShowNonSerializedFields
		{
			get
			{
				return showFields;
			}

			set
			{
				Set(ref showFields, value, "Show Fields: {0}");
			}
		}

		public PropertyVisibility ShowProperties
		{
			get
			{
				return showProperties;
			}

			set
			{
				Set(ref showProperties, value, "Show Properties: {0}");
			}
		}

		public MethodVisibility ShowMethods
		{
			get
			{
				return showMethods;
			}

			set
			{
				Set(ref showMethods, value, "Show Methods: {0}");
			}
		}

		public bool ShowHiddenComponents
		{
			get
			{
				return showHiddenComponents;
			}

			set
			{
				Set(ref showHiddenComponents, value, "Show Hidden Components: {0}");
			}
		}
		
		public UseEditorsOverDrawers UseEditorsOverDrawers
		{
			get
			{
				return useEditorsOverDrawers;
			}

			set
			{
				Set(ref useEditorsOverDrawers, value, "Prefer Editors Over Drawers: {0}");
			}
		}

		public InspectorGraphics graphics
		{
			get
			{
				try
				{
					return theme.graphics;
				}
				#if DEV_MODE
				catch(NullReferenceException e) //temp fix for issue where PowerInspectorWindow would throw exceptions when OnLostFocus was called during assembly reload process
				{
					Debug.LogError(e);
				#else
				catch(NullReferenceException)
				{
				#endif
					GUIUtility.ExitGUI(); //new test!
					return null;
				}
			}
		}
		
		public InspectorLabels labels
		{
			get
			{
				return theme.labels;
			}
		}
		
		public static InspectorPreferences GetSettingsCached(ref InspectorPreferences preferencesCached, bool editorMode)
		{
			if(preferencesCached == null)
			{
				preferencesCached = GetDefaultPreferences();
			}
			return preferencesCached;
		}
		
		public static InspectorPreferences GetDefaultPreferences()
		{
			if(fallbackPreferences != null)
			{
				return fallbackPreferences;
			}

			#if DEV_MODE
			var timer = new ExecutionTimeLogger();
			timer.Start("InspectorPreferences.GetDefaultPreferences");
			#endif

			#if UNITY_EDITOR
			if(Platform.EditorMode)
			{
				fallbackPreferences = GetDefaultPreferencesEditorMode();
				if(fallbackPreferences != null)
				{
					#if DEV_MODE && DEBUG_GET
					Debug.Log("GetDefaultPreferences returning result of GetDefaultPreferencesEditorMode: "+StringUtils.TypeToString(fallbackPreferences));
					#endif

					if(!fallbackPreferences.setupDone)
					{
						if(Event.current != null)
						{
							#if DEV_MODE && DEBUG_GET
							Debug.Log("Calling fallbackPreferences.Setup()");
							#endif
							fallbackPreferences.Setup();
						}
						#if DEV_MODE
						else { Debug.LogWarning("Could not run InspectorPreferences.Setup because Event.current was null!"); }
						#endif
					}

					return fallbackPreferences;
				}
				#if DEV_MODE
				Debug.LogWarning("GetDefaultPreferencesEditorMode returned null");
				#endif
			}
			#if DEV_MODE
			else { Debug.LogWarning("!Platform.EditorMode"); }
			#endif
			#endif

			var findAll = Resources.FindObjectsOfTypeAll<InspectorPreferences>();
			for(int n = findAll.Length - 1; n >= 0; n--)
			{
				fallbackPreferences = findAll[n];
				if(fallbackPreferences != null)
				{
					#if DEV_MODE
					Debug.Log("GetDefaultPreferences returning FindObjectsOfTypeAll result #"+n+": "+StringUtils.TypeToString(fallbackPreferences));
					#endif

					if(!fallbackPreferences.setupDone)
					{
						if(Event.current != null)
						{
							fallbackPreferences.Setup();
						}
						#if DEV_MODE
						else { Debug.LogWarning("Could not run InspectorPreferences.Setup because Event.current was null!"); }
						#endif
					}

					return fallbackPreferences;
				}
			}
			#if DEV_MODE
			Debug.LogWarning("FindObjectsOfTypeAll could not find non-null InspectorPreferences. FindObjectsOfTypeAll returned "+findAll.Length+" results.");
			#endif

			fallbackPreferences = Resources.Load<InspectorPreferences>("RuntimePowerInspectorPreferences");
			
			if(fallbackPreferences != null)
			{
				if(!fallbackPreferences.setupDone)
				{
					if(Event.current != null)
					{
						fallbackPreferences.Setup();
					}
					#if DEV_MODE
					else { Debug.LogWarning("Could not run InspectorPreferences.Setup because Event.current was null!"); }
					#endif
				}

				return fallbackPreferences;
			}

			#if UNITY_EDITOR
			if(!Platform.EditorMode)
			{
				fallbackPreferences = GetDefaultPreferencesEditorMode();
				if(fallbackPreferences != null)
				{
					return fallbackPreferences;
				}
			}
			#endif

			#if DEV_MODE
			timer.FinishAndLogResults();
			#endif

			throw new NullReferenceException("Failed to find default InspectorPreferences with Platform.EditorMode="+ Platform.EditorMode+"!");
		}

		#if UNITY_EDITOR
		private static InspectorPreferences GetDefaultPreferencesEditorMode()
		{
			var guids = AssetDatabase.FindAssets("t:InspectorPreferences");
			int found = guids.Length;
			if(found > 0)
			{
				string path;
				for(int n = found - 1; n >= 0; n--)
				{
					path = AssetDatabase.GUIDToAssetPath(guids[n]);
					if(path.IndexOf("editor", StringComparison.OrdinalIgnoreCase) != -1)
					{
						fallbackPreferences = AssetDatabase.LoadAssetAtPath<InspectorPreferences>(path);
						if(fallbackPreferences != null)
						{
							#if DEV_MODE && DEBUG_GET
							Debug.Log("GetDefaultPreferencesEditorMode returning asset @ "+path+": "+StringUtils.TypeToString(fallbackPreferences));
							#endif

							if(!fallbackPreferences.setupDone)
							{
								if(Event.current != null)
								{
									#if DEV_MODE && DEBUG_GET
									Debug.Log("Calling fallbackPreferences.Setup()");
									#endif
									fallbackPreferences.Setup();
								}
								#if DEV_MODE
								else { Debug.LogWarning("Could not run InspectorPreferences.Setup because Event.current was null!"); }
								#endif
							}

							return fallbackPreferences;
						}
					}
				}
			}
			#if DEV_MODE
			else { Debug.LogWarning("GetDefaultPreferencesEditorMode found 0 results with AssetDatabase.FindAssets(\"t:InspectorPreferences\")"); }
			#endif

			return null;
		}
		#endif
		
		/// <summary>
		/// This should be called from OnGUI before other properties are accessed
		/// because some of Unity's IMGUI methods can't be called outside of OnGUI.
		/// </summary>
		public void Setup()
		{
			if(setupDone)
			{
				return;
			}

			if(Event.current == null)
			{
				#if DEV_MODE
				Debug.LogWarning("InspectorPreferences.Setup called with Event.current null");
				#endif

				Styles = new InspectorStyles(null);
			}
			else
			{
				themes.Setup(DrawGUI.IsProSkin, GUI.skin);
				Styles = new InspectorStyles(theme.guiSkin);
				setupDone = true;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(theme != null, "InspectorPreferences.Setup - themes.Active was null. Event.current="+StringUtils.ToString(Event.current)+".");
			#endif
			
			theme.SyntaxHighlight.OnValidate();

			jsonSerializerSettings = new JsonSerializerSettings
			{
				DefaultValueHandling = DefaultValueHandling.Include,
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
				NullValueHandling = NullValueHandling.Include,
				ContractResolver = new Newtonsoft.Json.Serialization.JsonFullSerializationContractResolver()
			};
			var converters = jsonSerializerSettings.Converters;
			converters.Clear(); //remove any existing converters
			converters.Add(new BinaryConverter());
			converters.Add(new BsonObjectIdConverter());
			converters.Add(new ColorConverter());
			converters.Add(new CharJsonConverter());
			converters.Add(new DoubleJsonConverter()); //gets rid of the trailing ".0"
			converters.Add(new FloatJsonConverter()); //gets rid of the trailing ".0"
			converters.Add(new IsoDateTimeConverter());
			converters.Add(new KeyValuePairConverter());
			converters.Add(new Matrix4x4Converter());
			converters.Add(new QuaternionJsonConverter());
			converters.Add(new RectJsonConverter());
			converters.Add(new RectOffsetJsonConverter());
			converters.Add(new RegexConverter());
			converters.Add(new ResolutionConverter());
			converters.Add(new StringJsonConverter());
			converters.Add(new StringEnumConverter());
			converters.Add(new TypeJsonConverter()); //supports deserializing types from just short name
			converters.Add(new UnityObjectReferenceConverter());
			converters.Add(new VectorsJsonConverter()); //supports copy-pasting between Vector2, Vector3 and Vector4 and even pasting values of things like Rect
			converters.Add(new VersionConverter());
			converters.Add(new GUIStyleConverter());
			converters.Add(new DelegateJsonConverter());
			converters.Add(new AnimationCurveJsonConverter());
		}

		private void Set<T>(ref T subject, [NotNull]T value, string undoMessage)
		{
			if(!value.Equals(subject))
			{
				UndoHandler.RegisterUndoableAction(this, string.Format(undoMessage, value));

				subject = value;

				Platform.Active.SetDirty(this);
			}
		}
		
		private static void SetExternal<T>(Object target, ref T subject, [NotNull]T value, string undoMessage)
		{
			if(!value.Equals(subject))
			{
				UndoHandler.RegisterUndoableAction(target, string.Format(undoMessage, value));

				subject = value;

				Platform.Active.SetDirty(target);
			}
		}

		// Enable faster iteraction by applying setting changes immediately
		[UsedImplicitly]
		private void OnValidate()
		{
			if(onValidateInProgress)
			{
				return;
			}
			onValidateInProgress = true;

			var isBeingEdited = Platform.EditorMode && InspectorUtility.ActiveManager != null && InspectorUtility.ActiveManager.ActiveInspector != null && InspectorUtility.ActiveManager.ActiveInspector.State.drawers.First() != null && Array.IndexOf(InspectorUtility.ActiveManager.ActiveInspector.State.drawers.First().GetValues(), this) != -1;

			#if DEV_MODE && DEBUG_ON_VALIDATE
			Debug.Log("InspectorPreferences.OnValidate called with isBeingEdited="+StringUtils.ToColorizedString(isBeingEdited));
			#endif

			if(isBeingEdited)
			{
				isFirstOnValidate = false;
			
				//If Setup hasn't been called yet this can be null
				if(theme != null && DrawGUI.prefixLabelMouseovered != null)
				{
					var mouseoverColor = mouseoverEffects.prefixLabelTint ? theme.PrefixMouseoveredText : theme.PrefixIdleText;
					DrawGUI.prefixLabelMouseovered.SetAllTextColors(mouseoverColor);
					DrawGUI.prefixLabelMouseoveredModified.SetAllTextColors(mouseoverColor);
					DrawGUI.foldoutStyleMouseovered.SetAllTextColors(mouseoverColor);
					DrawGUI.foldoutStyleSelectedMouseovered.SetAllTextColors(mouseoverColor);
					PrefixDrawer.ClearCache();

					// This was happening when the themes field was edited
					if(theme.guiSkin == null)
					{
						#if DEV_MODE
						Debug.LogError("InspectorPreferences.OnValidate - guiSkin of theme \""+theme.Name+"\" was null!");
						#endif
						return;
					}

					Styles = new InspectorStyles(theme.guiSkin);
				}
			
				themes.OnValidate();
			
				if(onSettingsChanged != null)
				{
					onSettingsChanged(this);
				}
			}
			else
			{
				if(isFirstOnValidate)
				{
					isFirstOnValidate = false;

					#if !DISABLE_UPDATES && UNITY_EDITOR
					EditorApplication.delayCall += ApplyRelevantUpdates;
					#endif
				}
			}

			onValidateInProgress = false;
		}

		[ContextMenu("Reset To Defaults")]
		public void ResetToDefaults()
		{
			UndoHandler.RegisterUndoableAction(this, "Reset To Defaults");

			#if UNITY_EDITOR // in UnityEditor use Presets or EditorJsonUtility

			#if UNITY_2018_1_OR_NEWER // Presets were introduced in Unity 2018.1
			
			#if UNITY_2019_3_OR_NEWER // GetDefaultForObject became obsolete in Unity 2019.3 
			var presets = Preset.GetDefaultPresetsForObject(this);
			var preset = presets.Length > 0 ? presets[0] : null;
			#else
			var preset = Preset.GetDefaultForObject(this);
			#endif

			// if no default preset has been assigned for preferences asset, then try finding a preset
			// in the same directory with the preferences asset
			if(preset == null)
			{
				var preferencesPath = AssetDatabase.GetAssetPath(this);
				var directoryPath = FileUtility.GetParentDirectory(preferencesPath);
				var updateGuids = AssetDatabase.FindAssets("t:Preset", ArrayExtensions.TempStringArray(directoryPath));
				for(int n = updateGuids.Length - 1; n >= 0; n--)
				{
					var path = AssetDatabase.GUIDToAssetPath(updateGuids[n]);
					preset = AssetDatabase.LoadAssetAtPath<Preset>(path);
					if(!string.Equals(preset.GetTargetFullTypeName(), typeof(InspectorPreferences).FullName, StringComparison.OrdinalIgnoreCase))
					{
						preset = null;
					}
					else
					{
						break;
					}
				}
			}

			if(preset != null)
			{
				preset.ApplyTo(this);
			}
			else
			#endif
			{
				var freshInstance = CreateInstance<InspectorPreferences>();
				var jsonString = EditorJsonUtility.ToJson(freshInstance);
				Platform.Active.Destroy(freshInstance);
				EditorJsonUtility.FromJsonOverwrite(jsonString, this);				
			}
			#else
			// at runtime use JsonUtility to reset values to those of a freshly created instance
			var freshInstance = CreateInstance<InspectorPreferences>();
			var jsonString = JsonUtility.ToJson(freshInstance);
			Platform.Active.Destroy(freshInstance);
			JsonUtility.FromJsonOverwrite(jsonString, this);			
			#endif
			
			setupDone = false;
			isFirstOnValidate = true;

			if(Event.current != null)
			{
				Setup();
			}

			Platform.Active.SetDirty(this);					

			if(onSettingsChanged != null)
			{
				onSettingsChanged(this);
			}
		}

		[PHeader("Transform Drawer")]
		[Button("Classic Look", "Apply")]
		public void ClassicLook()
		{
			labels.Position = new GUIContent("Position");
			labels.Rotation = new GUIContent("Rotation");
			labels.Scale = new GUIContent("Scale");
			labels.X = new GUIContent("X");
			labels.Y = new GUIContent("Y");
			labels.Z = new GUIContent("Z");

			tintXYZLabels = false;

			RebuildInspectorDrawers();
		}

		[Button("Compact Look", "Apply")]
		public void CompactLook()
		{
			labels.Position = new GUIContent("P");
			labels.Rotation = new GUIContent("R");
			labels.Scale = new GUIContent("S");
			labels.X = new GUIContent("X");
			labels.Y = new GUIContent("Y");
			labels.Z = new GUIContent("Z");

			RebuildInspectorDrawers();
		}

		[Button("Iconographic Look", "Apply")]
		public void IconographicLook()
		{
			labels.Position = new GUIContent(graphics.Position);
			labels.Rotation = new GUIContent(graphics.Rotation);
			labels.Scale = new GUIContent(graphics.Scale);

			RebuildInspectorDrawers();
		}

		[Button("Colorful X/Y/Z Tint", "Apply")]
		public void ColorfulXYZTint()
		{
			labels.X = new GUIContent("X");
			labels.Y = new GUIContent("Y");
			labels.Z = new GUIContent("Z");

			tintXYZLabels = true;

			RebuildInspectorDrawers();
		}

		[Button("Colorful X/Y/Z Icons", "Apply")]
		public void ColorfulXYZIcons()
		{
			labels.X = new GUIContent(graphics.X);
			labels.Y = new GUIContent(graphics.Y);
			labels.Z = new GUIContent(graphics.Z);

			tintXYZLabels = false;

			RebuildInspectorDrawers();
		}
		
		private static void RebuildInspectorDrawers()
		{
			var manager = InspectorUtility.ActiveManager;
			if(manager != null)
			{
				var inspectors = manager.ActiveInstances;
				for(int n = inspectors.Count - 1; n >= 0; n--)
				{
					var inspector = inspectors[n];
					if(!(inspector is PreferencesInspector))
					{
						inspector.ForceRebuildDrawers();
					}
				}
			}
		}

		#if UNITY_EDITOR
		[PHeader("Create Script Wizard"), ShowInInspector]
		private void EditScriptTemplates()
		{
			// Try to find function file first in custom templates folder and then in built-in
			string functionDataFilePath = GetCustomScriptTemplateFullPath();
			if(Directory.Exists(functionDataFilePath))
			{
				EditorUtility.RevealInFinder(functionDataFilePath);
				return;
			}

			functionDataFilePath = GetBuiltinScriptTemplateFullPath();
			if(Directory.Exists(functionDataFilePath))
			{
				EditorUtility.RevealInFinder(functionDataFilePath);
				return;
			}
		}		

		private string GetBuiltinScriptTemplateFullPath()
		{
			return Path.Combine(EditorApplication.applicationContentsPath, "Resources/SmartScriptTemplates");
		}

		private string GetCustomScriptTemplateFullPath()
		{
			var templateDirs = Directory.GetDirectories(Application.dataPath, "SmartScriptTemplates", SearchOption.AllDirectories);
			if(templateDirs.Length > 0)
			{
				return templateDirs[0];
			}

			InspectorUtility.ActiveInspector.Message("Failed to locate templates directory \""+ "SmartScriptTemplates" + "\" inside \""+ Application.dataPath+"\"");
			return "";
		}
		#endif

		#if !DISABLE_UPDATES && UNITY_EDITOR
		public void ApplyRelevantUpdates()
		{
			if(currentVersion >= Version.CurrentAsInt)
			{
				return;
			}

			if(!ApplicationUtility.IsReady())
			{
				EditorApplication.delayCall += ApplyRelevantUpdates;
				return;
			}

			int appliedUpdateCount = 0;
			bool updateWasApplied;
			var updates = InspectorPreferencesUpdate.GetAllUpdates(this);

			do
			{
				updateWasApplied = false;

				foreach(var update in updates)
				{
					if(update.ShouldApplyNext(currentVersion))
					{
						appliedUpdateCount++;
						update.UpdateNow(this);
						updateWasApplied = true;
						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(currentVersion < update.ToVersion);
						#endif
						currentVersion = update.ToVersion;
						break;
					}
				}

				if(appliedUpdateCount > 100)
				{
					Debug.LogError("InspectorPreferences updater seems to have gotten stuck on an infinite loop!");
					break;
				}
			}
			while(updateWasApplied);

			#if DEV_MODE
			if(appliedUpdateCount > 0) { Debug.Log("Successfully applied " + appliedUpdateCount + "/" + updates.Length+" updates to preferences."); }
			#endif
		}
		#endif
	}
}