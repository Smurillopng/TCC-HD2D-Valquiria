#if UNITY_EDITOR
using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(Animator), false, true)]
	public class AnimatorDrawer : CustomEditorComponentDrawer
	{
		/*
		private static readonly string[] DontDisplayMembers =
		{
			"GetPlaybackTime", //Can't call GetPlaybackTime while not in playback mode. You must call StartPlayback before.
			"playbackTime", //Can't call GetPlaybackTime while not in playback mode. You must call StartPlayback before.
			"bodyRotation", //Setting and getting Body Position/Rotation, IK Goals, Lookat and BoneLocalRotation should only be done in OnAnimatorIK or OnStateIK
			"bodyPosition", //Setting and getting Body Position/Rotation, IK Goals, Lookat and BoneLocalRotation should only be done in OnAnimatorIK or OnStateIK
			"layerCount", //Animator is not playing an AnimatorController
			"parameters",  //Animator is not playing an AnimatorController
			"parameterCount"  //Animator is not playing an AnimatorController
		};

		/// <inheritdoc/>
		protected override string[] NeverDisplayMembers()
		{
			return DontDisplayMembers;
		}
		*/

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 137f;
		}
	}
}
#endif