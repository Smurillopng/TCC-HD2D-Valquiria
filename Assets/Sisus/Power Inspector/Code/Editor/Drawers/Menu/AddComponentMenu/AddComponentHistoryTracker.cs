//#define DEBUG_ENABLED

using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public class AddComponentHistoryTracker : IDisposable
	{
		private static StringBuilder sb = new StringBuilder(500);
		private int MaxHistorySize = 10;

		[NotNull]
		public static List<Type> LastAddedComponents
		{
			get
			{
				string historyString = PlayerPrefs.GetString("PI.AddCompHistory", "");
				
				#if DEV_MODE && DEBUG_ENABLED
				Debug.Log("historyString:\n"+ historyString.Replace(";", "\n"));
				#endif

				if(historyString.Length == 0)
				{
					return new List<Type>(0);
				}
				
				var typeStrings = historyString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				int count = typeStrings.Length;
				var list = new List<Type>(count);
				for(int n = 0; n < count; n++)
				{
					var type = Type.GetType(typeStrings[n], false);
					if(type != null)
					{
						#if DEV_MODE
						Debug.Assert(!list.Contains(type), "list already contained type "+type.Name+":\n"+StringUtils.ToString(list));
						#endif

						list.Add(type);
					}
				}
				return list;
			}

			set
			{
				int count = value.Count;
				if(count == 0)
				{
					Clear();
					return;
				}
				
				sb.Append(value[0].AssemblyQualifiedName);
				for(int n = 1; n < count; n++)
				{
					sb.Append(';');
					sb.Append(value[n].AssemblyQualifiedName);
				}
				PlayerPrefs.SetString("PI.AddCompHistory", sb.ToString());
				sb.Length = 0;
			}
		}
		public AddComponentHistoryTracker()
		{
			if(MaxHistorySize <= 0)
			{
				Clear();
			}
			AddComponentMenuDrawer.OnComponentAdded += OnComponentAdded;
		}

		public void Dispose()
		{
			AddComponentMenuDrawer.OnComponentAdded -= OnComponentAdded;
		}

		private static void Clear()
		{
			PlayerPrefs.DeleteKey("PI.AddCompHistory");
		}

		private void OnComponentAdded(Type type)
		{
			if(MaxHistorySize <= 0)
			{
				return;
			}

			var history = LastAddedComponents;
			history.Remove(type);
			history.Insert(0, type);
			while(history.Count > MaxHistorySize)
			{
				history.RemoveAt(history.Count - 1);
			}
			LastAddedComponents = history;

			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("Add Component History Now:\n"+StringUtils.ToString(LastAddedComponents, "\n"));
			#endif
		}
	}
}