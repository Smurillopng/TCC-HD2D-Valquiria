using System;
using JetBrains.Annotations;

namespace Sisus.Compatibility
{
	/// <summary>
	/// Registers plugin attribute to Power Inspector supported attribute conversion information.
	/// </summary>
	/// <param name="pluginAttributeType"> Plugin attribute type from which can be converted. </param>
	/// <param name="pluginAttributeType"> Power Inspector supported attribute type to which can be converted. </param>
	[CanBeNull]
	public delegate void RegisterAttributeConverter([NotNull]Type pluginAttributeType, [NotNull]Type supportedAttributeType, [NotNull]AttributeConverter converter);
}