using System;
using UnityEngine;
using JetBrains.Annotations;


namespace Sisus
{
	public interface IFieldDrawer : IReorderable
	{
		/// <summary> Gets the last draw position of the control component of these drawer (as
		/// opposed to the prefix label/header component). If these drawer don't contain separate
		/// control and prefix components, this should return the  whole bounds of the control. </summary>
		/// <value> The control position. </value>
		Rect ControlPosition
		{
			get;
		}

		/// <summary>
		/// Is it safe to read the value of this field without the risk of there being undesired side
		/// effects? Returns true for all fields, false for properties and methods that aren't considered
		/// safe based on their attributes and current display preferences.
		/// </summary>
		/// <value>
		/// True if we can read from field without risk of undesired side effects, false if not.
		/// </value>
		bool CanReadFromFieldWithoutSideEffects
		{
			get;
		}

		/// <summary>
		/// Sets up the IFieldDrawer so that its ready to be used.
		/// LateSetup should always be called right after this.
		/// </summary>
		/// <param name="setValue"> The starting cached value of the drawer. </param>
		/// <param name="setValueType"> The type constraint for the value of the drawer. Usually same as field type, but sometimes a more specify type can be specified. </param>
		/// <param name="setMemberInfo"> LinkedMemberInfo for field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="setParent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="setLabel"> The prefix label. </param>
		/// <param name="setReadOnly"> True if control should be read only. </param>
		void SetupInterface([NotNull]object setValue, [CanBeNull]Type setValueType, [CanBeNull]LinkedMemberInfo setMemberInfo, [CanBeNull]IParentDrawer setParent, [CanBeNull]GUIContent setLabel, bool setReadOnly);

		/// <summary> Generates and returns a deep copy of values of all targets. </summary>
		/// <returns> An array of target values. </returns>
		object[] GetCopyOfValues();
	}
}