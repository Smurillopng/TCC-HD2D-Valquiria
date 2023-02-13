using System;
using UnityEngine;

namespace Sisus
{
	public static class CodeLinePool
	{
		private static Pool<CodeLine> pool = new Pool<CodeLine>(50);
		
		public static CodeLine Create(string unformatted, string formatted)
		{
			CodeLine result;
			if(!pool.TryGet(out result))
			{
				return new CodeLine(unformatted, formatted);
			}
			result.formatted = formatted;
			result.unformatted = unformatted;

			return result;
		}
		
		public static CodeLine Create(string unformatted, ITextSyntaxFormatter builder)
		{
			CodeLine result;
			if(!pool.TryGet(out result))
			{
				return new CodeLine(unformatted, builder);
			}
			result.Set(unformatted, builder);
			return result;
		}
		
		public static void Create(string[] linesUnformatted, string[] linesFormatted, ITextSyntaxFormatter builder, ref CodeLine[] result)
		{
			int count = linesFormatted.Length;
			int unformattedCount = linesUnformatted.Length;
			if(unformattedCount != count)
			{
				//TO DO: set a boolean flag or something that triggers this warning from the main thread instead
				#if !USE_THREADING
				Debug.Log("WARNING: linesFormatted.Length ("+count+") != linesUnformatted.Length ("+unformattedCount+")");
				#endif
				Array.Resize(ref linesUnformatted, count);
			}


			int oldCount = result.Length;
			if(oldCount != count)
			{
				for(int n = oldCount - 1; n >= count; n--)
				{
					Dispose(ref result[n]);
				}
				Array.Resize(ref result, count);
			}
			
			for(int n = count - 1; n >= 0; n--)
			{
				Replace(ref result[n], linesUnformatted[n], builder);
			}
		}
		
		public static void Replace(ref CodeLine replace, string unformatted, ITextSyntaxFormatter builder)
		{
			if(replace == null)
			{
				replace = Create(unformatted, builder);
			}
			else
			{
				replace.Set(unformatted, builder);
			}
		}

		public static void Dispose(ref CodeLine disposing)
		{
			pool.Dispose(ref disposing);
		}
	}
}