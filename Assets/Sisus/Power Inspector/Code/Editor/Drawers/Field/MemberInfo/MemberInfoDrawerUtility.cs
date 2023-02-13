using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using UnityEditor;

namespace Sisus
{
	[InitializeOnLoad]
	public static class MemberInfoDrawerUtility
	{
		private static volatile SetupPhase setupDone = SetupPhase.Unstarted;

		public static List<PopupMenuItem> allRootItems = new List<PopupMenuItem>(64);
		public static Dictionary<string, PopupMenuItem> allGroupsByLabel = new Dictionary<string, PopupMenuItem>(10000);
		public static Dictionary<string, PopupMenuItem> allItemsByLabel = new Dictionary<string, PopupMenuItem>(10000);

		public static List<PopupMenuItem> fieldRootItems = new List<PopupMenuItem>(64);
		public static Dictionary<string, PopupMenuItem> fieldGroupsByLabel = new Dictionary<string, PopupMenuItem>(5000);
		public static Dictionary<string, PopupMenuItem> fieldItemsByLabel = new Dictionary<string, PopupMenuItem>(5000);

		public static List<PopupMenuItem> propertyRootItems = new List<PopupMenuItem>(64);
		public static Dictionary<string, PopupMenuItem> propertyGroupsByLabel = new Dictionary<string, PopupMenuItem>(5000);
		public static Dictionary<string, PopupMenuItem> propertyItemsByLabel = new Dictionary<string, PopupMenuItem>(5000);

		public static List<PopupMenuItem> methodRootItems = new List<PopupMenuItem>(64);
		public static Dictionary<string, PopupMenuItem> methodGroupsByLabel = new Dictionary<string, PopupMenuItem>(5000);
		public static Dictionary<string, PopupMenuItem> methodItemsByLabel = new Dictionary<string, PopupMenuItem>(5000);

		private static Action onSetupFinished;
		private static readonly object threadLock = new object();

		public static bool IsReady
		{
			get
			{
				lock(threadLock)
				{
					#if DEV_MODE
					if(setupDone != SetupPhase.Done) { UnityEngine.Debug.Log("MemberInfoDrawerUtility setupDone="+setupDone); }
					#endif
					return setupDone == SetupPhase.Done;
				}
			}
		}

		static MemberInfoDrawerUtility()
		{
			EditorApplication.delayCall += SetupDelayed;
		}

		public static void WhenReady([NotNull]Action onFinished)
		{
			if(IsReady)
			{
				onFinished();
				return;
			}

			lock(threadLock)
			{
				onSetupFinished += onFinished;
			}
		}

		public static void SetupDelayed()
		{
			lock(threadLock)
			{
				if(setupDone != SetupPhase.Unstarted)
				{
					return;
				}
			}

			if(!ApplicationUtility.IsReady())
			{
				EditorApplication.delayCall += SetupDelayed;
				return;
			}

			// TypeExtensions is currently needed for popupmenu generation
			if(!TypeExtensions.IsReady)
			{
				EditorApplication.delayCall += SetupDelayed;
				return;
			}

			lock(threadLock)
			{
				setupDone = SetupPhase.InProgress;
			}

			// Make sure that Preferences have been fetched via AssetDatabase.LoadAssetAtPath before moving on to threaded code
			var preferences = InspectorUtility.Preferences;
			UnityEngine.Debug.Assert(preferences != null);

			// build syntax formatting on another thread to avoid UI slow downs when user
			// selects a large formatted text file
			ThreadPool.QueueUserWorkItem(SetupThreaded);
		}

