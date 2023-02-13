using System;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Similar to DataSetDrawer except it is for displaying custom members.
	/// Won't automatically build its members, but expects SetMembers to be used for manually setting them.
	/// Also allows specifying manually whether should be drawn in as single row.
	/// </summary>
	[Serializable]
	public sealed class CustomDataSetDrawer : ParentFieldDrawer<object[]>, ICustomGroupDrawer
	{
		private bool drawInSingleRow;

		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				return typeof(object[]);
			}
		}

		/// <inheritdoc/>
		public override bool DrawInSingleRow
		{
			get
			{
				return drawInSingleRow;
			}
		}

		/// <inheritdoc/>
		protected override bool RebuildingMembersAllowed
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		protected override bool RebuildDrawersIfValueChanged
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		public override object[] Value
		{
			get
			{
				var array = base.Value;
				int count = members.Length;
				ArrayPool<object>.Resize(ref array, count);
				for(int n = count - 1; n >= 0; n--)
				{
					var member = members[n];
					array[n] = member.GetValue();
				}
				return array;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static CustomDataSetDrawer Create(IParentDrawer parent, GUIContent label, bool readOnly)
		{
			CustomDataSetDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new CustomDataSetDrawer();
			}
			result.Setup(null, typeof(object[]), null, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		public void SetupInterface(IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup(null, typeof(object[]), null, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		public sealed override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the Create method and then call SetMembers.");
		}

		/// <inheritdoc/>
		public override void LateSetup()
		{
			base.LateSetup();

			//keep inactive flag true until SetMembers has been called
			inactive = true;
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(object[] setValue, bool applyToField, bool updateMembers)
		{
			if(setValue != null)
			{
				#if DEV_MODE
				Debug.Assert(memberBuildState == MemberBuildState.MembersBuilt);
				#endif

				for(int n = Mathf.Min(setValue.Length - 1, members.Length - 1); n >= 0; n--)
				{
					var member = members[n];
					if(Converter.TryChangeType(ref setValue[n], member.Type))
					{
						member.SetValue(setValue[n], applyToField, updateMembers);
					}
				}
			}
			return base.DoSetValue(setValue, false, false);
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList() { }

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			#if DEV_MODE
			Debug.LogWarning(ToString()+".BuildMembers call ignored. SetMembers should be used instead.");
			#endif
		}

		/// <summary>
		/// Sets the member drawer of the drawer and sets inactive flag
		/// to false, marking the fact that the setup phase for the drawer has
		/// finished and they are now ready to be used.
		/// </summary>
		/// <param name="setMembers"> The members for the drawer. </param>
		/// <param name="setDrawInSingleRow"> Determine whether or not to draw all members in a single row. </param>
		/// <param name="sendVisibilityChangedEvents">
		/// True to broadcast events OnBecameInvisible and OnSelfOrParentBecameVisible events to applicable members.
		/// This should generally speaking be false during Setup and Dispose phases and otherwise true.
		/// </param>
		public void SetMembers(IDrawer[] setMembers, bool setDrawInSingleRow, bool sendVisibilityChangedEvents)
		{
			#if DEV_MODE
			Debug.Assert(!Array.Exists(setMembers, member=>member == null));
			#endif

			inactive = false;

			drawInSingleRow = setDrawInSingleRow;
			UpdatePrefixDrawer();

			SetMembers(setMembers, sendVisibilityChangedEvents);

			if(drawInSingleRow)
			{
				SetUnfolded(true, false);
			}
		}

		/// <inheritdoc/>
		public override void SetMembers(IDrawer[] setMembers, bool sendVisibilityChangedEvents = true)
		{
			inactive = false;
			base.SetMembers(setMembers, sendVisibilityChangedEvents);			
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			drawInSingleRow = false;
			base.Dispose();
		}

		/// <inheritdoc/>
		protected override void DoReset()
		{
			for(int n = members.Length - 1; n >= 0; n--)
			{
				members[n].Reset(false);
			}

			var newValue = Value;
			SetCachedValueSilent(newValue);
			OnValidate();
			if(OnValueChanged != null)
			{
				OnValueChanged(this, newValue);
			}
		}
		
		/// <inheritdoc/>
		public override object DefaultValue(bool _)
		{
			var array = base.Value;
			int count = members.Length;
			ArrayPool<object>.Resize(ref array, count);
			for(int n = count - 1; n >= 0; n--)
			{
				var member = members[n];
				array[n] = member.Type.DefaultValue();
			}
			return array;
		}
		
		/// <inheritdoc/>
		protected override bool TryToManuallyUpdateCachedValueFromMember(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			return true;
		}

		/// <inheritdoc/>
		protected override void RebuildMembers()
		{
			#if DEV_MODE
			Debug.LogWarning(ToString()+".BuildMembers call ignored. SetMembers should be used instead.");
			#endif
		}
	}
}