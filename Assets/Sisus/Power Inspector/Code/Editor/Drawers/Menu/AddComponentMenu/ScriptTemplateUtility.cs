//#define DEBUG_GET_BASE_CLASS

using System;

namespace Sisus.CreateScriptWizard
{
	public static class ScriptTemplateUtility
	{
		public static bool TryGetBaseClass(string templateContent, out string baseClass)
		{
			string firstLine = GetFirstLine(templateContent);
			if(firstLine.Length > 10 && firstLine.StartsWith("BASECLASS", StringComparison.Ordinal))
			{
				baseClass = firstLine.Substring(10).Trim();
				if(baseClass.Length > 0)
				{
					#if DEV_MODE && DEBUG_GET_BASE_CLASS
					UnityEngine.Debug.Log("GetBaseClass: \"" + baseClass+ "\" (parsed from templateContent first line)");
					#endif
					return true;
				}
			}

			#if DEV_MODE && DEBUG_GET_BASE_CLASS
			UnityEngine.Debug.Log("GetBaseClass: " + StringUtils.Null+" (because first line did not contain BASECLASS).\n"+ firstLine);
			#endif

			baseClass = null;
			return false;
		}

		public static string WithoutFirstLine(string templateContent)
		{
			int firstLineEnd = templateContent.IndexOf('\n');
			if(firstLineEnd == -1)
			{
				return "";
			}
			return templateContent.Substring(firstLineEnd + 1);
		}

		public static string GetFirstLine(string templateContent)
		{
			int firstLineEnd = templateContent.IndexOf('\n');
			if(firstLineEnd == -1)
			{
				return templateContent;
			}
			return templateContent.Substring(0, firstLineEnd);
		}
	}
}