		private static void SetupThreaded(object _)
		{
			#if DEV_MODE
			//var timer = new ExecutionTimeLogger();
			//timer.Start("MemberInfoBaseDrawerData.SetupThreaded");
			//timer.StartInterval("Generate PopupMenuItems");
			#endif

			var allRootItems = new List<PopupMenuItem>();
			var allGroupsByLabel = new Dictionary<string, PopupMenuItem>(10000);
			var allItemsByLabel = new Dictionary<string, PopupMenuItem>(10000);

			var fieldRootItems = new List<PopupMenuItem>(64);
			var fieldGroupsByLabel = new Dictionary<string, PopupMenuItem>(5000);
			var fieldItemsByLabel = new Dictionary<string, PopupMenuItem>(5000);

			var propertyRootItems = new List<PopupMenuItem>(64);
			var propertyGroupsByLabel = new Dictionary<string, PopupMenuItem>(5000);
			var propertyItemsByLabel = new Dictionary<string, PopupMenuItem>(5000);

			var methodRootItems = new List<PopupMenuItem>(64);
			var methodGroupsByLabel = new Dictionary<string, PopupMenuItem>(5000);
			var methodItemsByLabel = new Dictionary<string, PopupMenuItem>(5000);

			var sb = new StringBuilder();
			foreach(var type in TypeExtensions.GetAllTypesThreadSafe(true, false, false))
			{
				sb.Append(TypeExtensions.GetPopupMenuLabel(type, ""));
				sb.Append('/');
				string typePrefix = sb.ToString();
				sb.Length = 0;

				var fields = type.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				for(int f = fields.Length - 1; f >= 0; f--)
				{
					var field = fields[f];

					#if DEV_MODE
					if(field.Name.Length > 100) { UnityEngine.Debug.LogWarning(field.Name); }
					#endif

					sb.Append(typePrefix);
					sb.Append(field.Name);
					string menuPath = sb.ToString();
					sb.Length = 0;

					PopupMenuUtility.BuildPopupMenuItemWithLabel(fieldRootItems, fieldGroupsByLabel, fieldItemsByLabel, field, typeof(FieldInfo), menuPath, MenuItemValueType.Undefined, true);
				}

				var properties = type.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				for(int p = properties.Length - 1; p >= 0; p--)
				{
					var property = properties[p];
					sb.Append(typePrefix);
					StringUtils.ToString(property, sb);

					string menuPath = sb.ToString();
					sb.Length = 0;

					PopupMenuUtility.BuildPopupMenuItemWithLabel(propertyRootItems, propertyGroupsByLabel, propertyItemsByLabel, property, typeof(PropertyInfo), menuPath, MenuItemValueType.Undefined, true);
				}

				var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

				for(int m = methods.Length - 1; m >= 0; m--)
				{
					var method = methods[m];

					sb.Append(typePrefix);
					StringUtils.ToString(method, sb);
					string menuPath = sb.ToString();
					sb.Length = 0;

					PopupMenuUtility.BuildPopupMenuItemWithLabel(methodRootItems, methodGroupsByLabel, methodItemsByLabel, method, typeof(MethodInfo), menuPath, MenuItemValueType.Undefined, true);
				}
			}

			#if DEV_MODE
			//timer.FinishInterval();
			//timer.StartInterval("allRootItems.AddRange(fieldRootItems)");
			#endif

			allRootItems.AddRangeSorted(fieldRootItems);

			#if DEV_MODE
			//timer.FinishInterval();
			//timer.StartInterval("AddRange(allRootItems, propertyRootItems)");
			#endif

			PopupMenuUtility.AddRangeSorted(ref allRootItems, propertyRootItems);

			#if DEV_MODE
			//timer.FinishInterval();
			//timer.StartInterval("AddRange(allRootItems, methodRootItems)");
			#endif

			PopupMenuUtility.AddRangeSorted(ref allRootItems, methodRootItems);

			#if DEV_MODE
			//timer.FinishInterval();
			//timer.StartInterval("AddRange(allGroupsByLabel, fieldGroupsByLabel)");
			#endif

			PopupMenuUtility.AddRange(ref allGroupsByLabel, fieldGroupsByLabel);

			#if DEV_MODE
			//timer.FinishInterval();
			//timer.StartInterval("AddRange(allGroupsByLabel, propertyGroupsByLabel)");
			#endif

			PopupMenuUtility.AddRange(ref allGroupsByLabel, propertyGroupsByLabel);

			#if DEV_MODE
			//timer.FinishInterval();
			//timer.StartInterval("AddRange(allGroupsByLabel, methodGroupsByLabel)");
			#endif

			PopupMenuUtility.AddRange(ref allGroupsByLabel, methodGroupsByLabel);

			#if DEV_MODE
			//timer.FinishInterval();
			//timer.StartInterval("AddRange(allItemsByLabel, fieldItemsByLabel)");
			#endif

			PopupMenuUtility.AddRange(ref allItemsByLabel, fieldItemsByLabel);

			#if DEV_MODE
			//timer.FinishInterval();
			//timer.StartInterval("AddRange(allItemsByLabel, propertyItemsByLabel)");
			#endif

			PopupMenuUtility.AddRange(ref allItemsByLabel, propertyItemsByLabel);

			#if DEV_MODE
			//timer.FinishInterval();
			//timer.StartInterval("AddRange(allItemsByLabel, methodItemsByLabel)");
			#endif

			PopupMenuUtility.AddRange(ref allItemsByLabel, methodItemsByLabel);

			#if DEV_MODE
			//timer.FinishInterval();
			//timer.FinishAndLogResults();
			#endif

			lock(threadLock)
			{
				if(setupDone != SetupPhase.InProgress)
				{
					return;
				}

				setupDone = SetupPhase.Done;
			}

			MemberInfoDrawerUtility.fieldRootItems = fieldRootItems;
			MemberInfoDrawerUtility.allRootItems = allRootItems;
			MemberInfoDrawerUtility.allGroupsByLabel = allGroupsByLabel;
			MemberInfoDrawerUtility.allItemsByLabel = allItemsByLabel;

			MemberInfoDrawerUtility.fieldRootItems = fieldRootItems;
			MemberInfoDrawerUtility.fieldGroupsByLabel = fieldGroupsByLabel;
			MemberInfoDrawerUtility.fieldItemsByLabel = fieldItemsByLabel;

			MemberInfoDrawerUtility.propertyRootItems = propertyRootItems;
			MemberInfoDrawerUtility.propertyGroupsByLabel = propertyGroupsByLabel;
			MemberInfoDrawerUtility.propertyItemsByLabel = propertyItemsByLabel;

			MemberInfoDrawerUtility.methodRootItems = methodRootItems;
			MemberInfoDrawerUtility.methodGroupsByLabel = methodGroupsByLabel;
			MemberInfoDrawerUtility.methodItemsByLabel = methodItemsByLabel;

			lock(threadLock)
			{
				var action = onSetupFinished;
				if(action != null)
				{
					onSetupFinished = null;
					action();
				}
			}
		}
	}
}