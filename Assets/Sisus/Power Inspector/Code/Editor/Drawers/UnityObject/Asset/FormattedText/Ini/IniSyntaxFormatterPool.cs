namespace Sisus
{
	public static class IniSyntaxFormatterPool
	{
		private static Pool<IniSyntaxFormatter> pool = new Pool<IniSyntaxFormatter>(8);

		public static IniSyntaxFormatter Pop()
		{
			IniSyntaxFormatter result;
			if(!pool.TryGet(out result))
			{
				return new IniSyntaxFormatter();
			}
			return result;
		}

		public static void Dispose(ref IniSyntaxFormatter returning)
		{
			pool.Dispose(ref returning);
		}
	}
}