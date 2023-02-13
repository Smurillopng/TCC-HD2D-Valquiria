//#define DEBUG_EDITING_TEXT_FIELD
#define DEBUG_SYNC_EDITING_TEXT_FIELD

#if UNITY_EDITOR
#endif

using Object = UnityEngine.Object;

namespace Sisus.PI
{
	internal static class NullExtensions
	{
		/// <summary>
		/// A value against which any <see cref="object"/> can be compared to determine whether or not it is
		/// <see langword="null"/> or an <see cref="Object"/> which has been <see cref="Object.Destroy">destroyed</see>.
		/// <example>
		/// <code>
		/// using static Sisus.PI.NullExtensions;
		/// 
		/// public class EventListener : MonoBehaviour{IEvent}, IEventListener
		/// {
		///		private IEvent trigger;
		/// 
		///		protected override void Init(IEventBroadcaster trigger)
		///		{
		///			this.trigger = trigger;
		///		}
		/// 
		///		private void OnEnable()
		///		{
		///			trigger.AddListener(this);
		///		}
		/// 
		///		private void OnEvent()
		///		{
		///			Debug.Log($"{name} heard event {trigger}.");
		///		}
		/// 
		///		private void OnDisable()
		///		{
		///			if(trigger != Null)
		///			{
		///				trigger.RemoveListener(this);
		///			}
		///		}
		/// }
		/// </code>
		/// </example>
		/// </summary>
		public static readonly NullComparer Null = new NullComparer();

		public class NullComparer
		{
			internal NullComparer() { }

            public override bool Equals(object obj) => obj is Object unityObject ? unityObject == null : obj is null;
            public override int GetHashCode() => 0;
			public override string ToString() => "Null";

			public static bool operator ==(Object unityObject, NullComparer @null) => unityObject == null;
			public static bool operator !=(Object unityObject, NullComparer @null) => unityObject != null;
			public static bool operator ==(object @object, NullComparer @null) => @object is Object unityObject ? unityObject == null : @object is null;
			public static bool operator !=(object @object, NullComparer @null) => @object is Object unityObject ? unityObject != null : !(@object is null);
		}
	}
}