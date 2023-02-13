#define DEBUG_DRAW_IN_SINGLE_ROW

using JetBrains.Annotations;
using System;
//using System.Reflection;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer for generic unfoldable non-primitive class or struct with member drawer.
	/// inherited. </summary>
	[Serializable]
	public sealed class DebugModeDisplaySettingsDrawer : ParentFieldDrawer<DebugModeDisplaySettings>
	{
		public override bool DrawInSingleRow 
		{
			get
			{
				return true;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="parent"> The parent drawer of the created drawer. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static DebugModeDisplaySettingsDrawer Create<TParent>([NotNull]TParent parent) where TParent : IDebuggable, IUnityObjectDrawer
		{
			DebugModeDisplaySettingsDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new DebugModeDisplaySettingsDrawer();
			}
			result.Setup(new DebugModeDisplaySettings(), typeof(DebugModeDisplaySettings), null, parent, new GUIContent("Display Settings"), false);
			result.LateSetup();
			return result;
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="parent"> The parent drawer of the created drawer. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static DebugModeDisplaySettingsDrawer Create<TParent>([NotNull]TParent parent, DebugModeDisplaySettings settings) where TParent : IDebuggable, IUnityObjectDrawer
		{
			DebugModeDisplaySettingsDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new DebugModeDisplaySettingsDrawer();
			}
			result.Setup(settings, typeof(DebugModeDisplaySettings), null, parent, new GUIContent("Display Settings"), false);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((DebugModeDisplaySettings)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			ParentDrawerUtility.GetMemberBuildList(this, MemberHierarchy, ref memberBuildList, false);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(DrawInSingleRow || members.Length < 5, Msg(ToString(), " built ", Members.Length, " members but DrawInSingleRow was ", DrawInSingleRow, ": ", StringUtils.ToString(members)));
			#endif
		}

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			Array.Resize(ref members, 3);

			members[0] = ButtonDrawer.Create("Fields", ToggleShowFields, this, InspectorPreferences.Styles.Toolbar);
			members[1] = ButtonDrawer.Create("Properties", ToggleShowProperties, this, InspectorPreferences.Styles.Toolbar);
			members[2] = ButtonDrawer.Create("Methods", ToggleShowMethods, this, InspectorPreferences.Styles.Toolbar);
		}

		/// <inheritdoc/>
		public override bool DrawBodySingleRow(Rect position)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("ParentDrawer.DrawBodySingleRow(3)");
			#endif

			var memberPos = position;
			memberPos.width /= 3f;

			ParentDrawerUtility.HandleTooltipBeforeControl(Label, memberPos);
			
			bool dirty = false;
			
			IDrawer[] members;
			bool draw1, draw2, draw3;
			ParentDrawerUtility.UpdateMembersToDraw(this, out members, out draw1, out draw2, out draw3);
			
			var guiColorWas = GUI.color;
			GUI.color = Value.ShowFields ? guiColorWas : new Color(1f, 1f, 1f, 0.5f);

			if(draw1 && members[0].Draw(memberPos))
			{
				dirty = true;
			}
			
			memberPos.x += memberPos.width;

			GUI.color = Value.ShowProperties ? guiColorWas : new Color(1f, 1f, 1f, 0.5f);

			if(draw2 && members[1].Draw(memberPos))
			{
				dirty = true;
			}

			memberPos.x += memberPos.width;

			GUI.color = Value.ShowMethods ? guiColorWas : new Color(1f, 1f, 1f, 0.5f);

			if(draw3 && members[2].Draw(memberPos))
			{
				dirty = true;
			}

			GUI.color = guiColorWas;

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
			return dirty;
		}

		private void ToggleShowFields()
		{
			var setValue = Value;
			setValue.ShowFields = !setValue.ShowFields;
			SetValue(setValue, false, false);
			
			var unityObjectDrawer = (IDebuggable)parent;

			#if DEV_MODE
			Debug.Log("Calling ApplyDebugModeSettings");
			#endif

			unityObjectDrawer.ApplyDebugModeSettings(setValue);

			ExitGUIUtility.ExitGUI();
		}

		private void ToggleShowProperties()
		{
			var setValue = Value;
			setValue.ShowProperties = !setValue.ShowProperties;
			SetValue(setValue, false, false);
			
			var unityObjectDrawer = (IDebuggable)parent;

			#if DEV_MODE
			Debug.Log("Calling ApplyDebugModeSettings");
			#endif

			unityObjectDrawer.ApplyDebugModeSettings(setValue);

			ExitGUIUtility.ExitGUI();
		}

		private void ToggleShowMethods()
		{
			var setValue = Value;
			setValue.ShowMethods = !setValue.ShowMethods;
			SetValue(setValue, false, false);
			
			var unityObjectDrawer = (IDebuggable)parent;

			#if DEV_MODE
			Debug.Log("Calling ApplyDebugModeSettings");
			#endif

			unityObjectDrawer.ApplyDebugModeSettings(setValue);

			ExitGUIUtility.ExitGUI();
		}
	}
}