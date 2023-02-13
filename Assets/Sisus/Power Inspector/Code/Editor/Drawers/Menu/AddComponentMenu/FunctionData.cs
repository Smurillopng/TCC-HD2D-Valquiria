using JetBrains.Annotations;

namespace Sisus.CreateScriptWizard
{
	public struct FunctionData
	{
		public string attribute;
		public string prefix;
		public string name;
		public string returnType;
		public string returnDefault;
		[CanBeNull]
		public ParameterData[] parameters;
		public string comment;
		public bool isMethod;
		public bool include;

		public bool IsHeader
		{
			get
			{
				return name == null;
			}
		}

		public string HeaderName
		{
			get
			{
				return comment;
			}
		}

		public FunctionData(string headerName)
		{
			attribute = "";
			prefix = "";
			comment = headerName;
			name = null;
			returnType = null;
			returnDefault = null;
			parameters = null;
			include = false;
			isMethod = true;
		}
	}
}