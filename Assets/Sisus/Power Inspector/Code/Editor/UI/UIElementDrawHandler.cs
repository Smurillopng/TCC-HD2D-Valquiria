#if UNITY_2019_1_OR_NEWER // UI Toolkit doesn't exist in older versions
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Sisus
{
	[Serializable]
	public class UIElementDrawHandler : VisualElement
	{
		private readonly List<ElementData> elements = new List<ElementData>();

		public int ElementCount
		{
			get
			{
				return elements.Count;
			}
		}

		public void Add(VisualElement element, IDrawer drawer)
		{
			#if DEV_MODE
			if(drawer.Type == null)
			{
				UnityEngine.Debug.LogWarning(drawer+".Type was null. inactive="+drawer.Inactive);
			}
			Type editorType;
			if(drawer.Type != null && (CustomEditorUtility.TryGetCustomEditorType(drawer.Type, out editorType) || CustomEditorUtility.TryGetPropertyDrawerType(drawer.Type, out editorType)))
			{
				element.name = editorType.Name;
			}
			#endif

			element.style.position = new StyleEnum<Position>(Position.Absolute);
			element.style.height = new StyleLength(StyleKeyword.Auto);
			var data = new ElementData(drawer, element);
			elements.Add(data);
			Add(element);
			data.Apply();
		}

		public void OnScriptsReloaded()
		{
			for(int n = elements.Count - 1; n >= 0; n--)
			{
				var element = elements[n];
				
				// This can currently happen I think after assembly reloading.
				if(element.inspector == null || element.inspector.InspectorDrawer == null)
				{
					elements.RemoveAt(n);

					if(element.element == null)
					{
						continue;
					}

					int index = IndexOf(element.element);
					if(index != -1)
					{
						RemoveAt(index);
					}
				}
			}
		}

		public new void Remove(VisualElement element)
		{
			for(int n = elements.Count - 1; n >= 0; n--)
			{
				if(elements[n].element == element)
				{
					elements.RemoveAt(n);
				}
			}

			int index = IndexOf(element);
			if(index != -1)
			{
				RemoveAt(index);
			}
		}

		public void Update()
		{
			for(int n = elements.Count - 1; n >= 0; n--)
			{
				var element = elements[n];
				
				// This can currently happen I think after assembly reloading.
				if(element.inspector == null || element.inspector.InspectorDrawer == null)
				{
					continue;
				}

				element.Apply();
			}
		}

		[Serializable]
		public class ElementData
		{
			public readonly IDrawer drawer;
			public readonly IInspector inspector;
			public readonly VisualElement element;

			public ElementData(IDrawer drawer, VisualElement element)
			{
				this.drawer = drawer;
				inspector = drawer.Inspector;
				this.element = element;
			}

			public void Apply()
			{
				if(drawer.Inactive || !IsShownInInspector(drawer))
				{
					element.visible = false;
					return;
				}

				var unityObjectDrawer = drawer as IUnityObjectDrawer;
				if(unityObjectDrawer != null && unityObjectDrawer.Unfoldedness < 1f)
				{
					element.visible = false;
					return;
				}

				element.visible = true;

				var rect = drawer.Bounds;

				DrawGUI.AddMargins(ref rect);
				rect.y += DrawGUI.TopPadding;
				if(unityObjectDrawer != null)
				{
					rect.y += unityObjectDrawer.HeaderHeight;

					if(unityObjectDrawer.PrefixResizer == PrefixResizer.TopOnly)
					{
						rect.y += PrefixResizeUtility.TopOnlyPrefixResizerHeight;
					}
				}

				#if DEV_MODE && PI_ASSERTATIONS
				UnityEngine.Debug.Assert(rect.position.x >= 0f && rect.position.y >= 0f, rect + " for " + drawer);
				#endif

				if(rect.width > 0f)
				{
					element.style.width = rect.width;
				}

				if(rect.x >= 0f && rect.y >= 0f)
				{
					element.transform.position = rect.position;
				}
			}

			private bool IsShownInInspector(IDrawer drawer)
			{
				if(!drawer.ShouldShowInInspector)
				{
					return false;
				}
				var parent = drawer.Parent;
				if(parent == null)
				{
					return true;
				}
				if(parent.Unfoldedness < 1f)
				{
					return false;
				}
				if(Array.IndexOf(parent.VisibleMembers, drawer) == -1)
				{
					return false;
				}
				return IsShownInInspector(parent);
			}
		}
	}
}
#endif