using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public static class TransformExtensions
	{
		public static Transform[] GetChildren(this Transform parent)
		{
			int count = parent.childCount;
			var results = new Transform[count];
			for(int n = 0; n < count; n++)
			{
				results[n] = parent.GetChild(n);
			}
			return results;
		}
		
		public static void SetWorldScale(this Transform transform, Vector3 scale)
		{
			transform.localScale = Vector3.one;
			var lossyScale = transform.lossyScale;
			transform.localScale = new Vector3(scale.x / lossyScale.x, scale.y / lossyScale.y, scale.z / lossyScale.z);
		}

		public static string HierarchyPath([NotNull]this Transform transform)
		{
			var sb = new System.Text.StringBuilder();
			sb.Append(transform.name);
			while(transform.parent != null)
			{
				transform = transform.parent;
				sb.Insert(0, "/");
				sb.Insert(0, transform.name);
			}
			return sb.ToString();
		}

		public static bool ActiveInPrefabHierarchy([NotNull]this Transform target)
		{
			do
			{
				if(!target.gameObject.activeSelf)
				{
					return false;
				}
				target = target.parent;
			}
			while(target != null);

			return true;
		}

		public static void ResetWithoutAffectingChildren([NotNull]this Transform transform)
		{
			int childCount = transform.childCount;
			if(childCount == 0)
			{
				transform.localPosition = Vector3.zero;
				transform.localEulerAngles = Vector3.zero;
				transform.localScale = Vector3.one;
				return;
			}

			var rectTransform = transform as RectTransform;
			if(rectTransform != null)
			{
				rectTransform.ResetWithoutAffectingChildren();
				return;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!(transform is RectTransform));
			#endif

			var temp = (new GameObject()).transform;
			temp.parent = transform.parent;
			temp.localPosition = transform.localPosition;
			temp.localEulerAngles = transform.localEulerAngles;
			temp.localScale = transform.localScale;
			
			var children = ArrayPool<Transform>.Create(childCount);
			for(int n = childCount - 1; n >= 0; n--)
			{
				var child = transform.GetChild(n);
				children[n] = transform.GetChild(n);
				child.parent = temp;
			}

			transform.localPosition = Vector3.zero;
			transform.localEulerAngles = Vector3.zero;
			transform.localScale = Vector3.one;

			for(int n = childCount - 1; n >= 0; n--)
			{
				var child = temp.GetChild(n);
				child.parent = transform;
			}

			ArrayPool<Transform>.Dispose(ref children);

			Platform.Active.Destroy(temp.gameObject);
		}

		public static void ResetWithoutAffectingChildren([NotNull]this RectTransform transform)
		{
			int childCount = transform.childCount;
			if(childCount == 0)
			{
				transform.localPosition = Vector3.zero;
				transform.localEulerAngles = Vector3.zero;
				transform.localScale = Vector3.one;
				return;
			}

			var temp = (new GameObject("", typeof(RectTransform))).transform;
			temp.parent = transform.parent;
			temp.localPosition = transform.localPosition;
			temp.localEulerAngles = transform.localEulerAngles;
			temp.localScale = transform.localScale;
			
			var children = ArrayPool<Transform>.Create(childCount);
			for(int n = childCount - 1; n >= 0; n--)
			{
				var child = transform.GetChild(n);
				children[n] = transform.GetChild(n);
				child.parent = temp;
			}

			transform.localPosition = Vector3.zero;
			transform.localEulerAngles = Vector3.zero;
			transform.localScale = Vector3.one;

			for(int n = childCount - 1; n >= 0; n--)
			{
				var child = temp.GetChild(n);
				child.parent = transform;
			}

			ArrayPool<Transform>.Dispose(ref children);

			Platform.Active.Destroy(temp.gameObject);
		}

		public static bool LocalStateAtDefaultValues([NotNull]this Transform transform)
		{
			return transform.localPosition.IsZero() && transform.localEulerAngles.IsZero() && transform.localScale == Vector3.one;
		}
	}
}