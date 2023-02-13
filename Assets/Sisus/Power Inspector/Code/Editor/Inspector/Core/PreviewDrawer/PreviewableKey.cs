using System;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Each PreviewableKey struct, given the same set of target UnityEngine.Objects
	/// and the Type of an ObjectPreview, has the same hash code, which makes them
	/// useful as Dictionary keys for getting the Editor for a set of targets.
	/// </summary>
	public struct PreviewableKey : IEquatable<PreviewableKey>
	{
		public static readonly PreviewableKey None = new PreviewableKey(0);

		private readonly int hash;
		
		private PreviewableKey(int setHash)
		{
			hash = setHash;
		}

		public PreviewableKey([NotNull]Type setPreviewType, [NotNull]Object setTarget)
		{
			unchecked // Overflow is fine, just wrap
			{
				hash = 761 + setTarget.GetInstanceID();
				hash = hash * 1777 + setPreviewType.GetHashCode();
			}
		}

		public PreviewableKey([NotNull]Type setPreviewType, Object[] setTargets)
		{
			unchecked // Overflow is fine, just wrap
			{
				hash = 1;
				for(int n = setTargets.Length - 1; n >= 0; n--)
				{
					hash = hash * 761 + setTargets[n].GetInstanceID();
				}
				hash = hash * 1777 + setPreviewType.GetHashCode();
			}
		}
	
		public override bool Equals(object obj)
		{
			if(obj is PreviewableKey)
			{
				return Equals((PreviewableKey)obj);
			}
			return false;
		}

		public bool Equals(PreviewableKey other)
		{
			return hash == other.hash;
		}

		public override int GetHashCode()
		{
			return hash;
		}

		public override string ToString()
		{
			return StringUtils.ToString(hash);
		}

		public static bool operator == (PreviewableKey a, PreviewableKey b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(PreviewableKey a, PreviewableKey b)
		{
			return !a.Equals(b);
		}
	}
}