//#define DEBUG_SWITCH_ACTIVE_INSPECTOR

using System;

namespace Sisus
{
	public interface IDrawerDelayableAction
	{
		void InvokeIfInstanceReferenceIsValid();
	}

	/// <summary>
	/// Action targeting a specific Drawer target.
	/// Because Drawer might get Disposed at any moment if the inspected targets of an inspector
	/// are changed, we need to check whether a Drawer target still exists right before a delayed
	/// action is about to be invoked.
	/// This can also handle instances where IDrawer has been pooled before the action gets invoked.
	/// </summary>
	public struct DrawerDelayableAction : IDrawerDelayableAction
	{
		private readonly DrawerTarget targetInstance;
		private readonly Action action;

		public DrawerDelayableAction(IDrawer targetDrawer, Action delayedAction)
		{
			targetInstance = new DrawerTarget(targetDrawer);
			action = delayedAction;
		}

		public void InvokeIfInstanceReferenceIsValid()
		{
			if(HasValidInstanceReference())
			{
				Invoke();
			}
		}

		private bool HasValidInstanceReference()
		{
			return targetInstance.HasValidInstanceReference();
		}

		private void Invoke()
		{
			// Ad-hoc fix: for many drawers the Inspector property simply returns InspectorUtility.ActiveInspector, which isn't necessarily pointing to the correct inspector when a delayed action is being invoked.
			// When traversing up the parent chain, it becomes more likely that the Inspector property points to the correct inspector, since root drawers like DrawerGroup, GameObjectDrawer and UnityObjectDrawer always hold a reference to the inspector that contains them.
			// We should temporarily set this inspector as the active inspector when invoking the action.
			var activeInspectorWas = InspectorUtility.ActiveInspector;
			var root = targetInstance.Target;
			while(root.Parent != null)
			{
				root = root.Parent;
			}
			if(root != null)
			{
				#if DEBUG_SWITCH_ACTIVE_INSPECTOR
				if(InspectorUtility.ActiveInspector != root.Inspector) { UnityEngine.Debug.LogWarning("Changing active inspector temporarily from " + (activeInspectorWas == null ? "null" : activeInspectorWas.ToString()) + " to " + root.Inspector); }
				#endif

				InspectorUtility.ActiveManager.ActiveInspector = root.Inspector;
			}

			try
			{
				if(action != null)
				{
					action();
				}
			}
			// Once the action has finished, restore the previously active inspector.
			finally
			{
				InspectorUtility.ActiveManager.ActiveInspector = activeInspectorWas;
			}
		}
	}

	/// <summary>
	/// Action targeting a specific Drawer target.
	/// Because Drawer might get Disposed at any moment if the inspected targets of an inspector
	/// are changed, we need to check whether a Drawer target still exists right before a delayed
	/// action is about to be invoked.
	/// This can also handle instances where IDrawer has been pooled before the action gets invoked.
	/// </summary>
	public struct DrawerDelayableTargetedAction : IDrawerDelayableAction
	{
		private readonly DrawerTarget targetInstance;
		private readonly Action<IDrawer> action;

		public DrawerDelayableTargetedAction(IDrawer targetDrawer, Action<IDrawer> delayedAction)
		{
			targetInstance = new DrawerTarget(targetDrawer);
			action = delayedAction;
		}

		public void InvokeIfInstanceReferenceIsValid()
		{
			if(HasValidInstanceReference())
			{
				Invoke();
			}
		}

		private bool HasValidInstanceReference()
		{
			return targetInstance.HasValidInstanceReference();
		}

		private void Invoke()
		{
			if(action != null)
			{
				action(targetInstance.Target);
			}
		}
	}

	/// <summary>
	/// Action targeting a specific Drawer target.
	/// Because Drawer might get Disposed at any moment if the inspected targets of an inspector
	/// are changed, we need to check whether a Drawer target still exists right before a delayed
	/// action is about to be invoked.
	/// This can also handle instances where IDrawer has been pooled before the action gets invoked.
	/// </summary>
	public struct DrawerDelayableAction<T>
	{
		private readonly DrawerTarget targetInstance;
		private readonly Action<T> action;

		public DrawerDelayableAction(IDrawer targetDrawer, Action<T> delayedAction)
		{
			targetInstance = new DrawerTarget(targetDrawer);
			action = delayedAction;
		}

		public void InvokeIfInstanceReferenceIsValid(T parameter)
		{
			if(HasValidInstanceReference())
			{
				Invoke(parameter);
			}
		}

		public void Invoke(T parameter)
		{
			if(action != null)
			{
				action(parameter);
			}
		}

		public bool HasValidInstanceReference()
		{
			return targetInstance.HasValidInstanceReference();
		}

		public static implicit operator Action<T>(DrawerDelayableAction<T> delayable)
		{
			return delayable.InvokeIfInstanceReferenceIsValid;
		}

		public override string ToString()
		{
			return StringUtils.ToString(action) + "@" + (targetInstance.HasValidInstanceReference() ? "n/a" : targetInstance.Target.ToString());
		}
	}
}