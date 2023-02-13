//#define DEBUG_REMOVE_INVALID
//#define DEBUG_ADD_TO_HISTORY

using System;
using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable]
	public class SelectionHistory
	{
		private const int MaxSize = 30;

		[SerializeField]
		private List<Object[]> history = new List<Object[]>(MaxSize * 2);

		[SerializeField]
		private int currentIndex = -1;

		[NonSerialized]
		private bool ignoreNextRecordRequest;

		public List<Object[]> History
		{
			get
			{
				return history;
			}
		}

		public int CurrentIndex
		{
			get
			{
				return currentIndex;
			}
		}

		public bool HasPreviousItems()
		{
			return currentIndex > 0;
		}

		public bool HasNextItems()
		{
			return currentIndex < history.Count - 1;
		}
		
		public void OpenNavigateBackMenuAt(IInspector inspector, Rect backButtonRect)
		{
			var menuItems = new List<PopupMenuItem>(currentIndex);
			for(int n = currentIndex - 1; n >= 0; n--)
			{
				TryCreatePopupMenuItem(ref menuItems, n);
			}
			if(menuItems.Count > 0)
			{
				var unrollPosition = backButtonRect;
				unrollPosition.y += backButtonRect.height;
				PopupMenuManager.Open(inspector, menuItems, unrollPosition, OnMenuItemClick);
			}
		}

		private void OnMenuItemClick(PopupMenuItem item)
		{
			//new test: delaying action until next frame, so that can detect modifiers
			PopupMenuManager.LastmenuOpenedForInspector.OnNextLayout(()=>OnMenuItemClickNextLayout(item));
		}

		private void OnMenuItemClickNextLayout(PopupMenuItem item)
		{
			var inspector = PopupMenuManager.LastmenuOpenedForInspector;

			int itemIndex = (int)item.IdentifyingObject;
			var e = Event.current;
			//ctrl + click can be used to open item in other split view
			if(e != null && e.control && inspector.InspectorDrawer.CanSplitView)
			{
				var splittable = inspector.InspectorDrawer as ISplittableInspectorDrawer;
				if(splittable != null && inspector == splittable.MainView)
				{
					ShowInSplitView(splittable, itemIndex);
					return;
				}
			}
			
			Show(inspector, itemIndex);
		}
		
		public void OpenNavigateForwardMenuAt(IInspector inspector, Rect forwardButtonRect)
		{
			var menuItems = new List<PopupMenuItem>(currentIndex);
			int historyCount = history.Count;
			for(int n = currentIndex + 1; n < historyCount; n++)
			{
				TryCreatePopupMenuItem(ref menuItems, n);
			}

			if(menuItems.Count > 0)
			{
				var unrollPosition = forwardButtonRect;
				unrollPosition.y += forwardButtonRect.height;
				PopupMenuManager.Open(inspector, menuItems, unrollPosition, OnMenuItemClick);
			}
		}

		private void TryCreatePopupMenuItem(ref List<PopupMenuItem> menuItems, int itemIndex)
		{
			var objs = history[itemIndex];
			int objCount = objs.Length;
			if(objCount == 0)
			{
				return;
			}
			var obj = objs[0];
			if(obj == null)
			{
				return;
			}

			string name = objCount == 1 ? obj.name : StringUtils.NamesToString(objs);
			
			#if UNITY_EDITOR
			var mainObject = obj.GetAssetOrMainComponent();
			var preview = UnityEditor.AssetPreview.GetMiniThumbnail(mainObject);
			#else
			Texture2D preview = null;
			#endif
			
			var item = PopupMenuItem.Item(itemIndex, objs.GetType(), name, objCount == 1 ? obj.HierarchyOrAssetPath() : "", null, preview);
			menuItems.Add(item);
		}
		
		private void ShowInSplitView(ISplittableInspectorDrawer drawer, int itemIndex)
		{
			currentIndex = itemIndex;

			if(!drawer.ViewIsSplit)
			{
				if(InspectorUtility.IsSafeToChangeInspectorContents)
				{
					ShowCurrentInSplitViewNow(drawer);
				}
				else
				{
					drawer.MainView.OnNextLayout(()=>ShowCurrentInSplitViewNow(drawer));
				}
			}
			else
			{
				var splitView = drawer.SplitView;
				if(history[currentIndex].ContentsMatch(splitView.State.inspected))
				{
					return;
				}

				if(splitView.State.ViewIsLocked)
				{
					if(InspectorUtility.IsSafeToChangeInspectorContents)
					{
						splitView.OnNextLayout(()=>RebuildDrawerForCurrent(splitView));
					}
					else
					{
						RebuildDrawerForCurrent(splitView);
					}
				}
				else
				{
					IgnoreNextRecordRequest();
					splitView.Select(history[currentIndex]);
				}
			}
		}

		private void ShowCurrentInSplitViewNow(ISplittableInspectorDrawer drawer)
		{
			IgnoreNextRecordRequest();
			drawer.ShowInSplitView(history[currentIndex]);
		}

		private void Show(IInspector inspector, int itemIndex)
		{
			currentIndex = itemIndex;
			ShowCurrent(inspector);
		}

		public void Clear()
		{
			history.Clear();
			currentIndex = -1;
		}

		public void RecordNewSelection([NotNull]Object[] newSelection)
		{
			if(ignoreNextRecordRequest)
			{
				ResumeRecordingHistory();
				return;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!newSelection.ContainsNullObjects());
			Debug.Assert(!newSelection.ContainsObjectsOfType(typeof(Transform)));
			#endif
			
			int count = history.Count;

			// if new selection is empty, clear any items in the "forward" direction
			// and then stop.
			if(newSelection.Length == 0)
			{
				currentIndex++;
				for(int n = count - 1; n >= currentIndex; n--)
				{
					#if DEV_MODE
					Debug.Log("Removing from navigation history: "+StringUtils.ToString(history[n]));
					#endif

					history.RemoveAt(n);
					count--;
				}
				return;
			}
			
			//record the traversal down the history records
			//as part of the new history
			int lastIndex = count - 1;
			if(currentIndex >= 0 && currentIndex < lastIndex)
			{
				for(int n = count - 2; n >= currentIndex; n--)
				{
					count++;
					
					#if DEV_MODE
					if(n > lastIndex)
					{
						Debug.LogError("n (" + n + ") > lastIndex (" + lastIndex + ") !");
						break;
					}

					if(n < 0)
					{
						Debug.LogError("n (" + n + ") < 0 !");
						break;
					}
					#endif

					var move = history[n];
					history.Add(move);
				}
			}

			#if DEV_MODE && DEBUG_ADD_TO_HISTORY
			Debug.Log("Adding to navigation history: "+StringUtils.ToString(newSelection));
			#endif

			//finally record the new selection at the end of the history list
			history.Add(newSelection);
			currentIndex = count - 1;

			//trim history down to max size value
			for(int n = count - 1; n >= MaxSize; n--)
			{
				count--;
				if(currentIndex == 0)
				{
					history.RemoveAt(count);
				}
				else
				{
					history.RemoveAt(0);
					currentIndex--;
				}
			}

			#if DEV_MODE && DEBUG_ADD_TO_HISTORY
			UnityEngine.Debug.Log("history now: "+StringUtils.ToString(history));
			#endif

			currentIndex++;
		}

		[NotNull]
		public Object[] PeekPreviousInSelectionHistory()
		{
			if(!HasPreviousItems())
			{
				return ArrayPool<Object>.ZeroSizeArray;
			}
			return history[currentIndex - 1];
		}

		public bool StepBackInSelectionHistory(IInspector inspector)
		{
			if(!HasPreviousItems())
			{
				return false;
			}

			currentIndex--;
			ShowCurrent(inspector);
			return true;
		}

		[NotNull]
		public Object[] PeekNextInSelectionHistory()
		{
			if(!HasNextItems())
			{
				return ArrayPool<Object>.ZeroSizeArray;
			}
			return history[currentIndex + 1];
		}

		public bool StepForwardInSelectionHistory(IInspector inspector)
		{
			if(!HasNextItems())
			{
				return false;
			}
			
			currentIndex++;
			ShowCurrent(inspector);
			return true;
		}

		public void RemoveUnloadedAndInvalidTargets()
		{
			#if DEV_MODE && DEBUG_REMOVE_INVALID
			Debug.Log("SelectionHistory.RemoveReferencesToUnloadedOrInvalidScenes()");
			#endif

			int count = history.Count;
			for(int n = count - 1; n >= 0; n--)
			{
				var objs = history[n];

				for(int o = objs.Length - 1; o >= 0; o--)
				{
					var obj = objs[o];
					if(obj == null)
					{
						if(!UnityObjectExtensions.TryToFixNull(ref obj))
						{
							#if DEV_MODE && DEBUG_REMOVE_INVALID
							Debug.Log("Removing history[" + n + "][" + o + "]: because target no longer exists");
							#endif

							objs = objs.RemoveAt(o);
						}
					}
				}

				if(objs.Length == 0)
				{
					history.RemoveAt(n);
					if(currentIndex > n)
					{
						currentIndex--;
					}
					
					#if DEV_MODE && DEBUG_REMOVE_INVALID
					Debug.Log("Removed history[" + n + "] because none of the targets existed any longer. Index now "+index+" nad history.Count="+history.Count);
					#endif
				}
			}
		}

		private void ShowCurrent(IInspector inspector)
		{
			if(history[currentIndex].ContentsMatch(inspector.State.inspected))
			{
				return;
			}

			#if DEV_MODE
			Debug.Log("SelectionHistory.ShowCurrent with ViewIsLocked="+ inspector.State.ViewIsLocked);
			#endif

			if(inspector.State.ViewIsLocked)
			{
				inspector.OnNextLayout(()=>RebuildDrawerForCurrent(inspector));
			}
			else
			{
				IgnoreNextRecordRequest();
				inspector.Select(history[currentIndex]);
			}
		}
		
		private void RebuildDrawerForCurrent(IInspector inspector)
		{
			IgnoreNextRecordRequest();
			inspector.RebuildDrawers(history[currentIndex], false);
			ResumeRecordingHistory();
		}

		private void IgnoreNextRecordRequest()
		{
			ignoreNextRecordRequest = true;
		}

		private void ResumeRecordingHistory()
		{
			ignoreNextRecordRequest = false;
		}
	}
}