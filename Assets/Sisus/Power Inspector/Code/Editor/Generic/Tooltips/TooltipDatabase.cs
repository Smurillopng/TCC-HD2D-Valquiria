//#define DEBUG_GET_TOOLTIP_STEPS
//#define DEBUG_GET_TOOLTIP

using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Class that can handle fetching tooltips for fields, properties, methods and parameters. 
	/// First it tries fetching tooltips using Unity's TooltipAttribute.
	/// If no TooltipAttribute is found on the member, it tries generating one from XML documentation comments.
	/// </summary>
	public class TooltipDatabase
	{
		/// <summary>
		/// Cached tooltips for members sorted by type of owning class and then by member name.
		/// Using member name instead of ICustomAttributeProvider so that can generate Dictionaries from XML files.
		/// </summary>
		private readonly Dictionary<Type, Dictionary<string, string>> memberTooltipsByClass = new Dictionary<Type, Dictionary<string, string>>(128);
		
		/// <summary>
		/// Cached tooltips for members sorted by ICustomAttributeProvider (i.e. MemberInfo or ParameterInfo).
		/// </summary>
		private readonly Dictionary<ICustomAttributeProvider, string> memberTooltips = new Dictionary<ICustomAttributeProvider, string>(1024);
		
		[CanBeNull]
		public Dictionary<string, string> GetMemberTooltips([NotNull]MonoBehaviour monoBehaviour)
		{
			var classType = monoBehaviour.GetType();
			Dictionary<string, string> tooltips;
			if(memberTooltipsByClass.TryGetValue(classType, out tooltips))
			{
				return tooltips;
			}

			var monoScript = MonoScript.FromMonoBehaviour(monoBehaviour);
			if(monoScript != null)
			{
				tooltips = FetchMemberTooltips(monoScript);
				memberTooltipsByClass.Add(classType, tooltips);
				return tooltips;
			}
			
			tooltips = GetMemberTooltips(monoBehaviour.GetType());
			memberTooltipsByClass.Add(classType, tooltips);
			return tooltips;
		}

		[CanBeNull]
		public Dictionary<string, string> GetMemberTooltips([NotNull]MonoScript monoScript)
		{
			var classType = monoScript.GetClass();
			if(classType == null)
			{
				return null;
			}
			
			Dictionary<string, string> tooltips;
			if(memberTooltipsByClass.TryGetValue(classType, out tooltips))
			{
				return tooltips;
			}

			tooltips =  FetchMemberTooltips(monoScript);
			memberTooltipsByClass.Add(classType, tooltips);
			return tooltips;
		}

		[CanBeNull]
		public Dictionary<string, string> GetMemberTooltips([NotNull]ScriptableObject scriptableObject)
		{
			var classType = scriptableObject.GetType();
			Dictionary<string, string> tooltips;
			if(memberTooltipsByClass.TryGetValue(classType, out tooltips))
			{
				return tooltips;
			}

			var monoScript = MonoScript.FromScriptableObject(scriptableObject);
			if(monoScript != null)
			{
				tooltips = FetchMemberTooltips(monoScript);
				memberTooltipsByClass.Add(classType, tooltips);
				return tooltips;
			}

			tooltips = GetMemberTooltips(classType);
			memberTooltipsByClass.Add(classType, tooltips);
			return tooltips;
		}
		
		[CanBeNull]
		public Dictionary<string, string> GetMemberTooltips([NotNull]Object unityObject)
		{
			var monoScript = unityObject as MonoScript;
			if(monoScript != null)
			{
				return GetMemberTooltips(monoScript);
			}
			return GetMemberTooltips(unityObject.GetType());
		}
		
		[CanBeNull]
		public Dictionary<string, string> GetMemberTooltips([NotNull]Type classType)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(classType != null);
			Debug.Assert(classType != typeof(MonoScript));
			#endif

			Dictionary<string, string> tooltips;
			if(memberTooltipsByClass.TryGetValue(classType, out tooltips))
			{
				return tooltips;
			}

			var assembly = classType.Assembly;

			if(InspectorUtility.Preferences.enableTooltipsFromXmlComments)
			{
				// try fetching tooltips from xml documentation file
				if(XMLDocumentationCommentParser.TryGetMemberTooltips(classType, out tooltips))
				{
					memberTooltipsByClass.Add(classType, tooltips);
					return tooltips;
				}
			}
			
			var rootAssembly = assembly.GetName().Name;
			int i = rootAssembly.IndexOf('.');
			if(i != -1)
			{
				rootAssembly = rootAssembly.Substring(0, i);
			}

			switch(rootAssembly)
			{
				case "System":
				case "UnityEditor":
				case "UnityEngine":
					break;
				default:
					string className = classType.Name;

					//try fetching tooltips from MonoScript file
					var guids = AssetDatabase.FindAssets(className + " t:MonoScript");
					for(int n = guids.Length - 1; n >= 0; n--)
					{
						string scriptAssetLocalPath = FileUtility.GUIDToAssetPath(guids[n]);

						if(!string.Equals(System.IO.Path.GetFileNameWithoutExtension(scriptAssetLocalPath), className, StringComparison.OrdinalIgnoreCase))
						{
							#if DEV_MODE && DEBUG_GET_TOOLTIP_STEPS
							Debug.Log("GetMemberTooltips("+classType.Name + ") won't use script asset because name not an exact match: "+ scriptAssetLocalPath);
							#endif
							continue;
						}
						
						#if DEV_MODE && DEBUG_GET_TOOLTIP_STEPS
						Debug.Log("GetMemberTooltips("+classType.Name + ") found script asset @"+ scriptAssetLocalPath);
						#endif

						string scriptAssetPath = FileUtility.LocalToFullPath(scriptAssetLocalPath);
						if(TryFetchMemberTooltips(scriptAssetPath, classType, out tooltips))
						{
							memberTooltipsByClass.Add(classType, tooltips);
							return tooltips;
						}

						#if DEV_MODE && DEBUG_GET_TOOLTIP_STEPS
						if(guids.Length > n - 1) { Debug.Log("TryFetchMemberTooltips(\""+scriptAssetPath+"\", "+(classType == null ? "null" : classType.FullName) + ") failed. Testing next..."); }
						else { Debug.LogWarning("TryFetchMemberTooltips(\""+scriptAssetPath+"\", "+(classType == null ? "null" : classType.FullName) + ") failed - and it was the last one."); }
						#endif
					}

					#if DEV_MODE && DEBUG_GET_TOOLTIP_STEPS
					Debug.Log("GetMemberTooltips("+classType.Name + ") did not find script asset \"" + className + ".cs\"");
					#endif

					break;
			}

			memberTooltipsByClass.Add(classType, null);
			return null;
		}
		
		[NotNull]
		public Dictionary<string, string> FetchMemberTooltips([NotNull]MonoScript monoScript)
		{
			string scriptAssetLocalPath = AssetDatabase.GetAssetPath(monoScript);
			string scriptAssetPath = FileUtility.LocalToFullPath(scriptAssetLocalPath);
			return FetchMemberTooltips(scriptAssetPath, monoScript.GetClass());
		}

		[NotNull]
		private Dictionary<string, string> FetchMemberTooltips(string scriptAssetPath, [CanBeNull]Type classTypeMustMatch)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(scriptAssetPath.IndexOf(':') != -1, scriptAssetPath);
			Debug.Assert(scriptAssetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase), scriptAssetPath);
			#endif

			var tooltips = new Dictionary<string, string>();
			ScriptAssetDocumentationCommentParser.ParseComments(scriptAssetPath, tooltips, classTypeMustMatch);
			return tooltips;
		}

		private bool TryFetchMemberTooltips(string scriptAssetPath, [CanBeNull]Type classTypeMustMatch, [NotNull]out Dictionary<string, string> tooltips)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(scriptAssetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase), scriptAssetPath);
			#endif

			tooltips = new Dictionary<string, string>();
			return ScriptAssetDocumentationCommentParser.ParseComments(scriptAssetPath, tooltips, classTypeMustMatch);
		}
		
		[NotNull]
		public string GetTooltip([NotNull]MonoBehaviour monoBehaviour, [NotNull]MemberInfo member)
		{
			return GetTooltip(GetMemberTooltips(monoBehaviour), member);
		}

		[NotNull]
		public string GetTooltip([NotNull]ScriptableObject scriptableObject, [NotNull]MemberInfo member)
		{
			return GetTooltip(GetMemberTooltips(scriptableObject), member);
		}

		[NotNull]
		public string GetTooltip([NotNull]Object unityObject, [NotNull]MemberInfo member)
		{
			return GetTooltip(GetMemberTooltips(unityObject), member);
		}

		[NotNull]
		public string GetTooltip([NotNull]Object unityObject, [NotNull]string memberName)
		{
			return GetTooltip(GetMemberTooltips(unityObject), memberName);
		}
		
		[NotNull]
		public string GetTooltip([NotNull]Type classType, [NotNull]string memberName)
		{
			return GetTooltip(GetMemberTooltips(classType), memberName);
		}

		[NotNull]
		public string GetTooltip([NotNull]LinkedMemberInfo linkedInfo)
		{
			var memberInfo = linkedInfo.MemberInfo;

			string tooltip;

			if(memberInfo == null)
			{
				var parameterInfo = linkedInfo.ParameterInfo;
				if(parameterInfo != null)
				{
					var parent = linkedInfo.Parent;
					if(parent == null)
					{
						#if DEV_MODE && DEBUG_GET_TOOLTIP
						Debug.Log(linkedInfo + " can't have tooltip because had no parent.");
						#endif

						return "";
					}
					return GetTooltipFromParent(parameterInfo, parent, linkedInfo.DisplayName);
				}

				#if DEV_MODE && DEBUG_GET_TOOLTIP
				Debug.Log(linkedInfo + " can't have tooltip because had no MemberInfo and was not a parameter.");
				#endif

				return "";
			}
			
			if(memberTooltips.TryGetValue(memberInfo, out tooltip))
			{
				#if DEV_MODE && DEBUG_GET_TOOLTIP
				Debug.Log(linkedInfo + " cached tooltip: \"" + tooltip + "\"");
				#endif
				return tooltip;
			}
			
			var tooltipAttribute = linkedInfo.GetAttribute<TooltipAttribute>();
			if(tooltipAttribute != null)
			{
				tooltip = tooltipAttribute.tooltip;
				memberTooltips.Add(memberInfo, tooltip);
				#if DEV_MODE && DEBUG_GET_TOOLTIP
				Debug.Log(linkedInfo + " tooltip from TooltipAttribute: \"" + tooltip + "\"");
				#endif
				return tooltip;
			}
			
			Dictionary<string, string> tooltips;
			var memberName = memberInfo.Name;

			// Try finding tooltip via parent LinkedMemberInfos types
			for(var parent = linkedInfo.Parent; parent != null; parent = parent.Parent)
			{
				tooltips = GetMemberTooltips(parent.Type);
				if(TryGetTooltip(tooltips, memberName, out tooltip))
				{
					memberTooltips.Add(memberInfo, tooltip);
					
					#if DEV_MODE && DEBUG_GET_TOOLTIP
					if(tooltip.Length > 0)
					{
						Debug.Log(linkedInfo + " tooltip from xml docs via parent " + parent.Type + ": \"" + tooltip  +"\"");
					}
					else
					{
						Debug.Log(linkedInfo + " no tooltip found.");
					}
					#endif

					return tooltip;
				}
			}

			// Try finding tooltip via UnityEngine.Object target type.
			// NOTE: Doing this even if LinkedMemberInfo has parent LinkedMemberInfos, because it's possible that
			// there's another class defined inside a script file that also defines the target UnityEngine.Object class.
			var unityObject = linkedInfo.Hierarchy.Target;
			if(unityObject != null)
			{
				tooltips = GetMemberTooltips(unityObject);
				if(TryGetTooltip(tooltips, memberName, out tooltip))
				{
					memberTooltips.Add(memberInfo, tooltip);

					#if DEV_MODE && DEBUG_GET_TOOLTIP
					if(tooltip.Length > 0)
					{
						Debug.Log(linkedInfo + " tooltip from xml docs via UnityObject "+ unityObject.GetType().Name + ": \"" + tooltip  +"\"");
					}
					else
					{
						Debug.Log(linkedInfo + " no tooltip found.");
					}
					#endif

					return tooltip;
				}
			}
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(string.IsNullOrEmpty(tooltip));
			#endif

			memberTooltips.Add(memberInfo, "");
			
			#if DEV_MODE && DEBUG_GET_TOOLTIP
			Debug.Log(linkedInfo+ " no tooltip found.");
			#endif

			return "";
		}

		public string GetTooltipFromParent([NotNull]ParameterInfo subject, [NotNull]LinkedMemberInfo parent, string displayName)
		{
			string tooltip;
			if(memberTooltips.TryGetValue(subject, out tooltip))
			{
				#if DEV_MODE && DEBUG_GET_TOOLTIP
				Debug.Log(subject + " cached tooltip: \"" + tooltip + "\"");
				#endif
				return tooltip;
			}

			var parentTooltip = GetTooltip(parent);
			if(parentTooltip.Length == 0)
			{
				#if DEV_MODE && DEBUG_GET_TOOLTIP
				Debug.Log(subject + " returning \"\" because parent "+parent+" had no tooltip.");
				#endif

				memberTooltips.Add(subject, "");
				return "";
			}

			string parameterTooltipPrefix = displayName + " : ";
			int tooltipStart = parentTooltip.IndexOf(parameterTooltipPrefix, StringComparison.Ordinal);
			if(tooltipStart != -1)
			{
				int substringFrom = tooltipStart + parameterTooltipPrefix.Length;
				//TO DO: Currently doesn't support parsing multiple lines of comments for a single parameter.
				int tooltipEnd = parentTooltip.IndexOf('\n', substringFrom);
				if(tooltipEnd != -1)
				{
					tooltip = parentTooltip.Substring(substringFrom, tooltipEnd - substringFrom);
				}
				else
				{
					tooltip = parentTooltip.Substring(substringFrom);
				}

				#if DEV_MODE && DEBUG_GET_TOOLTIP
				Debug.Log(subject + " tooltip \""+tooltip+"\" parsed from parent "+parent+" tooltip \""+parentTooltip+"\".");
				#endif

				memberTooltips.Add(subject, tooltip);
				return tooltip;
			}

			#if DEV_MODE && DEBUG_GET_TOOLTIP
			Debug.Log(subject + " returning \"\" because parameter tooltip not found in parent "+parent+" tooltip \""+parentTooltip+"\".");
			#endif

			memberTooltips.Add(subject, "");
			return "";
		}
		
		[NotNull]
		public string GetTooltip([NotNull]Type classType, [NotNull]MemberInfo member)
		{
			return GetTooltip(GetMemberTooltips(classType), member);
		}
		
		[NotNull]
		private string GetTooltip([CanBeNull]Dictionary<string, string> tooltips,  [NotNull]MemberInfo member)
		{
			return GetTooltip(tooltips, member.Name);
		}

		[NotNull]
		private static string GetTooltip([CanBeNull]Dictionary<string, string> tooltips,  [NotNull]string memberName)
		{
			string result;
			TryGetTooltip(tooltips, memberName, out result);
			return result;
		}

		private static bool TryGetTooltip([CanBeNull]Dictionary<string, string> tooltips,  [NotNull]string memberName, [NotNull]out string tooltip)
		{
			if(tooltips == null)
			{
				#if DEV_MODE && DEBUG_GET_TOOLTIP_STEPS
				Debug.Log("GetTooltip(\""+memberName+ "\") returning null because tooltip Dictionary was null.");
				#endif
				tooltip = "";
				return false;
			}

			if(tooltips.TryGetValue(memberName, out tooltip))
			{
				#if DEV_MODE && DEBUG_GET_TOOLTIP_STEPS
				Debug.Log("GetTooltip(\""+memberName+ "\") returning \""+ tooltip + "\" from Dictionary.");
				#endif
				return true;
			}
			
			#if DEV_MODE && DEBUG_GET_TOOLTIP_STEPS
			Debug.Log("GetTooltip(\""+memberName+ "\") returning \"\"  because Dictionary of size "+tooltips.Count+" did not contain member.");
			#endif
			
			return false;
		}
	}
}