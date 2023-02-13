#if ODIN_INSPECTOR
using JetBrains.Annotations;
using Sisus.Attributes;

namespace Sisus.Compatibility
{
	/// <summary>
	/// Add supports for converting Odin Inspector attributes to Power Inspector supported attributes.
	/// </summary>
	public sealed class OdinInspectorAttributeConverterProvider : PluginAttributeConverterProvider
	{
		/// <inheritdoc/>
		public override void AddConverters([NotNull]RegisterAttributeConverter add)
		{
			add(typeof(Sirenix.OdinInspector.ShowInInspectorAttribute), typeof(ShowInInspectorAttribute), ConvertShowInInspector);
			add(typeof(Sirenix.Serialization.OdinSerializeAttribute), typeof(UnityEngine.SerializeField), ConvertOdinSerialize);
			add(typeof(Sirenix.OdinInspector.ButtonAttribute), typeof(ButtonAttribute), ConvertButton);
			add(typeof(Sirenix.OdinInspector.ReadOnlyAttribute), typeof(ReadOnlyAttribute), ConvertReadOnly);
			add(typeof(Sirenix.OdinInspector.RequiredAttribute), typeof(NotNullAttribute), ConvertRequired);
			add(typeof(Sirenix.OdinInspector.TitleAttribute), typeof(PHeaderAttribute), ConvertTitle);
			add(typeof(Sirenix.OdinInspector.InfoBoxAttribute), typeof(HelpBoxAttribute), ConvertInfoBox);
			add(typeof(Sirenix.OdinInspector.DetailedInfoBoxAttribute), typeof(HelpBoxAttribute), ConvertDetailedInfoBox);
			add(typeof(Sirenix.OdinInspector.ShowIfAttribute), typeof(ShowIfAttribute), ConvertShowIf);
			add(typeof(Sirenix.OdinInspector.DisableIfAttribute), typeof(DisableIfAttribute), ConvertDisableIf);
			add(typeof(Sirenix.OdinInspector.DisplayAsStringAttribute), typeof(ReadOnlyAttribute), ConvertDisplayAsString);
			add(typeof(Sirenix.OdinInspector.MultiLinePropertyAttribute), typeof(PMultilineAttribute), ConvertMultilineProperty);
			add(typeof(Sirenix.OdinInspector.PropertySpaceAttribute), typeof(PSpaceAttribute), ConvertPropertySpace);
			add(typeof(Sirenix.OdinInspector.InlinePropertyAttribute), typeof(DrawInSingleRowAttribute), ConvertInlineProperty);
		}

		[NotNull]
		private object ConvertShowInInspector([NotNull]object input)
		{
			return new ShowInInspectorAttribute();
		}


		[NotNull]
		private object ConvertOdinSerialize([NotNull]object input)
		{
			return new UnityEngine.SerializeField();
		}

		[NotNull]
		private object ConvertButton([NotNull]object input)
		{
			var odinButton = (Sirenix.OdinInspector.ButtonAttribute)input;
			if(string.IsNullOrEmpty(odinButton.Name))
			{
				return new ButtonAttribute();
			}
			return new ButtonAttribute(odinButton.Name);
		}

		[NotNull]
		private object ConvertReadOnly([NotNull]object input)
		{
			return new ReadOnlyAttribute();
		}

		[NotNull]
		private object ConvertRequired([NotNull]object input)
		{
			return new NotNullAttribute();
		}

		[NotNull]
		private object ConvertTitle([NotNull]object input)
		{
			return new PHeaderAttribute();
		}

		[NotNull]
		private object ConvertInfoBox([NotNull]object input)
		{
			var odininfoBox = (Sirenix.OdinInspector.InfoBoxAttribute)input;

			switch(odininfoBox.InfoMessageType)
			{
				default:
					if(!string.IsNullOrEmpty(odininfoBox.VisibleIf))
					{
						return new HelpBoxAttribute(odininfoBox.Message, HelpBoxMessageType.Info, odininfoBox.VisibleIf, true);
					}
					return new HelpBoxAttribute(odininfoBox.Message);
				case Sirenix.OdinInspector.InfoMessageType.Warning:
					if(!string.IsNullOrEmpty(odininfoBox.VisibleIf))
					{
						return new HelpBoxAttribute(odininfoBox.Message, HelpBoxMessageType.Warning, odininfoBox.VisibleIf, true);
					}
					return new HelpBoxAttribute(odininfoBox.Message, HelpBoxMessageType.Warning);
				case Sirenix.OdinInspector.InfoMessageType.Error:
					if(!string.IsNullOrEmpty(odininfoBox.VisibleIf))
					{
						return new HelpBoxAttribute(odininfoBox.Message, HelpBoxMessageType.Error, odininfoBox.VisibleIf, true);
					}
					return new HelpBoxAttribute(odininfoBox.Message, HelpBoxMessageType.Error);
			}
		}

		[NotNull]
		private object ConvertDetailedInfoBox([NotNull]object input)
		{
			var odininfoBox = (Sirenix.OdinInspector.DetailedInfoBoxAttribute)input;

			switch(odininfoBox.InfoMessageType)
			{
				default:
					if(!string.IsNullOrEmpty(odininfoBox.VisibleIf))
					{
						return new HelpBoxAttribute(odininfoBox.Details, HelpBoxMessageType.Info, odininfoBox.VisibleIf, true);
					}
					return new HelpBoxAttribute(odininfoBox.Details);
				case Sirenix.OdinInspector.InfoMessageType.Warning:
					if(!string.IsNullOrEmpty(odininfoBox.VisibleIf))
					{
						return new HelpBoxAttribute(odininfoBox.Details, HelpBoxMessageType.Warning, odininfoBox.VisibleIf, true);
					}
					return new HelpBoxAttribute(odininfoBox.Details, HelpBoxMessageType.Warning);
				case Sirenix.OdinInspector.InfoMessageType.Error:
					if(!string.IsNullOrEmpty(odininfoBox.VisibleIf))
					{
						return new HelpBoxAttribute(odininfoBox.Details, HelpBoxMessageType.Error, odininfoBox.VisibleIf, true);
					}
					return new HelpBoxAttribute(odininfoBox.Details, HelpBoxMessageType.Error);
			}
		}

		[NotNull]
		private object ConvertShowIf([NotNull]object input)
		{
			var odinShowIf = (Sirenix.OdinInspector.ShowIfAttribute)input;
			return new ShowIfAttribute(odinShowIf.Condition, odinShowIf.Value != null ? odinShowIf.Value : true);
		}

		[NotNull]
		private object ConvertDisableIf([NotNull]object input)
		{
			var odinDisableIf = (Sirenix.OdinInspector.DisableIfAttribute)input;
			return new DisableIfAttribute(odinDisableIf.Condition, odinDisableIf.Value != null ? odinDisableIf.Value : true);
		}

		[NotNull]
		private object ConvertDisplayAsString([NotNull]object input)
		{
			return new ReadOnlyAttribute();
		}

		[NotNull]
		private object ConvertMultilineProperty([NotNull]object input)
		{
			var multiline = (Sirenix.OdinInspector.MultiLinePropertyAttribute)input;
			return new PMultilineAttribute(multiline.Lines);
		}

		[NotNull]
		private object ConvertPropertySpace([NotNull]object input)
		{
			var space = (Sirenix.OdinInspector.PropertySpaceAttribute)input;
			return new PSpaceAttribute(space.SpaceBefore);
		}

		[NotNull]
		private object ConvertInlineProperty([NotNull]object input)
		{
			return new DrawInSingleRowAttribute();
		}
	}
}
#endif