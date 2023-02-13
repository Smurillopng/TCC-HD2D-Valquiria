#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(ParticleSystem), false, true)]
	public class ParticleSystemDrawer : CustomEditorComponentDrawer
	{
		/// <inheritdoc />
		public override PrefixResizer PrefixResizer
		{
			get
			{
				return PrefixResizer.Disabled;
			}
		}

		/// <inheritdoc />
		protected override float ControlsRowHeight
		{
			get
			{
				return 13f;
			}
		}

		/// <inheritdoc/>
		protected override float ControlsTopMargin
		{
			get
			{
				return 52f;
			}
		}

		/// <inheritdoc/>
		protected override float ControlsLeftMargin
		{
			get
			{
				return 8f;
			}
		}

		/// <inheritdoc/>
		protected override float ControlsRightMargin
		{
			get
			{
				return 1f;
			}
		}

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			// 133f would be the "correct" value, but EditorGUIUtility.labelWidth seems to be stuck at 146f
			// due to how the ParticleSystemInspector works internally
			return PrefixResizeUtility.LabelWidthFromEditorGUIUtilityToDrawGUI(146f);
		}
		
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static ParticleSystemDrawer Create(Component[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			ParticleSystemDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ParticleSystemDrawer();
			}
			result.Setup(targets, parent, inspector, Types.GetInternalEditorType("UnityEditor.ParticleSystemInspector"));
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		protected override bool WantsToOverrideFieldFocusing()
		{
			return false;
		}
		
		/// <inheritdoc/>
		public override bool Draw(Rect position)
		{
			// prefix width seems to be stuck at 150f due to how the ParticleSystemInspector works
			// set PrefixLabelWidth to this same value constantly to make them be in sync.
			if(UsesEditorForDrawingBody)
			{
				PrefixLabelWidth = GetOptimalPrefixLabelWidthForEditor(0);
			}
			return base.Draw(position);
		}
		

		/// <inheritdoc/>
		protected override float GetUpDownYMatchScore(Rect rect, float diffY)
		{
			int diffYInt = (int)diffY;
			float result = GetYMatchScoreBase(rect);

			if(!((float)diffYInt).Equals(diffY))
			{
				#if DEV_MODE && DEBUG_GET_NEXT_CONTROL_Y
				Debug.Log("rect "+rect+" returning "+result+" + 21000f because diffYInt ("+diffYInt+") != diffY ("+diffY+") with difference="+Mathf.Abs(diffYInt-diffY));
				#endif
				return result + 21000f;
			}

			switch(diffYInt)
			{
				case 0:
					//next row, with 1px margin for both fields
					return result - 10000f;
					/*
				case 8:
					//next row with a small gap between (e.g. RectTransform, AudioSource)
					return result - 8000f;
					*/
				case 20:
					//two row difference
					return result - 6000f;
					/*
				case 45:
					//two fields with a help box between them (e.g. MeshRenderer)
					return result - 7000f;
					*/
				default:
					#if DEV_MODE && DEBUG_GET_NEXT_CONTROL_Y
					Debug.Log("rect "+rect+" returning "+result+" + 20000f because diffYInt ("+diffYInt+") was not a value we like");
					#endif
					return result + 20000f;
			}
		}

		/// <inheritdoc/>
		protected override float GetUpDownWidthMatchScore(float prevControlWidth, Rect rect)
		{
			if(prevControlWidth.Equals(rect.width))
			{
				return -100f;
			}

			if(rect.x.Equals(ControlsLeftMargin))
			{
				float widthWithoutLeftMargin = Width - ControlsLeftMargin;
				float fullWidthDiff = widthWithoutLeftMargin - rect.width - ControlsRightMargin;
				if(fullWidthDiff.Equals(14f))
				{
					return 0f;
				}
			}
			return base.GetUpDownWidthMatchScore(prevControlWidth, rect);
		}
	}
}
#endif