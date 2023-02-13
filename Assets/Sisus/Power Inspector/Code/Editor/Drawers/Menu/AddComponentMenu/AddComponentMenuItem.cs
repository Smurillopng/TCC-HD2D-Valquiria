//#define DEBUG_PREVIEW_FECHED

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public class AddComponentMenuItem
	{
		public string label;
		public Type type;
		[NonSerialized]
		public AddComponentMenuItem parent;
		public AddComponentMenuItem[] children;
		[NonSerialized]
		private Texture preview;
		[NonSerialized]
		private bool previewFetched;
		[NonSerialized]
		private bool fetchedPreviewNotNull;

		public bool IsGroup
		{
			get
			{
				return type == null;
			}
		}

		public Texture Preview
		{
			get
			{
				if(preview == null && (!previewFetched || fetchedPreviewNotNull))
				{
					UpdatePreview();
				}

				return preview;
			}
		}

		[CanBeNull]
		public string Namespace
		{
			get
			{
				if(type != null)
				{
					return type.Namespace;
				}
				
				for(int n = children.Length - 1; n >= 0; n--)
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
			previewFetched = true;

			#if UNITY_EDITOR
			if(IsGroup)
			{
				var child = GetFirstNonGroupChild();
				if(child != null)
				{
					var source = child.Preview as Texture2D;
					preview = TextureUtility.Resize(source, 15, 15);

					if(preview != null)
					{
						preview.filterMode = FilterMode.Trilinear;
						fetchedPreviewNotNull = true;
						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(preview.width == 15 && preview.height == 15);
						#endif
					}
					#if DEV_MODE
					else { Debug.LogWarning("AddComponentMenuItem.UpdatePreview TextureUtility.Resize returned null for group \"" + label+"\"."); }
					#endif
				}
				#if DEV_MODE
				else { Debug.LogWarning("AddComponentMenuItem.UpdatePreview called for group \""+label+"\" but had no children."); }
				#endif
				return;
			}

			UnityEditor.EditorGUIUtility.SetIconSize(Vector2.zero);
			preview = UnityEditor.AssetPreview.GetMiniTypeThumbnail(type);
			
			if(preview == null && Types.MonoBehaviour.IsAssignableFrom(type))
			{
				preview = InspectorUtility.Preferences.graphics.CSharpScriptIcon;
			}

			if(preview != null)
			{
				fetchedPreviewNotNull = true;
				preview.filterMode = FilterMode.Point;
			}
			#endif
		}

		private AddComponentMenuItem(){}

		public static AddComponentMenuItem Group(string groupLabel, [CanBeNull]AddComponentMenuItem setParent, Texture setPreview)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(string.Equals(groupLabel, "Unity Engine"))
			{
				Debug.LogError("AddComponentMenu Group \""+ groupLabel + "\" being created.");
			}
			#endif

			var group = new AddComponentMenuItem();
			group.label = groupLabel;
			group.parent = setParent;
			group.children = ArrayPool<AddComponentMenuItem>.ZeroSizeArray;
			group.preview = setPreview;
			if(setPreview != null)
			{
				group.previewFetched = true;
				group.fetchedPreviewNotNull = true;
			}

			#if DEV_MODE &&  DEBUG_PREVIEW_FECHED
			if(group.previewFetched) { Debug.Log("Group \""+groupLabel+"\" fetched with previewFetched="+true+", preview="+setPreview.name);  }
			#endif

			return group;
		}
		
		public static AddComponentMenuItem Item([NotNull]Type setType, [NotNull]string classLabel, [CanBeNull]AddComponentMenuItem setParent)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(setParent != null && string.Equals(setParent.label, "Unity Engine"))
			{
				Debug.LogError("AddComponentMenu Item \"" + classLabel + "\" being created under \"Unity Engine\" group. Type should be added under a more descriptive Group in InspectorPreferences.addComponentMenuConfig.");
			}
			Debug.Assert(setType != null);
			#endif

			var item = new AddComponentMenuItem();
			item.parent = setParent;
			item.label = classLabel;
			item.type = setType;
			return item;
		}

		public bool Contains(Type targetType)
		{
			if(children != null)
			{
				for(int n = children.Length - 1; n >= 0; n--)
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

		public void AddChild(string classLabel, Type setType)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(string.Equals(label, "Unity Engine"))
			{
				Debug.LogError("AddComponentMenu Item \"" + classLabel + "\" being created under \"Unity Engine\" group. Type should be added under a more descriptive Group in InspectorPreferences.addComponentMenuConfig.");
			}
			if(setType.IsBaseComponentType())
			{
				Debug.LogError("AddComponentMenu Item \"" + classLabel + "\" type "+setType+" was a base component type and should not be shown in the add component menu.");
			}
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			int countBeforeAdd = children.Length;
			#endif

			var addItem = Item(setType, classLabel, this);
			ArrayExtensions.Add(ref children, addItem);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(children.Length == countBeforeAdd + 1);
			#endif
		}
		
		public string FullLabel()
		{
			if(parent != null)
			{
				return string.Concat(parent.FullLabel(), "/", label);
			}
			return label;
		}

		public void GetClassLabelsFlattened(ref List<string> labels, ref Dictionary<string, AddComponentMenuItem> items)
		{
			if(children == null)
			{
				string fullLabel = FullLabel();
				labels.Add(fullLabel);
				items[fullLabel] = this;
				return;
			}

			for(int n = children.Length - 1; n >= 0; n--)
			{
				children[n].GetClassLabelsFlattened(ref labels, ref items);
			}
		}

		[CanBeNull]
		public Type TypeOrAnyChildType()
		{
			if(type != null)
			{
				return type;
			}

			for(int n = children.Length - 1; n >= 0; n--)
			{
				var childType = children[n].type;
				if(childType != null)
				{
					return childType;
				}
			}

			return null;
		}

		[CanBeNull]
		public AddComponentMenuItem GetFirstNonGroupChild()
		{
			int count = children.Length;

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

		public override string ToString()
		{
			return label;
		}
	}
}