using System;
using Sisus.Attributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(TimeSpan), false, true)]
	public class TimeSpanDrawer : ParentFieldDrawer<TimeSpan>
	{
		private string asString;
		private Rect stringRect;

		private int Days
		{
			get
			{
				return Value.Days;
			}
		}

		private int Hours
		{
			get
			{
				return Value.Hours;
			}
		}

		private int Minutes
		{
			get
			{
				return Value.Minutes;
			}
		}

		private float SecondsWithFractions
		{
			get
			{
				return Seconds + Milliseconds * 0.001f;
			}
		}

		private int Seconds
		{
			get
			{
				return Value.Seconds;
			}
		}

		private int Milliseconds
		{
			get
			{
				return Value.Milliseconds;
			}
		}

		private int MinDays
		{
			get
			{
				return TimeSpan.MinValue.Days;
			}
		}

		private int MaxDays
		{
			get
			{
				return TimeSpan.MaxValue.Days;
			}
		}

		private int MinHours
		{
			get
			{
				if(Days <= TimeSpan.MinValue.Days)
				{
					return TimeSpan.MinValue.Hours;
				}
				return -23;
			}
		}

		private int MaxHours
		{
			get
			{
				if(Days >= TimeSpan.MaxValue.Days)
				{
					return TimeSpan.MaxValue.Hours;
				}
				return 23;
			}
		}

		private int MinMinutes
		{
			get
			{
				if(Days <= TimeSpan.MinValue.Days && Hours <= TimeSpan.MinValue.Hours)
				{
					return TimeSpan.MinValue.Minutes;
				}
				return -59;
			}
		}

		private int MaxMinutes
		{
			get
			{
				if(Days >= TimeSpan.MaxValue.Days && Hours >= TimeSpan.MaxValue.Hours)
				{
					return TimeSpan.MaxValue.Minutes;
				}
				return 59;
			}
		}

		private float MinSeconds
		{
			get
			{
				if(Days <= TimeSpan.MinValue.Days && Hours <= TimeSpan.MinValue.Hours && Minutes <= TimeSpan.MinValue.Minutes)
				{
					return TimeSpan.MinValue.Seconds + TimeSpan.MinValue.Milliseconds * 0.001f;
				}
				return float.Epsilon - 60f;
			}
		}

		private float MaxSeconds
		{
			get
			{
				if(Days >= TimeSpan.MaxValue.Days && Hours >= TimeSpan.MaxValue.Hours && Minutes >= TimeSpan.MaxValue.Minutes)
				{
					return TimeSpan.MaxValue.Seconds + TimeSpan.MaxValue.Milliseconds * 0.001f;
				}
				return 60f - float.Epsilon;
			}
		}

		private bool IsNegative
		{
			get
			{
				return Value.TotalMilliseconds < 0;
			}
		}

		/// <inheritdoc/>
		public override bool DrawInSingleRow
		{
			get
			{
				return ReadOnly;
			}
		}

		/// <inheritdoc/>
		protected override bool RebuildDrawersIfValueChanged
		{
			get
			{
				return RebuildingMembersAllowed;
			}
		}

		/// <inheritdoc/>
		protected override bool PrefixLabelClippedToColumnWidth
		{
			get
			{
				return true;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static TimeSpanDrawer Create(TimeSpan value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			TimeSpanDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new TimeSpanDrawer();
			}
			result.Setup(value, typeof(TimeSpan), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((TimeSpan)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void Setup(TimeSpan setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
			asString = StringUtils.ToString(Value);
		}

		/// <inheritdoc/>
		protected override void OnCachedValueChanged(bool applyToField, bool updateMembers)
		{
			base.OnCachedValueChanged(applyToField, updateMembers);
			
			//TO DO: Allow customizing display format in the preferences
			asString = StringUtils.ToString(Value);
		}
		
		private void SetDays(IDrawer changed, object set)
		{
			SetDays((int)set);
		}

		private void SetDays(int set)
		{
			var setValue = new TimeSpan(set, Hours, Minutes, Seconds, Milliseconds);
			UndoHandler.RegisterUndoableAction(memberInfo, setValue, "Days", true);
			SetValue(setValue, !ReadOnly, false);
		}
		
		private void SetHours(IDrawer changed, object set)
		{
			SetHours((int)set);
		}

		private void SetHours(int set)
		{
			if(set != Hours && set > -24 && set < 24)
			{
				var setValue = new TimeSpan(Days, set, Minutes, Seconds, Milliseconds);
				UndoHandler.RegisterUndoableAction(memberInfo, setValue, "Hours", true);
				SetValue(setValue, !ReadOnly, false);
			}
		}
		
		private bool ValidateHours(object[] values)
		{
			int value = (int)values[0];
			return value >= MinHours && value <= MaxHours;
		}


		private void SetMinutes(IDrawer changed, object set)
		{
			SetMinutes((int)set);
		}

		private void SetMinutes(int set)
		{
			if(set != Minutes && set >= TimeSpan.MinValue.Minutes && set <= TimeSpan.MaxValue.Minutes)
			{
				var setValue = new TimeSpan(Days, Hours, set, Seconds, Milliseconds);
				UndoHandler.RegisterUndoableAction(memberInfo, setValue, "Minutes", true);
				DoSetValue(setValue, !ReadOnly, false);
			}
		}

		private bool ValidateMinutes(object[] values)
		{
			int value = (int)values[0];
			return value >= MinMinutes && value <= MaxMinutes;
		}


		private void SetSeconds(IDrawer changed, object set)
		{
			SetSeconds((float)set);
		}

		private void SetSeconds(float set)
		{
			if(set != SecondsWithFractions)
			{
				int s = Mathf.FloorToInt(set);
				int ms = Mathf.RoundToInt((set - s) * 1000f);
				if(s >= TimeSpan.MinValue.Seconds && s <= TimeSpan.MaxValue.Seconds && ms >= TimeSpan.MinValue.Milliseconds && ms <= TimeSpan.MaxValue.Milliseconds)
				{
					var setValue = new TimeSpan(Days, Hours, Minutes, s, ms);
					UndoHandler.RegisterUndoableAction(memberInfo, setValue, "Seconds", true);
					SetValue(setValue, !ReadOnly, false);
				}
			}
		}

		private bool ValidateSeconds(object[] values)
		{
			float value = (float)values[0];
			return value >= MinSeconds && value >= MinSeconds;
		}

		/// <inheritdoc />
		protected override void DoGenerateMemberBuildList()
		{
			//we use OnValueChanged events to apply changes in member values
			//so we don't need to generte LinkedMemberInfos for the members
		}

		/// <inheritdoc />
		protected override void DoBuildMembers()
		{
			bool readOnly = memberInfo != null && !memberInfo.CanWrite;
			if(readOnly)
			{
				DrawerArrayPool.Resize(ref members, 0);
			}
			else
			{
				DrawerArrayPool.Resize(ref members, 2);
				
				var min = TimeSpan.MinValue;
				var max = TimeSpan.MaxValue;

				var group = CustomDataSetDrawer.Create(this, GUIContentPool.Create("Days / Hours"), ReadOnly);
				{
					var groupMembers = DrawerArrayPool.Create(2);

					var groupMember = ClampedIntDrawer.Create(Days, min.Days, max.Days, null, group, GUIContentPool.Create("D"), ReadOnly);
					groupMember.OnValueChanged += SetDays;
					groupMembers[0] = groupMember;

					groupMember = ClampedIntDrawer.Create(Hours, MinHours, MaxHours, null, group, GUIContentPool.Create("H"), ReadOnly);
					groupMember.OnValueChanged += SetHours;
					groupMember.OverrideValidateValue = ValidateHours;
					groupMembers[1] = groupMember;

					group.SetMembers(groupMembers, true);
					members[0] = group;
				}

				group = CustomDataSetDrawer.Create(this, GUIContentPool.Create("Minutes / Seconds"), ReadOnly);
				{
					var groupMembers = DrawerArrayPool.Create(2);
					
					var groupMember = ClampedIntDrawer.Create(Minutes, MinMinutes, MaxMinutes, null, group, GUIContentPool.Create("M"), ReadOnly);
					groupMember.OnValueChanged += SetMinutes;
					groupMember.OverrideValidateValue = ValidateMinutes;
					groupMembers[0] = groupMember;
					
					var secondsMember = ClampedFloatDrawer.Create(SecondsWithFractions, MinSeconds, MaxSeconds, null, group, GUIContentPool.Create("S"), ReadOnly);
					secondsMember.OnValueChanged += SetSeconds;
					secondsMember.OverrideValidateValue = ValidateSeconds;
					groupMembers[1] = secondsMember;

					group.SetMembers(groupMembers, true);
					members[1] = group;
				}
			}
		}

		/// <inheritdoc />
		protected override void GetDrawPositions(Rect position)
		{
			base.GetDrawPositions(position);

			Rect ignore;
			lastDrawPosition.GetLabelAndControlRects(label, out ignore, out stringRect);
		}

		/// <inheritdoc />
		public override bool DrawPrefix(Rect position)
		{
			bool result = base.DrawPrefix(position);
			GUI.Label(stringRect, asString);
			return result;
		}
		
		/// <inheritdoc />
		protected override TimeSpan GetRandomValue()
		{
			var min = TimeSpan.MinValue;
			var max = TimeSpan.MaxValue;

			bool positive = RandomUtils.Bool();
			int days, hours, minutes, s, ms;
			if(positive)
			{
				days = Random.Range(0, max.Days + 1);
				hours = Random.Range(0, 24);
				minutes = Random.Range(0, 60);
				s = Random.Range(0, 60);
				ms = Random.Range(0, 1000);

				if(days == max.Days)
				{
					hours = Random.Range(0, max.Hours + 1);
					if(hours == max.Hours)
					{
						minutes = Random.Range(0, max.Minutes + 1);
						if(minutes == max.Minutes)
						{
							s = Random.Range(0, max.Seconds + 1);
							if(s == max.Seconds)
							{
								ms = Random.Range(0, max.Milliseconds + 1);
							}
						}
					}
				}
			}
			else
			{
				days = Random.Range(min.Days, 1);
				hours = Random.Range(-23, 1);
				minutes = Random.Range(-59, 1);
				s = Random.Range(-59, 1);
				ms = Random.Range(-999, 1);

				if(days == min.Days)
				{
					hours = Random.Range(min.Hours, 1);
					if(hours == min.Hours)
					{
						minutes = Random.Range(min.Minutes, 1);
						if(minutes == min.Minutes)
						{
							s = Random.Range(min.Seconds, 1);
							if(s == min.Seconds)
							{
								ms = Random.Range(min.Milliseconds, 1);
							}
						}
					}
				}
			}
			
			return new TimeSpan(days, hours, minutes, s, ms);
		}

		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			base.BuildContextMenu(ref menu, extendedMenu);

			if(memberInfo == null || !memberInfo.MixedContent)
			{
				int copyIndex = menu.IndexOf("Copy");
				if(copyIndex != -1)
				{
					menu.Insert(copyIndex + 1, Menu.Item("Copy Total Seconds", CopyTotalSeconds));
				}
			}
		}

		private void CopyTotalSeconds()
		{
			Clipboard.Copy(Value.TotalSeconds);
			SendCopyToClipboardMessage("Copied{0} total seconds");
		}

		/// <inheritdoc/>
		protected override bool TryToManuallyUpdateCachedValueFromMember(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			// updating value from changing member is already handled by onValueChanged callbacks
			return true;
		}
	}
}