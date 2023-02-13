//#define SET_DIRTY_WHEN_NON_SERIALIZED_FIELDS_MODIFIED
#define SAFE_MODE

//#define DEBUG_UNDO

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Singleton class for handling of Undo both in the editor and at runtime.
	/// It also allows Undoing of actions that are not backed by a separate UnityEngine.Object
	/// </summary>
	public class UndoHandler : ScriptableObject
	{
		private const int MaxUndoableActions = 50;
		
		private static bool disabled;
		private static UndoHandler instance;
		private static Pool<UndoableModification> pool = new Pool<UndoableModification>(MaxUndoableActions);
		private static List<Object> registeredForUndoThisFrame = new List<Object>(5);

		/// <summary>
		/// Unity-serialized value representing count of modifications that can be undone.
		/// 
		/// When no undo commands have been given, this should match count of items in modifications.
		/// 
		/// The difference between this value and modifications.Count tells us how many custom modifications have been undone.
		/// 
		/// The difference between this value and undoIndexNonSerialized tells us whether the user just gave an undo or a redo command.
		/// 
		/// Whenever a new custom undoable modification is added to the modifications list, this is incremented by one,
		/// and the change in value is recorded by Unity's internal Undo system.
		/// 
		/// When an Undo command is given by the user, this value is decreased by one.
		/// We can use this information to determine which modification in the modifications list to undo.
		/// 
		/// When a Redo command is given by the user, this value is increased by one.
		/// We can use this information to determine which modification in the modifications list to redo.
		/// </summary>
		[SerializeField]
		private int undoIndexSerialized;

		private int undoIndexNonSerialized;

		/// <summary>
		/// List of custom undoable modifications.
		/// </summary>
		private List<UndoableModification> modifications = new List<UndoableModification>(MaxUndoableActions);
		

		public int UndoIndex
		{
			get
			{
				return undoIndexNonSerialized;
			}

			set
			{
				undoIndexSerialized = value;
				undoIndexNonSerialized = value;
			}
		}

		private bool listeningToEvents;
		private IInspectorDrawer onUpdateBroadcaster;

		public Action OnOndoOrRedoPerformed
		{
			get;
			set;
		}

		public static UndoHandler Instance()
		{
			if(instance == null)
			{
				var instances = Resources.FindObjectsOfTypeAll<UndoHandler>();
				if(instances.Length > 0)
				{
					instance = instances[0];
				}
				else
				{
					instance = CreateInstance<UndoHandler>();
					instance.name = "Undo Handler";
				}
				instance.hideFlags = HideFlags.DontSave;
			}
			return instance;
		}

		public static bool Enabled
		{
			get
			{
				return !disabled;
			}
		}
		
		private void Clear()
		{
			#if DEV_MODE && DEBUG_UNDO
			Debug.Log("UndoHandler.Clear()");
			#endif

			Dispose(modifications);
			UndoIndex = 0;
		}

		private static void Dispose(Stack<UndoableModification> disposing)
		{
			for(int n = disposing.Count - 1; n >= 0; n--)
			{
				var item = disposing.Pop();
				Dispose(ref item);
			}
			disposing.Clear();
		}

		private static void Dispose(List<UndoableModification> disposing)
		{
			for(int n = disposing.Count - 1; n >= 0; n--)
			{
				var item = disposing[n];
				Dispose(ref item);
			}
			disposing.Clear();
		}

		private static void Dispose(ref UndoableModification disposing)
		{
			disposing.Dispose();
			pool.Dispose(ref disposing);
		}

		public static string GetSetValueMenuText(string targetName)
		{
			return StringUtils.Concat("Set ", targetName, " Value");
		}

		/// <summary>
		/// Use this for adding Undo support for changes that are not serialized by Unity and Unity
		/// can't handle undoing the action
		/// </summary>
		/// <param name="memberInfo"> The LinkedMemberInfo for the member which is being modified. This cannot be null. </param>
		/// <param name="valueTo"> The value to which the member will be set. </param>
		/// <param name="menuItemText"> Message describing the undo. </param>
		/// <param name="unityObject"> The UnityEngine.Object which holds the member which is being modified. </param>
		/// <param name="registerCompleteObjectUndo"> Set to true if full object hierarchy should be serialized for the Undo. This is needed for undoing changes to array size fields to function properly. </param>
		/// <returns> True if should call EditorUtility.SetDirty after value has changed. </returns>
		public static bool RegisterUndoableAction([CanBeNull]Object unityObject, [NotNull]LinkedMemberInfo memberInfo, object valueTo, [NotNull]string menuItemText, bool registerCompleteObjectUndo)
		{
			if(disabled)
			{
				#if DEV_MODE && DEBUG_UNDO
				Debug.Log("RegisterUndoableAction with undoText=" + StringUtils.ToString(menuItemText) + " "+StringUtils.Red("ABORTING..."));
				#endif
				return false;
			}
			
			#if UNITY_EDITOR
			if(unityObject != null && memberInfo.IsUnitySerialized)
			{
				#if DEV_MODE && DEBUG_UNDO
				Debug.Log("RegisterUndoableSetValue "+StringUtils.Green("IsUnitySerialized")+" field="+ memberInfo + ", unityObject=" + StringUtils.ToString(unityObject.name) + ", valueTo=" + StringUtils.ToString(valueTo) + ", undoText=" + StringUtils.ToString(menuItemText) + ", registerCompleteObjectUndo="+ StringUtils.ToColorizedString(registerCompleteObjectUndo) +", IsUnitySerialized=" + StringUtils.ToColorizedString(true) + ", registeredForUndoThisFrame.Contains(unityObject)="+StringUtils.ToColorizedString(registeredForUndoThisFrame.Contains(unityObject)), unityObject);
				#endif

				Instance().DoRegisterUndoableActionInternal(unityObject, memberInfo.DisplayName, registerCompleteObjectUndo);
				return true;
			}
			#endif

			#if DEV_MODE && DEBUG_UNDO
			Debug.Log("RegisterUndoableSetValue " + StringUtils.Red("!IsUnitySerialized") + " field=" + memberInfo + ", unityObject=" + StringUtils.ToString(unityObject != null ? unityObject.name : "") + ", valueTo=" + StringUtils.ToString(valueTo) + ", undoText=" + StringUtils.ToString(menuItemText) + ", registerCompleteObjectUndo="+ StringUtils.ToColorizedString(registerCompleteObjectUndo) +", IsUnitySerialized=" + StringUtils.ToColorizedString(false)+ ", registeredForUndoThisFrame.Contains(unityObject)=" + (unityObject == null ? StringUtils.Null : StringUtils.ToColorizedString(registeredForUndoThisFrame.Contains(unityObject))), unityObject);
			#endif

			Instance().DoRegisterUndoableActionInternal(instance, menuItemText, registerCompleteObjectUndo);

			UndoableModification modification;
			try
			{
				modification = UndoableModification.Create(memberInfo, valueTo);
			}
			#if DEV_MODE
			catch(SerializationException e)
			{
				Debug.LogError(e);
			#else
			catch(SerializationException)
			{
			#endif
				return false;
			}

			instance.AddNewUndoableModification(modification);
			
			#if SET_DIRTY_WHEN_NON_SERIALIZED_FIELDS_MODIFIED
			return true;
			#else
			return false;
			#endif
		}

		private void AddNewUndoableModification(UndoableModification modification)
		{
			// Make sure that undoIndexNonSerialized and undoIndexSerialized are in sync.
			// They could perhaps be out of sync e.g. an assembly reload reset undoIndexNonSerialized to its default
			// value but did not reset undoIndexSerialized.
			if(undoIndexNonSerialized != undoIndexSerialized)
			{
				undoIndexSerialized = undoIndexNonSerialized;
			}

			// After a new undo command, discard all modifications that were undone by the user.
			// After this they can no longer be redone.
			int modificationsCount = modifications.Count;
			while(modificationsCount > UndoIndex)
			{
				modificationsCount--;
				modifications.RemoveAt(modificationsCount);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(modificationsCount == UndoIndex);
			Debug.Assert(modifications.Count == UndoIndex);
			Debug.Assert(undoIndexSerialized == undoIndexNonSerialized);
			Debug.Assert(undoIndexSerialized == UndoIndex);
			#endif

			if(modifications.Count >= MaxUndoableActions)
			{
				var oldestUndoable = modifications[0];
				Dispose(ref oldestUndoable);
				modifications.RemoveAt(0);
			}

			modifications.Add(modification);
			UndoIndex = modifications.Count;

			#if DEV_MODE && DEBUG_UNDO
			Debug.Log("New undoable registered: " + modification);
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(modifications.Count <= MaxUndoableActions);
			Debug.Assert(UndoIndex == modifications.Count);
			#endif
		}

		/// <summary>
		/// Use this for adding Undo support for changes that are not serialized by Unity and Unity
		/// can't handle undoing the action
		/// </summary>
		/// <param name="memberInfo"> The LinkedMemberInfo for the member which is being modified. This cannot be null. </param>
		/// <param name="valueTo"> The value to which the member will be set. </param>
		/// <param name="menuItemText"> Message describing the undo. </param>
		/// <param name="unityObject"> The UnityEngine.Object which holds the member which is being modified. </param>
		/// <param name="registerCompleteObjectUndo"> Set to true if full object hierarchy should be serialized for the Undo. This is needed for undoing changes to array size fields to function properly. </param>
		/// <returns> True if should call EditorUtility.SetDirty after value has changed. </returns>
		public static bool RegisterUndoableAction([NotNull]LinkedMemberInfo memberInfo, object valueTo, [NotNull]string menuItemText, bool registerCompleteObjectUndo)
		{
			return RegisterUndoableAction(memberInfo.UnityObjects, memberInfo, valueTo, menuItemText, registerCompleteObjectUndo);
		}

		/// <summary>
		/// Use this for adding Undo support for changes that are not serialized by Unity when Unity can't handle undoing the action
		/// </summary>
		/// <param name="targets"> The UnityEngine.Object targets which contain the members which are being modified. </param>
		/// <param name="memberInfo"> The LinkedMemberInfo for the members which are being modified. This cannot be null. </param>
		/// <param name="valueTo"> The value to which the members will be set. </param>
		/// <param name="menuItemText"> Message describing the undo. </param>
		/// <param name="registerCompleteObjectUndo"> Set to true if full object hierarchy should be serialized for the Undo. This is needed for undoing changes to array size fields to function properly. </param>
		/// <returns> True if should call EditorUtility.SetDirty after value has changed. </returns>
		public static bool RegisterUndoableAction(Object[] targets, [NotNull]LinkedMemberInfo memberInfo, object valueTo, [NotNull]string menuItemText, bool registerCompleteObjectUndo)
		{
			return RegisterUndoableAction(targets, memberInfo, ArrayPool<object>.CreateWithContent(valueTo), menuItemText, registerCompleteObjectUndo);
		}

		/// <summary>
		/// Use this for adding Undo support for changes that are not serialized by Unity when Unity can't handle undoing the action
		/// </summary>
		/// <param name="targets"> The UnityEngine.Object targets which contain the members which are being modified. </param>
		/// <param name="memberInfo"> The LinkedMemberInfo for the members which are being modified. This cannot be null. </param>
		/// <param name="valuesTo"> The value to which the members will be set. </param>
		/// <param name="menuItemText"> Message describing the undo. </param>
		/// <param name="registerCompleteObjectUndo"> Set to true if full object hierarchy should be serialized for the Undo. This is needed for undoing changes to array size fields to function properly. </param>
		/// <returns> True if should call EditorUtility.SetDirty after value has changed. </returns>
		public static bool RegisterUndoableAction(Object[] targets, [NotNull]LinkedMemberInfo memberInfo, object[] valuesTo, [NotNull]string menuItemText, bool registerCompleteObjectUndo)
		{
			if(disabled)
			{
				#if DEV_MODE && DEBUG_UNDO
				Debug.Log("RegisterUndoableAction with undoText=" + StringUtils.ToString(menuItemText) + " "+StringUtils.Red("ABORTING..."));
				#endif
				return false;
			}
			
			#if UNITY_EDITOR
			if(targets.Length > 0 && memberInfo.IsUnitySerialized)
			{
				#if DEV_MODE && DEBUG_UNDO
				Debug.Log("RegisterUndoableAction with " + StringUtils.Green("IsUnitySerialized") + " field=" + memberInfo + ", unityObject=" + StringUtils.ToString(targets[0].name) + ", valuesTo=" + StringUtils.ToString(valuesTo) + ", undoText=" + StringUtils.ToString(menuItemText) + ", registerCompleteObjectUndo="+ StringUtils.ToColorizedString(registerCompleteObjectUndo) +", IsUnitySerialized=" + StringUtils.ToColorizedString(true) + ", registeredForUndoThisFrame.Contains(targets[0])=" + StringUtils.ToColorizedString(registeredForUndoThisFrame.Contains(targets[0])), targets[0]);
				#endif

				Instance().DoRegisterUndoableActionInternal(targets, menuItemText, registerCompleteObjectUndo);
				return true;
			}
			#endif

			#if DEV_MODE && DEBUG_UNDO
			Debug.Log(StringUtils.ToColorizedString("RegisterUndoableAction with IsUnitySerialized=", memberInfo.IsUnitySerialized, " memberInfo=", memberInfo.ToString(), ", targets=", StringUtils.ToString(targets), ", valuesTo=", StringUtils.ToString(valuesTo), ", menuItemText=" + StringUtils.ToString(menuItemText), ", registerCompleteObjectUndo=", registerCompleteObjectUndo, ", registeredForUndoThisFrame.Contains(targets[0])=",  (targets.Length == 0 ? "n/a" : StringUtils.ToColorizedString(registeredForUndoThisFrame.Contains(targets[0])))));
			#endif

			Instance().DoRegisterUndoableActionInternal(instance, menuItemText); //register full undo or not?
			UndoableModification modification;
			try
			{
				modification = UndoableModification.Create(memberInfo, valuesTo);
			}
			#if DEV_MODE
			catch(SerializationException e)
			{
				Debug.LogError(e);
			#else
			catch(SerializationException)
			{
			#endif
				return false;
			}

			instance.AddNewUndoableModification(modification);

			#if SET_DIRTY_WHEN_NON_SERIALIZED_FIELDS_MODIFIED
			return true;
			#else
			return false;
			#endif
		}

		public static GameObject CreateGameObject(string menuItemText, string gameObjectName)
		{
			var gameObject = Platform.Active.CreateGameObject(gameObjectName);
			#if UNITY_EDITOR
			registeredForUndoThisFrame.Add(gameObject);
			UnityEditor.Undo.SetCurrentGroupName(menuItemText);
			#endif
			return gameObject;
		}

		public static void RegisterUndoableAction([NotNull]Object changingTarget, [NotNull]string menuItemText)
		{
			Instance().DoRegisterUndoableActionInternal(changingTarget, menuItemText);
		}

		public static void RegisterUndoableAction(Object[] changingTargets, string menuItemText)
		{
			#if DEV_MODE && DEBUG_UNDO
			Debug.Log("RegisterUndoableAction changingTargets=" + StringUtils.ToString(changingTargets) + ", menuItemText=" + StringUtils.ToString(menuItemText)+ ", registeredForUndoThisFrame.Contains(changingTargets[0])=" + StringUtils.ToColorizedString(registeredForUndoThisFrame.Contains(changingTargets[0])), changingTargets[0]);
			#endif

			if(changingTargets.Length == 0)
			{
				#if DEV_MODE
				Debug.LogWarning("UndoHandler.RegisterUndoableAction(\""+ menuItemText + "\") - targets array length was zero.");
				#endif
				return;
			}

			Instance().DoRegisterUndoableActionInternal(changingTargets, menuItemText);
		}
		
		/// <summary>
		/// Detect if the user gave an Undo command which undid an action that was backed by UndoHandler,
		/// and if so, then apply the undo effects to the field in question.
		/// </summary>
		private void DetectUndoneActions()
		{
			int changeInUndoIndex = undoIndexSerialized - undoIndexNonSerialized;

			if(changeInUndoIndex != 0)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(Mathf.Abs(changeInUndoIndex) == 1);
				#endif

				if(changeInUndoIndex < 0)
				{
					#if DEV_MODE && DEBUG_UNDO
					Debug.Log("UndoHandle - Undo command detected! undoIndexSerialized=" + undoIndexSerialized + " vs undoIndexNonSerialized=" + undoIndexNonSerialized+", modifications.Count="+modifications.Count);
					#endif

					var mod = modifications[undoIndexSerialized];					

					UndoIndex = undoIndexSerialized;

					#if DEV_MODE && DEBUG_UNDO
					Debug.Log("Undoing: " + mod + ". undoIndexSerialized=" + undoIndexSerialized + " vs undoIndexNonSerialized=" + undoIndexNonSerialized + ", modifications.Count=" + modifications.Count);
					#endif

					mod.Undo();
				}
				else
				{
					#if DEV_MODE && DEBUG_UNDO
					Debug.Log("UndoHandle - Redo command detected! undoIndexSerialized=" + undoIndexSerialized + " vs undoIndexNonSerialized=" + undoIndexNonSerialized + ", modifications.Count=" + modifications.Count);
					#endif

					var mod = modifications[undoIndexNonSerialized];

					UndoIndex = undoIndexSerialized;

					#if DEV_MODE && DEBUG_UNDO
					Debug.Log("Redoing: " + mod + ". undoIndexSerialized=" + undoIndexSerialized + " vs undoIndexNonSerialized=" + undoIndexNonSerialized + ", modifications.Count=" + modifications.Count);
					#endif

					mod.Redo();
				}

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(undoIndexSerialized == undoIndexNonSerialized);
				#endif
			}

			AfterUndoOrRedo();
		}

		private void AfterUndoOrRedo()
		{
			var inspector = InspectorUtility.ActiveInspector;
			if(inspector != null)
			{
				// Disable the undo system temporarily so that new undo entries aren't generated as unintended side effects
				// when cached values in the inspector are updated (this should not happen, but making sure).
				Disable();
				onUpdateBroadcaster = inspector.InspectorDrawer;
				onUpdateBroadcaster.OnUpdate += EnableDelayedAfterOnUpdate;

				// Stop editing text field when Undo is detected, so that if the value that was undone
				// is the field that is being edited, the changes will be seen immediately.
				if(DrawGUI.EditingTextField)
				{
					var focusedDrawer = inspector.Manager.FocusedDrawer;
					if(focusedDrawer != null)
					{
						DrawGUI.EditingTextField = false;
					}
				}
			}

			if(OnOndoOrRedoPerformed != null)
			{
				OnOndoOrRedoPerformed();
			}
		}
		
		private void EnableDelayedAfterOnUpdate(float deltaTime)
		{
			onUpdateBroadcaster.OnUpdate -= EnableDelayedAfterOnUpdate;
			
			// Delay the re-enabling of the undo system until all cached values have surely been updated, so that
			// new undo entries aren't generated as unintended side effects.
			InspectorUtility.ActiveManager.OnNextLayout(EnableDelayed);
		}


		/// <summary>
		/// Allows manually undoing a previously recorded undoable modification.
		/// This usually only needs to be called at runtime, since in the editor undo is usually initiated through the Edit menu.
		/// </summary>
		public void Undo()
		{
			UndoIndex--;
			var mod = modifications[UndoIndex];

			#if DEV_MODE && DEBUG_UNDO
			Debug.Log("Undoing: " + mod + ". undoIndexSerialized=" + undoIndexSerialized + " vs undoIndexNonSerialized=" + undoIndexNonSerialized + ", modifications.Count=" + modifications.Count);
			#endif

			mod.Undo();

			AfterUndoOrRedo();
		}



		/// <summary>
		/// Allows manually redoing a previously undone modification.
		/// This usually only needs to be called at runtime, since in the editor redo is usually initiated through the Edit menu.
		/// </summary>
		public void Redo()
		{
			var mod = modifications[UndoIndex];
			UndoIndex++;

			#if DEV_MODE && DEBUG_UNDO
			Debug.Log("Redoing: " + mod + ". undoIndexSerialized=" + undoIndexSerialized + " vs undoIndexNonSerialized=" + undoIndexNonSerialized + ", modifications.Count=" + modifications.Count);
			#endif

			mod.Redo();

			AfterUndoOrRedo();
		}

		private static void OnBeginOnGUI()
		{
			if(registeredForUndoThisFrame.Count > 0)
			{
				registeredForUndoThisFrame.Clear();
			}
		}

		private void DoRegisterUndoableActionInternal([NotNull]Object changingTarget, string menuItemText, bool registerCompleteObjectUndo = false)
		{
			#if DEV_MODE
			Debug.Assert(changingTarget != null);
			#endif

			#if UNITY_EDITOR
			if(registeredForUndoThisFrame.Contains(changingTarget))
			{
				#if DEV_MODE && DEBUG_UNDO
				Debug.Log("DoRegisterUndoableActionInternal(" + changingTarget.name+ ", \""+menuItemText+"\", complete="+ registerCompleteObjectUndo+"): <color=red>ABORTING...</color>");
				#endif
				return;
			}

			registeredForUndoThisFrame.Add(changingTarget);
						
			//this is necessary for array resizes
			if(registerCompleteObjectUndo)
			{
				#if DEV_MODE && DEBUG_UNDO
				Debug.Log("<color=green>Undo.RegisterCompleteObjectUndo</color>(" + changingTarget.name + ", \"" + menuItemText + "\")");
				#endif
				UnityEditor.Undo.RegisterCompleteObjectUndo(changingTarget, menuItemText);
			}
			else
			{
				#if DEV_MODE && DEBUG_UNDO
				Debug.Log("<color=green>Undo.RecordObjects</color>(\"" + changingTarget.name + "\", \"" + menuItemText + "\")");
				#endif
				UnityEditor.Undo.RecordObject(changingTarget, menuItemText);
			}
			#endif
		}

		private void DoRegisterUndoableActionInternal(Object[] changingTargets, string menuItemText, bool registerCompleteObjectUndo = true)
		{
			#if UNITY_EDITOR
			if(registeredForUndoThisFrame.Contains(changingTargets[0]))
			{
				#if DEV_MODE && DEBUG_UNDO
				Debug.Log("DoRegisterUndoableActionInternal(" + changingTargets[0].name+ ", \""+menuItemText+"\", complete="+ registerCompleteObjectUndo+ "): <color=red>ABORTING...</color>");
				#endif
				return;
			}

			registeredForUndoThisFrame.AddRange(changingTargets);

			//this is necessary for array resizes
			if(registerCompleteObjectUndo)
			{
				#if DEV_MODE && DEBUG_UNDO
				Debug.Log("<color=green>Undo.RegisterCompleteObjectUndo</color>(" + StringUtils.ToString(changingTargets) + ", \"" + menuItemText + "\")");
				#endif
				UnityEditor.Undo.RegisterCompleteObjectUndo(changingTargets, menuItemText);
			}
			else
			{
				#if DEV_MODE && DEBUG_UNDO
				Debug.Log("<color=green>Undo.RecordObjects</color>(" + StringUtils.ToString(changingTargets) + ", \"" + menuItemText + "\")");
				#endif
				UnityEditor.Undo.RecordObjects(changingTargets, menuItemText);
			}
			#endif
		}

		[UsedImplicitly]
		private void OnEnable()
		{
			Setup();
		}

		private void Setup()
		{
			if(instance != this)
			{
				if(instance == null)
				{
					instance = this;
				}
				else
				{
					#if DEV_MODE
					Debug.LogWarning("Destroying UndoHandler because there were multiple instances");
					#endif

					DestroyImmediate(this);
					return;
				}
			}

			if(!listeningToEvents)
			{
				listeningToEvents = true;
				DrawGUI.OnEveryBeginOnGUI(OnBeginOnGUI, false);

				#if UNITY_EDITOR
				UnityEditor.Undo.undoRedoPerformed += DetectUndoneActions;
				#endif
			}
			
			Clear();
		}

		[UsedImplicitly]
		private void OnDisable()
		{
			if(listeningToEvents)
			{
				listeningToEvents = false;
				DrawGUI.CancelOnEveryBeginOnGUI(OnBeginOnGUI);

				#if UNITY_EDITOR
				UnityEditor.Undo.undoRedoPerformed -= DetectUndoneActions;
				#endif
			}
		}

		public static void Disable()
		{
			disabled = true;
		}

		private void EnableDelayed()
		{
			InspectorUtility.ActiveManager.OnNextLayout(EnableDelayedStep1);
		}

		private void EnableDelayedStep1()
		{
			InspectorUtility.ActiveManager.OnNextLayout(EnableDelayedStep2);
		}

		private void EnableDelayedStep2()
		{
			InspectorUtility.ActiveManager.OnNextLayout(Enable);
		}

		public static void Enable()
		{
			disabled = false;
		}

		private class UndoableModification : IDisposable
		{
			private object[] fromValues;
			private object[] toValues;
			private byte[] memberInfoSerialized;

			private LinkedMemberInfo MemberInfo
			{
				get
				{
					try
					{
						return SerializableMemberInfo.Deserialize(memberInfoSerialized);
					}
					// This happened at one point when deserializing types that contained two indexed properties
					// with the same name but different numbers of parameters (so e.g. a type of a class that
					// contained both this[int index] and this[int x, int y]).
					// SerializableMemberInfo no longer serializes types from IndexerData, though, which should
					// get rid of this issue.
					catch(System.Reflection.AmbiguousMatchException e)
					{
						Debug.LogError(e);
						return null;
					}
				}

				set
				{
					memberInfoSerialized = SerializableMemberInfo.Serialize(value);

					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(value != null);
					Debug.Assert(memberInfoSerialized != null);
					var deserialized = SerializableMemberInfo.Deserialize(memberInfoSerialized);
					if(value.MemberInfo != null)
					{
						if(deserialized.MemberInfo == null)
						{
							Debug.LogError("UndoableModification deserializing test of MemberInfo failed for "+value);
						}
						else if(!deserialized.MemberInfo.EqualTo(value.MemberInfo))
						{
							Debug.LogError("UndoableModification deserializing test of MemberInfo failed for "+value);
						}
					}
					#endif
				}
			}

			[NotNull]
			public static UndoableModification Create([NotNull]LinkedMemberInfo setMemberInfo, object setValueTo)
			{
				UndoableModification created;
				if(!pool.TryGet(out created))
				{
					created = new UndoableModification();
				}
				created.Setup(setMemberInfo, ArrayPool<object>.CreateWithContent(setValueTo));
				return created;
			}

			[NotNull]
			public static UndoableModification Create([NotNull]LinkedMemberInfo setMemberInfo, [NotNull]object[] setValuesTo)
			{
				UndoableModification created;
				if(!pool.TryGet(out created))
				{
					created = new UndoableModification();
				}
				created.Setup(setMemberInfo, setValuesTo);
				return created;
			}

			private UndoableModification() { }

			private void Setup(LinkedMemberInfo setMemberInfo, [NotNull]object[] setValuesTo)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(setMemberInfo != null);
				Debug.Assert(setMemberInfo.CanRead);
				Debug.Assert(setMemberInfo.CanWrite);
				Debug.Assert(setValuesTo != null);
				#endif

				fromValues = setMemberInfo.GetValues();
				toValues = setValuesTo;
				
				MemberInfo = setMemberInfo;

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(fromValues != null);
				if(fromValues.ContentsMatch(toValues)) { Debug.LogError("UndoableModification value to and from are the same: "+ToString()); }
				#endif
			}

			public void Undo()
			{
				if(fromValues == null)
				{
					#if DEV_MODE
					Debug.LogError("Undo NullReferenceException - toValues was null.");
					#endif
					return;
				}

				var deserialized = MemberInfo;
				if(deserialized == null)
				{
					#if DEV_MODE
					Debug.LogError("Undo NullReferenceException - MemberInfo was null after deserialize.");
					#endif
					return;
				}

				if(!deserialized.CanWrite)
				{
					#if DEV_MODE
					Debug.LogError("Undo InvalidOperation - MemberInfo CanWrite was false after deserialize: " + deserialized);
					#endif
					return;
				}

				bool undoHandlerWasDisabled = disabled;
				disabled = true;

				try
				{
					if(fromValues.Length == 1)
					{
						deserialized.SetValue(fromValues[0]);
					}
					else
					{
						deserialized.SetValues(fromValues);
					}
					disabled = undoHandlerWasDisabled;
				}
				#if DEV_MODE
				catch(Exception e)
				{
					Debug.LogError(e);
				#else
				catch(Exception)
				{
				#endif
					disabled = undoHandlerWasDisabled;
				}
			}

			public void Redo()
			{
				if(toValues == null)
				{
					#if DEV_MODE
					Debug.LogError("Redo NullReferenceException - toValues was null.");
					#endif
					return;
				}

				var deserialized = MemberInfo;
				if(deserialized == null)
				{
					#if DEV_MODE
					Debug.LogError("Redo NullReferenceException - MemberInfo was null after deserialize.");
					#endif
					return;
				}

				if(!deserialized.CanWrite)
				{
					#if DEV_MODE
					Debug.LogError("Redo InvalidOperation - MemberInfo CanWrite was false after deserialize: "+ deserialized);
					#endif
					return;
				}

				bool undoHandlerWasDisabled = disabled;
				disabled = true;

				try
				{
					if(toValues.Length == 1)
					{
						deserialized.SetValue(toValues[0]);
					}
					else
					{
						deserialized.SetValues(toValues);
					}
					disabled = undoHandlerWasDisabled;
				}
				#if DEV_MODE
				catch(Exception e)
				{
					Debug.LogError(e);
				#else
				catch(Exception)
				{
				#endif
					disabled = undoHandlerWasDisabled;
				}
			}
			
			public override string ToString()
			{
				// fix for rare bug where Object reference field with UndoHandler value could result in infinite recursion
				if(fromValues.Length > 0)
				{
					var fromValue = fromValues[0];
					var toValue = toValues[0];
					if(fromValue != null)
					{
						if(fromValue.GetType() == typeof(UndoHandler))
						{
							return StringUtils.Concat(MemberInfo.DisplayName, " from ", StringUtils.TypeToString(fromValue), "[", StringUtils.ToString(fromValues.Length), "] to ", StringUtils.TypeToString(toValue), "[", StringUtils.ToString(toValues.Length), "]");
						}
					}
					else if(toValue != null && toValue.GetType() == typeof(UndoHandler))
					{
						return StringUtils.Concat(MemberInfo.DisplayName, " from ", StringUtils.TypeToString(fromValue), "[", StringUtils.ToString(fromValues.Length), "] to ", StringUtils.TypeToString(toValue), "[", StringUtils.ToString(toValues.Length), "]");
					}
				}

				return StringUtils.Concat(MemberInfo.DisplayName, " from ", StringUtils.ToString(fromValues), " to "+ StringUtils.ToString(toValues));
			}

			public void Dispose()
			{

			}
		}
	}
}