using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;
using System.Collections;

namespace Sisus
{
	public static class InspectorValues
	{
		private static readonly Dictionary<IDrawer, int> valuesByDrawer = new Dictionary<IDrawer, int>();
		private static readonly HashSet<int> values = new HashSet<int>();

		public static bool IsDuplicateReferenceFor([NotNull]LinkedMemberInfo member, out IDrawer drawer)
		{
			var value = member.GetValue(0);
			if(ReferenceEquals(value, null))
			{
				drawer = null;
				return false;
			}
			int hash = value.GetHashCode();

			foreach(var pair in valuesByDrawer)
			{
				if(pair.Value != hash)
				{
					continue;
				}

				if(ReferenceEquals(value, pair.Value) && pair.Key.MemberInfo.MemberInfo != member.MemberInfo)
				{
					drawer = pair.Key;
					return true;
				}
			}

			var collection = member.GetValue(0) as IEnumerable;
			if(collection == null)
			{
				drawer = null;
				return false;
			}

			foreach(var element in collection)
			{
				if(ReferenceEquals(element, null))
				{
					drawer = null;
					return false;
				}
				hash = element.GetHashCode();

				foreach(var pair in valuesByDrawer)
				{
					if(pair.Value != hash)
					{
						continue;
					}

					if(ReferenceEquals(value, pair.Value) && pair.Key.MemberInfo.MemberInfo != member.MemberInfo)
					{
						drawer = pair.Key;
						return true;
					}
				}
			}

			drawer = null;
			return false;
		}

		public static bool IsDuplicateReference([NotNull]LinkedMemberInfo member)
		{
			#if DEV_MODE
			Debug.Assert(member.CanReadWithoutSideEffects);
			#endif

			var type = member.Type;
			if(type.IsUnityObject())
			{
				return false;
			}

			if(type.IsValueType)
			{
				return false;
			}

			if(type == Types.String)
			{
				return false;
			}

			if(type == Types.Type)
			{
				return false;
			}

			var value = member.GetValue(0);
			if(ReferenceEquals(value, null))
			{
				return false;
			}

			int hash = value.GetHashCode();

			if(!values.Contains(hash))
			{
				return false;
			}

			foreach(var pair in valuesByDrawer)
			{
				if(pair.Value != hash)
				{
					continue;
				}

				if(ReferenceEquals(value, pair.Value) && pair.Key.MemberInfo.MemberInfo != member.MemberInfo)
				{
					return true;
				}
			}

			if(!member.IsCollection)
			{
				return false;
			}

			var collection = member.GetValue(0) as IEnumerable;
			if(collection == null)
			{
				return false;
			}

			foreach(var element in collection)
			{
				if(ReferenceEquals(element, null))
				{
					continue;
				}
				hash = element.GetHashCode();

				if(!values.Contains(hash))
				{
					continue;
				}

				foreach(var pair in valuesByDrawer)
				{
					if(pair.Value != hash)
					{
						continue;
					}

					if(ReferenceEquals(element, pair.Value) && (pair.Key.Parent == null || pair.Key.Parent.MemberInfo == null || pair.Key.Parent.MemberInfo.MemberInfo != member.MemberInfo))
					{
						return true;
					}
				}
			}
			return false;
		}

		public static void Register(IDrawer drawer, object value)
		{
			int hash = value == null ? 0 : value.GetHashCode();
			valuesByDrawer[drawer] = hash;
		}

		public static void Deregister([NotNull]IDrawer drawer)
		{
			int hash;
			if(valuesByDrawer.TryGetValue(drawer, out hash))
			{
				values.Remove(hash);
			}
			valuesByDrawer.Remove(drawer);
		}
	}
}