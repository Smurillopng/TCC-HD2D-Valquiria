using JetBrains.Annotations;
using System;
using System.Collections.Generic;

namespace Sisus.CreateScriptWizard
{
	[Serializable]
	public class ScriptPrescription
	{
		public string nameSpace = "";
		public string className = "";
		public string template = "";
		public string baseClass = "";

		[NotNull]
		public string[] usingNamespaces = new string[0];
		
		[CanBeNull]
		public FunctionData[] functions;

		[NotNull]
		public readonly Dictionary<string, string> stringReplacements = new Dictionary<string, string> ();

		public ScriptPrescription() {}

		public void SetTemplate(string template)
		{
			this.template = template;
			ScriptTemplateUtility.TryGetBaseClass(template, out baseClass);
		}
	}
}