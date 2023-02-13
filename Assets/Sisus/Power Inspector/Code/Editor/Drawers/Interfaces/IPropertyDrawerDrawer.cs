using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public interface IPropertyDrawerDrawer : IFieldDrawer
	{
		/// <summary> Gets a value indicating whether the drawer requires a PropertyDrawer type to be provided when SetupInterface is called. </summary>
		/// <value> True if requires PropertyDrawer type, false if not. </value>
		bool RequiresPropertyDrawerType
		{
			get;
		}

		/// <summary>
		/// Sets up the IPropertyDrawerDrawer so that they're ready to be used.
		/// LateSetup should always be called right after this.
		/// </summary>
		/// <param name="attribute"> The PropertyAttribute that determined the PropertyDrawer. Can be null if PropertyDrawer was selected using class member type instead. </param>
		/// <param name="setValue"> The starting cached value of the drawer. </param>
		/// <param name="setMemberInfo"> LinkedMemberInfo for field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="setParent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="setLabel"> The prefix label. </param>
		/// <param name="setReadOnly"> True if control should be read only. </param>
		void SetupInterface([CanBeNull]object attribute, [CanBeNull]object setValue, [CanBeNull]LinkedMemberInfo setMemberInfo, [CanBeNull]IParentDrawer setParent, [CanBeNull]GUIContent setLabel, bool setReadOnly);
	}
}