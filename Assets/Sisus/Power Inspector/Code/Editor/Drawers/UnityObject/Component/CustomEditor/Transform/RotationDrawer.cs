#define SAFE_MODE

#define DEBUG_UPDATE_CACHED_VALUES
#define DEBUG_ON_MEMBER_VALUE_CHANGED

using System;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class RotationDrawer : TransformMemberBaseDrawer
	{
		private static readonly int[] DraggingMembers = {1};

		/// <summary>
		/// Using Quaternion instead of Vector3 to detected changes to field value in UpdateCached values
		/// fixes issue where e.g. setting x of rotation (0,0,0) to value of 123
		/// would result in the fields changing to show (57,180,0) immediately
		/// now this will only happen when you deselect the Transform in question.
		/// </summary>
		private Quaternion? rotationCached;

		/// <summary>
		/// Toggle that determines whether were using local space when rotation was last cached.
		/// </summary>
		private bool rotationCachedForLocalSpace;
		
		/// <inheritdoc/>
		protected override int[] DraggingTargetsMembers
		{
			get
			{
				return DraggingMembers;
			}
		}

		/// <inheritdoc/>
		public override bool SnappingEnabled
		{
			get
			{
				return UserSettings.Snapping.Enabled && UserSettings.Snapping.EnabledForRotate;
			}

			set
			{
				UserSettings.Snapping.EnabledForRotate = value;
			}
		}

		/// <inheritdoc/>
		public override float GetSnapStep(int memberIndex = -1)
		{
			return UserSettings.Snapping.Rotation;
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static RotationDrawer Create(Vector3 value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			RotationDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new RotationDrawer();
			}
			result.Setup(value, typeof(Vector3), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <summary>
		/// Prevents a default instance of the drawer from being created.
		/// The Create method should be used instead of this constructor.
		/// </summary>
		private RotationDrawer() { }

		/// <inheritdoc/>
		public override void LateSetup()
		{
			base.LateSetup();

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(rotationCached == null);
			#endif

			UpdateCachedRotation();
		}

		/// <inheritdoc/>
		protected override string XPropertyPath() { return "m_LocalRotation.x"; }
		/// <inheritdoc/>
		protected override string YPropertyPath() { return "m_LocalRotation.y"; }
		/// <inheritdoc/>
		protected override string ZPropertyPath() { return "m_LocalRotation.z"; }

		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			if(!ReadOnly)
			{
				if(!IsPrefab)
				{
					menu.AddSeparatorIfNotRedundant();
					menu.Add("Align With Ground", AlignWithGround);
				}

				menu.AddSeparatorIfNotRedundant();
				menu.Add("Set To.../Zero", () => Value = Vector3.zero, Value.IsZero());
				menu.Add("Rotate/-90°\tLeft", RotateLeft90);
				menu.Add("Rotate/-45°\tLeft", RotateLeft45);
				menu.Add("Rotate/45°\tRight", Rotate45);
				menu.Add("Rotate/90°\tRight", Rotate90);
				menu.Add("Rotate/180°\tTurn", Rotate180);
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		private void RotateLeft45()
		{
			Rotate(-45);
		}

		private void RotateLeft90()
		{
			Rotate(-90);
		}

		private void Rotate45()
		{
			Rotate(45);
		}

		private void Rotate90()
		{
			Rotate(90);
		}

		private void Rotate180()
		{
			Rotate(180);
		}

		private void Rotate(float amount)
		{
			var setValue = Value;
			setValue.y += amount;
			Value = setValue;
		}

		/// <inheritdoc/>
		public override void Snap()
		{
			float snap = GetSnapStep();
			if(snap > 0f)
			{
				bool changed = false;
				var values = GetValues();
				for(int n = values.Length - 1; n >= 0; n--)
				{
					var was = (Vector3)values[n];
					var set = was;
					set.x = Mathf.Round(set.x / snap) * snap;
					set.y = Mathf.Round(set.y / snap) * snap;
					set.z = Mathf.Round(set.z / snap) * snap;
					if(!ValuesAreEqual(set, was))
					{
						values[n] = set;
						changed = true;
					}
				}

				if(changed)
				{
					SetValues(values);
				}
			}
		}
		
		public void AlignWithGround()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(typeof(ITransformDrawer).IsAssignableFrom(typeof(TransformDrawer)));
			#endif

			bool changed = false;
			var values = GetValues();
			var transforms = Transforms;
			var raycasts = GroundUtility.RaycastGround(transforms);
			
			for(int n = transforms.Length - 1; n >= 0; n--)
			{
				var transform = transforms[n];
				if(transform == null)
				{
					continue;
				}

				var hit = raycasts[n];
				if(!hit.HasValue)
				{
					continue;
				}

				var rotationWas = (Vector3)values[n];
				var targetRotation = new Vector3(hit.Value.normal.x, rotationWas.y, hit.Value.normal.z);
				if(ValuesAreEqual(rotationWas, targetRotation))
				{
					continue;
				}

				values[n] = targetRotation;
				changed = true;
			}

			if(changed)
			{
				SetValues(values);
			}
		}

		/// <inheritdoc/>
		public override void UpdateCachedValuesFromFieldsRecursively()
		{
			if(inactive)
			{
				return;
			}

			var firstTransform = Transform;

			#if SAFE_MODE
			if(firstTransform == null)
			{
				#if DEV_MODE
				Debug.LogError("RotationDrawer.UpdateCachedValuesFromFieldsRecursively called by target Transform was null.");
				#endif
				return;
			}
			#endif

			var localSpace = Inspector.State.usingLocalSpace;

			// Make sure that values remain stable when dragging prefix of this or one of its members.
			// So dragging event takes precedence over any possible external changes to rotation.
			var prefixDragged = Inspector.Manager.MouseDownInfo.DraggingPrefixOfDrawer;
			if(prefixDragged != null && (prefixDragged == this || prefixDragged.Parent == this))
			{
				// Still keep cached rotation up to date
				UpdateCachedRotation();
			}
			else if(!rotationCached.HasValue)
			{
				if(!MixedContent)
				{
					UpdateCachedRotation();
					SetValue(localSpace ? firstTransform.localEulerAngles : firstTransform.eulerAngles, false, true);
					GUI.changed = true;
				}
				else
				{
					UpdateCachedRotation();
				}
			}
			else if(rotationCachedForLocalSpace != localSpace)
			{
				UpdateCachedRotation();
				SetValue(localSpace ? firstTransform.localEulerAngles : firstTransform.eulerAngles, false, true);
				GUI.changed = true;
			}
			else
			{
				var currentRotation = localSpace ? firstTransform.localRotation : firstTransform.rotation;
				if(!ValuesAreEqual(currentRotation, rotationCached.Value))
				{
					#if DEV_MODE && DEBUG_UPDATE_CACHED_VALUES
					Debug.Log("Detected rotation value change from "+ rotationCached.Value + " to "+ currentRotation);
					#endif

					rotationCached = currentRotation;
					SetValue(Inspector.State.usingLocalSpace ? firstTransform.localEulerAngles : firstTransform.eulerAngles, false, true);
					GUI.changed = true;
				}
			}

			#if DEV_MODE && PI_ASSERTATIONS //keeps spamming with false failures?
			//if(Inspector.State.usingLocalSpace) { Debug.Assert(Quaternion.Angle(rotationCached, Quaternion.Euler(Value)) < 0.0001f, Msg("rotationCached (", rotationCached, ") != Quaternion.Euler(Value) (", Quaternion.Euler(Value), "), with Angle=", Quaternion.Angle(rotationCached, Quaternion.Euler(Value)), ", Value=", Value, ", inactive=", inactive)); }
			#endif
		}

		/// <inheritdoc/>
		public override void OnMemberValueChanged(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			// temporary warning about this method being called when mixed content is true, because that use case is probably not yet perfectly handled everywhere
			AssertWarn(!MixedContent, this, ".OnMemberValueChanged was called but subject had mixed content.");
			AssertWarn(memberLinkedMemberInfo == null|| !memberLinkedMemberInfo.MixedContent, this, ".OnMemberValueChanged was called but memberLinkedMemberInfo had mixed content.");
			#endif

			#if DEV_MODE && DEBUG_ON_MEMBER_VALUE_CHANGED
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".OnMemberValueChanged(index=", StringUtils.ToString(memberIndex), ", value=", memberValue, ") with ValueWasJustSet=", ValueWasJustSet, ", inactive = ", inactive, ", MixedContent=", MixedContent, ", memberInfo.CanRead=" + (memberInfo == null ? StringUtils.Null : StringUtils.ToColorizedString(memberInfo.CanRead))+ ", memberLinkedMemberInfo.CanRead = " + (memberLinkedMemberInfo == null ? StringUtils.Null : StringUtils.ToColorizedString(memberLinkedMemberInfo.CanRead))));
			#endif
			
			if(ValueWasJustSet)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+".OnMemberValueChanged("+memberIndex+") ignored because valueWasJustSet is "+StringUtils.True+". This message can probably be removed.");
				#endif
				return;
			}

			if(inactive)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+".OnMemberValueChanged("+memberIndex+") ignored because inactive is "+StringUtils.True+". This message can probably be removed.");
				#endif
				return;
			}

			#if DEV_MODE
			if(ReadOnly)
			{
				Debug.LogWarning(ToString()+".OnMemberValueChanged was called for parent which was ReadOnly. This should not be possible - unless external scripts caused the value change.");
			}
			#endif
			
			// Always update manually, to avoid form changing e.g. from (0, 0, 0) to (180, 180, 180).
			if(!TryToManuallyUpdateCachedValueFromMember(memberIndex, memberValue, memberLinkedMemberInfo))
			{
				#if DEV_MODE
				Debug.LogError(ToString()+ ".OnMemberValueChanged - Failed to update cached value via fieldInfo or via TryToManuallyUpdateCachedValueFromMember");
				#endif
			}

			for(int n = members.Length - 1; n >= 0; n--)
			{
				if(n != memberIndex)
				{
					members[n].OnSiblingValueChanged(memberIndex, memberValue, memberLinkedMemberInfo);
				}
			}

			if(parent != null)
			{
				parent.OnMemberValueChanged(Array.IndexOf(parent.Members, this), Value, memberLinkedMemberInfo);
			}

			UpdateDataValidity(true);
			HasUnappliedChanges = GetHasUnappliedChangesUpdated();
		}

		protected bool ValuesAreEqual(Quaternion a, Quaternion b)
		{
			return Quaternion.Angle(a, b) <= Mathf.Epsilon;
		}


		private void UpdateCachedRotation()
		{
			if(MixedContent)
			{
				rotationCached = null;
			}
			else
			{
				var firstTransform = Transform;
				if(firstTransform == null)
				{
					rotationCached = null;
				}
				else
				{
					rotationCachedForLocalSpace = Inspector.State.usingLocalSpace;
					rotationCached = rotationCachedForLocalSpace ? firstTransform.localRotation : firstTransform.rotation;
				}
			}
		}

		/// <inheritdoc/>
		protected override void OnCachedValueChanged(bool applyToField, bool updateMembers)
		{
			UpdateCachedRotation();
			base.OnCachedValueChanged(applyToField, updateMembers);
		}

		/// <inheritdoc/>
		protected override void UpdateTooltips()
		{
			var firstTransform = Transform;
			if(firstTransform != null)
			{
				label.tooltip = Inspector.State.usingLocalSpace ?
					StringUtils.Concat("World Rotation: ", StringUtils.ToString(firstTransform.eulerAngles)) :
					StringUtils.Concat("Local Rotation: ", StringUtils.ToString(firstTransform.localEulerAngles));
			}
		}

		/// <inheritdoc/>
		protected override Vector3 GetRandomValue()
		{
			return UnityEngine.Random.rotation.eulerAngles;
		}

		/// <inheritdoc/>
		protected override void UpdateDraggableMembers() { }

		/// <inheritdoc/>
		public override void Dispose()
		{
			rotationCached = null;
			base.Dispose();
		}
	}
}