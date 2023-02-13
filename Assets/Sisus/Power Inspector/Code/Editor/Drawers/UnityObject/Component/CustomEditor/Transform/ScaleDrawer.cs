#define DEBUG_NEXT_FIELD

using System;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class ScaleDrawer : TransformMemberBaseDrawer
	{
		private static readonly int[] DraggingMembers = {0, 1, 2};

		/// <inheritdoc/>
		protected override int[] DraggingTargetsMembers
		{
			get
			{
				return DraggingMembers;
			}
		}

		/// <inheritdoc/>
		public override bool SnappingEnabled
		{
			get
			{
				return UserSettings.Snapping.Enabled && UserSettings.Snapping.EnabledForScale;
			}
			
			set
			{
				UserSettings.Snapping.EnabledForScale = value;
			}
		}

		/// <inheritdoc cref="IDrawer.ReadOnly" />
		public override bool ReadOnly
		{
			get
			{
				// LinkedMemberInfo target lossyScale property is get-only, so ReadOnly is by default set to false.
				// We however handle writing manually in this class, so we can override this.
				return parent == null ? false : parent.ReadOnly;
			}
		}

		/// <inheritdoc/>
		public override float GetSnapStep(int memberIndex)
		{
			return UserSettings.Snapping.Scale;
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static ScaleDrawer Create(Vector3 value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			ScaleDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ScaleDrawer();
			}
			result.Setup(value, typeof(Vector3), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}
		
		/// <summary>
		/// Prevents a default instance of the drawer from being created.
		/// The Create method should be used instead of this constructor.
		/// </summary>
		private ScaleDrawer() { }

		/// <inheritdoc/>
		public override object DefaultValue(bool _ )
		{
			return Vector3.one;
		}

		/// <inheritdoc/>
		protected override void UpdateTooltips()
		{
			var firstTransform = Transform;
			if(firstTransform != null)
			{
				if(Inspector.State.usingLocalSpace)
				{
					label.tooltip = StringUtils.Concat("World Scale: ", StringUtils.ToString(firstTransform.localScale));
				}
				else
				{
					label.tooltip = StringUtils.Concat("Local Scale: ", StringUtils.ToString(firstTransform.lossyScale));
				}
			}
		}

		/// <inheritdoc/>
		protected override string XPropertyPath()
		{
			return "m_LocalScale.x";
		}

		/// <inheritdoc/>
		protected override string YPropertyPath()
		{
			return "m_LocalScale.y";
		}

		/// <inheritdoc/>
		protected override string ZPropertyPath()
		{
			return "m_LocalScale.z";
		}

		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			bool canChangeValue = !ReadOnly;

			if(canChangeValue)
			{
				menu.AddSeparatorIfNotRedundant();
				
				var val = Value;
				menu.Add("Set To.../Zero", () => Value = Vector3.zero, val.IsZero());
				menu.Add("Set To.../One", () => Value = Vector3.one, val == Vector3.one);
				
				menu.Add("Multiply/Scale\tXYZ/Halve", () => Value = Value * 0.5f, val == Value * 0.5f);
				menu.Add("Multiply/Scale\tXYZ/Double", () => Value = Value * 2f, val == Value * 2f);
				menu.Add("Multiply/Height\tY/Halve", () => Value = new Vector3(val.x, val.y * 0.5f, val.z));
				menu.Add("Multiply/Height\tY/Double", () => Value = new Vector3(val.x, val.y * 2f, val.z));
				menu.Add("Multiply/Spread\tXZ/Halve", () => Value = new Vector3(val.x * 0.5f, val.y, val.z * 0.5f));
				menu.Add("Multiply/Spread\tXZ/Double", () => Value = new Vector3(val.x * 2f, val.y, val.z * 2f));
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		/// <inheritdoc/>
		public override void Snap()
		{
			float snap = UserSettings.Snapping.Scale;
			if(snap > 0f)
			{
				bool changed = false;
				var values = GetValues();
				for(int n = values.Length - 1; n >= 0; n--)
				{
					var was = (Vector3)values[n];
					var set = was;
					set.x = Mathf.Round(set.x / snap) * snap;
					set.y = Mathf.Round(set.y / snap) * snap;
					set.z = Mathf.Round(set.z / snap) * snap;
					if(!ValuesAreEqual(set, was))
					{
						values[n] = set;
						changed = true;
					}
				}

				if(changed)
				{
					SetValues(values);
				}
			}
		}

		/// <inheritdoc />
		protected override void OnCachedValueChanged(bool applyToField, bool updateMembers)
		{
			base.OnCachedValueChanged(applyToField, updateMembers);

			// scale changes can affect Preview
			Inspector.ReloadPreviewInstances();
		}

		/// <inheritdoc/>
		protected override void UpdateDraggableMembers() { }

		/// <inheritdoc/>
		protected override void ApplyValueToField()
		{
			if(Inspector.State.usingLocalSpace)
			{
				base.ApplyValueToField();
				return;
			}

			bool changed = false;

			var setLossyScale = Value;

			// Can't write to field when using LinkedMemberInfo, because lossyScale property is get-only.
			// Convert to localScale and write directly to transform targets instead.
			var targets = UnityObjects;
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var transform = targets[n] as Transform;
				if(!ValuesAreEqual(transform.lossyScale, setLossyScale))
				{
					if(!changed)
					{
						changed = true;
						UndoHandler.RegisterUndoableAction(targets, UndoHandler.GetSetValueMenuText(Name));
					}
					transform.SetWorldScale(setLossyScale);
				}
			}
		}

		/// <inheritdoc/>
		protected override void ApplyValuesToFields(object[] values)
		{
			if(Inspector.State.usingLocalSpace)
			{
				base.ApplyValuesToFields(values);
				return;
			}

			// Can't write to field when using LinkedMemberInfo, because lossyScale property is get-only.
			// Convert to localScale and write directly to transform targets instead.
			var targets = UnityObjects;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(values.Length == targets.Length);
			#endif

			bool changed = false;
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var transform = targets[n] as Transform;
				var setLossyScale = (Vector3)values[n];
				if(!ValuesAreEqual(transform.lossyScale, setLossyScale))
				{
					if(!changed)
					{
						changed = true;
						UndoHandler.RegisterUndoableAction(targets, UndoHandler.GetSetValueMenuText(Name));
					}
					transform.SetWorldScale(setLossyScale);
				}
			}
		}
	}
}