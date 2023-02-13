using System;
using Sisus.Attributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(DateTime), false, true)]
	public class DateTimeDrawer : ParentFieldDrawer<DateTime>
	{
		private const int YearMemberIndex = 0;
		private const int MonthMemberIndex = 1;
		private const int DayMemberIndex = 2;

		private const int HourMemberIndex = 0;
		private const int MinuteMemberIndex = 1;
		private const int SecondMemberIndex = 2;

		private string asString;
		private Rect stringRect;

		private int Year
		{
			get
			{
				return Value.Year;
			}
		}

		private int Month
		{
			get
			{
				return Value.Month;
			}
		}

		private int Day
		{
			get
			{
				return Value.Day;
			}
		}

		private int Hour
		{
			get
			{
				return Value.Hour;
			}
		}

		private int Minute
		{
			get
			{
				return Value.Minute;
			}
		}

		private float SecondWithFractions
		{
			get
			{
				return Second + Millisecond * 0.001f;
			}
		}

		private int Second
		{
			get
			{
				return Value.Second;
			}
		}

		private int Millisecond
		{
			get
			{
				return Value.Millisecond;
			}
		}

		/// <inheritdoc />
		public override bool DrawInSingleRow
		{
			get
			{
				return ReadOnly;
			}
		}

		/// <inheritdoc />
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

		/// <inheritdoc />
		public override float GetOptimalPrefixLabelWidth(int indentLevel)
		{
			if(HasUnappliedChanges)
			{
				return Mathf.Max(80f, DrawerUtility.GetOptimalPrefixLabelWidth(indentLevel, label, true));
			}
			return Mathf.Max(71f, DrawerUtility.GetOptimalPrefixLabelWidth(indentLevel, label));
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static DateTimeDrawer Create(DateTime value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			DateTimeDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new DateTimeDrawer();
			}
			result.Setup(value, typeof(DateTime), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		private static bool ValidateYear(object[] values)
		{
			int value = (int)values[0];
			return value >= DateTime.MinValue.Year && value <= DateTime.MaxValue.Year;
		}

		private static bool ValidateMonth(object[] values)
		{
			int value = (int)values[0];
			return value >= 1 && value <= DateTime.MaxValue.Month;
		}

		private bool ValidateDay(object[] values)
		{
			int value = (int)values[0];
			return value >= 1 && value <= DateTime.DaysInMonth(Year, Month);
		}

		private static bool ValidateHour(object[] values)
		{
			int value = (int)values[0];
			return value >= 0 && value <= DateTime.MaxValue.Hour;
		}

		private static bool ValidateMinute(object[] values)
		{
			int value = (int)values[0];
			return value >= 0 && value < 60;
		}

		private static bool ValidateSecond(object[] values)
		{
			float value = (float)values[0];
			return value >= 0f && value < 60f;
		}

		private static void ResetToOne(IDrawer target)
		{
			target.SetValue(1);
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((DateTime)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void Setup(DateTime setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
			asString = StringUtils.TimeToString(Value);
		}
		
		/// <inheritdoc />
		protected override void OnCachedValueChanged(bool applyToField, bool updateMembers)
		{
			#if DEV_MODE
			Debug.Log(ToString()+ ".OnCachedValueChanged(value="+StringUtils.ToString(Value)+", applyToField=" + applyToField+", updateMembers="+updateMembers+")");
			#endif

			base.OnCachedValueChanged(applyToField, updateMembers);
			asString = StringUtils.TimeToString(Value);
			GUI.changed = true;
		}
		
		private void SetYear(IDrawer changed, object set)
		{
			SetYear((int)set);
		}

		private void SetYear(int set)
		{
			if(Year != set && set >= DateTime.MinValue.Year && set <= DateTime.MaxValue.Year)
			{
				var setValue = new DateTime(set, Month, Day, Hour, Minute, Second, Millisecond);
				UndoHandler.RegisterUndoableAction(memberInfo, setValue, "Year", true);
				SetValue(setValue, !ReadOnly, false);
			}
		}

		private void SetMonth(IDrawer changed, object set)
		{
			SetMonth((int)set);
		}

		private void SetMonth(int set)
		{
			if(Month != set && set >= 1 && set <= DateTime.MaxValue.Month)
			{
				var setValue = new DateTime(Year, set, Day, Hour, Minute, Second, Millisecond);
				UndoHandler.RegisterUndoableAction(memberInfo, setValue, UndoHandler.GetSetValueMenuText("Month"), true);
				SetValue(setValue, !ReadOnly, false);
			}
		}
		
		private void SetDay(IDrawer changed, object set)
		{
			SetDay((int)set);
		}

		private void SetDay(int set)
		{
			if(Day != set && set >= 1 && set <= DateTime.DaysInMonth(Year, Month))
			{
				var setValue = new DateTime(Year, Month, set, Hour, Minute, Second, Millisecond);
				UndoHandler.RegisterUndoableAction(memberInfo, setValue, UndoHandler.GetSetValueMenuText("Day"), true);
				SetValue(setValue, !ReadOnly, false);
			}
		}
		
		private void SetHour(IDrawer changed, object set)
		{
			SetHour((int)set);
		}

		private void SetHour(int set)
		{
			if(Hour != set && set >= 0 && set <= DateTime.MaxValue.Hour)
			{
				var setValue = new DateTime(Year, Month, Day, set, Minute, Second, Millisecond);
				UndoHandler.RegisterUndoableAction(memberInfo, setValue, UndoHandler.GetSetValueMenuText("Hour"), true);
				SetValue(setValue, !ReadOnly, false);
			}
		}
		
		private void SetMinute(IDrawer changed, object set)
		{
			SetMinute((int)set);
		}

		private void SetMinute(int set)
		{
			if(Minute != set && set >= 0 && set < 60)
			{
				var setValue = new DateTime(Year, Month, Day, Hour, set, Second, Millisecond);
				UndoHandler.RegisterUndoableAction(memberInfo, setValue, UndoHandler.GetSetValueMenuText("Minute"), true);
				SetValue(setValue, !ReadOnly, false);
			}
		}
		
		private void SetSeconds(IDrawer changed, object set)
		{
			SetSeconds((float)set);
		}

		private void SetSeconds(float set)
		{
			if(SecondWithFractions != set && set >= 0 && set <= 60)
			{
				int s = Mathf.FloorToInt(set);
				int ms = Mathf.RoundToInt((set - s) * 1000f);

				var setValue = new DateTime(Year, Month, Day, Hour, Minute, s, ms);
				UndoHandler.RegisterUndoableAction(memberInfo, setValue, UndoHandler.GetSetValueMenuText("Second"), true);
				SetValue(setValue, !ReadOnly, false);
			}
		}
		
		/// <inheritdoc />
		protected override void DoGenerateMemberBuildList() { }
		
		/// <inheritdoc />
		protected override void DoBuildMembers()
		{
			if(ReadOnly)
			{
				DrawerArrayPool.Resize(ref members, 0);
			}
			else
			{
				DrawerArrayPool.Resize(ref members, 2);

				var value = Value;
				asString = StringUtils.TimeToString(value);
				int year = value.Year;
				int month = value.Month;
				int day = value.Day;
				int hour = value.Hour;
				int minute = value.Minute;
				float second = value.Second + value.Millisecond * 0.001f;
				
				var min = DateTime.MinValue;
				var max = DateTime.MaxValue;

				var group = CustomDataSetDrawer.Create(this, GUIContentPool.Create("Date"), false);
				{
					var groupMembers = DrawerArrayPool.Create(3);
					
					var groupMember = ClampedIntDrawer.Create(year, min.Year, max.Year, null, group, GUIContentPool.Create("Y"), false);
					groupMember.OnValueChanged += SetYear;
					groupMember.OverrideValidateValue = ValidateYear;
					groupMember.overrideReset = ResetToOne;
					groupMembers[YearMemberIndex] = groupMember;

					groupMember = ClampedIntDrawer.Create(month, min.Month, max.Month, null, group, GUIContentPool.Create("M"), false);
					groupMember.OnValueChanged += SetMonth;
					groupMember.OverrideValidateValue = ValidateMonth;
					groupMember.overrideReset = ResetToOne;
					groupMembers[MonthMemberIndex] = groupMember;

					groupMember = ClampedIntDrawer.Create(day, min.Day, max.Day, null, group, GUIContentPool.Create("D"), false);
					groupMember.OnValueChanged += SetDay;
					groupMember.OverrideValidateValue = ValidateDay;
					groupMember.overrideReset = ResetToOne;
					groupMembers[DayMemberIndex] = groupMember;
					
					group.SetMembers(groupMembers, true);
					members[0] = group;
				}

				group = CustomDataSetDrawer.Create(this, GUIContentPool.Create("Time"), false);
				{
					var groupMembers = DrawerArrayPool.Create(3);

					var groupMember = ClampedIntDrawer.Create(hour, 0, 23, null, group, GUIContentPool.Create("H"), false);
					groupMember.OnValueChanged += SetHour;
					groupMember.OverrideValidateValue = ValidateHour;
					groupMembers[HourMemberIndex] = groupMember;

					groupMember = ClampedIntDrawer.Create(minute, 0, 59, null, group, GUIContentPool.Create("M"), false);
					groupMember.OnValueChanged += SetMinute;
					groupMember.OverrideValidateValue = ValidateMinute;
					groupMembers[MinuteMemberIndex] = groupMember;

					var secondsMember = ClampedFloatDrawer.Create(second, 0f, 60f - float.Epsilon, null, group, GUIContentPool.Create("S"), false);
					secondsMember.OnValueChanged += SetSeconds;
					secondsMember.OverrideValidateValue = ValidateSecond;
					groupMembers[SecondMemberIndex] = secondsMember;
					
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
		protected override DateTime GetRandomValue()
		{
			var min = DateTime.MinValue;
			var max = DateTime.MaxValue;

			int year = Random.Range(min.Year, max.Year + 1);
			int month = Random.Range(min.Month, max.Month + 1);
			int day = Random.Range(min.Day, DateTime.DaysInMonth(year, month) + 1);
			int hour = Random.Range(min.Hour, max.Hour + 1);
			int minute = Random.Range(0, 60);
			int s = Random.Range(0, 60);
			int ms = Random.Range(0, 1000);

			return new DateTime(year, month, day, hour, minute, s, ms);
		}

		/// <inheritdoc/>
		protected override bool TryToManuallyUpdateCachedValueFromMember(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			// updating value from changing member is already handled by onValueChanged callbacks
			return true;
		}
	}
}