using System;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Asset drawer whose members are not built automatically, but instead it
	/// relies on SetMembers to be used to set them manually.
	/// </summary>
	[Serializable]
	public class CustomAssetDrawer : AssetDrawer
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
		/// <param name="setLabel"> The label to be shown in the header of the asset. If left null, label will be generated from class name. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static CustomAssetDrawer Create(Object[] targets, IParentDrawer parent, [NotNull]IInspector inspector, GUIContent setLabel = null)
		{
			CustomAssetDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new CustomAssetDrawer();
			}
			result.Setup(targets, parent, setLabel, inspector);
			result.LateSetup();
			return result;
		}

		/// <summary>
		/// Prevents a default instance of the drawer from being created.
		/// The Create method should be used instead of this constructor.
		/// </summary>
		private CustomAssetDrawer() { }

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
			if(!inactive)
			{
				base.UpdateVisibleMembers();
			}
		}
		
		/// <inheritdoc/>
		public override void OnAfterMembersBuilt()
		{
			// CustomAssetDrawer don't automatically build any members of the target assets,
			// but instead rely on SetMembers to be used to set them manually. The CustomAssetDrawer
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