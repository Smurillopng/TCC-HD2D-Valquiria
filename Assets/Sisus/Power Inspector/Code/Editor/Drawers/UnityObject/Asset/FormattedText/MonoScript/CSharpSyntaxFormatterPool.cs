namespace Sisus
{
	public static class CSharpSyntaxFormatterPool
	{
		private static Pool<CSharpSyntaxFormatter> pool = new Pool<CSharpSyntaxFormatter>(8);
		
		public static CSharpSyntaxFormatter Pop()
		{
			CSharpSyntaxFormatter result;
			if(!pool.TryGet(out result))
			{
				return new CSharpSyntaxFormatter();
			}
			return result;
		}

		public static void Dispose(ref CSharpSyntaxFormatter returning)
		{
			pool.Dispose(ref returning);
		}
	}
}