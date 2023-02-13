//#define DEBUG_SNAP

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Sisus
{
	[Serializable]
	public class PositionDrawer : TransformMemberBaseDrawer
	{
		private static readonly int[] DraggingMembersX = {0};
		private static int[] draggingMembers = {0};
		private static readonly List<int> DraggingMembersBuilder = new List<int>(3);

		/// <inheritdoc/>
		protected override int[] DraggingTargetsMembers
		{
			get
			{
				return draggingMembers;
			}
		}

		/// <inheritdoc/>
		public override bool SnappingEnabled
		{
			get
			{
				return UserSettings.Snapping.Enabled && UserSettings.Snapping.EnabledForMove;
			}

			set
			{
				UserSettings.Snapping.EnabledForMove = value;
			}
		}

		/// <inheritdoc/>
		public override float GetSnapStep(int memberIndex)
		{
			switch(memberIndex)
			{
				case 0:
					return UserSettings.Snapping.MoveX;
				case 1:
					return UserSettings.Snapping.MoveY;
				case 2:
					return UserSettings.Snapping.MoveZ;
			}
			throw new IndexOutOfRangeException(StringUtils.ToString(memberIndex));
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static PositionDrawer Create(Vector3 value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			PositionDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new PositionDrawer();
			}
			result.Setup(value, typeof(Vector3), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <summary>
		/// Prevents a default instance of the drawer from being created.
		/// The Create method should be used instead of this constructor.
		/// </summary>
		private PositionDrawer() { }

		/// <inheritdoc/> <inheritdoc/>
		protected override string XPropertyPath() { return "m_LocalPosition.x"; }
		/// <inheritdoc/> <inheritdoc/>
		protected override string YPropertyPath() { return "m_LocalPosition.y"; }
		/// <inheritdoc/> <inheritdoc/>
		protected override string ZPropertyPath() { return "m_LocalPosition.z"; }

		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			bool canChangeValue = !ReadOnly;
			var val = Value;

			if(canChangeValue)
			{
				if(!IsPrefab)
				{
					menu.AddSeparatorIfNotRedundant();
					menu.Add("Align With NavMesh", AlignWithNavMesh);
					menu.Add("Ground", Ground);
				}

				menu.AddSeparatorIfNotRedundant();

				menu.Add("Set To.../Zero\t 0, 0, 0", () => Value = Vector3.zero, val.IsZero());
				menu.Add("Set To.../One\t 1, 1, 1", () => Value = Vector3.one, val == Vector3.one);
				menu.Add("Set To.../Up\t 0, 1, 0", () => Value = Vector3.up, val == Vector3.up);
				menu.Add("Set To.../Down\t 0,-1, 0", () => Value = Vector3.down, val == Vector3.down);
				menu.Add("Set To.../Forward\t 0, 0, 1", () => Value = Vector3.forward, val == Vector3.forward);
				menu.Add("Set To.../Back\t 0, 0,-1", () => Value = Vector3.back, val == Vector3.back);
				menu.Add("Set To.../Right\t 1, 0, 0", () => Value = Vector3.right, val == Vector3.right);
				menu.Add("Set To.../Left\t-1, 0, 0", () => Value = Vector3.left, val == Vector3.left);
				
				menu.Add("Step/Up\tY-Axis", () => Value += Vector3.up);
				menu.Add("Step/Down\tY-Axis", () => Value += Vector3.down);
				menu.Add("Step/Left\tX-Axis", () => Value += Vector3.left);
				menu.Add("Step/Right\tX-Axis", () => Value += Vector3.right);
				menu.Add("Step/Forward\tZ-Axis", () => Value += Vector3.forward);
				menu.Add("Step/Back\tZ-Axis", () => Value += Vector3.back);
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		#if DEV_MODE
		/// <inheritdoc/>
		protected override void AddDevModeDebuggingEntriesToRightClickMenu(ref Menu menu)
		{
			base.AddDevModeDebuggingEntriesToRightClickMenu(ref menu);
			menu.Add("Debugging/Print Info", () => Debug.Log("labelLastDrawPosition=" + labelLastDrawPosition));
		}
		#endif
		
		/// <summary>
		/// Snap position to grid based on Snapping preferences
		/// </summary>
		public override void Snap()
		{
			float snapX = UserSettings.Snapping.MoveX;
			float snapY = UserSettings.Snapping.MoveY;
			float snapZ = UserSettings.Snapping.MoveZ;
			
			bool changed = false;
			var values = GetValues();
			for(int n = values.Length - 1; n >= 0; n--)
			{
				var was = (Vector3)values[n];
				var set = was;
				if(snapX > 0f)
				{
					set.x = Mathf.Round(set.x / snapX) * snapX;
				}
				if(snapY > 0f)
				{
					set.y = Mathf.Round(set.y / snapY) * snapY;
				}
				if(snapZ > 0f)
				{
					set.z = Mathf.Round(set.z / snapZ) * snapZ;
				}

				if(set != was)
				{
					#if DEV_MODE && DEBUG_SNAP
					Debug.Log("Snap: " + StringUtils.ToString(set));
					#endif
					
					values[n] = set;
					changed = true;
				}
			}

			if(changed)
			{
				SetValues(values);
			}
		}
		
		/// <summary>
		/// Snap position to closes point on navigation mesh (if one is found within 10 units).
		/// </summary>
		public void AlignWithNavMesh()
		{
			bool changed = false;
			var values = GetValues();

			for(int n = values.Length - 1; n >= 0; n--)
			{
				var position = (Vector3)values[n];
				
				NavMeshHit hit;
				if(NavMesh.SamplePosition(position, out hit, 10f, NavMesh.AllAreas))
				{
					if(hit.position != position)
					{
						changed = true;
						values[n] = hit.position;
					}

					var unityObject = parent.UnityObjects[n];
					InspectorUtility.ActiveInspector.Message(unityObject.name +" was snapped to NavMesh @ "+StringUtils.ToString(hit.position)+"!", unityObject);
				}
				else
				{
					var unityObject = parent.UnityObjects[n];
					InspectorUtility.ActiveInspector.Message(unityObject.name + " could not be snapped to NavMesh; is it further than 10 units away?", unityObject, MessageType.Warning);
				}
			}

			if(changed)
			{
				SetValues(values);
			}
		}
		
		/// <summary>
		/// Casts a ray downward from current position, and if the ray hits a collider
		/// on the way down, the target Transforms are moved to the point of collision.
		/// </summary>
		public void Ground()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(typeof(ITransformDrawer).IsAssignableFrom(typeof(TransformDrawer)));
			#endif

			var transforms = Transforms;
			if(GroundUtility.Ground(transforms))
			{
				UpdateCachedValuesFromFieldsRecursively();
			}
		}

		/// <inheritdoc/>
		protected override void UpdateTooltips()
		{
			var firstTransform = Transform;
			if(firstTransform != null)
			{
				if(Inspector.State.usingLocalSpace)
				{
					label.tooltip = StringUtils.Concat("World Position: ", StringUtils.ToString(firstTransform.position));
				}
				else
				{
					label.tooltip = StringUtils.Concat("Local Position: ", StringUtils.ToString(firstTransform.localPosition));
				}
			}
		}

		/// <inheritdoc/>
		protected override void UpdateDraggableMembers()
		{
			if(Inspector.State.usingLocalSpace)
			{
				draggingMembers = DraggingMembersX;
				return;
			}

			var firstTransform = Transform;
			if(firstTransform.parent == null)
			{
				draggingMembers = DraggingMembersX;
				return;
			}

			var forward = firstTransform.forward;
			if(!forward.x.ApproximatelyZero())
			{
				DraggingMembersBuilder.Add(0);
			}
			if(!forward.y.ApproximatelyZero())
			{
				DraggingMembersBuilder.Add(1);
			}
			if(!forward.z.ApproximatelyZero())
			{
				DraggingMembersBuilder.Add(2);
			}

			draggingMembers = DraggingMembersBuilder.ToArray();
			DraggingMembersBuilder.Clear();
		}

		/// <inheritdoc/>
		protected override void DoOnPrefixDragged(ref Vector3 inputValue, Vector3 inputMouseDownValue, float mouseDelta)
		{
			if(Inspector.State.usingLocalSpace)
			{
				base.DoOnPrefixDragged(ref inputValue, inputMouseDownValue, mouseDelta);
				return;
			}

			var firstTransform = Transform;
			if(firstTransform.parent == null)
			{
				base.DoOnPrefixDragged(ref inputValue, inputMouseDownValue, mouseDelta);
				return;
			}

			var forward = firstTransform.forward;

			var draggingTargets = DraggingTargetsMembers;
			for(int n = draggingTargets.Length - 1; n >= 0; n--)
			{
				int index = draggingTargets[n];

				var draggableMember = (IDraggablePrefix<float>)members[index];
				if(draggableMember.ShouldShowInInspector)
				{
					DrawGUI.DrawMouseoverEffect(draggableMember.ControlPosition, localDrawAreaOffset);

					float memberValue = inputValue[index];
					float mouseDownValue = inputMouseDownValue[index];

					float mouseDeltaComponent = forward[index] * mouseDelta;

					draggableMember.OnPrefixDragged(ref memberValue, mouseDownValue, mouseDeltaComponent);
					inputValue[index] = memberValue;
				}
			}
		}
	}
}