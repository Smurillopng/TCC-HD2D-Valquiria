using System;
using UnityEngine;

namespace Sisus
{
	public static class MathUtils
	{
		public static bool Approximately(float a, float b)
		{
			return Difference(a, b) < 0.001f;
		}

		public static int Abs(int value)
		{
			if(value < 0)
			{
				return -value;
			}
			return value;
		}

		public static float Abs(float value)
		{
			if(value < 0f)
			{
				return -value;
			}
			return value;
		}

		public static float Difference(float a, float b)
		{
			if(a > b)
			{
				return a - b;
			}
			return b - a;
		}

		public static float VerticalDifference(Rect a, Rect b)
		{
			if(a.y < b.y)
			{
				return b.y - a.yMax;
			}
			return a.y - b.yMax;
		}

		public static double Clamp(double value, float min, float max)
		{
			if(value < min)
			{
				return min;
			}
			if(value > max)
			{
				return max;
			}
			return value;
		}

		public static int Clamp(int value, float min, float max)
		{
			if(value < min)
			{
				return Mathf.CeilToInt(min);
			}
			if(value > max)
			{
				return Mathf.FloorToInt(max);
			}
			return value;
		}

		public static short Clamp(short value, float min, float max)
		{
			if(value < min)
			{
				return (short)min;
			}
			if(value > max)
			{
				return (short)max;
			}
			return value;
		}
		
		public static double Subtract<TValue, TValue2>(TValue a, TValue2 b) where TValue : IConvertible where TValue2 : IConvertible
		{
			return Convert.ToDouble(a) - Convert.ToDouble(b);
		}

		public static TValue Min<TValue>(TValue a, TValue b) where TValue : IComparable
		{
			return a.CompareTo(b) <= 0 ? a : b;
		}

		public static TValue Max<TValue>(TValue a, TValue b) where TValue : IComparable
		{
			return a.CompareTo(b) >= 0 ? a : b;
		}
		
		public static double Lerp(float a, float b, double t)
		{
			t = Clamp(t, 0f, 1f);
			return a * (1d - t) + b * t;
		}

		public static double Lerp(double a, double b, double t)
		{
			t = Clamp(t, 0f, 1f);
			return a * (1d - t) + b * t;
		}

		public static int PositiveMin(int a, int b)
		{
			if(a >= 0)
			{
				if(b >= 0)
				{
					if(a <= b)
					{
						return a;
					}
					return b;
				}
				return a;
			}
			if(b >= 0)
			{
				return b;
			}
			return -1;
		}

		public static int PositiveMin(int a, int b, int c)
		{
			if(a >= 0)
			{
				if(b >= 0)
				{
					if(c >= 0)
					{
						if(a <= b)
						{
							if(a <= c)
							{
								return a;
							}
							return c;
						}
						if(b <= c)
						{
							return b;
						}
						return c;
					}
					if(a <= b)
					{
						return a;
					}
					return b;
				}
				if(c >= 0)
				{
					if(a <= c)
					{
						return a;
					}
					return c;
				}
				return a;
			}
			if(b >= 0)
			{
				if(c >= 0)
				{
					if(b <= c)
					{
						return b;
					}
					return c;
				}
				return b;
			}
			return -1;
		}
	}
}