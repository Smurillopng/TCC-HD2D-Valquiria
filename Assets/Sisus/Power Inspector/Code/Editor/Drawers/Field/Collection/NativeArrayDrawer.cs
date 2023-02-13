#if UNITY_2018_1_OR_NEWER
using Unity.Collections;
using Sisus.Attributes;

namespace Sisus
{
    [DrawerForField(typeof(NativeArray<>), true, true)]
	public sealed class NativeArrayDrawer<T> : EnumerableDrawer<T> { }
}
#endif