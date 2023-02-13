#define USE_FORMATTED_TEXT

//#define DISABLE_SCRIPT_WIZARD_MENU_ITEM

//#define DEBUG_SELECT
//#define DEBUG_FUNCTIONS
#define DEBUG_REBUILD_CODE

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus.CreateScriptWizard
{
	public sealed class CreateScriptWizardWindow : EditorWindow
	{
		private const float previewMinWidth = 100f;
		private const float optionsMinWidth = 200f;
		private const string FirstOptionsControlName = NamespaceControlName;
		private const string NamespaceControlName = "CSW.NamespaceControl";
		private const string ClassNameControlName = "CSW.ClassNameControl";
		private const string SavePathControlName = "CSW.SavePathControl";
		private const string TemplateControlName = "CSW.TemplateControl";
		private const string AddToControlName = "CSW.AddToControl";
		private const string EditorTargetControlName = "CSW.EditorTargetControl";
		private const string ScriptableObjectCreateAssetMenuControlName = "CSW.SOCreateAssetMenuControl";
		private const string ScriptableObjectFilenameControlName = "CSW.SOFilenameControl";
		private const string ScriptableObjectMenuNameControlName = "CSW.SOMenuNameControl";
		private const string ScriptableObjectOrderControlName = "CSW.SOOrderControl";
		private const string SingletonBaseClassControlName = "CSW.BaseClassControl";

		private static float optionsPreferredWidth = 380f;

		private const int ButtonWidth = 120;
		private const int LabelWidth = 85;
		private const string TemplateDirectoryName = "SmartScriptTemplates";
		private const string ResourcesTemplatePath = "Resources/SmartScriptTemplates";
		private const string MonoBehaviourName = "MonoBehaviour";
		private const string ScriptableObjectName = "Scriptable Object";
		private const string SingletonName = "Singleton";
		private const string PlainClassName = "Class";
		private const string StructName = "Struct";
		private const string EnumName = "Enum";
		private const string InterfaceName = "Interface";
		private const string CustomEditorClassName = "Editor";
		private const string TempEditorClassPrefix = "E:";
		private const string NoTemplateString = "No Template Found";

		// char array can't be const for compiler reasons but this should still be treated as such.
		private static readonly char[] InvalidPathChars = { '<', '>', ':', '"', '|', '?', '*', (char)0 };
		private static readonly char[] PathSeparatorChars = { '/', '\\' };

		private static readonly GUIContent PreviewLabel = new GUIContent("Preview");
		private static readonly GUIContent CreateAssetMenuLabel = new GUIContent("Create Menu", "List ScriptableObject in the Assets/Create submenu, so that instances of the type can be easily created and stored in the project as \".asset\" files.");
		private static readonly GUIContent FilenameLabel = new GUIContent("Filename", "The default file name used by newly created instances of this type.");
		private static readonly GUIContent MenuNameLabel = new GUIContent("Menu Name", "The display name for this type shown in the Assets/Create menu.");
		private static readonly GUIContent OrderLabel = new GUIContent("Order", "The position of the menu item within the Assets/Create menu.");
		private static readonly GUIContent SingletonBaseClassNameLabel = new GUIContent("Base Class");

		[SerializeField]
		private GUIContent directoryBrowseIcon = new GUIContent("");

		private static Styles styles;

		private ScriptPrescription scriptPrescription;

		private string baseClass
		{
			get
			{
				return scriptPrescription.baseClass;
			}
		}

		private string customEditorTargetClassName = "";
		[SerializeField]
		private bool scriptableObjectCreateAssetMenu = false;
		private string scriptableObjectFilename = "";
		private string scriptableObjectMenuName = "";
		private bool scriptableObjectFilenameIsInvalid;
		private int scriptableObjectOrder;
		private string singletonBaseClassName = "";

		private bool isEditorClass;
		private bool isCustomEditor;
		private bool isMonoBehaviour;
		private bool isScriptableObject;
		private bool isSingleton;

		private string focusControl = "";
		private string focusedControl = "";

		private GameObject gameObjectToAddTo;
		private string scriptOutputDirectoryLocalPathWithoutAssetsPrefix = "";
		private Vector2 optionsScroll;
		private bool clearKeyboardControl;

		[SerializeField]
		private int templateIndex;
		[SerializeField]
		private string[] templateNames = null;

		private bool curlyBracesOnNewLine;
		private bool addComments = true;
		private bool addCommentsAsSummary = true;
		private int wordWrapCommentsAfterCharacters = 100;
		private bool addUsedImplicitly;
		private bool spaceAfterMethodName = true;
		private string newLine = "\r\n";
		private string[] namespacesList;

		private bool namespaceIsInvalid;
		private bool classNameIsInvalid = true;
		private bool classAlreadyExists;
		private bool scriptAtPathAlreadyExists;
		private bool customEditorTargetClassDoesNotExist;
		private bool customEditortargetClassIsNotValidType;
		private bool invalidTargetPath;
		private bool invalidTargetPathForEditorScript;
		private bool canCreate;

		private InspectorPreferences preferencesAsset;
		
		[SerializeField]
		private string code = "";

		private static readonly List<string> reusableStringList = new List<string>(10);

		[NonSerialized]
		private bool setupDone;

		[NonSerialized]
		private bool resizing = false;

		[SerializeField]
		private CreateScriptWizardPreviewDrawer previewDrawer;

		private string LastOptionsControlName
		{
			get
			{
				return isMonoBehaviour ? AddToControlName : TemplateControlName;
			}
		}

		private float PreviewWidth
		{
			get
			{
				return position.width - OptionsWidth;
			}
		}

		private float OptionsWidth
		{
			get
			{
				float remainingWidth = position.width - optionsPreferredWidth - previewMinWidth;
				if(remainingWidth >= 0f)
				{
					return optionsPreferredWidth;
				}
				return position.width * 0.5f;
			}
		}

		private NewScriptWindowSettings Settings
		{
			get
			{
				return preferencesAsset.createScriptWizard;
			}
		}

		private string GetBuiltinTemplateFullPath()
		{
			return Path.Combine(EditorApplication.applicationContentsPath, ResourcesTemplatePath);
		}

		private string GetCustomTemplateFullPath()
		{
			var templateDirs = Directory.GetDirectories(Application.dataPath, TemplateDirectoryName, SearchOption.AllDirectories);
			if(templateDirs.Length > 0)
			{
				return templateDirs[0];
			}

			Debug.LogWarning("CreateScriptWizardWindow Failed to locate templates directory \""+TemplateDirectoryName+"\" inside \""+ Application.dataPath+"\"");
			return "";
		}

		private void UpdateTemplateNamesAndTemplate()
		{
			// Remember old selected template name
			string oldSelectedTemplateName = null;
			if(templateNames != null && templateNames.Length > templateIndex)
			{
				oldSelectedTemplateName = templateNames[templateIndex];
			}

			// Get new template names
			templateNames = GetTemplateNames();

			// Select template
			if(templateNames.Length == 0)
			{
				scriptPrescription.SetTemplate(NoTemplateString);

				#if DEV_MODE
				Debug.LogWarning("baseClass = "+StringUtils.Null+" because templateNames list was empty.");
				#endif
			}
			else
			{
				if(oldSelectedTemplateName != null)
				{
					templateIndex = Array.IndexOf(templateNames, oldSelectedTemplateName);
					if(templateIndex == -1)
					{
						templateIndex = 0;
					}
				}
				else
				{
					templateIndex = 0;
				}

				scriptPrescription.SetTemplate(GetTemplate(templateNames[templateIndex]));
			}

			RebuildCachedState();
		}

		private void OnTemplateChanged()
		{
			SaveSelectedTemplate();

			// Add or remove "Editor" from directory path
			if(isEditorClass && !FileUtility.IsEditorPath(scriptOutputDirectoryLocalPathWithoutAssetsPrefix))
			{
				if(string.Equals(preferencesAsset.defaultScriptPath, scriptOutputDirectoryLocalPathWithoutAssetsPrefix))
				{
					scriptOutputDirectoryLocalPathWithoutAssetsPrefix = preferencesAsset.defaultEditorScriptPath;
				}
				else
				{
					scriptOutputDirectoryLocalPathWithoutAssetsPrefix = FileUtility.GetChildDirectory(scriptOutputDirectoryLocalPathWithoutAssetsPrefix, "Editor");
				}
			}

			// Move keyboard focus to relevant field
			if(isCustomEditor)
			{
				focusControl = ClassNameControlName;
			}

			RebuildCachedState();
			UpdateIncludedNamespaces(false);
		}

		private bool GetFunctionIsIncluded(string baseClassName, string functionName, bool includeByDefault)
		{
			string prefName = "PI.FunctionData_" + (baseClassName != null ? baseClassName + "_" : string.Empty) + functionName;
			return EditorPrefs.GetBool(prefName, includeByDefault);
		}

		private void SetFunctionIsIncluded(string baseClassName, string functionName, bool include)
		{
			string prefName = "PI.FunctionData_" + (baseClassName != null ? baseClassName + "_" : string.Empty) + functionName;
			EditorPrefs.SetBool(prefName, include);
			RebuildCode();
		}

		private bool GetNamespaceIsIncluded(string nameSpace, bool includeByDefault)
		{
			string prefName = "PI.Namespace_" + nameSpace;

			#if DEV_MODE && DEBUG_USINGS
			Debug.Log($"EditorPrefs.GetBool({prefName}):{StringUtils.ToColorizedString(EditorPrefs.GetBool(prefName, includeByDefault))}, CanRemove({nameSpace}):{CanRemoveUsing(nameSpace)}");
			#endif

			return EditorPrefs.GetBool(prefName, includeByDefault) || !CanRemoveUsing(nameSpace);
		}

		private string GetSingletonBaseClass()
		{
			string prefName = "PI.SingletonBaseClass";
			return EditorPrefs.GetString(prefName, "Singleton");
		}

		private void SetNamespaceIsIncluded(string nameSpace, bool include)
		{
			#if DEV_MODE && DEBUG_USINGS
			Debug.Log($"SetNamespaceIsIncluded({nameSpace}):{StringUtils.ToColorizedString(include)}");
			#endif

			string prefName = "PI.Namespace_" + nameSpace;
			EditorPrefs.SetBool(prefName, include);
		}

		/// <summary>
		/// Rebuilds scriptPrescription.stringReplacements, scriptPrescription.functions, isEditorClass, isCustomEditor and other cached state.
		/// </summary>
		private void RebuildCachedState()
		{
			if(templateNames.Length == 0)
			{
				return;
			}

			isEditorClass = IsEditorClass(baseClass);
			SetIsCustomEditor(baseClass == CustomEditorClassName);
			string templateName = GetTemplateName();

			SetIsMonoBehaviour(string.Equals(templateName, MonoBehaviourName));
			SetIsScriptableObject(string.Equals(templateName, ScriptableObjectName));
			SetIsSingleton(string.Equals(templateName, SingletonName));

			RebuildStringReplacements();

			RebuildFunctionsList();
		}

		private void RebuildStringReplacements()
		{
			scriptPrescription.stringReplacements.Clear();

			if(scriptableObjectCreateAssetMenu)
			{
				scriptPrescription.stringReplacements["$CreateAssetMenu"] = "true";
			}
			else
			{
				scriptPrescription.stringReplacements.Remove("$CreateAssetMenu");
			}

			if(scriptableObjectFilename.Length == 0)
			{
				scriptPrescription.stringReplacements.Remove("$Filename");
			}
			else
			{
				scriptPrescription.stringReplacements["$Filename"] = scriptableObjectFilename;
			}

			if(scriptableObjectMenuName.Length == 0)
			{
				scriptPrescription.stringReplacements.Remove("$MenuName");
			}
			else
			{
				scriptPrescription.stringReplacements["$MenuName"] = scriptableObjectMenuName;
			}

			scriptPrescription.stringReplacements["$TargetClassName"] = customEditorTargetClassName;

			if(scriptableObjectOrder == 0)
			{
				scriptPrescription.stringReplacements.Remove("$Order");
			}
			else
			{
				scriptPrescription.stringReplacements["$Order"] = StringUtils.ToString(scriptableObjectOrder);
			}

			scriptPrescription.stringReplacements["$BaseClassName"] = singletonBaseClassName.Length == 0 ? "Singleton" : singletonBaseClassName;
		}

		private void RebuildFunctionsList()
		{
			string functionsFileFullPath;
			if(!TryGetFunctionsFileFullPath(out functionsFileFullPath))
			{
				scriptPrescription.functions = null;
				return;
			}

			var reader = new StreamReader(functionsFileFullPath);
			var functionList = new List<FunctionData>();
			int lineNr = 1;
			while(!reader.EndOfStream)
			{
				string functionLine = reader.ReadLine();
				string functionLineWhole = functionLine;
				try
				{
					if(functionLine.StartsWith("header ", StringComparison.OrdinalIgnoreCase))
					{
						functionList.Add(new FunctionData(functionLine.Substring(7)));
						continue;
					}

					var function = new FunctionData();

					bool defaultInclude = false;
					if(functionLine.StartsWith("DEFAULT ", StringComparison.Ordinal))
					{
						defaultInclude = true;
						functionLine = functionLine.Substring(8);
					}

					if(functionLine.StartsWith("[", StringComparison.Ordinal))
					{
						int stop = functionLine.LastIndexOf(']', 0);
						if(stop > 0)
						{
							function.attribute = functionLine.Substring(0, stop+1);
						}
					}

					bool repeat;
					do
					{
						repeat = false;
						if(functionLine.StartsWith("override ", StringComparison.Ordinal))
						{
							function.prefix += "override ";
							functionLine = functionLine.Substring(9);
							repeat = true;
						}

						if(functionLine.StartsWith("abstract ", StringComparison.Ordinal))
						{
							function.prefix += "abstract ";
							functionLine = functionLine.Substring(9);
							repeat = true;
						}

						if(functionLine.StartsWith("virtual ", StringComparison.Ordinal))
						{
							function.prefix += "virtual ";
							functionLine = functionLine.Substring(8);
							repeat = true;
						}

						if(functionLine.StartsWith("private ", StringComparison.Ordinal))
						{
							function.prefix += "private ";
							functionLine = functionLine.Substring(8);
							repeat = true;
						}

						if(functionLine.StartsWith("protected ", StringComparison.Ordinal))
						{
							function.prefix += "protected ";
							functionLine = functionLine.Substring(10);
							repeat = true;
						}

						if(functionLine.StartsWith("public ", StringComparison.Ordinal))
						{
							function.prefix += "public ";
							functionLine = functionLine.Substring(7);
							repeat = true;
						}

						if(functionLine.StartsWith("internal ", StringComparison.Ordinal))
						{
							function.prefix += "internal ";
							functionLine = functionLine.Substring(9);
							repeat = true;
						}
					}
					while(repeat);

					string returnTypeString = GetStringUntilSeparator(ref functionLine, " ");

					// constructor
					if(string.Equals(returnTypeString, "$ClassName()"))
					{
						function.returnType = null;
						function.name = "Constructor";
						function.comment = "";
						function.parameters = new ParameterData[0];
						function.isMethod = true;

						GetStringUntilSeparator(ref functionLine, ")");

						function.comment = functionLine;

						functionList.Add(function);
						lineNr++;

						#if DEV_MODE && DEBUG_FUNCTIONS
						Debug.Log($"Adding function: returnType:{function.returnType}, name:{function.name}, comment:{function.comment}");
						#endif

						continue;
					}
					// not constructor
					else
					{
						// Maybe a method...
						function.returnType = (string.Equals(returnTypeString, "void") ? null : returnTypeString);
						function.name = GetStringUntilSeparator(ref functionLine, "(");
						
						// Not a methods (e.g. enum value name).
						if(function.name == null)
						{
							function.name = function.returnType + functionLine;
							function.returnType = null;
							function.comment = "";
							function.parameters = new ParameterData[0];
							function.isMethod = false;
							functionList.Add(function);
							lineNr++;

							#if DEV_MODE && DEBUG_FUNCTIONS
							Debug.Log($"Adding function: returnType:{function.returnType}, name:{function.name}, comment:{function.comment}");
							#endif

							continue;
						}
					}

					function.isMethod = true;

					string parameterString = GetStringUntilSeparator(ref functionLine, ")");
					if(function.returnType != null)
					{
						function.returnDefault = GetStringUntilSeparator(ref functionLine, ";");
					}

					function.comment = functionLine;

					var parameterStrings = parameterString.Split(ArrayExtensions.TempCharArray(','), StringSplitOptions.RemoveEmptyEntries);
					var parameterList = new List<ParameterData>();
					for(int i = 0; i < parameterStrings.Length; i++)
					{
						var paramSplit = parameterStrings[i].Trim().Split(' ');
						parameterList.Add(new ParameterData(paramSplit[1], paramSplit[0]));
					}
					function.parameters = parameterList.ToArray();

					function.include = GetFunctionIsIncluded(baseClass, function.name, defaultInclude);

					#if DEV_MODE && DEBUG_FUNCTIONS
					Debug.Log($"Adding function: returnType:{function.returnType}, name:{function.name}, comment:{function.comment}, included:{function.include}");
					#endif

					functionList.Add(function);
				}
				catch(Exception e)
				{
					Debug.LogWarning("Malformed function line: \"" + functionLineWhole + "\"\n  at " + functionsFileFullPath + ":" + lineNr + "\n" + e);
				}
				lineNr++;
			}
			scriptPrescription.functions = functionList.ToArray();
		}

		private bool TryGetFunctionsFileFullPath(out string functionsFileFullPath)
		{
			string baseName = baseClass != null ? baseClass : GetTemplateName();
			string fileName = baseName + ".functions.txt";

			// Try to find function file first in custom templates folder and then in built-in
			functionsFileFullPath = Path.Combine(GetCustomTemplateFullPath(), fileName);

			if(File.Exists(functionsFileFullPath))
			{
				return true;
			}

			functionsFileFullPath = Path.Combine(GetBuiltinTemplateFullPath(), fileName);
			if(File.Exists(functionsFileFullPath))
			{
				return true;
			}
			functionsFileFullPath = null;
			return false;
		}

		private void SetIsCustomEditor(bool setIsCustomEditor)
		{
			if(isCustomEditor != setIsCustomEditor)
			{
				isCustomEditor = setIsCustomEditor;
				customEditorTargetClassDoesNotExist = CustomEditorTargetClassDoesNotExist();
				customEditortargetClassIsNotValidType = CustomEditorTargetClassIsNotValidType();
			}
		}

		private void SetIsMonoBehaviour(bool setIsMonoBehaviour)
		{
			isMonoBehaviour = setIsMonoBehaviour;
		}

		private void SetIsScriptableObject(bool setIsScriptableObject)
		{
			isScriptableObject = setIsScriptableObject;
		}

		private void SetIsSingleton(bool setIsSingleton)
		{
			isSingleton = setIsSingleton;
		}

		private string GetStringUntilSeparator(ref string source, string sep)
		{
			int index = source.IndexOf(sep);
			if(index == -1)
			{
				return null;
			}
			string result = source.Substring(0, index).Trim();
			source = source.Substring(index + sep.Length).Trim(' ');
			return result;
		}

		private string GetTemplate(string templateName)
		{
			string filename = templateName + ".cs.txt";
			string path = Path.Combine(GetCustomTemplateFullPath(), filename);
			if(File.Exists(path))
			{
				return File.ReadAllText(path);
			}

			path = Path.Combine(GetBuiltinTemplateFullPath(), filename);
			if(File.Exists(path))
			{
				return File.ReadAllText(path);
			}

			return NoTemplateString;
		}

		private string GetTemplateName()
		{
			if(templateNames.Length == 0)
			{
				return NoTemplateString;
			}
			return templateNames[templateIndex];
		}

		// Custom comparer to sort templates alphabetically,
		// but put certain items as the first elements
		private class TemplateNameComparer : IComparer<string>
		{
			private int GetRank(string s)
			{
				switch(s)
				{
					case MonoBehaviourName:
						return 0;
					case ScriptableObjectName:
						return 1;
					case PlainClassName:
						return 2;
					case StructName:
						return 3;
					case EnumName:
						return 4;
					case InterfaceName:
						return 5;
					case SingletonName:
						return 6;
					default:
						return s.StartsWith(TempEditorClassPrefix) ? 100 : 7;
				}
			}

			public int Compare(string x, string y)
			{
				int rankX = GetRank(x);
				int rankY = GetRank(y);
				if(rankX == rankY)
				{
					return x.CompareTo(y);
				}
				return rankX.CompareTo(rankY);
			}
		}

		private string[] GetTemplateNames()
		{
			var templatesNames = new List<string>();

			// Get all file names of custom templates
			if(Directory.Exists(GetCustomTemplateFullPath()))
			{
				templatesNames.AddRange(Directory.GetFiles(GetCustomTemplateFullPath()));
			}

			// Get all file names of built-in templates
			if(Directory.Exists(GetBuiltinTemplateFullPath()))
			{
				templatesNames.AddRange(Directory.GetFiles(GetBuiltinTemplateFullPath()));
			}

			if(templatesNames.Count == 0)
			{
				return new string[0];
			}

			// Filter and clean up list
			templatesNames = templatesNames
				.Distinct()
				.Where(f => (f.EndsWith(".cs.txt")))
				.Select(f => Path.GetFileNameWithoutExtension(f.Substring(0, f.Length - 4)))
				.ToList();

			// Determine which scripts have editor class base class
			for(int i = 0; i < templatesNames.Count; i++)
			{
				string templateContent = GetTemplate(templatesNames[i]);
				string templateBaseClass;
				if(ScriptTemplateUtility.TryGetBaseClass(templateContent, out templateBaseClass) && IsEditorClass(templateBaseClass))
				{
					templatesNames[i] = TempEditorClassPrefix + templatesNames[i];
				}
			}

			// Order list
			templatesNames = templatesNames.OrderBy(f => f, new TemplateNameComparer()).ToList();

			// Insert separator before first editor script template
			bool inserted = false;
			for(int i = 0; i < templatesNames.Count; i++)
			{
				if(templatesNames[i].StartsWith(TempEditorClassPrefix))
				{
					templatesNames[i] = templatesNames[i].Substring(TempEditorClassPrefix.Length);
					if(!inserted)
					{
						templatesNames.Insert(i, string.Empty);
						inserted = true;
					}
				}
			}
			
			return templatesNames.ToArray();
		}
		
		#if !DISABLE_POWER_INSPECTOR_MENU_ITEMS && !DISABLE_SCRIPT_WIZARD_MENU_ITEM
		[MenuItem(PowerInspectorMenuItemPaths.CreateScriptWizardFromCreateMenu, false, PowerInspectorMenuItemPaths.CreateScriptWizardFromCreateMenuPrority), UsedImplicitly]
		private static void OpenFromAssetsMenu()
		{
			var selected = Selection.activeObject;
			if(selected != null && !selected.IsSceneObject())
			{
				var path = AssetDatabase.GetAssetPath(selected);

				#if DEV_MODE && DEBUG_OPEN
				Debug.Log("Selected: "+selected.name+" ("+selected.GetType().Name+") @ "+ path+", IsFolder="+ AssetDatabase.IsValidFolder(path));
				#endif

				if(!AssetDatabase.IsValidFolder(path))
				{
					path = FileUtility.GetParentDirectory(path);

					var script = selected as MonoScript;
					if(script != null)
					{
						var scriptType = script.GetClass();
						if(scriptType != null)
						{
							var inNamespace = scriptType.Namespace;
							if(!string.IsNullOrEmpty(inNamespace))
							{
								Platform.Active.SetPrefs("PI.CreateScriptWizard/Namespace", inNamespace);
							}
						}
					}
				}
				else
				{
					var scriptsInsideFolder = AssetDatabase.FindAssets("t:MonoScript", new string[]{ path });
					for(int n = 0, count = Mathf.Min(scriptsInsideFolder.Length, 3); n < count; n++)
					{
						var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(scriptsInsideFolder[n]));
						if(script != null)
						{
							var scriptType = script.GetClass();
							if(scriptType != null)
							{
								var inNamespace = scriptType.Namespace;
								if(!string.IsNullOrEmpty(inNamespace))
								{
									Platform.Active.SetPrefs("PI.CreateScriptWizard/Namespace", inNamespace);
									break;
								}
							}
						}
					}
				}

				Platform.Active.SetPrefs("PI.CreateScriptWizard/SaveIn", AssetPathWithoutAssetPrefix(path));
			}

			Init();
		}
		#endif

		private static void Init()
		{
			var window = GetWindow<CreateScriptWizardWindow>(true, "Create Script Wizard");
			var pos = window.position;
			pos.x = Screen.currentResolution.width * 0.5f - pos.width * 0.5f;
			pos.y = Screen.currentResolution.height * 0.5f - pos.height * 0.5f;
			window.position = pos;
			window.Repaint();
		}

		public CreateScriptWizardWindow()
		{
			// Large initial size
			position = new Rect(50, 50, 770, 500);
			// But allow to scale down to smaller size
			minSize = new Vector2(550, 480);

			scriptPrescription = new ScriptPrescription();
		}

		[UsedImplicitly]

		private void OnFocus()
		{
			PowerInspectorDocumentation.ShowFeatureIfWindowOpen("create-script-wizard");
		}

		[UsedImplicitly]
		private void OnEnable()
		{
			optionsPreferredWidth = EditorPrefs.GetFloat(ScriptBuilder.InspectorWidth, optionsPreferredWidth);

			#if UNITY_2019_3_OR_NEWER
			directoryBrowseIcon = EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_Folder Icon" : "Folder Icon");
			#else // todo: figure out what are the icon names on classic skin?
			directoryBrowseIcon = EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_Folder Icon" : "Folder Icon");
			#endif
		}

		private void Setup()
		{
			setupDone = true;

			bool hasExistingCode = !string.IsNullOrEmpty(code);

			Fonts.SetupIfNotAlreadyDone();

			UpdateTemplateNamesAndTemplate();
			OnSelectionChange();

			preferencesAsset = InspectorUtility.Preferences;
			if(!preferencesAsset.SetupDone)
			{
				preferencesAsset.Setup();
			}

			if(preferencesAsset != null)
			{
				preferencesAsset.onSettingsChanged += OnSettingsChanged;
			}
			#if DEV_MODE
			else { Debug.LogWarning("CreateScriptWizardWindow.OnEnable - failed to find Inspector Preferences Asset!"); }
			#endif

			LoadSettings();

			if(EditorPrefs.HasKey(ScriptBuilder.Name))
			{
				SetClassName(EditorPrefs.GetString(ScriptBuilder.Name), false);
				EditorPrefs.DeleteKey(ScriptBuilder.Name);
			}

			if(EditorPrefs.HasKey(ScriptBuilder.Namespace))
			{
				SetNamespace(EditorPrefs.GetString(ScriptBuilder.Namespace), false);
				EditorPrefs.DeleteKey(ScriptBuilder.Namespace);
			}

			if(EditorPrefs.HasKey("PI.SingletonBaseClass"))
			{
				singletonBaseClassName = EditorPrefs.GetString("PI.SingletonBaseClass");
			}
			else
			{
				singletonBaseClassName = "Singleton";
			}

			var template = EditorPrefs.GetString(ScriptBuilder.Template, "");
			if(template.Length > 0)
			{
				templateIndex = Array.IndexOf(templateNames, template);
				if(templateIndex == -1)
				{
					templateIndex = 0;
				}
			}

			int attachToById = EditorPrefs.GetInt(ScriptBuilder.AttachTo, 0);
			if(attachToById != 0)
			{
				gameObjectToAddTo = EditorUtility.InstanceIDToObject(attachToById) as GameObject;
			}

			scriptOutputDirectoryLocalPathWithoutAssetsPrefix = EditorPrefs.GetString(ScriptBuilder.SaveIn, "");

			UpdateTemplateNamesAndTemplate();
			OnTemplateChanged();

			if(hasExistingCode)
			{
				UpdateCodePreview(code);
			}
			else
			{
				RebuildCode();
			}
		}

		[UnityEditor.Callbacks.DidReloadScripts, UsedImplicitly]
		private static void OnScriptsReloaded()
		{
			if(!EditorPrefs.HasKey(ScriptBuilder.CreatedAtPath))
			{
				return;
			}

			var createdScriptPath = EditorPrefs.GetString(ScriptBuilder.CreatedAtPath, "");
			if(createdScriptPath.Length > 0)
			{
				var createdScript = AssetDatabase.LoadAssetAtPath<MonoScript>(createdScriptPath);
				if(createdScript != null)
				{
					#if DEV_MODE && DEBUG_SELECT
					Debug.Log("Select("+StringUtils.ToString(createdScript)+")");
					#endif

					Selection.activeObject = createdScript;
					AssetDatabase.OpenAsset(createdScript, 0);
				}
			}

			EditorPrefs.DeleteKey(ScriptBuilder.CreatedAtPath);
		}

		private void UpdateIncludedNamespaces(bool rebuildCode)
		{
			if(preferencesAsset == null)
			{
				return;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(reusableStringList.Count == 0);
			#endif

			var list = reusableStringList;
			for(int n = 0, count = namespacesList.Length; n < count; n++)
			{
				var item = namespacesList[n];
				if(GetNamespaceIsIncluded(item, Settings.usingNamespaceOptions[n].StartsWith("*", StringComparison.Ordinal)))
				{
					#if DEV_MODE && DEBUG_USINGS
					Debug.Log($"<color=green>Adding using {item}</color> with template={GetTemplateName()}.");
					#endif
					list.Add(item);
				}
			}

			if(scriptPrescription.usingNamespaces != null && scriptPrescription.usingNamespaces.ContentsMatch(list))
			{
				list.Clear();
				return;
			}

			scriptPrescription.usingNamespaces = list.ToArray();
			list.Clear();

			if(rebuildCode)
			{
				RebuildCode();
			}
		}

		private void LoadSettings()
		{
			if(preferencesAsset == null)
			{
				return;
			}

			var settings = Settings;
			curlyBracesOnNewLine = settings.curlyBracesOnNewLine;
			addComments = settings.addComments;
			addCommentsAsSummary = settings.addCommentsAsSummary;
			wordWrapCommentsAfterCharacters = settings.wordWrapCommentsAfterCharacters;
			addUsedImplicitly = settings.addUsedImplicitly;
			spaceAfterMethodName = settings.spaceAfterMethodName;
			newLine = settings.NewLine;
			namespacesList = settings.usingNamespaceOptions;
			for(int n = namespacesList.Length - 1; n >= 0; n--)
			{
				if(namespacesList[n].StartsWith("*", StringComparison.Ordinal))
				{
					namespacesList[n] = namespacesList[n].Substring(1);
				}
			}
		}

		[UsedImplicitly]
		private void OnGUI()
		{
			if(styles == null)
			{
				styles = new Styles();
			}

			if(!setupDone)
			{
				Setup();
			}

			if(Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseDown)
			{
				GUI.changed = true;
				resizing = false;

				if(Event.current.mousePosition.x > PreviewWidth || Event.current.mousePosition.y < PreviewTitleHeight)
				{
					previewDrawer.SetSelectedLine(-1);
				}
			}
			
			if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && canCreate && !DrawGUI.EditingTextField && previewDrawer.SelectedLine == -1)
			{
				Create();
			}

			float labelWidthWas = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 85f;

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Space(PreviewWidth);

				EditorGUILayout.BeginVertical();
				{
					OptionsGUI();

					GUILayout.Space(10);

					CreateAndCancelButtonsGUI();
				}
				EditorGUILayout.EndVertical();

				GUILayout.Space(10);
			}
			EditorGUILayout.EndHorizontal();

			EditorGUIUtility.labelWidth = labelWidthWas;

			GUILayout.Space(10);

			PreviewGUI();

			// Clear keyboard focus if clicking a random place inside the dialog, or if ClearKeyboardControl flag is set.
			if(clearKeyboardControl || (Event.current.type == EventType.MouseDown && Event.current.button == 0))
			{
				KeyboardControlUtility.KeyboardControl = 0;
				clearKeyboardControl = false;
				Repaint();
			}

			if(Event.current.type == EventType.MouseDown && Event.current.button == 1)
			{
				var rect = position;
				rect.x = 0f;
				rect.y = 0f;
				if(rect.Contains(Event.current.mousePosition))
				{
					DrawGUI.Use(Event.current);
					var menu = Menu.Create();
					menu.Add("Edit Preferences", ShowPreferences);
					ContextMenuUtility.Open(menu, null);
				}
			}

			if(focusControl.Length > 0)
			{
				if(Event.current.type == EventType.Repaint)
				{
					DrawGUI.FocusControl(focusControl);
					focusedControl = focusControl;
					focusControl = "";
				}
				Repaint();
			}
		}

		private void OnKeyboardInputGiven(Event inputEvent)
		{
			if(previewDrawer.SelectedLine != -1)
			{
				return;
			}

			switch(inputEvent.keyCode)
			{
				case KeyCode.Return:
					if(canCreate && !DrawGUI.EditingTextField)
					{
						Create();
					}
					break;
				case KeyCode.Tab:
					if(!inputEvent.control && !inputEvent.alt)
					{
						if(inputEvent.shift)
						{
							switch(focusedControl)
							{
								default:
									focusControl = NamespaceControlName;
									break;
								case NamespaceControlName:
									previewDrawer.SetSelectedLine(0);
									break;
								case ClassNameControlName:
									focusControl = NamespaceControlName;
									break;
								case SavePathControlName:
									focusControl = ClassNameControlName;
									break;
								case TemplateControlName:
									focusControl = SavePathControlName;
									break;
								case AddToControlName:
								case EditorTargetControlName:
								case ScriptableObjectCreateAssetMenuControlName:
									focusControl = TemplateControlName;
									break;
								case ScriptableObjectMenuNameControlName:
									focusControl = ScriptableObjectFilenameControlName;
									break;
								case ScriptableObjectFilenameControlName:
									focusControl = ScriptableObjectCreateAssetMenuControlName;
									break;
							}
						}
						
						switch(focusedControl)
						{
							default:
								focusControl = NamespaceControlName;
								break;
							case NamespaceControlName:
								focusControl = ClassNameControlName;
								break;
							case ClassNameControlName:
								focusControl = SavePathControlName;
								break;
							case SavePathControlName:
								focusControl = TemplateControlName;
								break;
							case TemplateControlName:
								if(isMonoBehaviour)
								{
									focusControl = AddToControlName;
								}
								else if(isCustomEditor)
								{
									focusControl = EditorTargetControlName;
								}
								else if(isScriptableObject)
								{
									focusControl = ScriptableObjectCreateAssetMenuControlName;
								}
								else
								{
									previewDrawer.SetSelectedLine(0);
								}
								break;
							case ScriptableObjectCreateAssetMenuControlName:
								focusControl = ScriptableObjectFilenameControlName;
								break;
							case ScriptableObjectFilenameControlName:
								focusControl = ScriptableObjectMenuNameControlName;
								break;
							case AddToControlName:
							case EditorTargetControlName:
							case ScriptableObjectMenuNameControlName:
								previewDrawer.SetSelectedLine(0);
								break;
						}
					}
					break;
			}
		}

		private bool CanCreate()
		{
			return scriptPrescription.className.Length > 0 &&
				!scriptAtPathAlreadyExists &&
				!classAlreadyExists &&
				!classNameIsInvalid &&
				!customEditorTargetClassDoesNotExist &&
				!customEditortargetClassIsNotValidType &&
				!invalidTargetPath &&
				!invalidTargetPathForEditorScript &&
				!namespaceIsInvalid;
		}

		private void Create()
		{
			if(isSingleton && !OnCreatingSingleton())
			{
				return;
			}

			EditorPrefs.SetString(ScriptBuilder.CreatedAtPath, GetScriptOutputLocalFilePath());

			string fullFilePath = GetScriptOutputLocalFilePath();

			CreateScript(fullFilePath, code, true);

			if(CanAddComponent())
			{
				var addScriptMethod = typeof(UnityEditorInternal.InternalEditorUtility).GetMethod("AddScriptComponentUncheckedUndoable", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
				var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(fullFilePath);
				if(monoScript != null)
				{
					addScriptMethod.Invoke(null, new Object[] { gameObjectToAddTo, monoScript });
				}
			}

			Close();
			GUIUtility.ExitGUI();
		}

		private bool OnCreatingSingleton()
		{
			string baseClassName = singletonBaseClassName;
			if(baseClassName.Length == 0)
			{
				baseClassName = "Singleton";
			}

			var baseClassType = GetSingletonType(baseClassName + "`1");
			if(baseClassType != null)
			{
				return true;
			}

			if(!EditorUtility.DisplayDialog
			("Create Singleton Base Class?",
			"Base class by name " + baseClassName + " was not found. Would you like to generate a base class for Singletons?",
			"Create Base Class", "Cancel"))
			{
				return false;
			}

			CreateSingletonBaseClass(baseClassName);

			return true;
		}

		[CanBeNull]
		private static Type GetSingletonType(string typeDefinition)
		{
			if(typeDefinition.IndexOf('.') != -1)
			{
				return TypeExtensions.GetAllTypesThreadSafe(true, false, false).Where((t) => string.Equals(t.FullName, typeDefinition)).FirstOrDefault();
			}
			return TypeExtensions.GetAllTypesThreadSafe(true, false, false).Where((t) => string.Equals(t.Name, typeDefinition)).FirstOrDefault();
		}

		private void CreateSingletonBaseClass(string baseClassName)
		{
			int namespaceEnd = baseClassName.LastIndexOf('.');
			string nameSpace;
			if(namespaceEnd == -1)
			{
				nameSpace = scriptPrescription.nameSpace;
			}
			else
			{
				nameSpace = baseClassName.Substring(0, namespaceEnd);
				baseClassName = baseClassName.Substring(namespaceEnd + 1);
			}

			var template = GetSingletonBaseClassTemplate();
			if(string.IsNullOrEmpty(template))
			{
				return;
			}

			var prescription = new ScriptPrescription()
			{
				nameSpace = nameSpace,
				className = baseClassName,
				template = template,
				baseClass = "MonoBehaviour",
				usingNamespaces = new string[] { "System", "System.ComponentModel", "System.Threading", "UnityEngine", "JetBrains.Annotations" }
			};

			string baseClassCode;
			using(var scriptGenerator = new ScriptBuilder(prescription, curlyBracesOnNewLine, addComments, addCommentsAsSummary, wordWrapCommentsAfterCharacters, addUsedImplicitly, spaceAfterMethodName, newLine))
			{
				baseClassCode = scriptGenerator.ToString();
			}

			var filePath = GetScriptOutputLocalFilePath(baseClassName);
			CreateScript(filePath, baseClassCode, false);
		}

		private string GetSingletonBaseClassTemplate()
		{
			string filename = "Singleton.cs.base.txt";
			string path = Path.Combine(GetCustomTemplateFullPath(), filename);
			if(File.Exists(path))
			{
				return File.ReadAllText(path);
			}

			string builtInPath = Path.Combine(GetBuiltinTemplateFullPath(), filename);
			if(File.Exists(builtInPath))
			{
				return File.ReadAllText(builtInPath);
			}

			EditorUtility.DisplayDialog
				("Template Not Found",
				"Can't generate singleton base class because failed to find template for it at path:\n" + path + ".",
				"Ok");
			return "";
		}

		private string GetFullClassName()
		{
			if(scriptPrescription.nameSpace.Length > 0)
			{
				return string.Concat(scriptPrescription.nameSpace, ".", scriptPrescription.className);
			}
			return scriptPrescription.className;
		}

		private void CreateAndCancelButtonsGUI()
		{
			// Create string to tell the user what the problem is
			string blockReason = string.Empty;
			if(!canCreate && scriptPrescription.className.Length > 0)
			{
				if(scriptAtPathAlreadyExists)
				{
					blockReason = "A script called \"" + GetFullClassName() + "\" already exists at that path.";
				}
				else if(classAlreadyExists)
				{
					blockReason = "A class called \"" + GetFullClassName() + "\" already exists.";
				}
				else if(classNameIsInvalid)
				{
					blockReason = "The script name may only consist of a-z, A-Z, 0-9, _.";
				}
				else if(customEditorTargetClassDoesNotExist)
				{
					if(customEditorTargetClassName.Length == 0)
					{
						blockReason = "Fill in the script component to make an editor for.";
					}
					else
					{
						blockReason = "A class called \"" + customEditorTargetClassName + "\" could not be found.";
					}
				}
				else if(customEditortargetClassIsNotValidType)
				{
					blockReason = "The class \"" + customEditorTargetClassName + "\" is not of type UnityEngine.Object.";
				}
				else if(invalidTargetPath)
				{
					blockReason = "The folder path contains invalid characters.";
				}
				else if(invalidTargetPathForEditorScript)
				{
					blockReason = "Editor scripts should be stored in a folder called Editor.";
				}
			}

			// Warning about why the script can't be created
			if(blockReason.Length > 0)
			{
				styles.warningContent.text = blockReason;
				GUILayout.BeginHorizontal();
				{
					GUI.color = Color.red;
					GUILayout.Label(styles.warningContent, EditorStyles.wordWrappedMiniLabel);
					GUI.color = Color.white;
				}
				GUILayout.EndHorizontal();
			}

			// Cancel and create buttons
			GUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();

				if(GUILayout.Button("Cancel", GUILayout.Width(ButtonWidth)))
				{
					Close();
					GUIUtility.ExitGUI();
				}

				bool guiEnabledTemp = GUI.enabled;
				GUI.enabled = canCreate;
				if(GUILayout.Button(GetCreateButtonText(), GUILayout.Width(ButtonWidth)))
				{
					Create();
				}
				GUI.enabled = guiEnabledTemp;
			}
			GUILayout.EndHorizontal();
		}

		private bool CanAddComponent()
		{
			return (gameObjectToAddTo != null && baseClass == MonoBehaviourName);
		}

		private void OptionsGUI()
		{
			EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
			{
				NamespaceGUI();
				
				GUILayout.Space(10f);
				
				NameGUI();

				GUILayout.Space(10f);

				TargetPathGUI();

				GUILayout.Space(20f);

				bool isMonoBehaviour = string.Equals(GetTemplateName(), MonoBehaviourName);
				if(!isMonoBehaviour)
				{
					GUI.SetNextControlName(LastOptionsControlName);
				}
				TemplateSelectionGUI();

				if(isMonoBehaviour)
				{
					GUILayout.Space(10f);
					AttachToGUI();
				}
				else if(isCustomEditor)
				{
					GUILayout.Space(10f);
					CustomEditorTargetClassNameGUI();
				}
				else if(isScriptableObject)
				{
					GUILayout.Space(15f);
					ScriptableObjectCreateAssetMenuGUI();
					if(scriptableObjectCreateAssetMenu)
					{
						GUILayout.Space(5f);
						ScriptableObjectFilenameGUI();
						GUILayout.Space(5f);
						ScriptableObjectMenuNameGUI();
						GUILayout.Space(5f);
						ScriptableObjectOrderGUI();
					}
				}
				else if(isSingleton)
				{
					GUILayout.Space(5f);
					SingletonBaseClassGUI();
				}

				GUILayout.Space(10f);

				UsingGUI();
				FunctionsGUI();
			}
			EditorGUILayout.EndVertical();
		}

		private bool FunctionHeader(string header, bool expandedByDefault)
		{
			GUILayout.Space(5);
			bool expanded = GetFunctionIsIncluded(baseClass, header, expandedByDefault);
			bool expandedNew = GUILayout.Toggle(expanded, header, EditorStyles.foldout);
			if(expandedNew != expanded)
			{
				SetFunctionIsIncluded(baseClass, header, expandedNew);
			}

			return expandedNew;
		}

		private void UsingGUI()
		{
			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Label("Using", GUILayout.Width(LabelWidth - 4));

				if(Event.current.type == EventType.MouseDown && Event.current.button == 1)
				{
					var lastRect = GUILayoutUtility.GetLastRect();
					if(lastRect.Contains(Event.current.mousePosition))
					{
						DrawGUI.Use(Event.current);
						var menu = Menu.Create();
						menu.Add("Edit Using List", ShowUsingListInPreferences);
						ContextMenuUtility.Open(menu, null);
					}
				}

				EditorGUILayout.BeginVertical(styles.loweredBox);
				{
					for(int i = 0, count = namespacesList.Length; i < count; i++)
					{
						var item = namespacesList[i];
						int foundIndex = Array.IndexOf(scriptPrescription.usingNamespaces, item);
						var isUsing = foundIndex != -1;
						var rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.toggle);

						bool setUsing;
						if(!CanRemoveUsing(item))
						{
							GUI.enabled = false;
							GUI.Toggle(rect, true, item);
							setUsing = true;
							GUI.enabled = true;
						}
						else
						{
							setUsing = GUI.Toggle(rect, isUsing, item);
						}

						if(setUsing != isUsing)
						{
							// Only save using state if the control is actually enabled.
							// Otherwise some namespaces would become ticked all the time
							// from just changing the active template.
							if(CanRemoveUsing(item))
							{
								SetNamespaceIsIncluded(item, setUsing);
							}
							UpdateIncludedNamespaces(true);
						}
					}
				}
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndHorizontal();
		}

		private bool CanRemoveUsing(string usingNamespace)
		{
			#if DEV_MODE && DEBUG_USINGS
			Debug.Log($"CanRemoveUsing(\"{usingNamespace}\") with baseClass={baseClass}...");
			#endif

			switch(usingNamespace)
			{
				case "JetBrains.Annotations":
					return !AnnotationsNamespaceRequired();
				case "UnityEngine":
					switch(baseClass)
					{
						case "MonoBehaviour":
						case "ScriptableObject":

							#if DEV_MODE && DEBUG_USINGS
							Debug.Log("CanRemoveUsing(\""+usingNamespace+"\"): "+StringUtils.False);
							#endif

							return false;
						case "AssetPostprocessor":
							return !GetFunctionIsIncluded("AssetPostprocessor", "OnPostProcessTexture", false);
						default:
							return true;
					}
				case "UnityEditor":
					switch(baseClass)
					{
						case "AssetPostprocessor":
						case "Editor":
						case "EditorWindow":
						case "ScriptableWizard":
						case "CustomEditor":
							return false;
					}
					switch(scriptPrescription.template)
					{
						case "Menu Item":
							return false;
						default:
							return true;
					}
				default:
					return true;
			}
		}

		private bool AnnotationsNamespaceRequired()
		{
			if(!addUsedImplicitly)
			{
				return false;
			}

			switch(templateNames[templateIndex])
			{
				case ScriptableObjectName:
				case MonoBehaviourName:
				case "Asset Postprocessor":
				case "Custom Editor":
				case "Editor Window":
					for(int n = scriptPrescription.functions.Length - 1; n >= 0; n--)
					{
						if(scriptPrescription.functions[n].include)
						{
							#if DEV_MODE && DEBUG_USINGS
							Debug.Log($"AnnotationsNamespaceRequired <color=green>true</color> with template={GetTemplateName()} because function {scriptPrescription.functions[n]} was included.");
							#endif

							return true;
						}
					}

					#if DEV_MODE && DEBUG_USINGS
					Debug.Log($"AnnotationsNamespaceRequired <color=red>false</color> with template={GetTemplateName()} because no functions included.");
					#endif

					return false;
				case PlainClassName:
				case StructName:
				case EnumName:
				case InterfaceName:
					return false;
				case "Menu Item":
					return true;
				default:
					return false;
			}
		}

		private void FunctionsGUI()
		{
			if(scriptPrescription.functions == null)
			{
				GUILayout.FlexibleSpace();
				return;
			}

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Label("Functions", GUILayout.Width(LabelWidth - 4));

				if(Event.current.type == EventType.MouseDown && Event.current.button == 1)
				{
					var lastRect = GUILayoutUtility.GetLastRect();
					if(lastRect.Contains(Event.current.mousePosition))
					{
						DrawGUI.Use(Event.current);
						var menu = Menu.Create();
						menu.Add("Edit Functions", ShowCurrentTemplateFunctionsInExplorer);
						ContextMenuUtility.Open(menu, null);
					}
				}

				EditorGUILayout.BeginVertical(styles.loweredBox);
				optionsScroll = EditorGUILayout.BeginScrollView(optionsScroll);
				{
					bool hasHeader = false;
					bool expanded = false;

					for(int i = 0, count = scriptPrescription.functions.Length; i < count; i++)
					{
						var func = scriptPrescription.functions[i];
						if(func.IsHeader)
						{
							expanded = FunctionHeader(func.HeaderName, false);
							hasHeader = true;
							continue;
						}

						if(!hasHeader)
						{
							expanded = FunctionHeader("General", true);
							hasHeader = true;
						}

						if(!expanded)
						{
							continue;
						}

						var toggleRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.toggle);
						toggleRect.x += 15;
						toggleRect.width -= 15;
						bool include = GUI.Toggle(toggleRect, func.include, new GUIContent(func.name, func.comment));
						if(include != func.include)
						{
							DrawGUI.EditingTextField = false;
							scriptPrescription.functions[i].include = include;
							SetFunctionIsIncluded(baseClass, func.name, include);
							RebuildCode();
						}
					}
				}
				EditorGUILayout.EndScrollView();
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndHorizontal();
		}

		private bool BeginFunctionsList()
		{
			EditorGUILayout.BeginVertical(styles.loweredBox);
			optionsScroll = EditorGUILayout.BeginScrollView(optionsScroll);
			return FunctionHeader("General", true);
		}

		private void AttachToGUI()
		{
			GUILayout.BeginHorizontal();
			{
				GUI.SetNextControlName(LastOptionsControlName);

				gameObjectToAddTo = EditorGUILayout.ObjectField("Add To", gameObjectToAddTo, typeof(GameObject), true) as GameObject;

				if(ClearButton())
				{
					gameObjectToAddTo = null;
				}
			}
			GUILayout.EndHorizontal();

			HelpField("Click a GameObject or Prefab to select.");
		}

		private void SetClassNameBasedOnTargetClassName()
		{
			if(customEditorTargetClassName.Length == 0)
			{
				SetClassName(string.Empty, true);
			}
			else
			{
				SetClassName(customEditorTargetClassName + "Editor", true);
			}
		}

		private void CustomEditorTargetClassNameGUI()
		{
			GUI.SetNextControlName(EditorTargetControlName);

			if(customEditorTargetClassDoesNotExist)
			{
				GUI.color = Color.red;
			}
			string newName = EditorGUILayout.TextField("Editor for", customEditorTargetClassName);
			GUI.color = Color.white;
			scriptPrescription.stringReplacements["$TargetClassName"] = newName;
			SetCustomEditorTargetClassName(newName, true);

			HelpField("Script component to make an editor for.");
		}

		private void ScriptableObjectCreateAssetMenuGUI()
		{
			GUI.SetNextControlName(ScriptableObjectCreateAssetMenuControlName);
			bool set = EditorGUILayout.Toggle(CreateAssetMenuLabel, scriptableObjectCreateAssetMenu);
			if(set)
			{
				scriptPrescription.stringReplacements["$CreateAssetMenu"] = "true";
			}
			else
			{
				scriptPrescription.stringReplacements.Remove("$CreateAssetMenu");
			}
			if(set != scriptableObjectCreateAssetMenu)
			{
				scriptableObjectCreateAssetMenu = set;
				if(set)
				{
					if(scriptableObjectFilename.Length == 0)
					{
						SetScriptableObjectFilename(StringUtils.SplitPascalCaseToWords(scriptPrescription.className), false);
					}
					if(scriptableObjectMenuName.Length == 0)
					{
						SetScriptableObjectMenuName("Scriptable Objects/" + scriptableObjectFilename, false);
					}
				}
				RebuildCode();
			}
		}

		private void ScriptableObjectFilenameGUI()
		{
			GUI.SetNextControlName(ScriptableObjectFilenameControlName);

			if(scriptableObjectFilenameIsInvalid)
			{
				GUI.color = Color.red;
			}

			string newFilename = EditorGUILayout.TextField(FilenameLabel, scriptableObjectFilename);

			GUI.color = Color.white;

			if(newFilename.Length == 0)
			{
				scriptPrescription.stringReplacements.Remove("$Filename");
			}
			else
			{
				scriptPrescription.stringReplacements["$Filename"] = newFilename;
			}

			if(newFilename != scriptableObjectFilename)
			{
				SetScriptableObjectFilename(newFilename);
			}
		}

		private void SetScriptableObjectFilename(string newFilename, bool rebuildCode = true)
		{
			scriptableObjectFilename = newFilename;
			scriptableObjectFilenameIsInvalid = FilenameIsInvalid();

			if(newFilename.Length == 0)
			{
				scriptPrescription.stringReplacements.Remove("$Filename");
			}
			else
			{
				scriptPrescription.stringReplacements["$Filename"] = newFilename;
			}

			if(rebuildCode)
			{
				RebuildCode();
			}
		}

		private void ScriptableObjectMenuNameGUI()
		{
			GUI.SetNextControlName(ScriptableObjectMenuNameControlName);

			string newMenuName = EditorGUILayout.TextField(MenuNameLabel, scriptableObjectMenuName);

			if(newMenuName.Length == 0)
			{
				scriptPrescription.stringReplacements.Remove("$MenuName");
			}
			else
			{
				scriptPrescription.stringReplacements["$MenuName"] = newMenuName;
			}
			
			if(newMenuName != scriptableObjectMenuName)
			{
				SetScriptableObjectMenuName(newMenuName);
			}
		}

		private void SetScriptableObjectMenuName(string newMenuName, bool rebuildCode = true)
		{
			scriptableObjectMenuName = newMenuName;

			if(newMenuName.Length == 0)
			{
				scriptPrescription.stringReplacements.Remove("$MenuName");
			}
			else
			{
				scriptPrescription.stringReplacements["$MenuName"] = newMenuName;
			}

			if(rebuildCode)
			{
				RebuildCode();
			}
		}

		private void ScriptableObjectOrderGUI()
		{
			GUI.SetNextControlName(ScriptableObjectOrderControlName);

			int newOrder = EditorGUILayout.IntField(OrderLabel, scriptableObjectOrder);
			GUI.color = Color.white;

			if(newOrder == 0)
			{
				scriptPrescription.stringReplacements.Remove("$Order");
			}
			else
			{
				scriptPrescription.stringReplacements["$Order"] = StringUtils.ToString(newOrder);
			}
			if(scriptableObjectOrder != newOrder)
			{
				scriptableObjectOrder = newOrder;
				RebuildCode();
			}
		}
		
		private void SingletonBaseClassGUI()
		{
			GUI.SetNextControlName(SingletonBaseClassControlName);

			string newValue = EditorGUILayout.TextField(SingletonBaseClassNameLabel, singletonBaseClassName);

			scriptPrescription.stringReplacements["$BaseClassName"] = newValue.Length == 0 ? "Singleton" : newValue;
			
			if(newValue != singletonBaseClassName)
			{
				SetSingletonBaseClassName(newValue, true);
			}
		}

		private void SetSingletonBaseClassName(string newBaseClassName, bool rebuildCode)
		{
			singletonBaseClassName = newBaseClassName;

			string prefName = "PI.SingletonBaseClass";
			if(newBaseClassName.Length == 0)
			{
				EditorPrefs.DeleteKey(prefName);
			}
			else
			{
				EditorPrefs.SetString(prefName, newBaseClassName);
			}

			if(rebuildCode)
			{
				RebuildCode();
			}
		}

		private void TargetPathGUI()
		{
			GUILayout.BeginHorizontal();

			GUI.SetNextControlName(SavePathControlName);

			SetDirectory(EditorGUILayout.TextField("Save Path", scriptOutputDirectoryLocalPathWithoutAssetsPrefix, GUILayout.ExpandWidth(true)));

			if(Event.current.type == EventType.MouseDown && Event.current.button == 1)
			{
				var lastRect = GUILayoutUtility.GetLastRect();
				if(lastRect.Contains(Event.current.mousePosition))
				{
					DrawGUI.Use(Event.current);
					var menu = Menu.Create();
					menu.Add("Edit Default Script Path", ShowDefaultScriptPathInPreferences);
					ContextMenuUtility.Open(menu, null);
				}
			}

			if(directoryBrowseIcon.image != null)
			{
				var iconSizeWas = EditorGUIUtility.GetIconSize();
				EditorGUIUtility.SetIconSize(browseIconSize);
				if(GUILayout.Button(directoryBrowseIcon, EditorStyles.label, GUILayout.Width(browseIconSize.x)))
				{
					OpenFolderPanel();
				}
				EditorGUIUtility.SetIconSize(iconSizeWas);
			}

			GUILayout.EndHorizontal();

			HelpField("Click a folder in the Project view to select.");
		}

		private static readonly Vector2 browseIconSize = new Vector2(18f, 18f);

		private void OpenFolderPanel()
		{
			string targetDir = GetScriptOutputLocalDirectoryPath();
			string setTargetDir = EditorUtility.OpenFolderPanel("Save Path", targetDir, "");
			if(string.Equals(targetDir, setTargetDir))
			{
				return;
			}

			if(setTargetDir.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
			{
				scriptOutputDirectoryLocalPathWithoutAssetsPrefix = setTargetDir.Length <= 7 ? "" : setTargetDir.Substring(7);
				return;
			}

			setTargetDir = setTargetDir.Replace("\\", "/");
			if(setTargetDir.StartsWith(Application.dataPath))
			{
				int dataPathPrefixLength = Application.dataPath.Length + 1;
				setTargetDir = setTargetDir.Length <= dataPathPrefixLength ? "" : setTargetDir.Substring(dataPathPrefixLength);
			}
			scriptOutputDirectoryLocalPathWithoutAssetsPrefix = setTargetDir;
		}
		
		private bool ClearButton()
		{
			return GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(45));
		}

		private void TemplateSelectionGUI()
		{
			templateIndex = Mathf.Clamp(templateIndex, 0, templateNames.Length - 1);
			int templateIndexNew = EditorGUILayout.Popup("Template", templateIndex, templateNames);
			if(templateIndexNew != templateIndex)
			{
				templateIndex = templateIndexNew;
				UpdateTemplateNamesAndTemplate();
				OnTemplateChanged();

				RebuildCode();
				DrawGUI.EditingTextField = false;
			}

			if(Event.current.type == EventType.MouseDown && Event.current.button == 1)
			{
				var lastRect = GUILayoutUtility.GetLastRect();
				if(lastRect.Contains(Event.current.mousePosition))
				{
					DrawGUI.Use(Event.current);
					var menu = Menu.Create();
					menu.Add("Edit Template", ShowCurrentTemplateInExplorer);
					ContextMenuUtility.Open(menu, null);
				}
			}
		}

		private void ShowCurrentTemplateInExplorer()
		{
			if(string.IsNullOrEmpty(baseClass))
			{
				return;
			}
			
			// Try to find function file first in custom templates folder and then in built-in
			string functionDataFilePath = Path.Combine(GetCustomTemplateFullPath(), baseClass + ".cs.txt");
			if(File.Exists(functionDataFilePath))
			{
				EditorUtility.RevealInFinder(functionDataFilePath);
				return;
			}

			functionDataFilePath = Path.Combine(GetBuiltinTemplateFullPath(), baseClass + ".cs.txt");
			if(File.Exists(functionDataFilePath))
			{
				EditorUtility.RevealInFinder(functionDataFilePath);
				return;
			}
		}

		private void ShowCurrentTemplateFunctionsInExplorer()
		{
			if(string.IsNullOrEmpty(baseClass))
			{
				return;
			}
			
			string functionsFileFullPath;
			if(!TryGetFunctionsFileFullPath(out functionsFileFullPath))
			{
				return;
			}

			EditorUtility.RevealInFinder(functionsFileFullPath);
		}

		private void ShowPreferences()
		{
			if(Event.current == null)
			{
				DrawGUI.OnNextBeginOnGUI(ShowPreferences, true);
				return;
			}

			var preferencesDrawer = PowerInspectorPreferences.RequestGetExistingOrCreateNewWindow();
			preferencesDrawer.SetActiveView("Create Script Wizard");
		}

		private void ShowUsingListInPreferences()
		{
			if(Event.current == null)
			{
				DrawGUI.OnNextBeginOnGUI(ShowUsingListInPreferences, true);
				return;
			}

			var preferencesDrawer = PowerInspectorPreferences.RequestGetExistingOrCreateNewWindow();
			preferencesDrawer.SetActiveView("Create Script Wizard");
			var createScriptWizard = preferencesDrawer.FindVisibleMember("Create Script Wizard") as IParentDrawer;
			if(createScriptWizard != null)
			{
				createScriptWizard.SetUnfolded(true);
				var usingOptions = preferencesDrawer.FindVisibleMember("Using Namespace Options") as IParentDrawer;
				if(usingOptions != null)
				{
					usingOptions.SetUnfolded(true);
					usingOptions.Select(ReasonSelectionChanged.Command);
				}
			}
		}

		private void ShowDefaultNamespaceInPreferences()
		{
			if(Event.current == null)
			{
				DrawGUI.OnNextBeginOnGUI(ShowUsingListInPreferences, true);
				return;
			}

			var preferencesDrawer = PowerInspectorPreferences.RequestGetExistingOrCreateNewWindow();
			preferencesDrawer.SetActiveView("Create Script Wizard");
			var defaultNamespace = preferencesDrawer.FindVisibleMember("Default Namespace");
			if(defaultNamespace != null)
			{
				defaultNamespace.Select(ReasonSelectionChanged.Command);
			}
		}

		private void ShowDefaultScriptPathInPreferences()
		{
			if(Event.current == null)
			{
				DrawGUI.OnNextBeginOnGUI(ShowUsingListInPreferences, true);
				return;
			}

			var preferencesDrawer = PowerInspectorPreferences.RequestGetExistingOrCreateNewWindow();
			preferencesDrawer.SetActiveView("Create Script Wizard");
			var defaultScriptPath = preferencesDrawer.FindVisibleMember("Default Script Path");
			if(defaultScriptPath != null)
			{
				defaultScriptPath.Select(ReasonSelectionChanged.Command);
			}
		}

		private void NamespaceGUI()
		{
			if(namespaceIsInvalid)
			{
				GUI.color = Color.red;
			}

			GUI.SetNextControlName(NamespaceControlName);

			SetNamespace(EditorGUILayout.TextField("Namespace", scriptPrescription.nameSpace), true);

			if(Event.current.type == EventType.MouseDown && Event.current.button == 1)
			{
				var lastRect = GUILayoutUtility.GetLastRect();
				if(lastRect.Contains(Event.current.mousePosition))
				{
					DrawGUI.Use(Event.current);
					var menu = Menu.Create();
					menu.Add("Edit Default Namespace", ShowDefaultNamespaceInPreferences);
					ContextMenuUtility.Open(menu, null);
				}
			}

			GUI.color = Color.white;
		}

		private void NameGUI()
		{
			if(classNameIsInvalid || classAlreadyExists)
			{
				GUI.color = Color.red;
			}
			GUI.SetNextControlName(ClassNameControlName);
			SetClassName(EditorGUILayout.TextField("Name", scriptPrescription.className), true);
			GUI.color = Color.white;

			//if(focusTextFieldNow && !isCustomEditor && Event.current.type == EventType.Repaint)
			//{
			//	DrawGUI.FocusControl(ClassNameControlName);
			//	focusTextFieldNow = false;
			//}
		}

		private const float PreviewTitleHeight = 18f;

		private void PreviewGUI()
		{
			float viewWidth = PreviewWidth;

			var previewHeaderRect = new Rect(0f, 0f, viewWidth, 18f);

			bool openRightClickMenu;

			var codePreviewRect = new Rect(0f, 20f, viewWidth, position.height - 20f);
			previewDrawer.Draw(codePreviewRect);

			openRightClickMenu = Event.current.type == EventType.MouseDown && Event.current.button == 1 && codePreviewRect.Contains(Event.current.mousePosition);

			// Draw preview title after box itself because otherwise the top row
			// of pixels of the slider will overlap with the title
			GUI.Label(previewHeaderRect, PreviewLabel, styles.previewTitle);

			var resizerRect = codePreviewRect;
			resizerRect.x += resizerRect.width - 4f;
			resizerRect.width = 8f;

			EditorGUIUtility.AddCursorRect(resizerRect, MouseCursor.ResizeHorizontal);

			if(Event.current.type == EventType.MouseDown)
			{
				if(Event.current.button == 1 && previewHeaderRect.Contains(Event.current.mousePosition))
				{
					openRightClickMenu = true;
				}
				else if(Event.current.button == 0 && resizerRect.Contains(Event.current.mousePosition))
				{
					resizing = true;
				}
			}
			else if(resizing)
			{
				float minWidth = optionsMinWidth;
				float remainingWidth = position.width - minWidth;
				if(remainingWidth < previewMinWidth)
				{
					float halfOfTotalWidth = position.width * 0.5f;
					minWidth = halfOfTotalWidth;
				}
				float maxWidth = position.width - previewMinWidth;
				if(maxWidth < minWidth)
				{
					maxWidth = minWidth;
				}
				float setRightBarPreferredWidth = Mathf.Clamp(position.width - Event.current.mousePosition.x, minWidth, maxWidth);
				if(optionsPreferredWidth != setRightBarPreferredWidth)
				{
					optionsPreferredWidth = setRightBarPreferredWidth;
					EditorPrefs.GetFloat(ScriptBuilder.InspectorWidth, setRightBarPreferredWidth);
					Repaint();
				}
			}

			if(openRightClickMenu)
			{
				DrawGUI.Use(Event.current);
				var menu = Menu.Create();
				menu.Add("Edit Template", ShowCurrentTemplateInExplorer);
				ContextMenuUtility.Open(menu, null);
			}
		}

		private bool InvalidTargetPath()
		{
			if(scriptOutputDirectoryLocalPathWithoutAssetsPrefix.IndexOfAny(InvalidPathChars) >= 0)
			{
				return true;
			}

			if(GetScriptOutputLocalDirectoryPath().Split(PathSeparatorChars, StringSplitOptions.None).Contains(string.Empty))
			{
				return true;
			}

			return false;
		}

		private bool InvalidTargetPathForEditorScript()
		{
			return isEditorClass && !FileUtility.IsEditorPath(scriptOutputDirectoryLocalPathWithoutAssetsPrefix);
		}

		private bool IsFolder(Object obj)
		{
			return Directory.Exists(AssetDatabase.GetAssetPath(obj));
		}

		private void HelpField(string helpText)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(string.Empty, GUILayout.Width(LabelWidth - 4));
			GUILayout.Label(helpText, styles.helpBox);
			GUILayout.EndHorizontal();
		}

		private string GetScriptOutputLocalFilePath()
		{
			return GetScriptOutputLocalFilePath(scriptPrescription.className);
		}

		private string GetScriptOutputLocalFilePath(string className)
		{
			return Path.Combine(GetScriptOutputLocalDirectoryPath(), className + ".cs").Replace("\\", "/");
		}

		private string GetScriptOutputLocalDirectoryPath()
		{
			return Path.Combine("Assets", scriptOutputDirectoryLocalPathWithoutAssetsPrefix.Trim(PathSeparatorChars));
		}

		private void SetNamespace(string newNamespace, bool rebuildCodeIfChanged)
		{
			if(string.Equals(scriptPrescription.nameSpace, newNamespace))
			{
				return;
			}

			scriptPrescription.nameSpace = newNamespace;

			namespaceIsInvalid = NamespaceIsInvalid();
			classAlreadyExists = ClassAlreadyExists();
			customEditortargetClassIsNotValidType = CustomEditorTargetClassIsNotValidType();
			canCreate = CanCreate();

			if(rebuildCodeIfChanged)
			{
				RebuildCode();
			}
		}

		private void SetClassName(string newClassName, bool rebuildCodeIfChanged)
		{
			if(string.Equals(scriptPrescription.className, newClassName))
			{
				return;
			}

			scriptPrescription.className = newClassName;

			classNameIsInvalid = ClassNameIsInvalid();
			classAlreadyExists = ClassAlreadyExists();

			scriptAtPathAlreadyExists = File.Exists(GetScriptOutputLocalFilePath());
			invalidTargetPath = InvalidTargetPath();
			invalidTargetPathForEditorScript = InvalidTargetPathForEditorScript();

			customEditorTargetClassDoesNotExist = CustomEditorTargetClassDoesNotExist();
			customEditortargetClassIsNotValidType = CustomEditorTargetClassIsNotValidType();
				
			canCreate = CanCreate();

			if(rebuildCodeIfChanged)
			{
				RebuildCode();
			}
		}

		private void SetDirectory(string setDirectory)
		{
			if(!string.Equals(scriptOutputDirectoryLocalPathWithoutAssetsPrefix, setDirectory))
			{
				scriptOutputDirectoryLocalPathWithoutAssetsPrefix = setDirectory;

				scriptAtPathAlreadyExists = File.Exists(GetScriptOutputLocalFilePath());
				invalidTargetPath = InvalidTargetPath();
				invalidTargetPathForEditorScript = InvalidTargetPathForEditorScript();

				SaveDirectoryPath();
			}
		}

		private void SetCustomEditorTargetClassName(string newClassName, bool rebuildCodeIfChanged)
		{
			if(string.Equals(customEditorTargetClassName, newClassName))
			{
				return;
			}

			customEditorTargetClassName = newClassName;
			SetClassNameBasedOnTargetClassName();
			customEditorTargetClassDoesNotExist = CustomEditorTargetClassDoesNotExist();
			customEditortargetClassIsNotValidType = CustomEditorTargetClassIsNotValidType();

			if(rebuildCodeIfChanged)
			{
				RebuildCode();
			}
		}
		
		
		private bool ClassNameIsInvalid()
		{
			return scriptPrescription.className.Length == 0 || !CodeGenerator.IsValidLanguageIndependentIdentifier(scriptPrescription.className);
		}

		private bool NamespaceIsInvalid()
		{
			var n = scriptPrescription.nameSpace;
			int count = n.Length;
			if(count == 0)
			{
				return false;
			}

			//namespaces can contain dots, but not in the beginning, not in the end, and not two in a row
			return n[0] == '.' || n[count - 1] == '.' || !CodeGenerator.IsValidLanguageIndependentIdentifier(n.Replace("..", "!").Replace(".", ""));
		}

		private bool FilenameIsInvalid()
		{
			return scriptableObjectFilename.Length > 0 && scriptableObjectFilename.IndexOfAny(Path.GetInvalidPathChars()) != -1;
		}

		private bool CustomEditorTargetClassExists()
		{
			foreach(var type in TypeExtensions.GetAllTypesThreadSafe(typeof(Object).Assembly, true, true))
			{
				if(string.Equals(type.Name, customEditorTargetClassName))
				{
					string nameSpace = type.Namespace;
					if(string.Equals(nameSpace, scriptPrescription.nameSpace))
					{
						return true;
					}

					for(int u = scriptPrescription.usingNamespaces.Length - 1; u >= 0; u--)
					{
						if(string.Equals(nameSpace, scriptPrescription.usingNamespaces[u]))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		private bool ClassExists(string nameSpace, string className)
		{
			foreach(var type in TypeExtensions.GetAllTypesThreadSafe(true, true))
			{
				if(string.Equals(type.Name, className) && string.Equals(type.Namespace, nameSpace))
				{
					return true;
				}
			}
			return false;
		}

		private bool ClassAlreadyExists()
		{
			if(scriptPrescription.className.Length == 0)
			{
				return false;
			}

			return ClassExists(scriptPrescription.nameSpace, scriptPrescription.className);
		}

		private bool CustomEditorTargetClassDoesNotExist()
		{
			if(!isCustomEditor)
			{
				return false;
			}

			if(customEditorTargetClassName.Length == 0)
			{
				return true;
			}

			return !CustomEditorTargetClassExists();
		}

		private bool CustomEditorTargetClassIsNotValidType()
		{
			if(!isCustomEditor)
			{
				return false;
			}

			if(customEditorTargetClassName.Length == 0)
			{
				return true;
			}

			return AppDomain.CurrentDomain.GetAssemblies().All(a => !typeof(Object).IsAssignableFrom(a.GetType(customEditorTargetClassName, false)));
		}

		private string GetCreateButtonText()
		{
			return CanAddComponent() ? "Create and Add" : "Create";
		}

		private static void CreateScript(string scriptFullFilePath, string code, bool refreshAssetDatabase)
		{
			var directory = Path.GetDirectoryName(scriptFullFilePath);
			Directory.CreateDirectory(directory);

			using(var writer = new StreamWriter(scriptFullFilePath))
			{
				writer.Write(code);
			}

			if(refreshAssetDatabase)
			{
				AssetDatabase.Refresh();
			}
		}

		private void OnSelectionChange()
		{
			clearKeyboardControl = true;

			if(Selection.activeObject == null)
			{
				return;
			}

			if(IsFolder(Selection.activeObject))
			{
				scriptOutputDirectoryLocalPathWithoutAssetsPrefix = AssetPathWithoutAssetPrefix(Selection.activeObject);
				if(isEditorClass && InvalidTargetPathForEditorScript())
				{
					scriptOutputDirectoryLocalPathWithoutAssetsPrefix = Path.Combine(scriptOutputDirectoryLocalPathWithoutAssetsPrefix, "Editor");
				}
			}
			else if(Selection.activeGameObject != null)
			{
				gameObjectToAddTo = Selection.activeGameObject;
			}
			else if(isCustomEditor && Selection.activeObject is MonoScript)
			{
				SetCustomEditorTargetClassName(Selection.activeObject.name, true);
				SetClassNameBasedOnTargetClassName();
			}

			Repaint();
		}

		private static string AssetPathWithoutAssetPrefix([NotNull]Object obj)
		{
			return AssetPathWithoutAssetPrefix(AssetDatabase.GetAssetPath(obj));
		}

		private static string AssetPathWithoutAssetPrefix(string assetPath)
		{
			if(assetPath.Length < 7)
			{
				if(string.Equals(assetPath, "assets", StringComparison.OrdinalIgnoreCase))
				{
					return "";
				}
				return assetPath;
			}
			return assetPath.Substring(7);
		}

		private bool IsEditorClass(string className)
		{
			if(string.IsNullOrEmpty(className))
			{
				return false;
			}

			var types = Types.EditorAssembly.GetTypes();
			for(int n = types.Length - 1; n >= 0; n--)
			{
				var type = types[n];
				if(string.Equals(type.Name, className))
				{
					return string.Equals(type.Namespace, "UnityEditor");
				}
			}
			return false;
		}
		
		[UsedImplicitly]
		private void OnDestroy()
		{
			SaveDirectoryPath();
			SaveSelectedTemplate();
			EditorPrefs.DeleteKey(ScriptBuilder.AttachTo);

			if(preferencesAsset != null)
			{
				preferencesAsset.onSettingsChanged -= OnSettingsChanged;
			}

			if(previewDrawer != null)
			{
				previewDrawer.Dispose();
			}
		}

		private void SaveDirectoryPath()
		{
			EditorPrefs.SetString(ScriptBuilder.SaveIn, scriptOutputDirectoryLocalPathWithoutAssetsPrefix);
		}

		private void SaveSelectedTemplate()
		{
			if(templateIndex >= 0 && templateIndex < templateNames.Length)
			{
				EditorPrefs.SetString(ScriptBuilder.Template, templateNames[templateIndex]);
			}
		}

		private void OnSettingsChanged(InspectorPreferences preferences)
		{
			preferencesAsset = preferences;
			LoadSettings();
			RebuildCode();
		}

		private void RebuildCode()
		{
			#if DEV_MODE && DEBUG_REBUILD_CODE
			Debug.Log("RebuildCode");
			#endif

			using(var scriptGenerator = new ScriptBuilder(scriptPrescription, curlyBracesOnNewLine, addComments, addCommentsAsSummary, wordWrapCommentsAfterCharacters, addUsedImplicitly, spaceAfterMethodName, newLine))
			{
				UpdateCodePreview(scriptGenerator.ToString());
			}
		}

		private void UpdateCodePreview(string codeUnformatted)
		{
			#if DEV_MODE && DEBUG_REBUILD_CODE
			Debug.Log("UpdateCodePreview("+ codeUnformatted.Length + ")");
			#endif

			code = codeUnformatted;

			if(previewDrawer != null)
			{
				previewDrawer.Dispose();
			}
			previewDrawer = CreateScriptWizardPreviewDrawer.Create(codeUnformatted, OnTextChangedByUser, SelectNextControlFromPreviewArea, SelectPreviousControlToPreviewArea, SelectControlOnRightOfPreviewArea, SelectControlOnLeftOfPreviewArea);

			GUI.changed = true;
			Repaint();
		}

		private bool SelectNextControlFromPreviewArea()
		{
			SelectFirstInspectorControl();
			return true;
		}
		

		private bool SelectPreviousControlToPreviewArea()
		{
			SelectLastInspectorControl();
			return true;
		}
		
		private bool SelectControlOnRightOfPreviewArea()
		{
			SelectFirstInspectorControl();
			return true;
		}

		private void SelectFirstInspectorControl()
		{
			GUI.FocusControl(FirstOptionsControlName);
		}

		private void SelectLastInspectorControl()
		{
			GUI.FocusControl(LastOptionsControlName);
		}

		private bool SelectControlOnLeftOfPreviewArea()
		{
			return false;
		}

		private void OnTextChangedByUser(string newText)
		{
			#if DEV_MODE
			Debug.Log("OnTextChangedByUser(" + newText.Length + ")");
			#endif
			code = newText;
			Repaint();
		}

		private class Styles
		{
			public readonly GUIContent warningContent = new GUIContent("");

			public readonly GUIStyle previewBox;
			public readonly GUIStyle previewTitle;
			public readonly GUIStyle loweredBox;
			public readonly GUIStyle helpBox;
			public readonly GUIStyle previewArea;

			public Styles()
			{
				previewBox = new GUIStyle("OL Box");
				previewTitle = new GUIStyle("OL Title");
				loweredBox = new GUIStyle("TextField") { padding = new RectOffset(1, 1, 1, 1) };
				helpBox = new GUIStyle("helpbox");
				previewArea = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperLeft };
			}
		}
	}
}