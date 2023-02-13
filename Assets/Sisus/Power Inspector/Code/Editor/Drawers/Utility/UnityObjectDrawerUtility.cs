//#define DEBUG_WHEN_UNFOLDED
//#define DEBUG_WHEN_CHANGING
//#define DEBUG_WHEN_FOLDED

namespace Sisus
{
	public static class UnityObjectDrawerUtility
	{
		/// <summary>
		/// While true all UnityObjectDrawer are drawn without a header
		/// </summary>
		public static bool HeadlessMode;

		public static float CalculateHeight(IParentDrawer target)
		{
			float unfoldedness = target.Unfoldedness;
			if(unfoldedness > 0f)
			{
				var visibleMembers = target.VisibleMembers;
				int count = visibleMembers.Length;
				if(count > 0)
				{
					float membersHeight = 0f;
					for(int n = count - 1; n >= 0; n--)
					{
						membersHeight += visibleMembers[n].Height;
					}

					#if DEV_MODE && DEBUG_WHEN_UNFOLDED
					UnityEngine.Debug.Log("CalculateHeight(" + target + ") from " + count + " visible members: "+(target.HeaderHeight + membersHeight * unfoldedness + DrawGUI.RightPadding)+" with Unfolded=" + target.Unfolded+ " , Unfoldedness="+target.Unfoldedness+", HeaderHeight=" + target.HeaderHeight);
					#elif DEV_MODE && DEBUG_WHEN_CHANGING
					if(unfoldedness < 1f)
					{
						UnityEngine.Debug.Log("CalculateHeight(" + target + ") from " + count + " visible members: "+(target.HeaderHeight + membersHeight * unfoldedness + DrawGUI.RightPadding)+" with Unfolded=" + target.Unfolded+ " , Unfoldedness="+target.Unfoldedness+", HeaderHeight=" + target.HeaderHeight+ ", DrawGUI.RightPadding="+ DrawGUI.RightPadding);
					}
					#endif

					return target.HeaderHeight + membersHeight * unfoldedness + DrawGUI.RightPadding;
				}
			}

			#if DEV_MODE && DEBUG_WHEN_FOLDED
			UnityEngine.Debug.Log("CalculateHeight(" + target + ") returning HeaderHeight: "+ target.HeaderHeight);
			#endif

			return target.HeaderHeight;
		}
	}
}