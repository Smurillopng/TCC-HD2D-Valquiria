using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public static class DebugUtility
	{
		private static Dictionary<string, string> lastDebugged = new Dictionary<string, string>();

		public static void LogChanges(string key, string message, Object context = null, bool error = false)
		{
			string lastMessage;
			if(!lastDebugged.TryGetValue(key, out lastMessage) || !string.Equals(lastMessage, message))
			{
				lastDebugged[key] = message;

				if(error)
				{
					Debug.LogError(message, context);
				}
				else
				{
					Debug.Log(message, context);
				}
			}
		}

		public static void PrintFullStateInfo([CanBeNull]object target)
		{
			Debug.Log(GetFullStateInfo(target));
		}
		
		public static string GetFullStateInfo([CanBeNull]object target, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
		{
			return GetFullStateInfo(target, bindingFlags, ArrayPool<string>.ZeroSizeArray);
		}

		public static string GetFullStateInfo([CanBeNull]object target, params string[] blacklist)
		{
			return GetFullStateInfo(target, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, blacklist);
		}

		public static string GetFullStateInfo([CanBeNull]object target, BindingFlags bindingFlags, [CanBeNull]string[] blacklist)
		{
			if(target == null)
			{
				return "null";
			}

			if(blacklist == null)
			{
				blacklist = ArrayPool<string>.ZeroSizeArray;
			}

			var sb = StringBuilderPool.Create();
			sb.Append(target + " Full State:\r\n");

			if(bindingFlags.HasFlag(BindingFlags.DeclaredOnly))
			{
				GetFullStateInfoDeclaredOnly(target, target.GetType(), sb, bindingFlags, blacklist);
			}
			else
			{
				bindingFlags = (BindingFlags)bindingFlags.SetFlag(BindingFlags.DeclaredOnly);

				for(var type = target.GetType(); type != null; type = type.BaseType)
				{
					GetFullStateInfoDeclaredOnly(target, type, sb, bindingFlags, blacklist);
				}
			}
			
			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		private static void GetFullStateInfoDeclaredOnly([CanBeNull]object target, Type type, StringBuilder sb, BindingFlags bindingFlags, [CanBeNull]string[] blacklist)
		{
			var allFields = type.GetFields(bindingFlags);
			for(int n = 0, count = allFields.Length; n < count; n++)
			{
				var field = allFields[n];
				string name = field.Name;
				if(Array.IndexOf(blacklist, name) != -1)
				{
					continue;
				}

				sb.Append("\r\n");
				sb.Append(name);
				sb.Append(" : ");
				sb.Append(StringUtils.ToColorizedString(field.GetValue(target)));
			}

			sb.Append("\r\n");

			var allProperties = type.GetProperties(bindingFlags);
			for(int n = 0, count = allProperties.Length; n < count; n++)
			{
				var property = allProperties[n];
				string name = property.Name;
				if(Array.IndexOf(blacklist, name) != -1)
				{
					continue;
				}

				if(property.GetGetMethod() != null && property.GetIndexParameters().Length == 0)
				{
					sb.Append("\r\n");
					sb.Append(name);
					sb.Append(" : ");
					sb.Append(StringUtils.ToColorizedString(property.GetValue(target, null)));
				}
			}

			sb.Append("\r\n");
		}


		public static string GetFullStaticStateInfo([CanBeNull]Type type)
		{
			if(type == null)
			{
				return "null";
			}

			var sb = StringBuilderPool.Create();
			sb.Append("Type ");
			sb.Append(type.Name);
			sb.Append(" Full Static State:\r\n");
			
			var bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

			var allFields = type.GetFields(bindingFlags);
			for(int n = 0, count = allFields.Length; n < count; n++)
			{
				var field = allFields[n];
				sb.Append("\r\n");
				sb.Append(field.Name);
				sb.Append(" : ");
				sb.Append(StringUtils.ToColorizedString(field.GetValue(null)));
			}

			sb.Append("\r\n");

			var allProperties = type.GetProperties(bindingFlags);
			for(int n = 0, count = allProperties.Length; n < count; n++)
			{
				var property = allProperties[n];
				if(property.GetGetMethod() != null && property.GetIndexParameters().Length == 0)
				{
					sb.Append("\r\n");
					sb.Append(property.Name);
					sb.Append(" : ");
					sb.Append(StringUtils.ToColorizedString(property.GetValue(null, null)));
				}
			}

			sb.Append("\r\n");

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}
	}
}