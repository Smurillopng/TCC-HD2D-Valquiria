using Sisus.Attributes;
using System;
using UnityEngine.Serialization;

namespace Sisus.CreateScriptWizard
{
	[Serializable]
	public class NewScriptWindowSettings
	{
		public bool curlyBracesOnNewLine;
		[FormerlySerializedAs("addMethodComments")]
		public bool addComments = true;
		public bool addCommentsAsSummary = true;
		public int wordWrapCommentsAfterCharacters = 100;
		public bool addUsedImplicitly;
		public bool spaceAfterMethodName = true;
		[PTooltip("Automatic: Environment.NewLine", "Windows Style: CR LF", "Unix Style: LF")]
		public NewLineSequence newLineSequence = NewLineSequence.WindowsStyle;

		public string[] usingNamespaceOptions =
		{
			"System",
			"System.Collections",
			"System.Collections.Generic",
			"System.Linq",
			"UnityEngine",
			"UnityEditor",
			"Object = UnityEngine.Object",
			"JetBrains.Annotations"
		};

		public string NewLine
		{
			get
			{
				return newLineSequence == NewLineSequence.UnixStyle ? "\n" : "\r\n";
            }
		}
	}
}