using System;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Attribute that can be used to specify for which inspector class type this toolbar is used.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true), MeansImplicitUse(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature, ImplicitUseTargetFlags.Itself)]
	public class ToolbarForAttribute : Attribute
	{
		/// <summary>
		/// Type of the inspector class into which this toolbar should be attached.
		/// The class needs to implement IInspector.
		/// </summary>
		[NotNull]
		public readonly Type inspectorType;

		/// <summary>
		/// If true the toolbar class which has this attribute has lower priority than any toolbar classes with the attribute where this is false.
		/// </summary>
		public readonly bool isFallback;

		public ToolbarForAttribute([NotNull]Type setTnspectorType, bool setIsFallback = false)
		{
			inspectorType = setTnspectorType;
			isFallback = setIsFallback;

			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(inspectorType != null);
			UnityEngine.Debug.Assert(typeof(IInspector).IsAssignableFrom(inspectorType));
			#endif
		}
	}
}