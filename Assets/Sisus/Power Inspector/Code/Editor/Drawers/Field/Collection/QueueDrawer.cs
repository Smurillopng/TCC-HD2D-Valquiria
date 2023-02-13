#define SAFE_MODE

using System;
using System.Collections.Generic;
using Sisus.Attributes;

namespace Sisus
{
    [Serializable, DrawerForField(typeof(Queue<>), true, true)]
	public class QueueDrawer<T> : EnumerableDrawer<T> { }
}