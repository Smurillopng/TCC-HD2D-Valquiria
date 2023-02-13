#define ENABLE_UNFOLD_ANIMATIONS
#define ENABLE_UNFOLD_ANIMATIONS_ALPHA_TWEENING

#define DEBUG_ALPHA_TWEENING

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Helper class for GUI drawing with the height of a GUI block scaled down by a multiplier.
	/// Used by IParentDrawer for animating their unfolded state.
	/// 
	/// There are two ways you can use the scaler.
	/// 
	/// 1. Use the constructor with parameters combined with using statement. Scaling  
	/// will begin immediately, and stops after the scope of the using statement is left.
	///   
	/// 2. Use the parameterless constructor to create a cached instance of MemberScaler,
	/// then manually call BeginScaling and EndScaling to control the scaling.
	/// 
	/// </summary>
	/// <example> Usage with the using statement.
	/// <code>
	/// using(new MemberScaler(startPos, scale))
	/// {
	///     // draw scaled GUI here
	/// }
	/// </code>
	/// </example>
	/// <example> Usage with a cached MemberScaler instance.
	/// <code>
	/// MemberScaler memberScaler = new memberScaler();
	/// 
	/// void OnGUI()
	/// {
	///		memberScaler.BeginScaling(Vector3.zero, 0.5f);
	///		{
	///			// draw scaled GUI here
	///		}
	///		memberScaler.EndScaling();
	/// }
	/// </code>
	/// </example>
	public class MemberScaler : IDisposable
	{
		public static float CurrentScale = 1f;

		private static readonly List<float> activeScalers = new List<float>();
		

		private Matrix4x4 guiMatrixWas = GUI.matrix;
		private Color guiColorWas = GUI.color;
		private bool nowScaling;

		/// <summary>
		/// Initializes a new instance of MemberScaler.
		/// </summary>
		public MemberScaler() { }

		/// <summary>
		/// Initializes a new instance of MemberScaler and instantly starts scaling.
		/// </summary>
		/// <param name="startPosition">
		/// The top left corner of first scaled member's draw position;
		/// the pivot point point towards which scaled members will shrink
		/// as scale value gets closer to zero. </param>
		/// <param name="scale"> Scale of members. </param>
		/// <returns> A MemberScaler. </returns>
		public MemberScaler(Vector3 startPosition, float scale)
		{
			BeginScaling(startPosition, scale);
		}
		
		public void BeginScaling(Vector3 startPosition, float scale)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!nowScaling);
			Debug.Assert(scale > 0f, "You should only use MemberScaler with a scale value ("+scale+") larger than zero.");
			Debug.Assert(scale <= 1f, "MemberScaler scale value ("+scale+") was larger than one.");
			#endif

			#if ENABLE_UNFOLD_ANIMATIONS
			if(scale >= 1f)
			{
				return;
			}

			nowScaling = true;

			activeScalers.Add(scale);
			RecalculateCurrentScale();

			guiMatrixWas = GUI.matrix;
			guiColorWas = GUI.color;
			
			var matrixScale = Vector2.one;
			matrixScale.y = scale <= float.Epsilon ? float.Epsilon : scale;
			GUIUtility.ScaleAroundPivot(matrixScale, startPosition);

			GUI.changed = true;

			#if ENABLE_UNFOLD_ANIMATIONS_ALPHA_TWEENING
			var color = guiColorWas;
			color.a = scale;
			GUI.color = color;
			#endif

			#endif
		}

		private void RecalculateCurrentScale()
		{
			CurrentScale = 1f;
			for(int n = activeScalers.Count - 1; n >= 0; n--)
			{
				CurrentScale *= activeScalers[n];
			}
		}

		public void EndScaling()
		{
			#if ENABLE_UNFOLD_ANIMATIONS
			if(nowScaling)
			{
				nowScaling = false;
				GUI.color = guiColorWas;
				GUI.matrix = guiMatrixWas;

				activeScalers.RemoveAt(activeScalers.Count - 1);
				RecalculateCurrentScale();
			}
			#endif
		}

		public void Dispose()
		{
			EndScaling();

			#if DEV_MODE && DEBUG_ALPHA_TWEENING
			if(GUI.color.a < 1f) { Debug.LogWarning("MemberScaler.Dispose - GUI.color.a < 1: " + GUI.color.a); }
			#endif
		}
	}
}