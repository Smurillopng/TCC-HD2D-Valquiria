using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(ParticleSystemRenderer), false, true)]
	public class ParticleSystemRendererDrawer : RendererDrawer
	{
		/// <inheritdoc/>
		public override bool ShouldShowInInspector
		{
			get
			{
				// ParticleSystemRenderer should never be shown in the inspector (except in debug mode) 
				if(DebugMode)
				{
					return base.ShouldShowInInspector;
				}
				return false;
			}
		}
	}
}