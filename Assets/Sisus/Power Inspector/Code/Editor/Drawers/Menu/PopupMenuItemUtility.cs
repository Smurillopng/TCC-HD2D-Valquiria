using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public static class PopupMenuItemUtility
	{
		public static bool drawWithFullPath;
		private static readonly GUIContent DrawLabel = new GUIContent("");
		
		public static bool Draw(Rect position, bool selected, [NotNull]PopupMenuItem item)
		{
			float previewSize = 17f;

			var buttonRect = position;
			buttonRect.x += previewSize;
			buttonRect.width -= previewSize;
			
			if(drawWithFullPath)
			{
				DrawLabel.text = item.FullLabel('.');
			}
			else
			{
				DrawLabel.text = item.label;
			}
			DrawLabel.tooltip = item.secondaryLabel;

			bool itemClicked = false;
			if(GUI.Button(buttonRect, DrawLabel, selected ? DrawGUI.richTextLabelWhite : DrawGUI.richTextLabel))
			{
				if(Event.current.button == 0)
				{
					Debug.Log(item +" - GUI.Button clicked");
					itemClicked = true;
					DrawGUI.Use(Event.current);
					GUI.changed = true;
				}
				else if(Event.current.button == 1)
				{
					var menu = BuildRightClickMenu(item);
					menu.Open();
					DrawGUI.Use(Event.current);
					GUI.changed = true;
				}

			}
			
			var preview = item.Preview;
			if(preview != null)
			{
				var iconRect = buttonRect;
				iconRect.width = previewSize;
				iconRect.height = previewSize;
				iconRect.x -= previewSize;
				GUI.DrawTexture(iconRect, preview, ScaleMode.ScaleToFit);
			}

			if(item.IsGroup)
			{
				var arrowRect = buttonRect;
				arrowRect.x += buttonRect.width - previewSize;
				arrowRect.y += 2f;
				arrowRect.width = 13f;
				arrowRect.height = 13f;
				
				GUI.Label(arrowRect, GUIContent.none, "AC RightArrow");
			}
			
			return itemClicked;
		}
		
		public static Menu BuildRightClickMenu(PopupMenuItem item)
		{
			var menu = Menu.Create();
			#if !POWER_INSPECTOR_LITE
			menu.Add("Copy", ()=>CopyToClipboard(item));
			#endif

			#if UNITY_EDITOR
			if(item.type != null)
			{
				if(item.type.IsComponent())
				{
					if(Types.MonoBehaviour.IsAssignableFrom(item.type))
					{
						menu.Add("Ping", ()=> PingMonoScriptAsset(item.type));
					}
				}
			}
			#endif
			return menu;
		}

		private static void CopyToClipboard(PopupMenuItem item)
		{
			Clipboard.Copy(item.type, Types.Type);
			Clipboard.SendCopyToClipboardMessage(item.label);
		}

		#if UNITY_EDITOR
		private static void PingMonoScriptAsset(Type type)
		{
			if(type != null)
			{
				var go = new GameObject()
				{
					hideFlags = HideFlags.HideAndDontSave
				};
				go.SetActive(false);
				var tempInstance = go.AddComponent(type) as MonoBehaviour;
				if(tempInstance != null)
				{
					GUI.changed = true;
					DrawGUI.Active.PingObject(UnityEditor.MonoScript.FromMonoBehaviour(tempInstance));
				}
				Platform.Active.Destroy(tempInstance);
			}
		}
		#endif
	}
}