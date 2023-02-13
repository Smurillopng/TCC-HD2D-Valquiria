using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(Collider), true, true)]
	public class ColliderDrawer : CustomEditorComponentDrawer
	{
		/// <inheritdoc />
		protected override float EstimatedUnfoldedHeight
		{
			get
			{
				#if UNITY_2019_3_OR_NEWER
				return 147f;
				#else
				return 123f;
				#endif
			}
		}

		/// <inheritdoc/>
		protected override bool Enabled
		{
			get
			{
				return (Target as Collider).enabled;
			}
		}
	}
}