#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable]
	public class FolderDrawer : CustomEditorAssetDrawer
	{
		/// <inheritdoc/>
		protected override float EstimatedUnfoldedHeight
        {
            get
            {
				return 54f;
            }
        }

        /// <inheritdoc/>
        protected override string OverrideDocumentationUrl(out string documentationTitle)
		{
			var path = LocalPath;

			if(path.StartsWith("Packages", StringComparison.OrdinalIgnoreCase))
			{
				documentationTitle = "Asset Packages";
				return "AssetPackages";
			}

			int i = path.LastIndexOf('/');
			string dirName = i == -1 ? path : path.Substring(i + 1);
			switch(dirName.ToLowerInvariant())
			{
				case "resources":
					documentationTitle = "Resources Folder";
					return "LoadingResourcesatRuntime";
				case "streamingassets":
					documentationTitle = "Streaming Assets Folder";
					return "StreamingAssets";
				case "webplayertemplates":
					documentationTitle = "Webplayer Templates Folder";
					return "https://docs.unity3d.com/510/Documentation/Manual/UsingWebPlayertemplates.html";
				//case "editor":
				//case "standard assets":
				default:
					documentationTitle = "Special Folders";
					return "SpecialFolders";
			}
		}

		/// <inheritdoc />
		public override PrefixResizer PrefixResizer
		{
			get
			{
				return PrefixResizer.Disabled;
			}
		}

		/// <inheritdoc />
		protected override bool HasDebugModeIcon
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
		public override float Height
		{
			get
			{
				return HeaderHeight;
			}
		}

		/// <inheritdoc />
		protected override bool UsesEditorForDrawingBody
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc />
		protected override bool DrawGreyedOut
		{
			get
			{
				return false;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		[NotNull]
		public static new FolderDrawer Create(Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			FolderDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new FolderDrawer();
			}
			result.Setup(targets, targets, null, parent, inspector);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		protected override void DoBuildHeaderButtons()
		{
			AddHeaderButton(Button.Create(InspectorLabels.Current.ShowInExplorer, ShowInExplorer));
		}

		/// <inheritdoc />
		protected override void DrawHeaderButtons()
		{
			HideInternalOpenButton();
			base.DrawHeaderButtons();
		}

		/// <inheritdoc />
		protected override void Open()
		{
			ShowInExplorer();
		}

		/// <inheritdoc />
		protected override void DoGenerateMemberBuildList() { }

		/// <inheritdoc />
		protected override void DoBuildMembers() { }

		/// <inheritdoc/>
		public override bool DrawBody(Rect position)
		{
			//don't draw the "m_ExternalObjects" field of the FolderImporter
			return false;
		}

		/// <inheritdoc />
		protected override void GetHeaderSubtitle(ref GUIContent subtitle)
		{
			var path = LocalPath;

			if(path.StartsWith("Packages", StringComparison.OrdinalIgnoreCase))
			{
				subtitle.text = "Unity Package";
				return;
			}

			int i = path.LastIndexOf('/');
			string dirName = i == -1 ? path : path.Substring(i + 1);
			switch(dirName.ToLowerInvariant())
			{
				case "assets":
					if(path.Length == 6)
					{
						subtitle.text = "Assets Directory";
						subtitle.tooltip = "The Assets folder is the main folder that contains the Assets used by a Unity project.";
						return;
					}
					break;
				case "editor":
					subtitle.text = "Editor-Only Script Directory";
					subtitle.tooltip = "Scripts inside an Editor folder will not be included in your game's build. They are only used in the Unity Editor.";
					return;
				case "resources":
					subtitle.text = "Resources Directory";
					subtitle.tooltip = "The Resources folder is a special folder which allows you to access assets by file path and name in your scripts (using Resources.Load), rather than using direct references.";
					return;
				case "editor default resources":
					subtitle.text = "Editor Resources Directory";
					subtitle.tooltip = "This folder functions like a Resources folder, but is meant for editor scripts only. Use this if your editor plugin needs to load assets (e.g. icons, GUI skins, etc.) while making sure said assets won't get included in the user's build (putting such files in a normal Resources folder would have meant that those assets would be included in the user's game when built).";
					return;
				case "standard assets":
					subtitle.text = "Standard Assets Directory";
					subtitle.tooltip = "Scripts in here are always compiled first and output to Assembly-CSharp-firstpass.";
					return;
				case "streamingassets":
					subtitle.text = "Streaming Assets Directory";
					subtitle.tooltip = "Any files in here are copied to the build folder as is, without any changes (except for mobile and web builds, where they get embedded into the final build file). The path where they are can vary per platform but is accessible via Application.streamingAssetsPath.";
					return;
				case "gizmos":
					subtitle.text = "Gizmos Directory";
					subtitle.tooltip = "The gizmos folder holds all the texture/icon assets for use with Gizmos.DrawIcon. Texture assets placed inside this folder can be called by name, and drawn on-screen as a gizmo in the editor.";
					return;
				case "webplayertemplates":
					subtitle.text = "Web Player Templates Directory";
					subtitle.tooltip = "Used to replace the default web page used for web builds. Any scripts placed here will not be compiled at all. This folder has to be in your top-level Assets folder (it should not be in any subfolder in your Assets directory).";
					return;
				case "plugins":
					if(string.Equals(path, "assets/plugins"))
					{
						subtitle.text = "Plugins Directory";
						subtitle.tooltip = "The Plugins folder is where you must put any native plugins, which you want to be accessible by your scripts. They will also be automatically included in your build.\nLike the Standard Assets folder, any scripts in here are compiled earlier, allowing them to be accessed by other scripts that are outside the Plugins folder.";
						return;
					}
					break;
				case "x86":
					if(string.Equals(path, "assets/plugins/x86"))
					{
						subtitle.text = "32-Bit Plugins Directory";
						subtitle.tooltip = "If you are building for 32-bit or a universal (both 32 and 64 bit) platform, and if this subfolder exists, any native plugin files in this folder will automatically be included in your build. If this folder does not exist, Unity will look for native plugins inside the parent Plugins folder instead.";
						return;
					}
					break;
				case "x86_64":
					if(string.Equals(path, "assets/plugins/x86"))
					{
						subtitle.text = "64-Bit Plugins Directory";
						subtitle.tooltip = "If you are building for 64-bit or a universal (both 32 and 64 bit) platform, and if this subfolder exists, any native plugin files in this folder will automatically be included in your build. If this folder does not exist, Unity will look for native plugins inside the parent Plugins folder instead.";
						return;
					}
					break;
				case "android":
					if(string.Equals(path, "assets/plugins/android"))
					{
						subtitle.text = "Android Plugins Directory";
						subtitle.tooltip = "Place here any Java .jar files you want included in your Android project, used for Java-based plugins. Any .so file (when having Android NDK-based plugins) will also be included.";
						return;
					}
					break;
				case "ios":
					if(string.Equals(path, "assets/plugins/ios"))
					{
						subtitle.text = "iOS Plugins Directory";
						subtitle.tooltip = "A limited, simple way to automatically add (as symbolic links) any .a, .m, .mm, .c, or .cpp files into the generated Xcode project.";
						return;
					}
					break;
				case "cvs.":
					subtitle.text = "Hidden Directory";
					subtitle.tooltip = "Folders named \"cvs.\" are ignored by Unity. Any assets in there are not imported, and any scripts in there are not compiled. They will not show up in the Project view.";
					return;
				default:
					if(dirName.StartsWith(".", StringComparison.Ordinal))
					{
						subtitle.text = "Hidden Directory";
						subtitle.tooltip = "Folders that start with a dot are ignored by Unity. Any assets in there are not imported, and any scripts in there are not compiled. They will not show up in the Project view.";
						return;
					}

					if(dirName.EndsWith("~", StringComparison.Ordinal))
					{
						subtitle.text = "Hidden Directory";
						subtitle.tooltip = "Folders that end with '~' are ignored by Unity. Any assets in there are not imported, and any scripts in there are not compiled. They will not show up in the Project view.";
						return;
					}
					break;
			}

			subtitle.text = "Directory";
			subtitle.tooltip = "";
		}
	}
}
#endif