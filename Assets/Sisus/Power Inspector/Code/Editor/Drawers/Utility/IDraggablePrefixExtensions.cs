using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public static class IDraggablePrefixExtensions
	{
		public static float GetMouseDelta([NotNull]this IDraggablePrefix subject, [NotNull]Event inputEvent, Vector2 mouseDownPosition)
		{
			var delta = inputEvent.mousePosition.x - mouseDownPosition.x;

			// In default Unity Inspector Control / Cmd is used to lower the drag sensitivity,
			// but in Power Inspector that is already used for snapping, so we use alt instead.
			if(inputEvent.alt)
			{
				return delta * 0.25f;
			}
			if(inputEvent.shift)
			{
				return delta * 4f;
			}
			return delta;
		}
	}
}