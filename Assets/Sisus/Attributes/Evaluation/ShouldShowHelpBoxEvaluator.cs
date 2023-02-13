using System;
using System.Reflection;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	public class ShouldShowHelpBoxEvaluator : IShowInInspectorIf
	{
		private readonly HelpBoxAttribute attribute;

		public ShouldShowHelpBoxEvaluator([NotNull]HelpBoxAttribute helpBoxAttribute)
		{
			attribute = helpBoxAttribute;
		}

		/// <inheritdoc/>
		public bool ShowInInspector([NotNull]Type containingClassType, [CanBeNull]object containingClassInstance, [CanBeNull]MemberInfo classMember)
		{
			return attribute.ShowInInspector(containingClassType, containingClassInstance, classMember);
		}
	}
}