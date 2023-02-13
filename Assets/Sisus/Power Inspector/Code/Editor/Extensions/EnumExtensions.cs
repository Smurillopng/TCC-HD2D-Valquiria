using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public static class EnumExtensions
	{
		[Pure]
		public static Enum SetFlag([NotNull]this Enum currentValue, [NotNull]Enum flagToSet)
		{
			#if DEV_MODE
			Debug.Assert(Enum.IsDefined(currentValue.GetType(), flagToSet));
			#endif

			var setFlagInt = (ulong)Convert.ChangeType(flagToSet, Types.ULong);

			if(setFlagInt == 0)
			{
				return ClearFlags(currentValue);
			}

			var currentValueInt = (ulong)Convert.ChangeType(currentValue, Types.ULong);
			
			return Enum.ToObject(currentValue.GetType(), currentValueInt | setFlagInt) as Enum;
		}

		[Pure]
		public static Enum RemoveFlag([NotNull]this Enum currentValue, [NotNull]Enum flagToRemove)
		{
			#if DEV_MODE
			Debug.Assert(Enum.IsDefined(currentValue.GetType(), flagToRemove));
			#endif

			var currentValueInt = (ulong)Convert.ChangeType(currentValue, Types.ULong);
			var removeFlagInt = (ulong)Convert.ChangeType(flagToRemove, Types.ULong);
			return Enum.ToObject(currentValue.GetType(), currentValueInt & ~removeFlagInt) as Enum;
		}

		/// <summary>
		/// Check to see if a flags enumeration has a specific flag set.
		/// </summary>
		/// <param name="enumValue"> Flags enumeration to check. </param>
		/// <param name="flag"> Flag to find. </param>
		/// <returns>True if enum value has flag set, false if not. </returns>
		[Pure]
		public static bool HasFlag([NotNull]this Enum enumValue, [NotNull]Enum flag)
		{
			#if DEV_MODE
			Debug.Assert(Enum.IsDefined(enumValue.GetType(), flag));
			#endif
		
			var enumValueInt = (ulong)Convert.ChangeType(enumValue, Types.ULong);
			var flagValueInt = (ulong)Convert.ChangeType(flag, Types.ULong);

			if(flagValueInt == 0)
			{
				return enumValueInt == 0;
			}

			return (enumValueInt & flagValueInt) == flagValueInt;
		}

		public static List<Enum> GetFlags([NotNull]this Enum enumValue)
		{
			var result = new List<Enum>();
			var enumValueInt = (ulong)Convert.ChangeType(enumValue, Types.ULong);
			var enumType = enumValue.GetType();
			var allFlags = Enum.GetValues(enumType);
			for(int n = allFlags.Length - 1; n >= 0; n--)
			{
				var flag = allFlags.GetValue(n);
				var flagInt = (ulong)Convert.ChangeType(flag, Types.ULong);
				if((enumValueInt & flagInt) == flagInt)
				{
					result.Add(Enum.ToObject(enumType, flag) as Enum);
				}
			}
			return result;
		}

		[Pure]
		public static Enum ClearFlags([NotNull]this Enum enumValue)
		{
			return Enum.ToObject(enumValue.GetType(), 0) as Enum;
		}

		[Pure, NotNull]
		public static Enum SetAllFlags([NotNull]this Enum enumValue)
		{
			ulong result = 0;
			var allFlags = Enum.GetValues(enumValue.GetType());
			for(int n = allFlags.Length - 1; n >= 0; n--)
			{
				var flag = (ulong)Convert.ChangeType(allFlags.GetValue(n), Types.ULong);
				result = result | flag;
			}

			return Enum.ToObject(enumValue.GetType(), result) as Enum;
		}

		/// <summary>
		/// Given an enum value, returns the next enum value of same type.
		/// Loops back to first enum value from last value.
		/// </summary>
		/// <param name="enumValue"> The current enum value. This cannot be null. </param>
		/// <returns> An Enum. This will never be null. </returns>
		[Pure, NotNull]
		public static Enum NextEnumValue([NotNull]this Enum enumValue)
		{
			var enumValues = Enum.GetValues(enumValue.GetType());
			int nextIndex = Array.IndexOf(enumValues, enumValue) + 1;
			return enumValues.Length == nextIndex ? (Enum)enumValues.GetValue(0) : (Enum)enumValues.GetValue(nextIndex);
		}

				/// <summary>
		/// Given an enum value, returns the previous enum value of same type.
		/// Loops back to last enum value from first value.
		/// </summary>
		/// <param name="enumValue"> The current enum value. This cannot be null. </param>
		/// <returns> An Enum. This will never be null. </returns>
		[Pure, NotNull]
		public static Enum PreviousEnumValue([NotNull]this Enum enumValue)
		{
			var enumValues = Enum.GetValues(enumValue.GetType());
			int previousIndex = Array.IndexOf(enumValues, enumValue) - 1;
			return previousIndex < 0 ? (Enum)enumValues.GetValue(enumValues.Length - 1) : (Enum)enumValues.GetValue(previousIndex);
		}
	}
}