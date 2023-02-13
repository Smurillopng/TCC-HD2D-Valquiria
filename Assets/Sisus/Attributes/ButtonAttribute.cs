using System;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Attribute that specifies that its target should be shown in Power Inspector as a button.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method), MeansImplicitUse(ImplicitUseKindFlags.Access | ImplicitUseKindFlags.Assign)]
	public class ButtonAttribute : ShowInInspectorAttribute, IDrawerSetupDataProvider
	{
		[NotNull]
		public readonly string prefixLabelText;
		[NotNull]
		public readonly string buttonText;
		[NotNull]
		public readonly string guiStyle;

		public ButtonAttribute()
		{
			prefixLabelText = "";
			buttonText = "";
			guiStyle = "";
		}

		public ButtonAttribute([NotNull]string setButtonText)
		{
			prefixLabelText = "";
			buttonText = setButtonText;
			guiStyle = "";
		}

		public ButtonAttribute([NotNull]string setPrefixLabelText, [NotNull]string setButtonText)
		{
			prefixLabelText = setPrefixLabelText;
			buttonText = setButtonText;
			guiStyle = "";
		}

		public ButtonAttribute([NotNull]string setPrefixLabelText, [NotNull]string setButtonText, [NotNull]string setGuiStyle)
		{
			prefixLabelText = setPrefixLabelText;
			buttonText = setButtonText;
			guiStyle = setGuiStyle;
		}

		/// <inheritdoc />
		public object[] GetSetupParameters()
		{
			return new[] { prefixLabelText , buttonText, guiStyle };
		}
	}
}