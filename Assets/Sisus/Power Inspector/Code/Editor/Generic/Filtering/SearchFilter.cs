//#define USE_HIGHLIGHTER

//#define DEBUG_PASSES_SEARCH_FILTER_BASIC
//#define DEBUG_PASSES_SEARCH_FILTER
//#define DEBUG_FAILS_SEARCH_FILTER

//#define REQUIRE_EXACT_MATCH_FOR_UNITY_OBJECT_TYPE

using System;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Object = UnityEngine.Object;
using UnityEngine;

namespace Sisus
{
	/// <summary> (Serializable) Represents the filtering data of the filter field of an inspector. </summary>
	[Serializable]
	public class SearchFilter
	{
		#if DEV_MODE && (DEBUG_PASSES_SEARCH_FILTER || DEBUG_PASSES_SEARCH_FILTER_BASIC || DEBUG_FAILS_SEARCH_FILTER)
		private static string OnlyDebugFieldByName = "";
		#endif

		[SerializeField]
		private string rawInput = "";

		[NonSerialized]
		private bool hasFilter;

		[NonSerialized]
		private bool hasLabelFilter;
		[NonSerialized]
		private string filterLabel = "";
		[NonSerialized]
		private bool filterLabelForExactMatch;

		[NonSerialized]
		private bool hasTypeFilter;
		[NonSerialized]
		private string filterType = "";
		[NonSerialized]
		private bool filterTypeForExactMatch;

		[NonSerialized]
		private bool hasValueFilter;
		[NonSerialized]
		private string filterFieldValue = "";
		[NonSerialized]
		private bool filterValueForExactMatch;

		[NonSerialized]
		private bool hasGenericFilter;
		[NonSerialized]
		private string filterGeneric = "";
		[NonSerialized]
		private readonly List<string> filtersGeneric = new List<string>(2);
		[NonSerialized]
		private readonly List<bool> filtersGenericForExactMatch = new List<bool>(2);

		public FilteringMethod FilteringMethod
		{
			get;
			private set;
		}

		public ReadOnlyCollection<string> FiltersGeneric
		{
			get
			{
				return new ReadOnlyCollection<string>(filtersGeneric);
			}
		}

