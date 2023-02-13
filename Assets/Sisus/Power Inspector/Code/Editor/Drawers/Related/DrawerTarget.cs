using JetBrains.Annotations;

namespace Sisus
{
	/// <summary>
	/// Since drawers are reused to avoid garbage generation, to reliably refer to a specific target, we need both
	/// the reference to the target instance, as well as its the instance ID, which gets incremented each time that an drawer
	/// instance is placed in the object pool.
	/// </summary>
	public struct DrawerTarget
	{
		[CanBeNull]
		private readonly IDrawer drawer;

		/// <summary>
		/// Instance id of targeted drawer at the time that the DrawerTarget instance was created, or 0 if drawer was null.
		/// </summary>
		private readonly int instanceId;

		[CanBeNull]
		public IDrawer Target
		{
			get
			{
				return HasValidInstanceReference() ? drawer : null;
			}
		}
		
		public DrawerTarget([CanBeNull]IDrawer target)
		{
			drawer = target;

			instanceId = target == null ? 0 : target.InstanceId;

			#if DEV_MODE && PI_ASSERTATIONS
			if(target != null) { UnityEngine.Debug.Assert(instanceId > 0); }
			else { UnityEngine.Debug.Assert(instanceId == 0); }
			#endif
		}

		public bool Equals([CanBeNull]IDrawer test)
		{
			if(test == null)
			{
				return instanceId == 0;
			}
			return ReferenceEquals(test, drawer) && test.InstanceId.Equals(instanceId);
		}

		public bool Equals([CanBeNull]DrawerTarget test)
		{
			return ReferenceEquals(test.drawer, drawer) && test.instanceId.Equals(instanceId);
		}

		/// <summary>
		/// Returns true if target drawer is not null and hasn't been pooled since DrawerTarget instance was created.
		/// </summary>
		/// <returns> True if drawer target exists. </returns>
		public bool HasValidInstanceReference()
		{
			return !ReferenceEquals(drawer, null) && drawer.InstanceId.Equals(instanceId);
		}
	}
}