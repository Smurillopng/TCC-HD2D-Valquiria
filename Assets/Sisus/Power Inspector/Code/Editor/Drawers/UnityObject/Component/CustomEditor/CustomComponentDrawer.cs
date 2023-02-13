using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Component drawer whose members are not built automatically, but instead it
	/// relies on SetMembers to be used to set them manually.
	/// </summary>
	[Serializable]
	public class CustomComponentDrawer : ComponentDrawer
	{
		/// <inheritdoc/>
		protected override bool RebuildingMembersAllowed
		{
			get
			{
				// rebuilding members is never allowed, because they are set using
				// the SetMembers method, and there's no logic for rebuilding them.
				return false;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <param name="setLabel"> The label to be shown in the header of the Component. If left null, label will be generated from class name. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static CustomComponentDrawer Create(Component[] targets, IParentDrawer parent, [NotNull]IInspector inspector, GUIContent setLabel = null)
		{
			CustomComponentDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new CustomComponentDrawer();
			}
			result.Setup(targets, parent, setLabel, inspector);
			result.LateSetup();
			return result;
		}

		/// <summary>
		/// Prevents a default instance of the drawer from being created.
		/// The Create method should be used instead of this constructor.
		/// </summary>
		private CustomComponentDrawer() { }

		/// <inheritdoc/>
		public override void LateSetup()
		{
			base.LateSetup();

			//keep inactive flag true until SetMembers has been called
			inactive = true;
		}

		/// <inheritdoc/>
		public override void UpdateVisibleMembers()
		{
			if(inactive)
			{
				return;
			}

			base.UpdateVisibleMembers();
		}
		
		/// <inheritdoc/>
		public override void OnAfterMembersBuilt()
		{
			//CustomComponentDrawer don't automatically build any members of the target Components,
			// but instead rely on SetMembers to be used to set them manually. The CustomComponentDrawer
			// will remain in "inactive" state, with most method calls being ignored, until SetMembers has been called.
			inactive = false;

			base.OnAfterMembersBuilt();
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList() { }

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			#if DEV_MODE
			Debug.LogWarning(ToString()+".BuildMembers call ignored. SetMembers should be used instead.");
			#endif
		}
	}
}