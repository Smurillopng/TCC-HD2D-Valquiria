#define SAFE_MODE

//#define DEBUG_SET_INSPECTOR
//#define DEBUG_SET_INSPECTOR_PART
//#define DEBUG_SET_SELECTABLE
//#define DEBUG_CLEAR

using JetBrains.Annotations;
using UnityEngine;
using static Sisus.PI.NullExtensions;

namespace Sisus
{
	public delegate void InspectorChanged(IInspector from, IInspector to);

	/// <summary>
	/// Container for information about which inspector, selectable control
	/// and right clickable control are currently under the cursor.
	/// </summary>
	public class Mouseovered
	{
		public InspectorChanged onInspectorChanged;

		private IDrawer selectable;
		private IDrawer rightClickable;
		private IInspector inspector;
		private InspectorPart inspectorPart = InspectorPart.None;
		
		/// <summary>
		/// Gets the currently mouseovered selectable control
		/// </summary>
		/// <value>
		/// currently mouseovered selectable control
		/// </value>
		public IDrawer Selectable
		{
			get
			{
				#if DEV_MODE && PI_ASSERTATIONS
				if(selectable != null)
				{
					Debug.Assert(selectable.Clickable, "Mouseovered "+selectable+" - Clickable was "+StringUtils.False);
					Debug.Assert(selectable.ShouldShowInInspector, "Mouseovered "+selectable+" - ShowInInspector was "+StringUtils.False);
				}
				#endif
				return selectable;
			}

			private set
			{
				if(selectable != value)
				{
					#if DEV_MODE && DEBUG_SET_SELECTABLE
					Debug.Log(StringUtils.ToColorizedString("Mouseovered.Selectable = ", value));
					#endif

					var previous = selectable;

					selectable = value;

					var e = Event.current;
					if(previous != null)
					{
						var manager = InspectorUtility.ActiveManager;
						var activeInspectorWas = manager.ActiveInspector;
						if(inspector != null && inspector != activeInspectorWas)
						{
							manager.ActiveInspector = inspector;
						}

						previous.OnMouseoverExit(e);

						manager.ActiveInspector = activeInspectorWas;
					}

					if(value != null)
					{
						#if SAFE_MODE || DEV_MODE
						if(inspector == null)
						{
							#if DEV_MODE
							Debug.LogError("Mouseovered.inspector was null");
							#endif
							value.OnMouseoverEnter(e, false);
							return;
						}
						if(inspector.Manager == null)
						{
							#if DEV_MODE
							Debug.LogError("Mouseovered.inspector.Manager was null");
							#endif
							value.OnMouseoverEnter(e, false);
							return;
						}
						#endif

						value.OnMouseoverEnter(e, inspector.Manager.MouseDownInfo.IsDrag());
					}
				}
			}
		}

		/// <summary>
		/// Gets the currently mouseovered right clickable control
		/// </summary>
		/// <value>
		/// currently mouseovered right clickable control
		/// </value>
		public IDrawer RightClickable
		{
			get
			{
				return rightClickable;
			}

			private set
			{
				rightClickable = value;
			}
		}

		/// <summary>
		/// Gets the inspector which is currently being mouseovered, or null if none.
		/// </summary>
		/// <value>
		/// inspector which is currently being mouseovered
		/// </value>
		public IInspector Inspector
		{
			get
			{
				return inspector;
			}

			private set
			{
				#if DEV_MODE && DEBUG_SET_INSPECTOR
				if(inspector != value) { Debug.Log(StringUtils.ToColorizedString("Mouseovered.Inspector = ", value, " (was: ", inspector, ")")); }
				#endif

				inspector = value;
			}
		}

		/// <summary>
		/// Gets value indicating which the part of the mouseovered inspector the cursor is currently on.
		/// </summary>
		/// <value>
		/// Part of inspector that is currently being mouseovered, or None if cursor is not over an inspector.
		/// </value>
		public InspectorPart InspectorPart
		{
			get
			{
				return inspectorPart;
			}

			private set
			{
				#if DEV_MODE && DEBUG_SET_INSPECTOR_PART
				if(inspectorPart != value){ Debug.Log(StringUtils.ToColorizedString("Mouseovered.InspectorPart = ", value, " (was: ", inspectorPart, ")")); }
				#endif

				inspectorPart = value;
			}
		}
		