		/// <summary> Gets value indicating whether the filter field currently has any text input. </summary>
		/// <value> True if filter field has any text, false it it's empty. </value>
		public bool IsNotEmpty
		{
			get
			{
				return hasFilter;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the filter field currently has a filter
		/// which potentially affects what content is displayed for the selected target.
		/// 
		/// If only has filter related to specifying the inspected target, this is still false.
		/// </summary>
		/// </summary>
		/// <value> True if has content-affecting filter, otherwise false. </value>
		public bool HasFilterAffectingInspectedTargetContent
		{
			get
			{
				if(!hasFilter)
				{
					return false;
				}
				return hasLabelFilter || hasTypeFilter || hasValueFilter || hasGenericFilter;
			}
		}

		/// <summary> Filter string in its raw input form, containing original casing etc. </summary>
		/// <value> The raw text input. </value>
		public string RawInput
		{
			get
			{
				return rawInput;
			}
		}

		/// <summary> Filter by type. E.g. "t:vector3" </summary>
		/// <value> The type filter. </value>
		public string FilterType
		{
			get
			{
				return filterType;
			}
		}

		/// <summary> Filter by field label. E.g. "l:position.x" </summary>
		/// <value> The label filter. </value>
		public string FilterFieldLabel
		{
			get
			{
				return filterLabel;
			}
		}

		/// <summary> Filter by field value. E.g. "v:True" </summary>
		/// <value> The value filter. </value>
		public string FilterFieldValue
		{
			get
			{
				return filterFieldValue;
			}
		}

		/// <summary> Generic filter. This is the default filter type used when the user did not input a specific filtering type prefix.
		/// This will result in filtering done against type, field label AND field value.  </summary>
		public string FilterGeneric
		{
			get
			{
				return filterGeneric;
			}
		}

		public bool DeterminesInspectedTarget()
		{
			switch(FilteringMethod)
			{
				case FilteringMethod.Class:
				case FilteringMethod.Window:
				case FilteringMethod.Scene:
				case FilteringMethod.Asset:
				case FilteringMethod.Icon:
				#if DEV_MODE
				case FilteringMethod.GUIStyle:
				#endif
					return true;
				default:
					return false;
			}
		}

		public bool SetFilter([NotNull]string setFilterValue, [CanBeNull]IInspector inspector)
		{
			if(inspector == null)
			{
				return SetFilter(setFilterValue, null, null, null);
			}
			return SetFilter(setFilterValue, inspector, inspector.OnFilterChanging, inspector.BroadcastOnFilterChanged);
		}

		internal void ReapplyFilter([CanBeNull]IInspector inspector)
		{
			if(rawInput.Length == 0)
			{
				return;
			}

			var setFilter = rawInput;
			rawInput = "";
			if(inspector == null)
			{
				SetFilter(setFilter, null, null, null);
			}
			SetFilter(setFilter, inspector, inspector.OnFilterChanging, inspector.BroadcastOnFilterChanged);
		}

		/// <summary> Sets the filter to given value. </summary>
		/// <param name="setFilterValue"> The raw text input for the filter. This cannot be null. </param>
		/// <param name="inspector"> The inspector whose filter field is changing. This may be null. </param>
		/// <param name="onFilterChanging"> Callback that should be called right after filter data has been set to new values if the filter changed. This may be null. </param>
		/// <param name="onFilterChanged"> Callback that should be called the during next layout event if the filter changed. This may be null. </param>
		/// <returns> True if filter was changed, false if current raw input matches setFilterValue. </returns>
		public bool SetFilter([NotNull]string setFilterValue, [CanBeNull]IInspector inspector, [CanBeNull]Action<SearchFilter> onFilterChanging, [CanBeNull]Action onFilterChanged)
		{
			if(string.Equals(setFilterValue, rawInput))
			{
				return false;
			}

			if(setFilterValue == null)
			{
				setFilterValue = "";
			}

			#if DEV_MODE
			Debug.Log("SearchFilter = \"" + setFilterValue + "\" (was : \"" + rawInput + "\")");
			#endif

			rawInput = setFilterValue;
			filterFieldValue =  "";
			filterType = "";
			filterLabel = "";
			filterGeneric =  "";
			filtersGeneric.Clear();
			filtersGenericForExactMatch.Clear();

			hasFilter = false;
			hasTypeFilter = false;
			hasGenericFilter = false;
			hasValueFilter = false;
			hasLabelFilter = false;
			FilteringMethod = FilteringMethod.Any;

			if(!string.IsNullOrEmpty(rawInput))
			{
				var filterLower = rawInput;
				RemoveUnnecessaryParts(ref filterLower);
				filterLower = filterLower.ToLower();

				int charCount = filterLower.Length;

				for(int start = 0, end = filterLower.IndexOf(' '); start < charCount; start = end + 1)
				{
					end = filterLower.IndexOf(' ', start);

					bool exactMatch = false;

					string s;
					if(end == -1)
					{
						end = charCount;
					}

					s = filterLower.Substring(start, end - start);
					int quotationMark = filterLower.IndexOf('"', start);
					if(quotationMark != -1 && quotationMark < end)
					{
						int secondQuotationMark = filterLower.IndexOf('"', quotationMark + 1);
						if(secondQuotationMark != -1)
						{
							exactMatch = true;
							end = filterLower.IndexOf(' ', secondQuotationMark + 1);
							if(end == -1)
							{
								end = charCount;
							}
							s = filterLower.Substring(start, end - start).Replace("\"", "");
							#if DEV_MODE
							Debug.Log("\""+s+"\" with start="+start+", end="+end+ ", quotationMark="+ quotationMark+ ", secondQuotationMark="+ secondQuotationMark+", charCount = " + charCount+ ", filterLower="+ filterLower);
							#endif
						}
					}

					int length = s.Length;
					if(length > 0)
					{
						if(length >= 2 && s[1] == ':')
						{
							switch(s[0])
							{
								case 't': // type

									FilteringMethod = FilteringMethod.Type;

									if(length > 2)
									{
										filterType = s.Substring(2);
										hasTypeFilter = true;
										hasFilter = true;
										filterTypeForExactMatch = exactMatch;
									}
									break;
								case 'l': // label

									FilteringMethod = FilteringMethod.Label;

									if(length > 2)
									{
										filterLabel = s.Substring(2);
										hasLabelFilter = true;
										hasFilter = true;
										filterLabelForExactMatch = exactMatch;
									}
									break;
								case 'v': // value

									FilteringMethod = FilteringMethod.Value;

									if(length > 2)
									{
										filterFieldValue = s.Substring(2);
										hasValueFilter = true;
										hasFilter = true;
										filterValueForExactMatch = exactMatch;
									}
									break;
								case 'c': // class

									FilteringMethod = FilteringMethod.Class;

									if(inspector != null && filterLower.StartsWith("c:", StringComparison.OrdinalIgnoreCase) && inspector.GetType() != typeof(PreferencesInspector))
									{
										var className = rawInput.Substring(2);
										var classType = TypeExtensions.GetType(className);
										if(classType != null)
										{
											#if DEV_MODE
											Debug.Log("Inspecting static members for "+StringUtils.ToString(classType));
											#endif
											inspector.RebuildDrawers(null, classType);
										}
										#if DEV_MODE
										else { Debug.Log("Failed to find class type from string "+StringUtils.ToString(className)); }
										#endif
									}
									break;
								case 'w': // window
									FilteringMethod = FilteringMethod.Window;
									if(inspector != null && inspector.GetType() != typeof(PreferencesInspector))
									{
										UnityEditor.EditorWindow window = null;
										var windowName = s.Substring(2);

										switch(windowName)
										{
											case "powerinspectorwindow":
											case "powerinspector":
												if(inspector.GetType() == typeof(PowerInspector))
												{
													window = inspector.InspectorDrawer as UnityEditor.EditorWindow;
												}
												else
												{
													windowName = "PowerInspectorWindow";
												}
												break;
											case "this":
												window = inspector.InspectorDrawer as UnityEditor.EditorWindow;
												break;
											case "inspector":
												windowName = "InspectorWindow";
												break;
											case "scene":
												windowName = "SceneView";
												break;
											case "game":
												windowName = "GameView";
												break;
											case "project":
												windowName = "ProjectBrowser";
												break;
											case "hierarchy":
												windowName = "SceneHierarchyWindow";
												break;
											case "console":
												windowName = "ConsoleWindow";
												break;
											case "assetstore":
												windowName = "AssetStoreWindow";
												break;
										}

										// to do: handle prompting user to select which one of multiple instances to pick
										if(window == null && windowName.Length > 0)
										{
											var windows = Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>();
											for(int n = windows.Length - 1; n >= 0; n--)
											{
												var testWindow = windows[n];
												if(string.Equals(testWindow.GetType().Name, windowName, StringComparison.OrdinalIgnoreCase))
												{
													window = testWindow;
													break;
												}
											}
										}

										if(window != null)
										{
											inspector.State.ViewIsLocked = true;
											inspector.RebuildDrawers(ArrayPool<Object>.CreateWithContent(window), true);
										}
									}
									break;
								case 's': // select scene objects
									if(inspector != null && filterLower.StartsWith("s:", StringComparison.OrdinalIgnoreCase) && inspector.GetType() != typeof(PreferencesInspector))
									{
										FilteringMethod = FilteringMethod.Scene;

										var className = rawInput.Substring(2);
										var classType = TypeExtensions.GetType(className);
										if(classType != null)
										{
											if(classType.IsComponent())
											{
												#if DEV_MODE
												Debug.Log("Selecting instances of Component "+StringUtils.ToString(classType)+ " in scene.");
												#endif

												#if UNITY_2023_1_OR_NEWER
												var sceneInstances = Object.FindObjectsByType(classType, FindObjectsSortMode.None);
												#else
												var sceneInstances = Object.FindObjectsOfType(classType);
												#endif
												var gameObjects = sceneInstances.GameObjects();
												inspector.Select(gameObjects);
												if(inspector.State.ViewIsLocked)
												{
													inspector.RebuildDrawers(gameObjects, false);
												}
											}
										}
									}
									break;
								case 'a': // select assets
									if(inspector != null && inspector.GetType() != typeof(PreferencesInspector))
									{
										string assetFilter = s.Substring(2);
										if(assetFilter.Length >= 2) //for now skipping one and two letter search strings to avoid huge numbers of results being found
										{
											var guids = UnityEditor.AssetDatabase.FindAssets(assetFilter);
											var assets = FileUtility.LoadAssetsByGuids(guids);
											inspector.Select(assets);
											if(inspector.State.ViewIsLocked)
											{
												inspector.RebuildDrawers(assets, false);
											}
										}
									}
									break;
								case 'i': // icons
									if(inspector != null && inspector.GetType() != typeof(PreferencesInspector))
									{
										FilteringMethod = FilteringMethod.Icon;

										string iconFilter = s.Substring(2);
										if(iconFilter.Length > 0)
										{
											var icons = new HashSet<Object>(); //TO DO: reuse existing instance

											if(iconFilter.Length >= 3) // e.g. "box"
											{
												if(filterLower.StartsWith("i:", StringComparison.OrdinalIgnoreCase))
												{
													string iconExactName = rawInput.Substring(2);
													var iconWithExactName = UnityEditor.EditorGUIUtility.FindTexture(iconExactName);
													if(iconWithExactName != null)
													{
														icons.Add(iconWithExactName);
													}
												}
											}

											foreach(var type in typeof(Object).GetExtendingUnityObjectTypes(true, false))
											{
												var content = UnityEditor.EditorGUIUtility.ObjectContent(null, type);
												if(content != null && content.image != null && content.text.IndexOf(iconFilter, StringComparison.OrdinalIgnoreCase) != -1)
												{
													icons.Add(content.image);
												}
											}

											const float minWidth = 4f;
											const float minHeight = 4f;
											const float maxWidth = 512f;
											const float maxHeight = 512f;

											var allTextures = Resources.FindObjectsOfTypeAll<Texture>();
											for(int n = allTextures.Length - 1; n >= 0; n--)
											{
												var texture = allTextures[n];

												if(texture.name.Length > 0 && texture.name.IndexOf(iconFilter, StringComparison.OrdinalIgnoreCase) != -1 && texture.width >= minWidth && texture.height >= minHeight && texture.width <= maxWidth && texture.height <= maxHeight && !string.Equals(texture.name, "Font Texture"))
												{
													icons.Add(texture);
												}
											}

											var skin = GUI.skin;
											var styles = skin.customStyles;
											for(int n = styles.Length - 1; n >= 0; n--)
											{
												var style = styles[n];
												var background = style.normal.background;
												if(background != null && background.name.IndexOf(iconFilter, StringComparison.OrdinalIgnoreCase) != -1 && background.width >= minWidth && background.height >= minHeight && background.width <= maxWidth && background.height <= maxHeight)
												{
													icons.Add(background);
												}
											}

											int count = icons.Count;
											if(count > 0)
											{
												var array = new Object[count];
												icons.CopyTo(array);
												Array.Sort(array, (a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase)); // to do: use method instead of delegate

												inspector.State.drawers.DisposeMembersAndClearVisibleMembers();
												inspector.State.inspected = array;
												var drawer = CustomAssetDrawer.Create(array, inspector.State.drawers, inspector, GUIContentPool.Create("Icon Results"));
												{
													var customMembers = DrawerArrayPool.Create(count);
													for(int n = 0; n < count; n++)
													{
														customMembers[n] = ObjectReferenceDrawer.Create(array[n], Types.Texture, drawer, GUIContentPool.Create(array[n].name), false, false, true);
													}
													drawer.SetMembers(customMembers);
												}
												inspector.State.drawers.SetMembers(DrawerArrayPool.Create(drawer));
											}
											else
											{
												inspector.RebuildDrawers(ArrayPool<Object>.ZeroSizeArray, true);
											}
										}
									}
									break;
								#if DEV_MODE
								case 'g': // GUIStyles
									if(inspector != null && filterLower.StartsWith("g:", StringComparison.OrdinalIgnoreCase) && inspector.GetType() != typeof(PreferencesInspector))
									{
										FilteringMethod = FilteringMethod.GUIStyle;

										bool found = false;
										var skin = GUI.skin;
										GUIStyle style = null;
										string filterText = rawInput.Substring(2);
										if(filterText.Length > 0)
										{
											string styleNameFilter;

											for(int e = filterText.IndexOf(' '); e != -1; e = filterText.IndexOf(' ', e + 1))
											{
												styleNameFilter = filterText.Substring(0, e);
												style = skin.FindStyle(styleNameFilter);
												if(style != null)
												{
													found = true;
													break;
												}

												if(e == filterText.Length)
												{
													break;
												}
											}

											styleNameFilter = filterText;
											style = skin.FindStyle(styleNameFilter);
											if(style != null)
											{
												found = true;
											}

											if(!found)
											{
												for(int e = filterText.IndexOf(' '); e != -1; e = filterText.IndexOf(' ', e + 1))
												{
													styleNameFilter = filterText.Substring(0, e);

													for(int n = skin.customStyles.Length - 1; n >= 0 ; n--)
													{
														style = skin.customStyles[n];
														if(style.name.StartsWith(styleNameFilter, StringComparison.OrdinalIgnoreCase))
														{
															found = true;
															break;
														}
													}

													if(found)
													{
														break;
													}

													if(e == filterText.Length)
													{
														break;
													}
												}
											}

											if(found)
											{
												string previewText;
												if(styleNameFilter.Length >= filterText.Length - 2)
												{
													previewText = "The quick brown fox jumps over the lazy dog";
												}
												else
												{
													previewText = filterText.Substring(styleNameFilter.Length + 1);
												}

												#if DEV_MODE
												Debug.Log("previewText="+ previewText+", style="+style.name);
												#endif

												inspector.State.drawers.DisposeMembersAndClearVisibleMembers();
												inspector.State.inspected = ArrayPool<Object>.ZeroSizeArray;
												inspector.State.drawers.SetMembers(DrawerArrayPool.Create(StyledTextDrawer.Create(style, previewText, inspector.State.drawers, null, GUIContentPool.Create(style.name))));
											}
										}
										start = charCount;
									}
									break;
								#endif
								#if DEV_MODE && UNITY_EDITOR
								case 'z':
									//dev-only method for resizing the inspector window the specific sizes
									if(inspector != null)
									{
										var window = inspector.InspectorDrawer as UnityEditor.EditorWindow;
										if(window != null)
										{
											int x = s.IndexOf('x');
											if(x != -1)
											{
												float height;
												if(float.TryParse(s.Substring(x+1), out height) && height >= 50f)
												{
													float width;
													if(float.TryParse(s.Substring(2, x - 2), out width) && width >= 280f)
													{
														Debug.LogWarning("RESIZING INSPECTOR WINDOW TO: "+width+"x"+height);
														var pos = window.position;
														pos.width = width;
														pos.height = height;
														window.position = pos;
													}
												}
											}
											else
											{
												float width;
												if(float.TryParse(s.Substring(2), out width) && width >= 280f)
												{
													Debug.LogWarning("RESIZING INSPECTOR WINDOW TO WIDTH: "+width);
													var pos = window.position;
													pos.width = width;
													window.position = pos;
												}
											}
										}
									}
									break;
								#endif
								default:
									filterGeneric = filterGeneric.Length == 0 ? s : string.Concat(filterGeneric, " ", s);
									filtersGeneric.Add(s);
									filtersGenericForExactMatch.Add(exactMatch);
									hasGenericFilter = true;
									hasFilter = true;
									break;
							}
						}
						else
						{
							filterGeneric = filterGeneric.Length == 0 ? s : string.Concat(filterGeneric, " ", s);
							filtersGeneric.Add(s);
							filtersGenericForExactMatch.Add(exactMatch);
							hasGenericFilter = true;
							hasFilter = true;
						}
					}
				}
			}

			if(onFilterChanging != null)
			{
				onFilterChanging(this);
			}

			if(onFilterChanged != null)
			{
				if(InspectorUtility.IsSafeToChangeInspectorContents)
				{
					onFilterChanged();
				}
				else if(inspector != null)
				{
					inspector.OnNextLayout(onFilterChanged);
				}
				else
				{
					InspectorUtility.ActiveInspector.OnNextLayout(onFilterChanged);
				}
			}

			return true;
		}

		/// <summary>
		/// Searches through string for space delimited parts, and removes each part which is a substring of another part. </summary>
		/// <param name="s"> [in,out] Search string with no unnecessary parts. </param>
		private void RemoveUnnecessaryParts(ref string s)
		{
			s = s.Trim();

			int aEnd = s.IndexOf(' ');
			if(aEnd == -1)
			{
				return;
			}

			int aStart = 0;
			int bStart = aEnd + 1;

			string a;
			string b;
			int contains;

			for(int bEnd = s.IndexOf(' ', bStart + 1); bEnd != -1; bEnd = s.IndexOf(' ', bStart + 1))
			{
				a = s.Substring(aStart, aEnd - aStart);
				b = s.Substring(bStart, bEnd - bStart);
				contains = Contains(a, b);

				if(contains == 0)
				{
					aStart = bStart;
					aEnd = bEnd;
					bStart = aEnd + 1;
					continue;
				}

				if(contains == -1)
				{
					s = a + s.Substring(bEnd);
				}
				else if(contains == 1)
				{
					s = s.Substring(bStart);
				}
				RemoveUnnecessaryParts(ref s);
				return;
			}

			// check the last part
			a = s.Substring(aStart, aEnd - aStart);
			b = s.Substring(bStart);
			contains = Contains(a, b);
			if(contains == -1)
			{
				s = a;
			}
			else if(contains == 1)
			{
				s = b;
			}
		}

		/// <summary> Determines if a contains b or vice versa. </summary>
		/// <param name="a"> A string to process. </param>
		/// <param name="b"> A string to process. </param>
		/// <param name="stringComparison"> String comparison type. </param>
		/// <returns> -1 if a contains b, 1 if b contains a, or 0 if neither contain the other. </returns>
		private int Contains(string a, string b, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
		{
			if(a.IndexOf(b, stringComparison) != -1)
			{
				return -1;
			}

			if(b.IndexOf(a, stringComparison) != -1)
			{
				return 1;
			}

			return 0;
		}

		/// <summary> Does this subject pass the type filter? </summary>
		/// <param name="subject"> The subject whose type to test. </param>
		/// <returns> True if no type filter exists of if type passes it. </returns>
		public bool PassesTypeFilter(IDrawer subject)
		{
			return !hasTypeFilter || PassesTypeFilter(subject, filterType, filterTypeForExactMatch);
		}

		/// <summary> Do these Drawer for an UnityObject pass the type filter? </summary>
		/// <param name="subject"> The drawers for an UnityObject whose type to test. </param>
		/// <returns> True passes type test or if there is not filter. </returns>
		public bool PassesFilter(IUnityObjectDrawer subject)
		{
			if(!hasFilter)
			{
				return true;
			}
			
			if(hasTypeFilter)
			{
				// Test type against type filters.
				#if REQUIRE_EXACT_MATCH_FOR_UNITY_OBJECT_TYPE
				// E.g. "t:camera"
				if(string.Equals(filterType, subject.Type.Name, StringComparison.OrdinalIgnoreCase))
				#else
				// E.g. "t:camera" or "t:cam"
				if(PassesTypeFilter(subject, filterType, filterTypeForExactMatch))
				#endif
				{
					#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER_BASIC
					if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
					{ Debug.Log(subject.ToString()+" - PassesFilter(\"" + rawInput + "\"): "+StringUtils.True); }
					#endif

					return true;
				}
			}

			for(int n = filtersGeneric.Count - 1; n >= 0; n--)
			{
				// Test type against generic filters.
				#if REQUIRE_EXACT_MATCH_FOR_UNITY_OBJECT_TYPE
				// E.g. "camera"
				if(string.Equals(filtersGeneric[n], subject.Type.Name, StringComparison.OrdinalIgnoreCase))
				#else
				// E.g. "t:camera" or "t:cam"
				if(PassesTypeFilter(subject, filtersGeneric[n], filtersGenericForExactMatch[n]))
				#endif
				{
					#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER_BASIC
					if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
					{ Debug.Log(subject.ToString()+" - PassesFilter(\"" + rawInput + "\"): "+StringUtils.True); }
					#endif
					return true;
				}
			}

			return false;
		}

		/// <summary> Does this subject pass the type filter? </summary>
		/// <param name="subject"> The subject whose type is tested. </param>
		/// <param name="filter"> The filter string against which to test. </param>
		/// <param name="requireExactMatch"> If true a partial match is not enough. </param>
		/// <returns> True subject passes type test. </returns>
		private static bool PassesTypeFilter([NotNull]IDrawer subject, [NotNull]string filter, bool requireExactMatch)
		{
			if(filter.Length == 0)
			{
				#if DEV_MODE
				Debug.LogWarning("PassesTypeFilter("+subject+") called with an empty filter string. Returning true.");
				#endif
				return true;
			}
			
			var type = subject.Type;

			if(type == null)
			{
				#if DEV_MODE
				Debug.Log(subject.ToString()+".Type was null");
				#endif
				return false;
			}

			string typeName = StringUtils.ToStringSansNamespace(type);

			//if filter contains a dot match left side against namespace and right side against type name
			//e.g. "unity.object" matches Object, "sys.obj" matches "System.Object"
			int namespaceSeparator = filter.LastIndexOf('.');
			if(namespaceSeparator != -1)
			{
				if(requireExactMatch)
				{
					if(typeName.Equals(filter.Substring(namespaceSeparator + 1), StringComparison.OrdinalIgnoreCase) && type.Namespace.Equals(filter.Substring(0, namespaceSeparator), StringComparison.OrdinalIgnoreCase))
					{
						#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
						if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
						{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesTypeFilter(\"" + filter + "\"): "+StringUtils.True); }
						#endif

						return true;
					}
				}
				else if(typeName.IndexOf(filter.Substring(namespaceSeparator + 1), StringComparison.OrdinalIgnoreCase) != -1 && type.Namespace.IndexOf(filter.Substring(0, namespaceSeparator), StringComparison.OrdinalIgnoreCase) != -1)
				{
					#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
					if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
					{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesTypeFilter(\"" + filter + "\"): "+StringUtils.True); }
					#endif

					return true;
				}
			}
			else if(requireExactMatch)
			{
				if(typeName.Equals(filter, StringComparison.OrdinalIgnoreCase))
				{
					#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
					if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
					{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesTypeFilter(\"" + filter + "\"): "+StringUtils.True); }
					#endif

					return true;
				}
			}
			else if(typeName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) != -1)
			{
				#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
				if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
				{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesTypeFilter(\"" + filter + "\"): "+StringUtils.True); }
				#endif

				return true;
			}

			#if DEV_MODE && DEBUG_FAILS_SEARCH_FILTER
			if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
			{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesTypeFilter(\"" + filter + "\"): "+StringUtils.False); }
			#endif

			return false;
		}

		public bool PassesFilter(UnityEditor.SerializedProperty property, ref FilterTestType passedTestMethod)
		{
			if(!hasFilter)
			{
				return true;
			}

			//if this does not pass field label test, return false
			//e.g. "l:position" or "l:position.x"
			if(!PassesLabelFilter(property.propertyPath.Replace('/', '.'), property.displayName, filterLabelForExactMatch, ref passedTestMethod))
			{
				return false;
			}

			//if this does not pass field type test, return false
			//e.g. "t:vector3"
			if(!PassesTypeFilter(property.type, filterType, filterTypeForExactMatch))
			{
				return false;
			}
			
			//if passed all filter tests, return true

			#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER_BASIC
			if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(property.displayName, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
			{ Debug.Log(property.propertyPath+ " (" + property.type + ") - PassesFilter(\"" + rawInput + "\"): "+StringUtils.True); }
			#endif

			return true;
		}

		/// <summary> Does this type pass the type filter? </summary>
		/// <param name="typeName"> The type name (without namespace) which is tested. </param>
		/// <param name="filter"> The filter string against which to test. </param>
		/// <param name="requireExactMatch"> If true a partial match is not enough. </param>
		/// <returns> True if type passes filter test. </returns>
		private static bool PassesTypeFilter(string typeName, string filter, bool requireExactMatch)
		{
			if(filter.Length == 0)
			{
				#if DEV_MODE
				Debug.LogWarning("PassesTypeFilter("+typeName+") called with an empty filter string. Returning true.");
				#endif
				return true;
			}
			
			if(requireExactMatch)
			{
				if(typeName.Equals(filter, StringComparison.OrdinalIgnoreCase))
				{
					#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
					Debug.Log(StringUtils.ToString(typeName) + " - PassesTypeFilter(\"" + filter + "\"): "+StringUtils.True);
					#endif

					return true;
				}
			}
			else if(typeName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) != -1)
			{
				#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
				Debug.Log(StringUtils.ToString(typeName) + " - PassesTypeFilter(\"" + filter + "\"): "+StringUtils.True);
				#endif

				return true;
			}

			#if DEV_MODE && DEBUG_FAILS_SEARCH_FILTER
			Debug.Log(StringUtils.ToString(typeName) + " - PassesTypeFilter(\"" + filter + "\"): "+StringUtils.False);
			#endif

			return false;
		}

		public bool PassesFilter(IDrawer subject, out FilterTestType passedTestMethod)
		{
			passedTestMethod = FilterTestType.None;

			if(!hasFilter)
			{
				return true;
			}

			if(hasLabelFilter)
			{
				// If this does not pass field label test, return false.
				// E.g. "l:position" or "l:position.x".
				if(!PassesLabelFilter(subject, ref passedTestMethod))
				{
					return false;
				}

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(passedTestMethod.HasFlag(FilterTestType.Label) || passedTestMethod.HasFlag(FilterTestType.FullClassName));
				#endif
			}

			if(hasTypeFilter)
			{
				// If this does not pass field type test, return false.
				// E.g. "t:vector3".
				if(!PassesTypeFilter(subject))
				{
					return false;
				}
				passedTestMethod = (FilterTestType)passedTestMethod.SetFlag(FilterTestType.Type);
			}

			if(hasValueFilter)
			{
				// If this does not pass field value test, return false.
				// E.g. "v:true".
				if(!PassesValueFilter(subject))
				{
					return false;
				}
				passedTestMethod = (FilterTestType)passedTestMethod.SetFlag(FilterTestType.Value);
			}

			// For generic search input check type, label AND value,
			// and if any of them passes the test, return true.
			if(!PassesGenericFilters(subject, ref passedTestMethod))
			{
				return false;
			}
			
			// If passed all filter tests, return true.

			#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER_BASIC
			Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesFilter(\"" + rawInput + "\"): "+StringUtils.True);
			#endif

			return true;
		}

		/// <summary>
		/// Determines whether or not the drawer passes the current search filter.
		/// 
		/// If the drawer passes the filter sets passedTestMethod(s) to a value indicating
		/// by which method the subject passed the test. E.g. if the user has added a general
		/// filter without specifying whether to target the label, the type or the value,
		/// then it can be useful thing to know which method helped the drawer pass the filtering.
		/// </summary>
		/// <param name="subject"> The subject. </param>
		/// <param name="passedTestMethod"> [out] The passed test method. </param>
		/// <returns> True if it succeeds, false if it fails. </returns>
		public bool PassesFilter([NotNull]IFieldDrawer subject, out FilterTestType passedTestMethod)
		{
			passedTestMethod = FilterTestType.None;
			if(!hasFilter)
			{
				return true;
			}

			if(hasLabelFilter)
			{
				// If this does not pass field label test, return false.
				// E.g. "l:position" or "l:position.x".
				if(!PassesLabelFilter(subject, ref passedTestMethod))
				{
					return false;
				}

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(passedTestMethod.HasFlag(FilterTestType.Label) || passedTestMethod.HasFlag(FilterTestType.FullClassName));
				#endif
			}
			
			if(hasTypeFilter)
			{
				// If this does not pass field type test, return false.
				// E.g. "t:vector3".
				if(!PassesTypeFilter(subject))
				{
					return false;
				}
				passedTestMethod = (FilterTestType)passedTestMethod.SetFlag(FilterTestType.Type);
			}

			if(hasValueFilter)
			{
				// If this does not pass field value test, return false.
				// E.g. "v:true".
				if(!PassesValueFilter(subject))
				{
					return false;
				}
				passedTestMethod = (FilterTestType)passedTestMethod.SetFlag(FilterTestType.Value);
			}

			// For generic search input check type, label AND value,
			// and if any of them passes the test, return true.
			if(!PassesGenericFilters(subject, ref passedTestMethod))
			{
				return false;
			}
			
			// If passed all filter tests, return true.

			#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER_BASIC
			if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
			{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesFilter(\"" + rawInput + "\"): "+StringUtils.True); }
			#endif

			return true;
		}

		/// <summary> Does this type pass the label filter? </summary>
		/// <param name="subject"> The subject whose label is tested. </param>
		/// <param name="passedTestMethod">
		/// Which method was used when filter was passed. Note that this will only add the first method
		/// that was used for passing the filter, ignoring other methods that could have also been used
		/// to pass the test.
		/// </param>
		/// <returns> True if no type filter exists of if type passes it. </returns>
		public bool PassesLabelFilter(IDrawer subject, ref FilterTestType passedTestMethod)
		{
			return !hasLabelFilter || PassesLabelFilter(subject, filterLabel, filterLabelForExactMatch, ref passedTestMethod);
		}

		/// <summary> Does the subject pass the label filter? </summary>
		/// <param name="subject"> The subject whose label is tested. </param>
		/// <param name="filter"> The filter string against which to test. </param>
		/// <param name="requireExactMatch"> If true a partial match is not enough. </param>
		/// <param name="passedTestMethod">
		/// Which method was used when filter was passed. Note that this will only add the first method
		/// that was used for passing the filter, ignoring other methods that could have also been used
		/// to pass the test.
		/// </param>
		/// <returns> True if subject passes filter test. </returns>
		private static bool PassesLabelFilter(IDrawer subject, string filter, bool requireExactMatch, ref FilterTestType passedTestMethod)
		{
			if(filter.Length == 0)
			{
				#if DEV_MODE
				Debug.LogWarning("PassesLabelFilter("+subject+") called with an empty filter string. Returning true.");
				#endif
				return true;
			}

			return PassesLabelFilter(subject.FullClassName, subject.Name, filter, requireExactMatch, ref passedTestMethod);
		}

		public bool PassesLabelFilter(string fullName, string displayName, bool requireExactMatch, ref FilterTestType passedTestMethod)
		{
			return !hasLabelFilter || PassesLabelFilter(fullName, displayName, filterLabel, requireExactMatch, ref passedTestMethod);
		}

		/// <summary> Does the subject pass the label filter? </summary>
		/// <param name="fullName"> Full name of subject. For example "transform.position.x". </param>
		/// <param name="displayName"> Display name of subject. For example "My Field". </param>
		/// <param name="filter"> The filter string against which to test. </param>
		/// <param name="requireExactMatch"> If true a partial match is not enough. </param>
		/// <param name="passedTestMethod">
		/// Which method was used when filter was passed. Note that this will only add the first method
		/// that was used for passing the filter, ignoring other methods that could have also been used
		/// to pass the test.
		/// </param>
		/// <returns> True if subject passes filter test. </returns>
		public static bool PassesLabelFilter(string fullName, string displayName, string filter, bool requireExactMatch, ref FilterTestType passedTestMethod)
		{
			if(filter.Length == 0)
			{
				#if DEV_MODE
				Debug.LogWarning("PassesLabelFilter("+fullName+") called with an empty filter string. Returning true.");
				#endif
				return true;
			}

			//test full class name of subject. E.g. "transform.position.x"
			//string fullName = subject.FullClassName;
			if(requireExactMatch)
			{
				if(fullName.Equals(filter, StringComparison.OrdinalIgnoreCase))
				{
					passedTestMethod = (FilterTestType)passedTestMethod.SetFlag(FilterTestType.FullClassName);
					if(displayName.Length == fullName.Length)
					{
						passedTestMethod = (FilterTestType)passedTestMethod.SetFlag(FilterTestType.Label);
					}

					#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
					if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(displayName, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
					{ Debug.Log(fullName + ") - PassesLabelFilter(\"" + filter + "\"): "+StringUtils.True+ " (via fullName)"); }
					#endif
					return true;
				}
			}
			else if(fullName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) != -1)
			{
				passedTestMethod = (FilterTestType)passedTestMethod.SetFlag(FilterTestType.FullClassName);
				if(displayName.Length == fullName.Length)
				{
					passedTestMethod = (FilterTestType)passedTestMethod.SetFlag(FilterTestType.Label);
				}

				#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
				if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(displayName, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
				{ Debug.Log(fullName + ") - PassesLabelFilter(\"" + filter + "\"): "+StringUtils.True+ " (via fullName)"); }
				#endif
				return true;
			}

			//test user-displayed label, E.g. "Local Position"
			//string displayName = subject.Name;
			if(requireExactMatch)
			{
				if(displayName.Length != fullName.Length && displayName.Equals(filter, StringComparison.OrdinalIgnoreCase))
				{
					#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
					if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(displayName, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
					{ Debug.Log(fullName + ") - PassesLabelFilter(\"" + filter + "\"): "+StringUtils.True+ " (via display name)"); }
					#endif
					return true;
				}
			}
			else if(displayName.Length != fullName.Length && displayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) != -1)
			{
				#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
				if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(displayName, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
				{ Debug.Log(fullName + ") - PassesLabelFilter(\"" + filter + "\"): "+StringUtils.True+ " (via display name)"); }
				#endif
				return true;
			}

			#if DEV_MODE && DEBUG_FAILS_SEARCH_FILTER
			if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(displayName, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
			{ Debug.Log(fullName + ") - PassesLabelFilter(\"" + filter + "\"): "+StringUtils.False+ " (full name and display failed test)"); }
			#endif
			return false;
		}

		public bool PassesValueFilter(IDrawer subject)
		{
			return !hasValueFilter || PassesValueFilter(subject, filterFieldValue, filterValueForExactMatch);
		}

		/// <summary> Does the subject pass the value filter? </summary>
		/// <param name="subject"> The subject whose value is tested. </param>
		/// <param name="filter"> The filter string against which to test. </param>
		/// <param name="requireExactMatch"> If true a partial match is not enough. </param>
		/// <param name="result"> [out] True to result. </param>
		/// <returns> True if result applies to subject, false if not applicable. </returns>
		private static bool TryTestValueFilter(IDrawer subject, string filter, bool requireExactMatch, out bool result)
		{
			if(filter.Length == 0)
			{
				result = true;
				return false;
			}

			var contentString = subject.ValueToStringForFiltering();
			if(contentString == null)
			{
				#if DEV_MODE && DEBUG_FAILS_SEARCH_FILTER
				if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
				{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesValueFilter(\"" + filter + "\"): "+StringUtils.Green("n/a")+ " (ValueToStringForFiltering returned null)"); }
				#endif
				result = true;
				return false;
			}
			
			if(requireExactMatch)
			{
				if(contentString.Equals(filter, StringComparison.OrdinalIgnoreCase))
				{
					#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
					if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
					{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesValueFilter(\"" + filter + "\"): "+StringUtils.True+ " (\"" + contentString + "\" did not contain filter)"); }
					#endif

					result = true;
					return true;
				}
			}
			else if(contentString.IndexOf(filter, StringComparison.OrdinalIgnoreCase) != -1)
			{
				#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
				if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
				{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesValueFilter(\"" + filter + "\"): "+StringUtils.True+ " (\"" + contentString + "\" did not contain filter)"); }
				#endif

				result = true;
				return true;
			}

			#if DEV_MODE && DEBUG_FAILS_SEARCH_FILTER
			if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
			{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesValueFilter(\"" + filter + "\"): "+StringUtils.False+ " (\""+contentString+ "\" did not contain filter)"); }
			#endif
			result = false;
			return true;
		}

		/// <summary> Does the subject pass the value filter? </summary>
		/// <param name="subject"> The subject whose value is tested. </param>
		/// <param name="filter"> The filter string against which to test. </param>
		/// <param name="requireExactMatch"> If true a partial match is not enough. </param>
		/// <returns> True if subject passes filter, false if not. </returns>
		private static bool PassesValueFilter(IDrawer subject, string filter, bool requireExactMatch)
		{
			if(filter.Length == 0)
			{
				#if DEV_MODE
				Debug.LogWarning("PassesValueFilter("+subject+") called with an empty filter string. Returning true.");
				#endif
				return true;
			}

			var contentString = subject.ValueToStringForFiltering();
			if(contentString == null)
			{
				#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
				if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
				{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesValueFilter(\"" + filter + "\"): "+StringUtils.True+ " (ValueToStringForFiltering returned null)"); }
				#endif
				return true;
			}
			
			if(requireExactMatch)
			{
				if(contentString.Equals(filter, StringComparison.OrdinalIgnoreCase))
				{
					#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
					if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
					{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesValueFilter(\"" + filter + "\"): "+StringUtils.True+ " (\"" + contentString + "\" contained filter)"); }
					#endif

					return true;
				}
			}
			else if(contentString.IndexOf(filter, StringComparison.OrdinalIgnoreCase) != -1)
			{
				#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
				if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
				{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesValueFilter(\"" + filter + "\"): "+StringUtils.True+ " (\"" + contentString + "\" contained filter)"); }
				#endif

				return true;
			}

			#if DEV_MODE && DEBUG_FAILS_SEARCH_FILTER
			if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
			{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesValueFilter(\"" + filter + "\"): "+StringUtils.False+ " (\""+contentString+ "\" did not contain filter)"); }
			#endif
			return false;
		}

		public bool PassesGenericFilters(IDrawer subject, ref FilterTestType passedTestMethod)
		{
			if(!hasGenericFilter)
			{
				return true;
			}

			for(int n = filtersGeneric.Count - 1; n >= 0; n--)
			{
				if(!PassesGenericFilter(subject, filtersGeneric[n], filtersGenericForExactMatch[n], ref passedTestMethod))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Test if subject passes generic filter test.
		/// Returns true if label, type name or value as string contains filter.
		/// </summary>
		/// <param name="subject"> The subject. </param>
		/// <param name="filter"> The filter against which to test. </param>
		/// <param name="requireExactMatch"> If true a partial match is not enough. </param>
		/// <param name="passedTestMethod">
		/// Which method was used when filter was passed. Note that this will only add the first method
		/// that was used for passing the filter, ignoring other methods that could have also been used
		/// to pass the test.
		/// </param>
		/// <returns> True if subject passes filter test, false if it fails. </returns>
		public static bool PassesGenericFilter(IDrawer subject, string filter, bool requireExactMatch, ref FilterTestType passedTestMethod)
		{
			if(filter.Length == 0)
			{
				#if DEV_MODE
				Debug.LogWarning("PassesGenericFilter("+subject+") called with an empty filter string. Returning true.");
				#endif
				return true;
			}

			//test label...
			if(PassesLabelFilter(subject, filter, requireExactMatch, ref passedTestMethod))
			{
				#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
				if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
				{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesGenericFilter(\"" + filter + "\"): "+StringUtils.True+ " (via label)"); }
				#endif
				return true;
			}
				
			//...test type...
			if(PassesTypeFilter(subject, filter, requireExactMatch))
			{
				passedTestMethod = (FilterTestType)passedTestMethod.SetFlag(FilterTestType.Type);

				#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
				if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
				{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesGenericFilter(\"" + filter + "\"): "+StringUtils.True+ " (via type)"); }
				#endif
				return true;
			}
			
			bool passedValueTest;
			if(TryTestValueFilter(subject, filter, requireExactMatch, out passedValueTest))
			{
				if(passedValueTest)
				{
					passedTestMethod = (FilterTestType)passedTestMethod.SetFlag(FilterTestType.Value);

					#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
					if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
					{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesGenericFilter(\"" + filter + "\"): "+StringUtils.True+ " (via value)"); }
					#endif
					return true;
				}
				
				#if DEV_MODE && DEBUG_FAILS_SEARCH_FILTER
				if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
				{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesGenericFilter(\"" + filter + "\"): "+StringUtils.False+ " (via value)"); }
				#endif
				return false;
			}

			//if failed all three tests, return false
			#if DEV_MODE && DEBUG_FAILS_SEARCH_FILTER
			if(string.IsNullOrEmpty(OnlyDebugFieldByName) || string.Equals(subject.Name, OnlyDebugFieldByName, StringComparison.OrdinalIgnoreCase))
			{ Debug.Log(subject.FullClassName+ " (" + StringUtils.ToString(subject.Type) + ") - PassesGenericFilter(\"" + filter + "\"): "+StringUtils.False); }
			#endif
			return false;
		}

		public override string ToString()
		{
			return "\""+rawInput+"\"";
		}

		public void OnActivateInputGiven(IInspector inspector)
		{
			if(rawInput.StartsWith("c:", StringComparison.OrdinalIgnoreCase))
			{
				SetFilter("", inspector);
			}

			inspector.Manager.Select(inspector, InspectorPart.Viewport, null, ReasonSelectionChanged.KeyPressShortcut);
		}
	}
}