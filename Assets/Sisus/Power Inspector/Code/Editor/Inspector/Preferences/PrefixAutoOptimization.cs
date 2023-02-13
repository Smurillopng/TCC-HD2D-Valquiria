namespace Sisus
{
	public enum PrefixAutoOptimization
	{
		None = 0, //all components will use the default width
		AllSeparately = 2, //all components' label width will be optimized based on their fields
		AllTogether = 3 //all components' label width will be the same based on the largest common denominator of all displayed components
	}
}