		/// <summary>
		/// Sets mouseovered Drawer to given value.
		/// </summary>
		/// <param name="setInspector"> The inspector to set as the mouseovered inspector. Can be null as long as setControl is also null. </param>
		/// <param name="setControl"> The seleactable Drawer whose click-to-select area is now being mouseovered. Can be null. </param>
		/// <returns> True if mouseovered control changed. </returns>
		public bool SetSelectable([CanBeNull]IInspector setInspector, [CanBeNull]IDrawer setControl)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(setInspector == null)
			{
				Debug.Assert(setControl == null);
			}
			if(setControl != null)
			{
				Debug.Assert(setInspector != null);
			}
			#endif

			bool changed;

			if(setControl != selectable)
			{
				changed = true;
				
				var setInspectorPart = setControl != null ? InspectorPart.Viewport : setInspector == null ? InspectorPart.None : inspectorPart;

				#if DEV_MODE && PI_ASSERTATIONS
				if(setInspectorPart == InspectorPart.None) { if(setInspector != null) { Debug.LogError("setInspectorPart was "+StringUtils.ToColorizedString(InspectorPart)+" but setInspector was "+setInspector+" with setControl="+StringUtils.ToString(setControl)); } }
				else if(setInspector == null) { Debug.LogError("setInspectorPart was "+StringUtils.ToColorizedString(InspectorPart)+" but setInspector was "+StringUtils.Null+" with setControl="+StringUtils.ToString(setControl)); }
				#endif

				SetInspectorAndPart(setInspector, setInspectorPart, false);

				Selectable = setControl;

				if(setInspector != null && setInspector.InspectorDrawer != Null)
				{
					// Make sure changes to mouseover effects take effect instantly
					setInspector.InspectorDrawer.RefreshView();
				}
			}
			else if(setInspector != inspector)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(setControl == null);
				#endif
				changed = true;
				SetInspector(setInspector, true);
			}
			else
			{
				changed = false;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Inspector == setInspector);
			Debug.Assert(Selectable == setControl);
			AssertStateIsValid();
			#endif

