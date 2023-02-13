namespace Sisus.CreateScriptWizard
{
	public struct ParameterData
	{
		public string name;
		public string type;
		
		public ParameterData(string name, string type)
		{
			this.name = name;
			this.type = type;
		}
	}
}