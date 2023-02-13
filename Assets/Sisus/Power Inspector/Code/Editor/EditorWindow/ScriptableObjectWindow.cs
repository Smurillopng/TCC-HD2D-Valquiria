using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Sisus
{
	public class ScriptableObjectWindow<TScriptableObject> : EditorWindow where TScriptableObject : ScriptableObject
	{
		private TScriptableObject[] targets;
		private AssetDrawer drawer;

		public AssetDrawer Drawer
		{
			get
			{
				return drawer;
			}
		}

		public TScriptableObject[] Targets
		{
			get
			{
				return targets;
			}

			set
			{
				var valueWas = targets;
				targets = value;
				if(!ArrayExtensions.ContentsMatch(value, valueWas))
				{
					RebuildDrawer(InspectorUtility.ActiveInspector);
				}
			}
		}

		public static TScriptableObjectWindow Create<TScriptableObjectWindow>() where TScriptableObjectWindow : ScriptableObjectWindow<TScriptableObject>
		{
			return Create<TScriptableObjectWindow>(CreateInstance<TScriptableObject>());
		}

		public static TScriptableObjectWindow Create<TScriptableObjectWindow>(TScriptableObject scriptableObject) where TScriptableObjectWindow : ScriptableObjectWindow<TScriptableObject>
		{
			var scriptableObjects = ArrayPool<TScriptableObject>.Create(1);
			scriptableObjects[0] = scriptableObject;
			return Create<TScriptableObjectWindow>(scriptableObjects);
		}

		public static TScriptableObjectWindow Create<TScriptableObjectWindow>(TScriptableObject[] scriptableObjects) where TScriptableObjectWindow : ScriptableObjectWindow<TScriptableObject>
		{
			var result = CreateInstance<TScriptableObjectWindow>();
			result.Targets = scriptableObjects;
			return result;
		}
		
		[UsedImplicitly]
		private void OnGUI()
		{
			var drawPosition = position;
			drawPosition.x = 0f;
			drawPosition.y = 0f;
			Draw(drawPosition);
		}

		protected virtual void Draw(Rect drawPosition)
		{
			drawer.Draw(drawPosition);
		}

		[UsedImplicitly]
		protected virtual void OnInspectorUpdate()
		{
			UpdateCachedValuesFromFields();
		}

		private void UpdateCachedValuesFromFields()
		{
			drawer.UpdateCachedValuesFromFieldsRecursively();
		}

		[UsedImplicitly]
		private void OnDestroy()
		{
			Dispose();
		}

		protected virtual void Dispose()
		{
			drawer.Dispose();
		}

		private void RebuildDrawer(IInspector inspector)
		{
			drawer = AssetDrawer.Create(targets, null, inspector);
		}
	}
}