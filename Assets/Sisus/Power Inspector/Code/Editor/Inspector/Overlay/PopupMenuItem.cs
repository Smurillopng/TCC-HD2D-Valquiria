using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	public class PopupMenuItem : IComparable<PopupMenuItem>
	{
		private static readonly Pool<PopupMenuItem> Pool = new Pool<PopupMenuItem>(9500);
		private static readonly StringBuilder CachedStringBuilder = new StringBuilder(200);

		/// <summary>
		/// The label for this group or leaf item
		/// </summary>
		public string label;
		
		public string secondaryLabel;

		[CanBeNull]
		public Type type;

		/// <summary>
		/// The parent of this group or leaf item
		/// </summary>
		[NonSerialized]
		public PopupMenuItem parent;

		/// <summary>
		/// The child PopupMenuItems that this group contains.
		/// For leaf items this is an empty list.
		/// </summary>
		public List<PopupMenuItem> children = new List<PopupMenuItem>();
		
		/// <summary>
		/// Preview icon to be displayed next to the item or group
		/// </summary>
		private Texture preview;

		/// <summary>
		/// This is set true if preview value is set manually or an attempt has been
		/// made to fetch it manually.  This is true even if resulting preview value is null.
		/// </summary>
		private bool previewFetched;

		/// <summary>
		/// True if this item is group containing more items, instead of a leaf menu item.
		/// </summary>
		private bool isGroup;

		private object value;

		private MenuItemValueType valueType;

		public object IdentifyingObject
		{
			get
			{
				return valueType != MenuItemValueType.Disregard ? value : type;
			}
		}

		public bool IsGroup
		{
			get
			{
				return isGroup;
			}
		}

		public Texture Preview
		{
			get
			{
				if(!previewFetched)
				{
					UpdatePreview();
				}

				return preview;
			}

			set
			{
				previewFetched = true;
				preview = value;
			}
		}

		public string Namespace
		{
			get
			{
				if(type != null)
				{
					return type.Namespace;
				}
				
				for(int n = children.Count - 1; n >= 0; n--)
				{
					var childNamespace = children[n].Namespace;
					if(!string.IsNullOrEmpty(childNamespace))
					{
						return childNamespace;
					}
				}

				return null;
			}
		}

		private void UpdatePreview()
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("UpdatePreview");
			#endif

			previewFetched = true;

			if(IsGroup)
			{
				var child = GetFirstNonGroupChild();
				if(child != null)
				{
					var childValue = child.IdentifyingObject;
					if(childValue is Component || childValue is GameObject)
					{
						preview = InspectorUtility.Preferences.graphics.PrefabIcon;
					}
					else
					{
						preview = InspectorUtility.Preferences.graphics.DirectoryIcon;
					}
				}
				else
				{
					preview = InspectorUtility.Preferences.graphics.DirectoryIcon;
				}
				
				#if DEV_MODE || PROFILE_POWER_INSPECTOR
				Profiler.EndSample();
				#endif
				return;
			}
			
			GetPreviewUsingValueOrType(value, type, valueType, ref preview);
			
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}

		[Pure]
		private static void GetPreviewUsingValueOrType([CanBeNull]object value, [CanBeNull]Type type, MenuItemValueType valueType, ref Texture preview)
		{
			#if UNITY_EDITOR
			if(TryGetPreviewUsingValue(value, type, valueType, ref preview))
			{
				return;
			}
			#endif

			if(type != null)
			{
				#if UNITY_EDITOR
				preview = AssetPreview.GetMiniTypeThumbnail(type);
				if(preview != null)
				{
					return;
				}
				#endif

				var typeNamespace = type.Namespace;
				if(typeNamespace != null)
				{
					int i = typeNamespace.IndexOf('.');
					if(i != -1)
					{
						typeNamespace = typeNamespace.Substring(0, i);
					}

					switch(typeNamespace)
					{
						case "System":
							preview = InspectorUtility.Preferences.graphics.DotNetFileIcon;
							#if DEV_MODE || PROFILE_POWER_INSPECTOR
							Profiler.EndSample();
							#endif
							return;
						case "UnityEngine":
						case "UnityEngineInternal":
							preview = InspectorUtility.Preferences.graphics.UnityFileIcon;
							#if DEV_MODE || PROFILE_POWER_INSPECTOR
							Profiler.EndSample();
							#endif
							return;
						case "UnityEditor":
						case "UnityEditorInternal":
							preview = InspectorUtility.Preferences.graphics.UnityEditorFileIcon;
							#if DEV_MODE || PROFILE_POWER_INSPECTOR
							Profiler.EndSample();
							#endif
							return;
					}
				}

				preview = InspectorUtility.Preferences.graphics.CSharpScriptIcon;
			}
		}

		#if UNITY_EDITOR
		[Pure]
		private static bool TryGetPreviewUsingValue([CanBeNull]object value, [CanBeNull]Type type, MenuItemValueType valueType, ref Texture preview)
		{
			if(value == null)
			{
				return false;
			}

			switch(valueType)
			{
				case MenuItemValueType.Undefined:
					if(TryGetPreviewUsingValue(value as Object, type, MenuItemValueType.UnityObject, ref preview))
					{
						return true;
					}
					string valueString = value as string;
					return !string.IsNullOrEmpty(valueString) && TryGetPreviewUsingStringValue(value as string, type, MenuItemValueType.Undefined, ref preview);
				case MenuItemValueType.UnityObject:
					var obj = value as Object;
					if(obj != null)
					{
						preview = AssetPreview.GetAssetPreview(obj);
						return preview != null;
					}
					return false;
				case MenuItemValueType.Disregard:
					return false;
				default:
					valueString = value as string;
					return !string.IsNullOrEmpty(valueString) && TryGetPreviewUsingStringValue((string)value, type, valueType, ref preview);

			}
		}

		private static bool TryGetPreviewUsingStringValue([NotNull]string stringValue, [CanBeNull]Type type, MenuItemValueType valueType, ref Texture preview)
		{
			switch(valueType)
			{
				case MenuItemValueType.Undefined:
				
					if(TryGetPreviewUsingStringValue(stringValue, type, MenuItemValueType.AssetPath, ref preview))
					{
						return true;
					}
					if(TryGetPreviewUsingStringValue(stringValue, type, MenuItemValueType.AssetGuid, ref preview))
					{
						return true;
					}
					return TryGetPreviewUsingStringValue(stringValue, type, MenuItemValueType.HierarchyPath, ref preview);
				case MenuItemValueType.AssetPath:
					preview = AssetDatabase.GetCachedIcon(stringValue);
					return preview != null;
				case MenuItemValueType.AssetGuid:
					var guid = AssetDatabase.GUIDToAssetPath(stringValue);
					if(!string.IsNullOrEmpty(guid))
					{
						preview = AssetDatabase.GetCachedIcon(guid);
						return preview != null;
					}
					return false;
				case MenuItemValueType.HierarchyPath:
					if(type != null && type.IsComponent())
					{
						var component = HierarchyUtility.FindComponentByHierarchyPath(stringValue, type);
						preview = AssetPreview.GetAssetPreview(component);
						return preview != null;
					}

					var gameObject = HierarchyUtility.FindByHierarchyPath(stringValue);
					if(gameObject != null)
					{
						preview = AssetPreview.GetAssetPreview(gameObject);
						return preview != null;
					}
					return false;
			}

			throw new NotImplementedException("TryGetPreviewUsingStringValue does not support MenuItemValueType " + valueType);
		}
		#endif

		private PopupMenuItem(){}

		public static PopupMenuItem Group(string groupLabel, string secondaryLabel, PopupMenuItem setParent, Texture setPreview = null)
		{
			PopupMenuItem group;
			if(!Pool.TryGet(out group))
			{
				group = new PopupMenuItem();
			}
			group.isGroup = true;
			group.label = groupLabel;
			group.secondaryLabel = secondaryLabel;
			group.type = null;
			group.parent = setParent;
			group.children.Clear();
			group.preview = setPreview;
			group.previewFetched = setPreview != null;
			return group;
		}
		
		/// <summary> Create PopupMenuItem, with preview icon generated based on value only if/when needed. </summary>
		/// <param name="type"> Type represented by the menu item. This can be used when generating preview icon for the item. </param>
		/// <param name="label"> The label for the item. </param>
		/// <param name="secondaryLabel"> The secondary label for the item (currently shown as a tooltip). </param>
		/// <param name="parent"> Parent PopupMenuItem which contains the created item. This may be null. </param>
		/// <returns> A PopupMenuItem. </returns>
		public static PopupMenuItem Item([CanBeNull]Type type, string label, string secondaryLabel, PopupMenuItem parent)
		{
			PopupMenuItem item;
			if(!Pool.TryGet(out item))
			{
				item = new PopupMenuItem();
			}
			item.value = type;
			item.isGroup = false;
			item.parent = parent;
			item.children.Clear();
			item.label = label;
			item.valueType = MenuItemValueType.Disregard;
			if(secondaryLabel == null)
			{
				if(type != null)
				{
					item.secondaryLabel = type.Namespace + "\n" + type.Assembly.GetName().Name;
				}
				else
				{
					item.secondaryLabel = "";
				}
			}
			else
			{
				item.secondaryLabel = secondaryLabel;
			}
			item.type = type;
			return item;
		}

		/// <summary> Create PopupMenuItem, with preview icon generated based on type or value only if/when needed. </summary>
		/// <param name="value"> The value represented by the menu item. This may be null. </param>
		/// <param name="type"> Type represented by the menu item. This can be used when generating preview icon for the item. </param>
		/// <param name="label"> The label for the item. </param>
		/// <param name="secondaryLabel"> The secondary label for the item (currently shown as a tooltip). </param>
		/// <param name="parent"> Parent PopupMenuItem which contains the created item. This may be null. </param>
		/// <param name="valueType"> Describes the type of value. Used when generating a preview for the menu item using the value if/when the menu item is shown. </param>
		/// <returns> A PopupMenuItem. </returns>
		public static PopupMenuItem Item([CanBeNull]object value, [CanBeNull]Type type, string label, string secondaryLabel, [CanBeNull]PopupMenuItem parent, MenuItemValueType valueType = MenuItemValueType.Undefined)
		{
			PopupMenuItem item;
			if(!Pool.TryGet(out item))
			{
				item = new PopupMenuItem();
			}
			item.isGroup = false;
			item.parent = parent;
			item.children.Clear();
			item.label = label;
			item.type = type;
			item.value = value;
			item.valueType = valueType;

			if(secondaryLabel == null)
			{
				if(type != null)
				{
					item.secondaryLabel = type.Namespace + "\n" + type.Assembly.GetName().Name;
				}
				else
				{
					item.secondaryLabel = "";
				}
			}
			else
			{
				item.secondaryLabel = secondaryLabel;
			}
			return item;
		}

		public static PopupMenuItem Item([NotNull]Object unityObject, string itemLabel, string secondaryLabel, PopupMenuItem setParent = null)
		{
			return Item(unityObject, unityObject.GetType(), itemLabel, secondaryLabel, setParent, MenuItemValueType.UnityObject);
		}

		public static PopupMenuItem Item([NotNull]Object unityObject, PopupMenuItem setParent = null)
		{
			return Item(unityObject, unityObject.GetType(), unityObject.name, unityObject.HierarchyOrAssetPath(), setParent, MenuItemValueType.UnityObject);
		}

		/// <summary> Create PopupMenuItem with given preview texture. </summary>
		/// <param name="value"> The value represented by the menu item. This may be null. </param>
		/// <param name="type"> Type represented by the menu item. This can be used when generating </param>
		/// <param name="label"> The label for the item. </param>
		/// <param name="secondaryLabel"> The secondary label for the item (currently shown as a tooltip). </param>
		/// <param name="parent"> Parent PopupMenuItem which contains the created item. This may be null. </param>
		/// <param name="preview"> The icon to display next to the item. If null, item will have no icon. </param>
		/// <returns> A PopupMenuItem. </returns>
		public static PopupMenuItem Item([CanBeNull]object value, [CanBeNull]Type type, string label, string secondaryLabel, [CanBeNull]PopupMenuItem parent, [CanBeNull]Texture preview)
		{
			var result = Item(value, type, label, secondaryLabel, parent);
			result.Preview = preview;
			return result;
		}

		public bool Contains(Type targetType)
		{
			if(IsGroup)
			{
				for(int n = children.Count - 1; n >= 0; n--)
				{
					if(children[n].Contains(targetType))
					{
						return true;
					}
				}
				return false;
			}
			return type == targetType;
		}

		public PopupMenuItem AddChild(string itemLabel, string setSecondaryLabel, [CanBeNull]Type setType)
		{
			var addItem = Item(setType, itemLabel, setSecondaryLabel, this);
			children.Add(addItem);
			return addItem;
		}

		public PopupMenuItem AddChild(string itemLabel, string itemSecondaryLabel, [CanBeNull]object itemValue, [CanBeNull]Type itemType, MenuItemValueType setValueType = MenuItemValueType.Undefined, bool addSorted = false)
		{
			var addItem = Item(itemValue, itemType, itemLabel, itemSecondaryLabel, this, setValueType);
			if(addSorted)
			{
				children.AddSorted(addItem);
			}
			else
			{
				children.Add(addItem);
			}
			return addItem;
		}

		public PopupMenuItem AddChild(string itemLabel, string itemSecondaryLabel)
		{
			var addItem = Item(null, null, itemLabel, itemSecondaryLabel, this, MenuItemValueType.Disregard);
			children.Add(addItem);
			return addItem;
		}

		public void AddChild(PopupMenuItem addItem)
		{
			addItem.parent = this;
			children.Add(addItem);
		}
		
		public string FullLabel(char delimiter = '/')
		{
			if(parent != null)
			{
				parent.FullLabel(CachedStringBuilder, delimiter);
				CachedStringBuilder.Append(delimiter);
				CachedStringBuilder.Append(label);
				var result = CachedStringBuilder.ToString();
				CachedStringBuilder.Length = 0;
				return result;
			}
			return label;
		}

		/// <summary>
		/// Gets label of this item along with prefixes from labels of all the groups it is nested inside.
		/// </summary>
		/// <param name="sb"> [in,out] StringBuilder into which the label should be appended. </param>
		/// <param name="delimiter">character to use between group labels and item label</param>
		public void FullLabel(StringBuilder sb, char delimiter)
		{
			if(parent != null)
			{
				parent.FullLabel(sb, delimiter);
				sb.Append(delimiter);
			}
			sb.Append(label);
		}

		/// <summary>
		/// Gets full labels from this item its children and grandchildren labels into one flat list.
		/// </summary>
		/// <param name="labels"> [in,out] The labels. </param>
		public void GetFullLabelsInChildren(ref List<string> labels)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("PopupMenuItem.GetClassLabelsFlattened");
			#endif

			string fullLabel = FullLabel();

			if(IsGroup)
			{
				for(int n = children.Count - 1; n >= 0; n--)
				{
					children[n].GetFullLabelsInChildren(ref labels);
				}
			}
			else
			{
				labels.Add(fullLabel);
			}

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
		}

		public void GetFullLabelsInChildren(ref List<string> labels, ref Dictionary<object, PopupMenuItem> itemsById)
		{
			string fullLabel = FullLabel();
			if(!IsGroup)
			{
				labels.Add(fullLabel);
				itemsById[IdentifyingObject] = this;
				return;
			}

			for(int n = children.Count - 1; n >= 0; n--)
			{
				children[n].GetFullLabelsInChildren(ref labels, ref itemsById);
			}
		}

		public int CompareTo(PopupMenuItem other)
		{
			if(isGroup)
			{
				if(!other.isGroup)
				{
					return -1;
				}
			}
			else if(other.isGroup)
			{
				return 1;
			}

			return string.Compare(label, other.label);
		}

		public override string ToString()
		{
			if(isGroup)
			{
				return label + "(Group)";
			}
			return label + "("+StringUtils.ToString(type)+")";
		}

		public void Sort()
		{
			if(IsGroup)
			{
				children.Sort();
				for(int n = children.Count - 1; n >= 0; n--)
				{
					children[n].Sort();
				}
			}
		}

		[CanBeNull]
		public PopupMenuItem GetFirstNonGroupChild()
		{
			int count = children.Count;

			for(int n = 0; n < count; n++)
			{
				var child = children[n];
				if(!child.IsGroup)
				{
					return child;
				}
			}

			for(int n = 0; n < count; n++)
			{
				var child = children[n].GetFirstNonGroupChild();
				if(child != null)
				{
					return child;
				}
			}

			return null;
		}

		public void Dispose()
		{
			value = null;
			type = null;
			secondaryLabel = "";
			parent = null;
			preview = null;
			previewFetched = false;
			isGroup = false;
			valueType = MenuItemValueType.Disregard;

			for(int n = children.Count - 1; n >= 0; n--)
			{
				children[n].Dispose();
			}
			children.Clear();
			
			var dispose = this;
			Pool.Dispose(ref dispose);
		}

		public static void PrintPoolSize()
		{
			Debug.Log("PopupMenuItem Pool Size Now: "+Pool.Count);
		}
	}
}