			return changed;
		}
		
		/// <summary>
		/// Sets mouseovered right-clickable Drawer to given value.
		/// </summary>
		/// <param name="setInspector"> The inspector to set as the mouseovered inspector. Can be null as long as setControl is also null. </param>
		/// <param name="setControl"> The Drawer whose right-clicka area is now mouseovered. Can be null. </param>
		/// <returns> True if mouseovered right-clickable drawer changed. </returns>
		public bool SetRightClickable([CanBeNull]IInspector setInspector, [CanBeNull]IDrawer setControl)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(setControl != null)
			{
				Debug.Assert(Inspector != null);
			}
			if(setInspector == null)
			{
				Debug.Assert(setControl == null);
			}
			#endif

			bool changed;

			if(setControl != rightClickable)
			{
				changed = true;

				SetInspector(setInspector, setInspector == null);
				RightClickable = setControl;
			}
			else if(setInspector != inspector)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(setControl == null);
				#endif
				changed = true;
				SetInspector(setInspector, true);
			}
			else
			{
				changed = false;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Inspector == setInspector);
			Debug.Assert(RightClickable == setControl);
			AssertStateIsValid();
			#endif

			return changed;
		}

		/// <summary>
		/// Sets mouseovered inspector part to given value. Optionally clears controls.
		/// </summary>
		/// <param name="setInspector"> The inspector to set as the mouseovered inspector. Can be null. </param>
		/// <param name="setInspectorPart"> The inspector part to set as the mouseovered part. </param>
		/// <param name="clearControls">
		/// True to also clear mouseovered selectable and right-clickable drawer.
		/// If setInspector is null or InspectorPart is not Viewport, drawer will get cleared regardless of the value of this flag.
		/// </param>
		/// <returns> True if mouseovered inspector or part changed. </returns>
		public bool SetInspectorAndPart([CanBeNull]IInspector setInspector, InspectorPart setInspectorPart, bool clearControls)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			var inspectorWasForDebugging = Inspector;
			var partWasForDebugging = InspectorPart;
			if(setInspector == null && !clearControls)
			{
				Debug.LogWarning("Mouseovered.SetInspector setInspector was "+StringUtils.Null+" but clearControls was "+StringUtils.False);
			}
			#endif

			bool changed;

			if(InspectorPart != setInspectorPart)
			{
				changed = true;

				#if DEV_MODE && (DEBUG_SET_INSPECTOR || DEBUG_SET_INSPECTOR_PART)
				Debug.Log(StringUtils.ToColorizedString("Mouseovered.SetInspectorAndPart(", setInspector, ", ", setInspectorPart, ")"));
				#endif
				
				var inspectorWas = Inspector;

				if(setInspectorPart == InspectorPart.None)
				{
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(clearControls);
					Debug.Assert(setInspector == null, "Mouseovered.SetInspectorAndPart called with inspectorPart "+StringUtils.Red("None") +" but setInspector "+setInspector);
					#endif
					Clear();

					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(Inspector == setInspector);
					#endif
				}
				else if(setInspectorPart != InspectorPart.Viewport || clearControls)
				{
					InspectorPart = setInspectorPart;
					SetInspector(setInspector, true);
					
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(Inspector == setInspector);
					#endif
				}
				else
				{
					InspectorPart = setInspectorPart;
					SetInspector(setInspector, false);

					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(Inspector == setInspector);
					#endif
				}
			}
			else if(setInspector != Inspector)
			{
				var inspectorWas = Inspector;

				changed = true;

				#if DEV_MODE && (DEBUG_SET_INSPECTOR || DEBUG_SET_INSPECTOR_PART)
				Debug.Log(StringUtils.ToColorizedString("Mouseovered.SetInspectorAndPart(", setInspector, ", ", setInspectorPart, ")"));
				#endif

				Inspector = setInspector;
				if(setInspector == null)
				{
					SetInspector(null, true);
				}
				else
				{
					SetInspector(setInspector, clearControls);
				}

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(inspectorWas != setInspector);
				#endif
			}
			else
			{
				if(clearControls)
				{
					#if DEV_MODE
					if(setInspector != null && setInspectorPart == InspectorPart.Viewport)
					{
						Debug.LogWarning("Mouseovered.SetInspectorAndPart was called with clearControls "+StringUtils.True+" even though mouseovered inspector "+setInspector+" or part "+setInspectorPart+" did not change. Was this intentional?");
					}
					#endif

					changed = ClearControls();
				}
				else
				{
					changed = false;
				}
			}

			#if DEV_MODE && PI_ASSERTATIONS
			if(Inspector != setInspector) { Debug.LogError("Mouseovered.Inspector value was "+(Inspector == null ? "null" : Inspector.ToString())+" after SetInspectorAndPart("+(setInspector == null ? "null" : setInspector.ToString())+", "+StringUtils.ToColorizedString(InspectorPart)+") finished. Inspector was "+(inspectorWasForDebugging == null ? "null" : inspectorWasForDebugging.ToString())+" and part "+partWasForDebugging+" when method was called."); }
			AssertStateIsValid();
			#endif

			return changed;
		}
		
		/// <summary>
		/// Sets mouseovered inspector part to given value. Optionally clears controls.
		/// </summary>
		/// <param name="setInspector"> The inspector to set as the mouseovered inspector. Can be null. </param>
		/// <param name="clearControls">
		/// True to also clear mouseovered selectable and right-clickable drawer.
		/// If setInspector is null, drawer will get cleared regardless of the value of this flag.
		/// </param>
		/// <returns> True if mouseovered inspector changed. </returns>
		public bool SetInspector([CanBeNull]IInspector setInspector, bool clearControls)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(setInspector == null && !clearControls)
			{
				Debug.LogWarning("Mouseovered.SetInspector setInspector was "+StringUtils.Null+" but clearControls was "+StringUtils.False);
			}
			#endif

			bool changed;
			if(setInspector != inspector)
			{
				changed = true;

				#if DEV_MODE && DEBUG_SET_INSPECTOR
				Debug.Log(StringUtils.ToColorizedString("Mouseovered.SetInspector(", setInspector, ")"));
				#endif

				var inspectorWas = Inspector;

				Inspector = setInspector;
				if(setInspector == null)
				{
					InspectorPart = InspectorPart.None;
					ClearControls();
				}
				else if(clearControls)
				{
					ClearControls();
				}

				if(onInspectorChanged != null)
				{
					onInspectorChanged(inspectorWas, setInspector);
				}
			}
			else if(clearControls)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				if(inspectorPart == InspectorPart.Viewport)
				{
					Debug.LogWarning("Mouseovered.SetInspector was called with clearControls "+StringUtils.True+" but inspector did not change and inspectorPart "+StringUtils.ToColorizedString(inspectorPart));
				}
				#endif

				var manager = InspectorUtility.ActiveManager;
				var activeInspectorWas = manager.ActiveInspector;
				if(inspector != null && inspector != activeInspectorWas)
				{
					manager.ActiveInspector = inspector;
				}

				changed = ClearControls();

				manager.ActiveInspector = activeInspectorWas;
			}
			else
			{
				changed = false;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Inspector == setInspector);
			AssertStateIsValid();
			#endif

			return changed;
		}

		/// <summary>
		/// Sets mouseovered inspector part to given value.
		/// </summary>
		/// <param name="setInspectorPart"> The inspector part to set as the mouseovered part. </param>
		/// <returns> True if mouseovered inspector part changed. </returns>
		public bool SetInspectorPart(InspectorPart setInspectorPart)
		{
			bool changed;
			if(InspectorPart != setInspectorPart)
			{
				changed = true;

				#if DEV_MODE && DEBUG_SET_INSPECTOR_PART
				Debug.Log(StringUtils.ToColorizedString("Mouseovered.SetInspectorPart(", setInspectorPart, ")"));
				#endif

				InspectorPart = setInspectorPart;

				if(inspectorPart == InspectorPart.None)
				{
					Clear();
				}
				else
				{
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(Inspector != null);
					#endif

					if(inspectorPart != InspectorPart.Viewport)
					{
						ClearControls();
					}
				}
			}
			else
			{
				changed = false;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(InspectorPart == setInspectorPart);
			if(setInspectorPart == InspectorPart.None)
			{
				Debug.Assert(Inspector == null);
			}
			else
			{
				Debug.Assert(Inspector != null);
			}
			AssertStateIsValid();
			#endif

			return changed;
		}

		private bool ClearControls()
		{
			bool changed;
			if(Selectable != null)
			{
				changed = true;
				Selectable = null;
			}
			else
			{
				changed = false;
			}

			if(RightClickable != null)
			{
				changed = true;
				RightClickable = null;
			}

			if(changed && inspector != null && inspector.InspectorDrawer != null)
			{
				// Make sure changes to mouseover effects take effect instantly
				inspector.InspectorDrawer.RefreshView();
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Selectable == null);
			Debug.Assert(RightClickable == null);
			AssertStateIsValid();
			#endif

			return changed;
		}

		/// <summary>
		/// Clears all mouseovered items.
		/// </summary>
		/// <returns> True if anything was mouseovered. </returns>
		public bool Clear()
		{
			#if DEV_MODE && DEBUG_CLEAR
			Debug.Log(StringUtils.ToColorizedString("Mouseovered.Clear called with inspector=", inspector, ", part=", inspectorPart, ", Selectable=", Selectable));
			#endif

			bool changed;
			if(inspector != null)
			{
				changed = true;
				SetInspector(null, true);
			}
			else
			{
				changed = false;
			}
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Inspector == null);
			Debug.Assert(InspectorPart == InspectorPart.None, InspectorPart.ToString());
			Debug.Assert(Selectable == null);
			Debug.Assert(RightClickable == null);
			AssertStateIsValid();
			#endif

			return changed;
		}

		public void OnDisposing([NotNull]IInspector disposing)
		{
			if(inspector == disposing)
			{
				SetInspectorAndPart(null, InspectorPart.None, true);
			}
		}

		#if DEV_MODE && PI_ASSERTATIONS
		private void AssertStateIsValid()
		{
			if(Inspector == null)
			{
				if(Selectable != null)
				{
					Debug.LogError("Mouseovered Inspector was "+StringUtils.Null+" but Selectable was "+Selectable);
				}
				if(RightClickable != null)
				{
					Debug.LogError("Mouseovered Inspector was "+StringUtils.Null+" but RightClickable was "+RightClickable);
				}
				if(InspectorPart != InspectorPart.None)
				{
					Debug.LogError("Mouseovered Inspector was "+StringUtils.Null+" but InspectorPart was "+InspectorPart);
				}
			}
			else if(InspectorPart == InspectorPart.None)
			{
				Debug.LogError("Mouseovered inspector was "+Inspector+" but InspectorPart was "+StringUtils.ToColorizedString(InspectorPart));
			}
			
			if(Selectable != null && InspectorPart != InspectorPart.Viewport)
			{
				Debug.LogError("Mouseovered Selectable was "+StringUtils.ToString(Selectable)+" but InspectorPart was "+StringUtils.ToColorizedString(InspectorPart));
			}

			if(RightClickable != null && InspectorPart != InspectorPart.Viewport)
			{
				Debug.LogError("Mouseovered RightClickable "+StringUtils.ToString(RightClickable)+" but InspectorPart was "+StringUtils.ToColorizedString(InspectorPart));
			}
		}
		#endif
	}
}