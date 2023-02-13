//#define DEBUG_ON_PARENT_ASSIGNED

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	/// <summary>
	/// Drawer for missing components. Components can be missing due to
	/// a MonoScript asset being deleted, or if there are compile errors.
	/// </summary>
	public class MissingScriptDrawer : ComponentDrawer
	{
		private static readonly List<Component> GetComponents = new List<Component>();
		private static readonly List<GameObject> GetGameObjects = new List<GameObject>();
		private static readonly List<GameObject> GetGameObjects2 = new List<GameObject>();

		private GUIContent contextMenuLabel;

		#if UNITY_EDITOR
		private MonoScript monoScript;
		private string guid = "";
		private string assetPath = "";
		#endif

		/// <inheritdoc/>
		public override bool Foldable
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc/>
		protected override bool HasEnabledFlag
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		protected override bool Reordering
		{
			get
			{
				return this == InspectorUtility.ActiveManager.MouseDownInfo.Reordering.Drawer;
			}
		}

		/// <inheritdoc/>
		protected override bool HasReferenceIcon
		{
			get
			{
				return false;
			}
		}

		#if UNITY_2018_1_OR_NEWER && UNITY_EDITOR
		/// <inheritdoc/>
		protected override bool HasPresetIcon
		{
			get
			{
				return false;
			}
		}
		#endif

		/// <inheritdoc/>
		protected override bool HasExecuteMethodIcon
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		protected override bool HasDebugModeIcon
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		protected override bool Editable
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		protected override bool Destroyable
		{
			get
			{
				return base.Editable;
			}
		}

		/// <inheritdoc/>
		protected override bool DrawGreyedOut
		{
			get
			{
				return !Destroyable;
			}
		}

		private static bool ScriptCompilationFailed
		{
			get
			{
				#if UNITY_EDITOR
				return EditorUtility.scriptCompilationFailed;
				#else
				return false;
				#endif
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="parent"> The parent drawers of the created drawers. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static MissingScriptDrawer Create([NotNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			MissingScriptDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new MissingScriptDrawer();
			}
			result.Setup(ArrayPool<Component>.Create(1), parent, null, inspector);
			result.LateSetup();
			return result;
		}

		/// <summary>
		/// Attempts to get a unique identifier for missing component, returning a default value rather
		/// than throwing an exception if it fails.
		/// </summary>
		/// <param name="gameObject"> The game object which contains the missing Component. </param>
		/// <param name="index"> Zero-based index of the missing component on the GameObject. </param>
		/// <param name="sceneData"> Serialized scene data in YAML format. </param>
		/// <returns> Guid string. </returns>
		private static string TryGetGuidForMissingComponent(GameObject gameObject, int index, string sceneData)
		{
			var name = gameObject.name;
			int nameStart = sceneData.IndexOf("m_Name: " +name);
			if(nameStart == -1)
			{
				return "";
			}
			int compsStart = sceneData.LastIndexOf("m_Component:", nameStart);
			if(compsStart == -1)
			{
				return "";
			}

			int fileIDStart = compsStart + 12;

			//index - 1 is because we ignore Transform component
			for(int n = 0; n <= index - 1; n++)
			{
				fileIDStart = sceneData.IndexOf("- component: {fileID: ", fileIDStart, nameStart - fileIDStart);
				if(fileIDStart == -1)
				{
					break;
				}
				fileIDStart += 22;
			}

			if(fileIDStart == -1)
			{
				return "";
			}

			var fileIDEnd = sceneData.IndexOf("}", fileIDStart, nameStart - fileIDStart);
			if(fileIDEnd == -1)
			{
				return "";
			}
			
			var fileID = sceneData.Substring(fileIDStart, fileIDEnd - fileIDStart);
			
			return TryGetGuidForMissingComponent(fileID, sceneData);
		}

		private static string TryGetGuidForMissingComponent(string fileID, string sceneData)
		{
			int compDataEnd = sceneData.IndexOf(" &"+fileID);
			if(compDataEnd == -1)
			{
				return "";
			}
			int guidStart = sceneData.LastIndexOf(", guid: ", compDataEnd);
			if(guidStart == -1)
			{
				return "";
			}

			guidStart = guidStart + 8;

			int guidEnd = sceneData.IndexOf(", type", guidStart);
			if(guidEnd == -1)
			{
				return "";
			}
			return sceneData.Substring(guidStart, guidEnd - guidStart);
		}
		
		/// <inheritdoc/>
		protected override void Setup(Component[] setTargets, IParentDrawer setParent, GUIContent setLabel, IInspector setInspector)
		{
			contextMenuLabel = setInspector.Preferences.labels.ContextMenu;

			base.Setup(setTargets, setParent, setLabel ?? GUIContentPool.Create("Missing Script"), setInspector);
		}
		
		/// <inheritdoc/>
		public override void OnParentAssigned(IParentDrawer newParent)
		{
			#if DEV_MODE && DEBUG_ON_PARENT_ASSIGNED
			Debug.Assert(Array.IndexOf(newParent.Members, this) != -1, ToString()+".OnParentAssigned: index in members "+StringUtils.ToString(newParent.Members)+" of parent "+newParent+" was -1");
			#endif

			#if UNITY_EDITOR
			if(EditorSettings.serializationMode != SerializationMode.ForceBinary)
			{
				var scene = gameObject.scene;

				if(!scene.IsValid())
				{
					#if DEV_MODE
					Debug.Log("MissingScript OnParentAssigned: GameObject \""+gameObject.name+"\" had no scene.");
					#endif
				}
				else
				{
					var path = scene.path;
					if(string.IsNullOrEmpty(path))
					{
						#if DEV_MODE
						Debug.Log("MissingScript OnParentAssigned: Path of scene \""+scene.name+"\" was empty.");
						#endif
					}
					else
					{
						var scenePath = FileUtility.LocalAssetsPathToFullPath(path);
						try
						{
							string sceneData = System.IO.File.ReadAllText(scenePath);
							int index = Array.IndexOf(newParent.Members, this);
							guid = TryGetGuidForMissingComponent(gameObject, index, sceneData);
						}
						catch(Exception e)
						{
							Debug.LogError("MissingScript.OnParentAssigned - File.ReadAllText " + e);
							guid = "";
						}

						if(guid.Length > 0)
						{
							assetPath = guid.Length == 0 ? "" : AssetDatabase.GUIDToAssetPath(guid);
							monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
						}
					}
				}
			}
			#endif
			
			base.OnParentAssigned(newParent);

			if(memberBuildState != MemberBuildState.MembersBuilt && MembersAreVisible)
			{
				memberBuildState = MemberBuildState.BuildListGenerated;
				BuildMembers();
			}
		}

		/// <inheritdoc/>
		protected override bool ShouldRebuildDrawers()
		{
			return false;
		}

		#if UNITY_EDITOR
		private void SetMonoScript(IDrawer changed, object newValue)
		{
			var setValue = newValue as MonoScript;
			if(setValue != monoScript && setValue != null)
			{
				var type = setValue.GetClass();
				if(type == null)
				{
					Debug.LogError("Assigned Type " + type.Name + " can not be loaded. Class name doesn't match filename?");
					((ScriptReferenceDrawer)members[0]).Members[0].SetValue(null);
					return;
				}

				if(!type.IsComponent())
				{
					Debug.LogError("Assigned Type "+type.Name+" does not inherit from Component!");
					((ScriptReferenceDrawer)members[0]).Members[0].SetValue(null);
					return;
				}

				var gameObjectDrawer = GameObjectDrawer;
				if(gameObjectDrawer != null)
				{
					gameObjectDrawer.OnNextLayout(()=>
					{
						gameObjectDrawer.ReplaceComponent(this, type);
					});
				}
			}
		}
		#endif

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList() { }

		/// <inheritdoc/>
		public override void OnAfterMembersBuilt()
		{
			UpdateVisibleMembers();
		}

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			if(parent == null)
			{
				return;
			}
			
			#if UNITY_EDITOR
			DrawerArrayPool.Resize(ref members, 2);
			var scriptReference = ScriptReferenceDrawer.Create(monoScript, this, monoScript != null);
			scriptReference.OnValueChanged += SetMonoScript;
			members[0] = scriptReference;
			int index = 1;
			#else
			int index = 0;
			#endif
			
			DrawerArrayPool.Resize(ref members, index + 1);
			
			members[index] = BoxDrawer.Create(this, GUIContentPool.Create(GetWarningMessage()), MessageType.Warning, ReadOnly);
		}

		private string GetWarningMessage()
		{
			#if UNITY_EDITOR
			if(ScriptCompilationFailed)
			{
				return "The associated script can not be loaded.\nPlease fix all compile errors.";
			}
			if(monoScript != null)
			{
				return "The script can not be loaded. This happens if class name doesn't match filename or script is inside Editor folder.";
			}
			if(guid.Length > 0)
			{
				return "Script not found. Has it been destroyed? Script guid was " + guid+".";
			}
			return "The associated script cannot be found.\nHas it been destroyed?";
			#else
			return "The associated script cannot be found.";
			#endif
		}

		/// <inheritdoc/>
		public override bool DrawPrefix(Rect position)
		{
			bool guiChangedWas = GUI.changed;
			GUI.changed = false;

			bool dirty = false;

			position.height = DrawGUI.Active.InspectorTitlebarHeight;

			var foldoutPos = position;
			foldoutPos.x += 2f;
			foldoutPos.y += 2f;
			foldoutPos.height -= 2f;
			foldoutPos.width -= 2f;
			DrawGUI.Active.Foldout(foldoutPos, new GUIContent(" "), Unfolded, Selected, Mouseovered, false);

			var settings = InspectorUtility.Preferences;

			var linePos = position;
			linePos.height = 1f;
			DrawGUI.DrawLine(linePos, settings.theme.ComponentSeparatorLine);

			var assetIconPos = position;
			assetIconPos.x += 15f;
			assetIconPos.y += 2f;
			assetIconPos.width = 15f;
			assetIconPos.height = 15f;
			GUI.DrawTexture(assetIconPos, settings.graphics.missingAssetIcon);

			var labelPos = position;
			labelPos.x += 47f;
			labelPos.y += 2f;
			GUI.Label(labelPos, label, DrawGUI.prefixLabelModified);

			GUI.Label(GetRectForFirstHeaderToolbarControl(position), contextMenuLabel, InspectorPreferences.Styles.Centered);

			if(GUI.changed)
			{
				dirty = true;
			}
			GUI.changed = guiChangedWas;
			return dirty;
		}

		#if UNITY_EDITOR
		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
			


			if(guid.Length > 0)
			{
				menu.AddSeparatorIfNotRedundant();
				menu.Add("Copy Missing Script Guid", ()=>
				{
					Clipboard.Copy(guid);
					Clipboard.SendCopyToClipboardMessage("Copied{0} guid", "Missing Script", guid);
				});
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}
		#endif

		#if UNITY_EDITOR
		public override void Dispose()
		{
			monoScript = null;
			guid = "";
			assetPath = "";
			base.Dispose();
		}
		#endif

		/// <inheritdoc/>
		public override void SelectPreviousOfType()
		{
			GameObject select;
			if(TryGetPreviousOfType(this, out select))
			{
				InspectorUtility.ActiveInspector.Select(select);
			}
		}

		/// <inheritdoc/>
		public override void SelectNextOfType()
		{
			GameObject select;
			if(TryGetNextOfType(this, out select))
			{
				var wereSelected = inspector.SelectedObjects;
				if(wereSelected.Length != 1 || wereSelected[0] != select)
				{
					inspector.OnNextInspectedChanged(ScrollToShowMissingScriptOnSelectedGameObject);
					inspector.Select(select);
				}
			}
		}

		private static void ScrollToShowMissingScriptOnSelectedGameObject()
		{
			var inspector = InspectorUtility.ActiveInspector;
			if(inspector != null)
			{
				inspector.SelectAndShow(null as Component, ReasonSelectionChanged.Command);
			}
		}
		
		private static bool TryGetPreviousOfType(MissingScriptDrawer subject, out GameObject result)
		{
			GameObject[] gameObjectsWithMissingComponents;
			int index = FindMissingScriptsInHierarchyAndSubjectIndex(subject, out gameObjectsWithMissingComponents);
			int count = gameObjectsWithMissingComponents.Length;
			if(count == 0)
			{
				result = null;
				return false;
			}

			if(index == -1)
			{
				result = gameObjectsWithMissingComponents[0];
			}
			else if(index == 0)
			{
				result = gameObjectsWithMissingComponents[count - 1];
			}
			else
			{
				result = gameObjectsWithMissingComponents[index - 1];
			}
			return true;
		}

		private static bool TryGetNextOfType(MissingScriptDrawer subject, out GameObject result)
		{
			GameObject[] gameObjectsWithMissingComponents;
			int index = FindMissingScriptsInHierarchyAndSubjectIndex(subject, out gameObjectsWithMissingComponents);
			int lastIndex = gameObjectsWithMissingComponents.Length - 1;
			if(lastIndex == -1)
			{
				result = null;
				return false;
			}
		
			if(index == -1 || index >= lastIndex)
			{
				result = gameObjectsWithMissingComponents[0];
			}
			else
			{
				result = gameObjectsWithMissingComponents[index + 1];
			}
			return result != subject.Component;
		}

		/// <inheritdoc/>
		public override void Duplicate()
		{
			#if DEV_MODE
			Debug.LogWarning("Duplicate command not yet supported for missing scripts");
			#endif
		}

		private static int FindMissingScriptsInHierarchyAndSubjectIndex(MissingScriptDrawer subject, out GameObject[] targetsWithNullComponents)
		{
			for(int s = 0, sceneCount = SceneManager.sceneCount; s < sceneCount; s++)
			{
				var scene = SceneManager.GetSceneAt(s);
				scene.GetAllGameObjects(GetGameObjects);
				for(int g = 0, gameObjectCount = GetGameObjects.Count; g < gameObjectCount; g++)
				{
					var gameObject = GetGameObjects[g];
					gameObject.GetComponents(GetComponents);
					if(GetComponents.Contains(null))
					{
						GetGameObjects2.Add(gameObject);
					}
					GetComponents.Clear();
				}
				GetGameObjects.Clear();
			}

			targetsWithNullComponents = GetGameObjects2.ToArray();
			GetGameObjects2.Clear();
			
			var subjectGameObject = subject.gameObject;
			for(int index = targetsWithNullComponents.Length - 1; index >= 0; index--)
			{
				if(targetsWithNullComponents[index] == subjectGameObject)
				{
					return index;
				}
			}
			return -1;
		}
	}
}