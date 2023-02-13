using JetBrains.Annotations;
using Sisus.Attributes;
using System;
using UnityEditor;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForAsset(typeof(MonoScript), false, true)]
	public class MonoScriptDrawer : FormattedTextAssetDrawer<CSharpSyntaxFormatter>
	{
		/// <inheritdoc/>
		protected override MonoScript MonoScript
		{
			get
			{
				return Target as MonoScript;
			}
		}

		/// <inheritdoc/>
		public override string DocumentationPageUrl
		{
			get
			{
				return PowerInspectorDocumentation.GetDrawerInfoUrl("script-drawer");
			}
		}
		
		/// <inheritdoc/>
		protected override CSharpSyntaxFormatter CreateSyntaxFormatter()
		{
			return CSharpSyntaxFormatterPool.Pop();
		}

		/// <inheritdoc />
		protected override void GetHeaderSubtitle(ref GUIContent subtitle)
		{
			TextAssetUtility.GetHeaderSubtitle(ref subtitle, MonoScript);
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		[NotNull]
		public static new MonoScriptDrawer Create(UnityEngine.Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			var result = Create<MonoScriptDrawer>(targets, parent, inspector);
			return result;
		}

		/// <inheritdoc />
		protected override void Setup(UnityEngine.Object[] setTargets, UnityEngine.Object[] setEditorTargets, Type setEditorType, IParentDrawer setParent, IInspector setInspector)
		{
			//for the default references view
			if(setEditorType == null)
			{
				setEditorType = Types.GetInternalEditorType("UnityEditor.MonoScriptImporterInspector");
			}

			base.Setup(setTargets, setEditorTargets, setEditorType, setParent, setInspector);
		}

		/// <inheritdoc />
		protected override string GetTextFromFile()
		{
			var monoScript = MonoScript;
			if(monoScript != null)
			{
				return monoScript.text;
			}
			return base.GetTextFromFile();
		}

		/// <inheritdoc />
		protected override bool HasAddressableBar()
		{
			return false;
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
			
			menu.AddSeparatorIfNotRedundant();
			
			#if UNITY_2018_3_OR_NEWER
			menu.Add("Script Execution Order", () => DrawGUI.ExecuteMenuItem("Edit/Project Settings..."));
			#else
			menu.Add("Script Execution Order", () => DrawGUI.ExecuteMenuItem("Edit/Project Settings/Script Execution Order"));
			#endif

			#if DEV_MODE // not implemented yet
			menu.Add("Default References", () => editDefaultReferences = !editDefaultReferences);
			#endif

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		/// <inheritdoc/>
		protected override string OverrideDocumentationUrl([NotNull] out string documentationTitle)
		{
			documentationTitle = "Script Drawer";
			return PowerInspectorDocumentation.GetDrawerInfoUrl("script-drawer");
		}
	}
}