namespace Sisus
{
	public static class PlainTextSyntaxFormatterPool
	{
		private static Pool<PlainTextSyntaxFormatter> pool = new Pool<PlainTextSyntaxFormatter>(8);

		public static PlainTextSyntaxFormatter Pop()
		{
			PlainTextSyntaxFormatter result;
			if(!pool.TryGet(out result))
			{
				return new PlainTextSyntaxFormatter();
			}
			return result;
		}

		public static void Dispose(ref PlainTextSyntaxFormatter returning)
		{
			pool.Dispose(ref returning);
		}
	}
}