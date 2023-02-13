using System;
using SharpNeatLib.Maths;
using System.Globalization;

namespace Sisus
{
	public static class RandomUtils
	{
		private const string AlphaNumerics = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
		private const int AlphaNumericsCount = 62;

		private static FastRandom random = new FastRandom(123);
		private static Random systemRandom = new Random(123);
		private static readonly byte[] bufferLong = new byte[8];
		private static readonly byte[] bufferByte = new byte[1];

		public static long Long(long min, long max)
		{
			ulong range = (ulong)(max - min);
			ulong randomUlong;

			do
			{
				random.NextBytes(bufferLong);
				randomUlong = (ulong)BitConverter.ToInt64(bufferLong, 0);
			}while (randomUlong > ulong.MaxValue - ((ulong.MaxValue % range) + 1) % range);

			return (long)(randomUlong % range + (ulong)min);
		}

		public static ulong ULong(ulong min, ulong max)
		{
			ulong range = max - min;
			ulong randomUlong;

			do
			{
				random.NextBytes(bufferLong);
				randomUlong = (ulong)BitConverter.ToInt64(bufferLong, 0);
			}while (randomUlong > ulong.MaxValue - ((ulong.MaxValue % range) + 1) % range);

			return (randomUlong % range + min);
		}

		public static int Int()
		{
			return systemRandom.Next(int.MinValue, int.MaxValue);
		}

		public static short Short()
		{
			return (short)systemRandom.Next(short.MinValue, short.MaxValue);
		}

		public static ushort UShort()
		{
			return (ushort)systemRandom.Next(ushort.MinValue, ushort.MaxValue);
		}

		public static float Float(float min, float max)
		{
			//return random.NextFloat(min, max);
			return UnityEngine.Random.Range(min, max);
		}

		public static double Double(double min, double max)
		{
			return random.NextDouble(min, max);
		}

		public static decimal Decimal()
		{
			return new decimal(Int(), Int(), Int(), Bool(), (byte)systemRandom.Next(29));
		}

		public static uint UInt()
		{
			return random.NextUInt();
		}

		public static bool Bool()
		{
			return random.NextBool();
		}

		public static string String(int minCharacterCount, int maxCharacterCount)
		{
			return String(random.Next(minCharacterCount, maxCharacterCount));
		}

		public static string String(int characterCount)
		{
			if(characterCount == 0)
			{
				return "";
			}
			var bitCount = (characterCount * 6);
			var byteCount = ((bitCount + 7) / 8);
			var bytes = new byte[byteCount];
			random.NextBytes(bufferLong);
			var result = Convert.ToBase64String(bytes);
			return result;
		}

		public static string Alphanumeric(int minCharacterCount, int maxCharacterCount)
		{
			return Alphanumeric(random.Next(minCharacterCount, maxCharacterCount));
		}

		public static string Alphanumeric(int characterCount)
		{
			if(characterCount == 0)
			{
				return "";
			}

			var chars = new char[characterCount];
			for(int n = chars.Length - 1; n >= 0; n--)
			{
				chars[n] = AlphaNumerics[UnityEngine.Random.Range(0, AlphaNumericsCount)];
			}

			return new string(chars);
		}

		public static char Alphanumeric()
		{
			return AlphaNumerics[UnityEngine.Random.Range(0, AlphaNumericsCount)];
		}

		/// <summary>
		/// Returns a randomly chosen character.
		/// </summary>
		/// <returns>
		/// A char.
		/// </returns>
		public static char Char()
		{
			do
			{
				int i = UnityEngine.Random.Range(char.MinValue, char.MaxValue + 1);
				char c = Convert.ToChar(i);
				switch(char.GetUnicodeCategory(c))
				{
					case UnicodeCategory.Control:
					case UnicodeCategory.Surrogate:
						break;
					default:
						return c;
				}
			}while(true);
		}

		/// <summary>
		/// Returns a randomly chosen byte with a value between 0 and 255
		/// </summary>
		/// <returns>
		/// A random byte.
		/// </returns>
		public static byte Byte()
		{
			random.NextBytes(bufferByte);
			return bufferByte[0];
		}

		/// <summary>
		/// Returns a randomly chosen sbyte with a value between -128 and 127
		/// </summary>
		/// <returns>
		/// A random sbyte.
		/// </returns>
		public static sbyte SByte()
		{
			return (sbyte)UnityEngine.Random.Range(-128, 128);
		}
	}
}