using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class GUIThemeColorsList
	{
		[SerializeField]
		private GUIThemeColors[] themes = { new GUIThemeColors { Name="Default" } };
		
		public void Setup(bool useProSkin, [NotNull]GUISkin baseSkin)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(GUI.skin != null);
			Debug.Assert(Event.current != null);
			#endif

			var active = Active;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(active != null, "GUIThemeColorsList.Active was null!");
			Debug.Assert(active.guiStyles != null, "GUIThemeColorsList.Active ("+active.Name+") guiStyles was null!");
			Debug.Assert(baseSkin != null);
			#endif

			active.guiSkin = active.guiStyles.AddTo(baseSkin);
			active.SyntaxHighlight.OnValidate();

			#if DEV_MODE && DEBUG_IS_PRO_SKIN
			Debug.Log("IsProSkin="+StringUtils.ToColorizedString(useProSkin)+" - Using GUISkin "+active.guiSkin.name, active.guiSkin);
			#endif
		}

		public GUIThemeColors Active
		{
			get
			{
				return DrawGUI.IsProSkin ? Pro : Personal;
			}
		}

		public GUIThemeColors Personal
		{
			get
			{
				#if UNITY_2019_3_OR_NEWER
				return PersonalModern;
				#else
				return PersonalClassic;
				#endif
			}
		}

		public GUIThemeColors Pro
		{
			get
			{
				#if UNITY_2019_3_OR_NEWER
				return ProModern;
				#else
				return ProClassic;
				#endif
			}
		}

		public GUIThemeColors Classic
		{
			get
			{
				return DrawGUI.IsProSkin ? ProClassic : PersonalClassic;
			}
		}

		public GUIThemeColors Modern
		{
			get
			{
				return DrawGUI.IsProSkin ? ProModern : PersonalModern;
			}
		}

		public GUIThemeColors PersonalClassic
		{
			get
			{
				return themes.Length > 0 ? themes[0] : null;
			}
		}

		public GUIThemeColors PersonalModern
		{
			get
			{
				return themes.Length > 1 ? themes[1] : null;
			}
		}

		public GUIThemeColors ProClassic
		{
			get
			{
				return themes.Length > 2 ? themes[2] : null;
			}
		}

		public GUIThemeColors ProModern
		{
			get
			{
				return themes.Length > 3 ? themes[3] : null;
			}
		}

		public int Count
		{
			get
			{
				return themes.Length;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public bool TryGet(string themeName, out GUIThemeColors result)
		{
			for(int n = 0, count = themes.Length; n < count; n++)
			{
				var theme = themes[n];
				if(string.Equals(theme.Name, themeName))
				{
					result = theme;
					return true;
				}
			}
			result = null;
			return false;
		}

		public GUIThemeColors this[string themeName]
		{
			get
			{
				for(int n = 0, count = themes.Length; n < count; n++)
				{
					var theme = themes[n];
					if(string.Equals(theme.Name, themeName))
					{
						return theme;
					}
				}
				Debug.LogError("GUIThemeColors with name \""+ themeName + "\" not found among "+themes.Length+" items!");
				return Personal;
			}
		}

		public GUIThemeColors this[int hash]
		{
			get
			{
				for(int n = 0, count = themes.Length; n < count; n++)
				{
					var theme = themes[n];
					if(theme.NameHashEquals(hash))
					{
						return theme;
					}
				}
				Debug.LogError("GUIThemeColors with hash "+hash+" not found among "+themes.Length+" thenes:\n"+ StringUtils.ToString(themes.Select(theme => theme.NameHash), "\n"));
				return Personal;
			}
		}

		public bool TryGet(int themeNameHash, out GUIThemeColors result)
		{
			for(int n = 0, count = themes.Length; n < count; n++)
			{
				var theme = themes[n];
				if(theme.NameHashEquals(themeNameHash))
				{
					result = theme;
					return true;
				}
			}
			result = null;
			return false;
		}

		public int IndexOf(GUIThemeColors item)
		{
			return Array.IndexOf(themes, item);
		}

		public void Insert(int index, GUIThemeColors item)
		{
			themes = themes.InsertAt(index, item);
		}

		public void RemoveAt(int index)
		{
			themes = themes.RemoveAt(index);
		}
		
		public void OnValidate()
		{
			int count = themes.Length;

			#if UNITY_EDITOR
			if(Platform.EditorMode && InspectorUtility.ActiveManager != null && InspectorUtility.ActiveManager.ActiveInspector != null && InspectorUtility.ActiveManager.ActiveInspector.Preferences.themes == this)
			{
				if(count <= 0)
				{
					Debug.LogError("GUIThemeColorsList has zero themes. It should have at least one.");
				}
				else if(count < 2)
				{
					Debug.LogError("GUIThemeColorsList only has "+themes.Length+" themes. It should have at least two themes: \"Default\" and \"Pro\".");
				}
				else
				{
					Debug.Assert(Personal.Name.Contains("Default"), "GUIThemeColorsList.Default name doesn't contain \"Default\".");
					Debug.Assert(Pro.Name.Contains("Pro"), "GUIThemeColorsList.Pro name doesn't contain \"Pro\".");
				}
			}
			else
			#endif
			{
				if(count <= 0)
				{
					Debug.LogError("GUIThemeColorsList has zero themes. It should have at least one.");
				}
				else if(!Personal.Name.Contains("Default"))
				{
					Debug.LogError("GUIThemeColorsList.Default name (\""+Personal.Name+"\") did not contain substring \"Default\".");
				}
			}

			for(int n = 0; n < count; n++)
			{
				var theme = themes[n];
				theme.OnValidate();
			}
		}

		public IEnumerator<GUIThemeColors> GetEnumerator()
		{
			return (IEnumerator<GUIThemeColors>)themes.GetEnumerator();
		}

		public void Add(GUIThemeColors item)
		{
			themes = themes.Add(item);
		}

		public void Clear()
		{
			if(themes.Length > 0)
			{
				themes = new GUIThemeColors[0];
			}
		}

		public bool Contains(GUIThemeColors item)
		{
			return Array.IndexOf(themes, item) != -1;
		}

		public void CopyTo(GUIThemeColors[] array, int arrayIndex)
		{
			themes.CopyTo(array, arrayIndex);
		}

		public bool Remove(GUIThemeColors item)
		{
			int index = Array.IndexOf(themes, item);
			if(index == -1)
			{
				return false;
			}
			themes = themes.RemoveAt(index);
			return true;
		}
	}
}