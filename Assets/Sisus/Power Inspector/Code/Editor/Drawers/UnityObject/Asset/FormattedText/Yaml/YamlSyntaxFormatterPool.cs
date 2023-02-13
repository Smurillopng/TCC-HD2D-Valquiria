namespace Sisus
{
	public static class YamlSyntaxFormatterPool
	{
		private static Pool<YamlSyntaxFormatter> pool = new Pool<YamlSyntaxFormatter>(8);

		public static YamlSyntaxFormatter Pop()
		{
			YamlSyntaxFormatter result;
			if(!pool.TryGet(out result))
			{
				return new YamlSyntaxFormatter();
			}
			return result;
		}

		public static void Dispose(ref YamlSyntaxFormatter returning)
		{
			pool.Dispose(ref returning);
		}
	}
}