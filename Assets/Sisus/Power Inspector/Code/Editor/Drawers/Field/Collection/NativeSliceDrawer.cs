#if UNITY_2018_1_OR_NEWER
using Unity.Collections;
using Sisus.Attributes;

namespace Sisus
{
    [DrawerForField(typeof(NativeSlice<>), true, true)]
	public sealed class NativeSliceDrawer<T> : EnumerableDrawer<T> { }
}
#endif