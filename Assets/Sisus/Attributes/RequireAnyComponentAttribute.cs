using JetBrains.Annotations;
using System;

namespace Sisus.Attributes
{
	/// <summary>
	/// Like RequireComponent
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class RequireAnyComponentAttribute : RequireComponentsAttribute
	{
		public RequireAnyComponentAttribute([NotNull]params Type[] requireAnyComponent) : base(false, requireAnyComponent)
		{

		}
	}
}