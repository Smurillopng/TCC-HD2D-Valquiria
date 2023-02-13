using UnityEngine;
using JetBrains.Annotations;

namespace Sisus
{
	public interface ICustomGroupDrawer : IParentDrawer
	{
		/// <summary>
		/// Sets up the ICustomGroupDrawer so that its ready to be used.
		/// LateSetup should always be called right after this.
		/// </summary>
		/// <param name="setParent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="setLabel"> The prefix label. </param>
		/// <param name="setReadOnly"> True if control should be read only. </param>
		void SetupInterface([CanBeNull]IParentDrawer setParent, [CanBeNull]GUIContent setLabel, bool setReadOnly);

		/// <summary>
		/// Sets the members of the drawer and sets inactive flag to false, marking the fact that the setup phase for the drawer has finished and they are now ready to be used.
		/// </summary>
		/// <param name="setMembers"> The members for the drawer. </param>
		/// <param name="setDrawInSingleRow"> Determine whether or not to draw all members in a single row. </param>
		/// <param name="sendVisibilityChangedEvents">
		/// True to broadcast events OnBecameInvisible and OnSelfOrParentBecameVisible events to applicable members.
		/// This should generally speaking be false during Setup and Dispose phases and otherwise true.
		/// </param>
		void SetMembers([NotNull]IDrawer[] setMembers, bool setDrawInSingleRow, bool sendVisibilityChangedEvents);
	}
}
