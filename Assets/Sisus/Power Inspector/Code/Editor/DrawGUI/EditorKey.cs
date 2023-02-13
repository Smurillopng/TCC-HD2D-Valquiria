#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// An EditorKey is a stuct that represents an Editor for specific targets.
	/// 
	/// Given the same target parameters, an EditorKey with the same hash code
	/// will get returned every time inside the same session.
	/// 
	/// The hash code of each EditorKey is also unique, meaning that two EditorKeys
	/// with different sets of targets should always have different hash codes.
	/// 
	/// Can be used as Dictionary keys for caching and then fetching Editors.
	/// </summary>
	public struct EditorKey : IEquatable<EditorKey>
	{
		public static readonly EditorKey None = new EditorKey(0);

		private readonly int hash;

		private EditorKey(int setHash)
		{
			hash = setHash;
		}

		/// <summary> Gets EditorKey representing an Editor with the given target. </summary>
		/// <param name="target"> Editor target. This cannot be null. </param>
		/// <param name="isAssetImporterEditor"> True is Editor is an asset importer editor. </param>
		public EditorKey([NotNull]Object target, bool isAssetImporterEditor)
		{
			if(ReferenceEquals(target, null))
			{
				hash = 1;
			}
			else
			{
				unchecked // Overflow is fine, just wrap
				{
					hash = 761 + target.GetInstanceID();
					
					// This was needed to distinguish between GameObjectInspector and ModelImporterEditor,
					// since both refer to same GameObject target.
					if(isAssetImporterEditor)
					{
						hash *= 761;
					}
					#if DEV_MODE && PI_ASSERTATIONS
					else { UnityEngine.Debug.Assert(!(target is UnityEditor.AssetImporter), StringUtils.TypeToString(target)+ " was an AssetImporter but isAssetImporterEditor was "+StringUtils.False); }
					#endif
				}
			}
		}

		/// <summary> Gets EditorKey representing an Editor with the given target. </summary>
		/// <param name="targets"> Editor targets. This cannot be null or empty. </param>
		/// <param name="isAssetImporterEditor"> True is Editor is an asset importer editor. </param>
		public EditorKey([NotNull]Object[] targets, bool isAssetImporterEditor)
		{
			#if UNITY_EDITOR && DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(targets.Length > 0);
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				if(ReferenceEquals(targets[n], null))
				{
					UnityEngine.Debug.LogWarning("EditorKey called for targets which contained null elements: " + StringUtils.ToString(targets));
				}
			}
			#endif

			unchecked // Overflow is fine, just wrap
			{
				hash = 1;
				for(int n = targets.Length - 1; n >= 0; n--)
				{
					var target = targets[n];
					if(!ReferenceEquals(target, null))
					{
						hash = hash * 761 + target.GetInstanceID();
					}
				}

				// This was needed to distinguish between GameObjectInspector and ModelImporterEditor,
				// since both refer to same GameObject targets.
				if(isAssetImporterEditor)
				{
					hash *= 761;
				}
				#if DEV_MODE && PI_ASSERTATIONS
				// NOTE: This warning is shown with Script Inspector 3's custom Editor, but there is no bug.
				else if(targets[0] is UnityEditor.AssetImporter) { UnityEngine.Debug.LogWarning("Editor target "+StringUtils.TypeToString(targets[0])+ " was AssetImporter but isAssetImporterEditor was " + StringUtils.False +". Sure that Editor isn't AssetImporterEditor?"); }
				#endif
			}
		}
	
		public override bool Equals(object obj)
		{
			if(obj is EditorKey)
			{
				return Equals((EditorKey)obj);
			}
			return false;
		}

		public bool Equals(EditorKey other)
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

		public static bool operator == (EditorKey a, EditorKey b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(EditorKey a, EditorKey b)
		{
			return !a.Equals(b);
		}
	}
}
#endif