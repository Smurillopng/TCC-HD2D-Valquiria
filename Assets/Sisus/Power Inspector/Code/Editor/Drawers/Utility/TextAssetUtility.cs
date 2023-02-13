using System.IO;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public static class TextAssetUtility
	{
		/// <summary> Sets subtitle text and tooltip for display below the main header text of TextAsset. </summary>
		/// <param name="subtitle"> [in,out] The subtitle GUContent to edit. This cannot be null. </param>
		/// <param name="localPath"> Local path to asset. </param>
		public static void GetHeaderSubtitle([NotNull]ref GUIContent subtitle, string localPath)
		{
			subtitle.tooltip = "A text asset.";

			string extension = Path.GetExtension(localPath);
			if(extension != null && extension.Length > 1)
			{
				//subtitle.text = extension.Substring(1, 1).ToUpper() + extension.Substring(2).ToLower(); //remove leading dot and capitalize
				subtitle.text = StringUtils.SplitPascalCaseToWords(extension.Substring(1));
				if(!subtitle.text.EndsWith("Asset"))
				{
					subtitle.text = string.Concat(subtitle.text, " Asset");
				}
			}
			else
			{
				subtitle.text = "Asset";
			}
		}

		#if UNITY_EDITOR
		/// <summary> Sets subtitle text and tooltip for display below the main header text of MonoScript asset. </summary>
		/// <param name="subtitle"> [in,out] The subtitle GUContent to edit. This cannot be null. </param>
		/// <param name="monoScript"> The monoScript asset. </param>
		public static void GetHeaderSubtitle([NotNull]ref GUIContent subtitle, [NotNull]UnityEditor.MonoScript monoScript)
		{
			var classType = monoScript.GetClass();

			subtitle.tooltip = "A C# MonoScript asset";

			if(classType == null)
			{
				subtitle.text = "Script";
			}
			else if(Types.MonoBehaviour.IsAssignableFrom(classType))
			{
				subtitle.text = "MonoBehaviour Script";
			}
			else if(Types.ScriptableObject.IsAssignableFrom(classType))
			{
				if(Types.EditorWindow.IsAssignableFrom(classType))
				{
					subtitle.text = "ScriptableObject Script";
				}
			}
			else if(classType.IsValueType)
			{
				if(classType.IsEnum)
				{
					subtitle.text = "Enum Script";
				}
				else
				{
					subtitle.text = "Struct Script";
				}
			}
			else if(classType.IsAbstract)
			{
				if(classType.IsInterface)
				{
					subtitle.text = "Interface Script";
				}
				else if(classType.IsStatic())
				{
					subtitle.text = "Static Script";
				}
				else
				{
					subtitle.text = "Abstract Script";
				}
			}
			else if(classType.IsClass)
			{
				subtitle.text = "Class Script";
			}
			else
			{
				subtitle.text = "Script";
			}
		}
		#endif
	}
}