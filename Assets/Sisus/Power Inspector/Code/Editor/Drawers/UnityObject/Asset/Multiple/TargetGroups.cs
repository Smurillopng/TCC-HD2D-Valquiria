using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public class TargetsGroupedByType
	{
		private readonly List<TargetGroup> groups = new List<TargetGroup>(2);
		private int count;

		public TargetGroup this[int index]
		{
			get
			{
				return groups[index];
			}
		}
		
		public int Count
		{
			get
			{
				return count;
			}
		}

		public void Setup(Object[] targets)
		{
			Clear();

			#if UNITY_EDITOR
			var sizeWas = UnityEditor.EditorGUIUtility.GetIconSize();
			UnityEditor.EditorGUIUtility.SetIconSize(new Vector2(16f, 16f));
			#endif

			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var target = targets[n];
				// not sure how this could happen, but making sure
				if(target == null)
				{
					#if DEV_MODE
					Debug.LogWarning("TargetsGroupedByType target @ #"+n+" was null. Skipping");
					#endif
					continue;
				}

				var type = target.GetType();
				
				TargetGroup addToGroup = null;
				for(int g = count - 1; g >= 0; g--)
				{
					var group = groups[g];
					if(group.type == type)
					{
						addToGroup = group;
						break;
					}
				}
				if(addToGroup == null)
				{
					addToGroup = TargetGroup.Create(type);
					groups.Add(addToGroup);
					count++;
				}
				addToGroup.Add(target);
			}

			for(int n = count - 1; n >= 0; n--)
			{
				groups[n].OnAfterAllMembersAdded();
			}

			#if UNITY_EDITOR
			UnityEditor.EditorGUIUtility.SetIconSize(sizeWas);
			#endif
		}

		public void Clear()
		{
			if(count > 0)
			{
				for(int g = count - 1; g >= 0; g--)
				{
					groups[g].Dispose();
				}
				groups.Clear();
				count = 0;
			}
		}
	}

	public class TargetGroup
	{
		private static readonly Dictionary<Type, TargetGroup> Pool = new Dictionary<Type, TargetGroup>(2);

		public Type type;
		public GUIContent label = new GUIContent();
		public Texture preview;
		public readonly List<Object> targets = new List<Object>();

		internal static TargetGroup Create([NotNull]Type type)
		{
			TargetGroup result;
			if(!Pool.TryGetValue(type, out result))
			{
				result = new TargetGroup();
				result.type = type;
			}

			#if UNITY_EDITOR
			result.preview = UnityEditor.AssetPreview.GetMiniTypeThumbnail(type);
			#else
			result.preview = InspectorUtility.Preferences.graphics.PrefabIcon;
			#endif
			return result;
		}

		private TargetGroup() { }

		internal void Add(Object target)
		{
			targets.Add(target);
		}

		internal void OnAfterAllMembersAdded()
		{
			label.text = StringUtils.ToString(targets.Count) + " x " + StringUtils.ToStringSansNamespace(type);
		}

		internal void Dispose()
		{
			targets.Clear();
			if(type != null)
			{
				Pool[type] = this;
			}
		}
	}
}