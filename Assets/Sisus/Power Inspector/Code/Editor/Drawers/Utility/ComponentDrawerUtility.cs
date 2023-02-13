//#define DEBUG_CREATE_DRAWER

using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public static class ComponentDrawerUtility
	{
		public static IComponentDrawer singleInspectedInstance;
		
		public static void NameByType([NotNull]IComponentDrawer componentDrawer)
		{
			var goDrawer = componentDrawer.Parent;
			if(goDrawer != null)
			{
				NameByType(goDrawer.GetValues() as GameObject[], componentDrawer.GetValues() as Component[]);
			}
		}

		public static void NameByType(GameObject[] targets, Component[] components)
		{
			UndoHandler.RegisterUndoableAction(targets, "Auto-Name");
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				NameByType(targets[n], components[n]);
			}
		}

		public static void NameByType(GameObject target, Component component)
		{
			if(target != null)
			{
				UndoHandler.RegisterUndoableAction(target, "Auto-Name");
				string nameFromType;
				if(component == null)
				{
					nameFromType = "Missing Script";
				}
				else
				{
					var spriteRenderer = component as SpriteRenderer;
					if(spriteRenderer != null)
					{
						nameFromType = spriteRenderer.sprite == null ? "Sprite Renderer" : spriteRenderer.sprite.name;
					}
					else
					{
						var animator = component as Animator;
						if(animator != null)
						{
							var avatar = animator.avatar;
							nameFromType = avatar == null ? "Avatar" : avatar.name;
						}
						else
						{
							nameFromType = StringUtils.SplitPascalCaseToWords(component.GetType().Name);
						}
					}
				}

				target.name = nameFromType;

				if(target.IsPrefab())
				{
					Platform.Active.SetDirty(target);
				}
			}
		}

		/// <summary>
		/// Selects the previous visible member on the parent of the subject.
		/// </summary>
		/// <param name="subject"> The subject Component or other member of GameObject drawers. </param>
		public static void SelectPreviousVisibleComponent([NotNull]IDrawer subject)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(subject.Parent != null, StringUtils.ToString(subject.Parent));
			#endif

			//for CustomEditorBaseDrawer
			KeyboardControlUtility.KeyboardControl = 0;

			var parent = subject.Parent;
			if(parent != null)
			{
				var visibleMembers = parent.VisibleMembers;
				int myIndex = Array.IndexOf(visibleMembers, subject);
				if(myIndex != -1)
				{
					for(int n = myIndex - 1; n >= 0; n--)
					{
						var select = visibleMembers[n];
						if(select.Selectable)
						{
							select.Select(ReasonSelectionChanged.SelectPrevComponent);
							return;
						}
					}
					parent.Select(ReasonSelectionChanged.SelectPrevComponent);
				}
				#if DEV_MODE
				else { Debug.LogWarning(subject.ToString()+ ".SelectPreviousComponent: could not find subject in visible members of parent "+parent.ToString()); }
				#endif
			}
			#if DEV_MODE
			else { Debug.LogWarning(subject.ToString() + ".SelectPreviousComponent parent was null: "+subject.ToString()); }
			#endif
		}

		/// <summary>
		/// Selects the next visible member on the parent of the subject.
		/// </summary>
		/// <param name="subject"> The subject Component or other member of GameObject drawers. </param>
		public static void SelectNextVisibleComponent([NotNull]IDrawer subject)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(subject.Parent != null && subject.Parent is IGameObjectDrawer, StringUtils.ToString(subject.Parent));
			#endif

			//for CustomEditorBaseDrawer
			KeyboardControlUtility.KeyboardControl = 0;

			var parent = subject.Parent;
			if(parent != null)
			{
				var visibleMembers = parent.VisibleMembers;
				int currentIndex = Array.IndexOf(visibleMembers, subject);
				var gameObjectDrawer = parent as IGameObjectDrawer;

				int lastCompIndex = gameObjectDrawer != null ? gameObjectDrawer.VisibleComponentMemberCount() - 1 : visibleMembers.Length - 1;
				if(currentIndex >= lastCompIndex)
				{
					var go = gameObjectDrawer != null ? gameObjectDrawer.GameObject : subject.UnityObject.GameObject();
					if(go != null)
					{
						Component next = go.transform.NextVisibleInInspector(false);

						#if DEV_MODE
						Debug.Assert(next != null);
						#endif

						for(int n = 0; n < 50; n++) // 50 is an arbitrarily chosen number of repeats to do in order to avoid infinite loops if hierarchy has only hidden GameObjects
						{
							if(next.gameObject.hideFlags != HideFlags.HideInHierarchy && (next.hideFlags != HideFlags.HideInInspector || subject.Inspector.State.DebugMode || subject.Inspector.Preferences.ShowHiddenComponents))
							{
								#if DEV_MODE
								Debug.Log("Selecting Next GameObject: " + next.name, next.gameObject);
								#endif
								InspectorUtility.ActiveInspector.SelectAndShow(next.gameObject, ReasonSelectionChanged.SelectNextComponent);
								return;
							}
							next = next.NextComponent();
						}
					}
				}
				else if(currentIndex == -1)
				{
					//this could perhaps happen with missing components or something?
					#if DEV_MODE
					Debug.LogWarning(subject.ToString()+ ".SelectNextComponent: could not find self in members of parent "+parent.ToString());
					#endif
				}
				else
				{
					var select = visibleMembers[currentIndex + 1];
					select.Select(ReasonSelectionChanged.SelectNextComponent);
				}
			}
			#if DEV_MODE
			else
			{
				Debug.LogWarning("parent was null: "+ subject.ToString());
			}
			#endif
		}

		public static void SelectPreviousOfType([NotNull]IComponentDrawer subject)
		{
			var inspector = InspectorUtility.ActiveInspector;

			var selected = inspector.FocusedDrawer;
			var selectedIndexPath = selected == null ? null : selected.GenerateMemberIndexPath(subject);

			Component component;
			if(!HierarchyUtility.TryGetPreviousOfType(subject.Component, out component))
			{
				if(component == null)
				{
					inspector.Message("No instances to select found in scene.");
				}
				else
				{
					inspector.Message("No additional instances found in scene.");
				}
				return;
			}
			
			if(component.gameObject != subject.gameObject)
			{
				// UPDATE: New test to preserve selected path
				// TO DO: Support custom editors
				inspector.OnNextInspectedChanged(()=>
				{
					if(selectedIndexPath == null)
					{
						inspector.SelectAndShow(component, ReasonSelectionChanged.SelectPrevOfType);
					}
					else
					{
						var componentDrawer = inspector.State.drawers.FindDrawer(component);
						componentDrawer.SelectMemberAtIndexPath(selectedIndexPath, ReasonSelectionChanged.SelectPrevOfType);
					}
				});
				inspector.Select(component);
			}
			else
			{
				inspector.SelectAndShow(component, ReasonSelectionChanged.SelectPrevOfType);
			}
		}

		public static void SelectNextOfType([NotNull]IComponentDrawer subject)
		{
			var inspector = InspectorUtility.ActiveInspector;

			var selected = inspector.FocusedDrawer;
			var selectedIndexPath = selected == null ? null : selected.GenerateMemberIndexPath(subject);

			Component component;
			if(!HierarchyUtility.TryGetNextOfType(subject.Component, out component))
			{
				if(component == null)
				{
					inspector.Message("No instances to select found in scene");
				}
				else
				{
					inspector.Message("No additional instances found in scene");
				}
				return;
			}

			if(component.gameObject != subject.gameObject)
			{
				#if DEV_MODE
				Debug.Log("Selecting "+ component.GetType().Name + " on "+ component.name+" during OnNextInspectedChanged.");
				#endif

				//UPDATE: New test to preserve selected path
				// TO DO: Support custom editors
				inspector.OnNextInspectedChanged(()=>
				{
					if(selectedIndexPath == null)
					{
						inspector.SelectAndShow(component, ReasonSelectionChanged.SelectNextOfType);
					}
					else
					{
						var componentDrawer = inspector.State.drawers.FindDrawer(component);
						componentDrawer.SelectMemberAtIndexPath(selectedIndexPath, ReasonSelectionChanged.SelectNextOfType);
					}
				});
				inspector.Select(component.gameObject);
			}
			else
			{
				#if DEV_MODE
				Debug.Log("Component "+ component.GetType().Name + " found on same GameObject "+ component.name+".");
				#endif

				if(selectedIndexPath == null)
				{
					inspector.SelectAndShow(component, ReasonSelectionChanged.SelectNextOfType);
				}
				else
				{
					var componentDrawer = inspector.State.drawers.FindDrawer(component);
					componentDrawer.SelectMemberAtIndexPath(selectedIndexPath, ReasonSelectionChanged.SelectNextOfType);
				}
			}
		}

		public static void Duplicate<TComponent>(TComponent[] targets) where TComponent : Component
		{
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(targets[n] != null, "Duplicate - "+typeof(TComponent)+" "+(n+1)+" / "+n+" was null!");
				#endif

				#if UNITY_EDITOR
				UnityEditorInternal.ComponentUtility.CopyComponent(targets[n]);
				UnityEditorInternal.ComponentUtility.PasteComponentAsNew(targets[n].gameObject);
				#else
				var source = targets[n];
				var clone = source.gameObject.AddComponent(source.GetType());
				byte[] bytes = null;
				System.Collections.Generic.List<UnityEngine.Object> references = null;
				#if !DONT_USE_ODIN_SERIALIZER
				Sisus.OdinSerializer.UnitySerializationUtility.SerializeUnityObject(source, ref bytes, ref references, OdinSerializer.DataFormat.Binary, true);
				Sisus.OdinSerializer.UnitySerializationUtility.DeserializeUnityObject(clone, ref bytes, ref references, OdinSerializer.DataFormat.Binary);
				#else
				PrettySerializer.SerializeUnityObject(source, ref bytes, ref references);
				PrettySerializer.DeserializeUnityObject(bytes, clone, ref references);
				#endif
				#endif
			}
		}

		public static void DrawCustomEnabledField(IComponentDrawer drawer, Rect position)
		{
			var components = drawer.Components;
			bool wasEnabled = ((Behaviour)components[0]).enabled;
			bool mixed = false;
			for(int n = components.Length - 1; n >= 1; n--)
			{
				bool enabled = ((Behaviour)components[n]).enabled;
				if(enabled != wasEnabled)
				{
					mixed = true;
					break;
				}
			}

			DrawGUI.ShowMixedValue = mixed;

			GUI.Toggle(position, wasEnabled, GUIContent.none);

			DrawGUI.ShowMixedValue = false;
		}

		public static void OnCustomEnabledControlClicked(IComponentDrawer drawer, Event inputEvent)
		{
			DrawGUI.Use(inputEvent);

			var targets = drawer.Components;

			var firstBehaviour = targets[0] as Behaviour;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(firstBehaviour != null, "createCustomEnabledFlag was true but target was not a Behaviour");
			#endif

			bool setEnabled = !firstBehaviour.enabled;

			var changed = targets;
			for(int n = targets.Length - 1; n >= 1; n--)
			{
				if(((Behaviour)targets[n]).enabled == setEnabled)
				{
					changed = changed.RemoveAt(n);
				}
			}

			UndoHandler.RegisterUndoableAction(changed, changed.Length == 1 ? (setEnabled ? "Enable Component" : "Disable Component") : (setEnabled ? "Enable Components" : "Disable Components"));
					
			firstBehaviour.enabled = setEnabled;
			for(int n = targets.Length - 1; n >= 1; n--)
			{
				((Behaviour)targets[n]).enabled = setEnabled;
			}
		}
	}
}