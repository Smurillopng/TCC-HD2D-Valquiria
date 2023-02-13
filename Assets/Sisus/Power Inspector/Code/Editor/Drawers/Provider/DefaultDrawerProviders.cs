#define CACHE_TO_DISK
#define DEBUG_SETUP

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEditor;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Class that handles creating, caching and returning default drawer providers for inspectors.
	/// </summary>
	[InitializeOnLoad]
	public static class DefaultDrawerProviders
	{
		private static Dictionary<Type, IDrawerProvider> drawerProvidersByInspectorType = new Dictionary<Type, IDrawerProvider>(2);

		private static bool selfAndAllProvidersReady;
		private static bool selfReady;

		private volatile static ConcurrentDictionary<Type, IDrawerProvider> drawerProvidersByInspectorTypeRebuilt;

		public static bool IsReady
		{
			get
			{
				if(!selfAndAllProvidersReady)
				{
					if(!selfReady)
					{
						return false;
					}

					foreach(var provider in drawerProvidersByInspectorType.Values)
					{
						if(!provider.IsReady)
						{
							#if DEV_MODE
							Debug.LogWarning("DefaultDrawerProviders.IsReady false because provider "+provider.GetType().Name+".IsReady=false");
							#endif
							return false;
						}
					}

					selfAndAllProvidersReady = true;
				}

				return true;
			}
		}

		static DefaultDrawerProviders()
		{
			// Delay is needed because can't access asset database to load InspectorPreferences asset from constructor.
			EditorApplication.delayCall -= Setup;
			EditorApplication.delayCall += Setup;

			#if CACHE_TO_DISK && (!NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0) // HashSet.GetObjectData is not implemented in older versions
			if(System.IO.File.Exists(SavePath()))
			{
				Deserialize();
			}
			#endif
		}

		#if CACHE_TO_DISK && (!NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0) // HashSet.GetObjectData is not implemented in older versions
		public static void Deserialize()
		{
			var cachePath = SavePath();
			if(!System.IO.File.Exists(cachePath))
			{
				return;
			}

			selfAndAllProvidersReady = false;
			selfReady = false;

			try
			{
				var bytes = System.IO.File.ReadAllBytes(cachePath);
				using (var memStream = new System.IO.MemoryStream())
				{
					var binForm = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
					memStream.Write(bytes, 0, bytes.Length);
					memStream.Seek(0, System.IO.SeekOrigin.Begin);
					drawerProvidersByInspectorType = (Dictionary<Type, IDrawerProvider>)binForm.Deserialize(memStream);
				}

				if(drawerProvidersByInspectorType is null)
				{
					drawerProvidersByInspectorType = new Dictionary<Type, IDrawerProvider>(2);
					return;
				}

				foreach(var deserializedItem in drawerProvidersByInspectorType)
				{
					if(deserializedItem.Key == null || deserializedItem.Value == null)
					{
						drawerProvidersByInspectorType.Clear();
						return;
					}
				}

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(drawerProvidersByInspectorType != null, "Drawer providers dictionary was null");
				IDrawerProvider found;
				Debug.Assert(drawerProvidersByInspectorType.TryGetValue(typeof(IInspector), out found), "Default drawer provider not found after deserialize");
				foreach(var drawerProvider in drawerProvidersByInspectorType.Values)
				{
					Debug.Assert(drawerProvider.IsReady, drawerProvider.GetType()+ ".IsReady was false after deserialize.");
				}
				#endif

				if(!drawerProvidersByInspectorType.ContainsKey(typeof(PowerInspector)) || !drawerProvidersByInspectorType[typeof(PowerInspector)].DrawerProviderData.ValidateData())
				{
					drawerProvidersByInspectorType.Clear();
					return;
				}

				foreach(var drawerProvider in drawerProvidersByInspectorType.Values)
                {
					drawerProvider.UsingDeserializedDrawers = true;
				}

				selfAndAllProvidersReady = true;
				selfReady = true;

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(IsReady, "DefaultDrawerProviders.IsReady was false");
				#endif
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogWarning(e);
			#else
			catch
			{
			#endif
				if(drawerProvidersByInspectorType == null)
				{
					drawerProvidersByInspectorType = new Dictionary<Type, IDrawerProvider>(2);
				}
			}
		}

		private static string SavePath()
		{
			return System.IO.Path.Combine(Application.temporaryCachePath, "PowerInspector.DefaultDrawerProviders.data");
		}

		public static void Serialize()
		{
			using(var stream = new System.IO.MemoryStream())
			{
				var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				formatter.Serialize(stream, drawerProvidersByInspectorType);
				System.IO.File.WriteAllBytes(SavePath(), stream.ToArray());
			}
		}
		#endif

		private static void Setup()
		{
			// Wait until Inspector contents have been rebuilt using deserialized cached drawers until moving on to fully rebuilding drawers from scratch.
			// This is because the process of building all the drawers can take a couple of seconds, and we don't want to keep the user waiting for this duration.
			// If isReady is false then no existing state was deserialized before Setup was called, and we can skip this part.
			if(selfAndAllProvidersReady)
			{
				foreach(var inspector in InspectorManager.Instance().ActiveInstances)
				{
					if(!inspector.SetupDone)
					{
						#if DEV_MODE && DEBUG_SETUP
						Debug.Log("DefaultDrawerProviders - waiting until inspector Setup Done...");
						#endif
						EditorApplication.delayCall -= Setup;
						EditorApplication.delayCall += Setup;
						return;
					}
				}
				#if DEV_MODE && DEBUG_SETUP
				Debug.Log("Setup now done for all "+ InspectorManager.Instance().ActiveInstances.Count+" inspectors");
				#endif
			}

			// Make sure that Preferences have been fetched via AssetDatabase.LoadAssetAtPath before moving on to threaded code
			var preferences = InspectorUtility.Preferences;
			Debug.Assert(preferences != null, "Preferences null");

			#if CSHARP_7_3_OR_NEWER
			Task.Run(async () => await SetupAsync());
			#else
			SetupSync();
			ApplyRebuiltSetupDataWhenReady();
			#endif
		}

		private static void ApplyRebuiltSetupDataWhenAllProvidersReady()
		{
			foreach(var provider in drawerProvidersByInspectorTypeRebuilt.Values)
			{
				if(!provider.IsReady)
				{
					#if DEV_MODE && DEBUG_SETUP
					Debug.LogWarning("DefaultDrawerProviders.ApplyRebuiltSetupDataWhenReady delaying because provider !" + provider.GetType().Name+".IsReady");
					#endif
					EditorApplication.delayCall -= ApplyRebuiltSetupDataWhenAllProvidersReady;
					EditorApplication.delayCall += ApplyRebuiltSetupDataWhenAllProvidersReady;
					return;
				}
			}

			selfAndAllProvidersReady = false;
			selfReady = true;

			#if DEV_MODE && DEBUG_SETUP
			Debug.Log("DefaultDrawerProviders - Applying rebuilt setup data now!");
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(drawerProvidersByInspectorTypeRebuilt.Count > 0, "drawerProvidersByInspectorTypeRebuilt.Count was 0");
			#endif

			drawerProvidersByInspectorType.Clear();
			foreach(var item in drawerProvidersByInspectorTypeRebuilt)
			{
				drawerProvidersByInspectorType.Add(item.Key, item.Value);
			}
			drawerProvidersByInspectorTypeRebuilt.Clear();

			#if CACHE_TO_DISK && (!NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0) // HashSet.GetObjectData is not implemented in older versions
			if(drawerProvidersByInspectorType.Count > 0)
			{
				Serialize();
			}
			#endif

			#if ODIN_INSPECTOR
			// It takes some time for Odin inspector to inject its OdinEditor to the inspector,
			// so rebuild all open inspectors at this point so that any custom editors are using
			// Odin inspector when they should be.
			var manager = InspectorUtility.ActiveManager;
			if(manager == null)
			{
				return;
			}

			foreach(var inspector in manager.ActiveInstances)
			{
				if(Event.current == null)
                {
					inspector.OnNextLayout(inspector.ForceRebuildDrawers);
				}
				else
                {
					inspector.ForceRebuildDrawers();
				}
			}
			#endif
		}

		#if CSHARP_7_3_OR_NEWER
		private async static Task SetupAsync()
		#else
		private static void SetupSync()
		#endif
		{
			#if DEV_MODE
			var timer = new ExecutionTimeLogger();
			timer.Start("DefaultDrawerProviders.SetupAsync");
			#endif

			drawerProvidersByInspectorTypeRebuilt = new ConcurrentDictionary<Type, IDrawerProvider>();

			#if DEV_MODE
			timer.StartInterval("FindDrawerProviderForAttributesInTypes");
			#endif

			var typesToCheck = TypeCache.GetTypesDerivedFrom<IDrawerProvider>().Where(t => !t.IsAbstract);
			#if CSHARP_7_3_OR_NEWER
			await SetupTypesInParallel(typesToCheck);
			#else
			SetupTypes(typesToCheck);
			#endif

			#if DEV_MODE
			timer.FinishInterval();
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			IDrawerProvider defaultDrawerProvider;
			Debug.Assert(drawerProvidersByInspectorTypeRebuilt.TryGetValue(typeof(IInspector), out defaultDrawerProvider));
			Debug.Assert(defaultDrawerProvider != null);
			#endif

			bool allReady = true;
			foreach(var drawerProvider in drawerProvidersByInspectorTypeRebuilt.Values)
			{
				if(!drawerProvider.IsReady)
				{
					allReady = false;
					drawerProvider.OnBecameReady += OnDrawerProviderBecameReady;
				}
			}

			if(allReady)
			{
				// Trigger immediate Repaint for all active inspectors, so they'll rebuild their contents asap.
				if(InspectorUtility.ActiveManager != null)
				{
					foreach(var inspector in InspectorUtility.ActiveManager.ActiveInstances)
					{
						inspector.RefreshView();
					}
				}
			}

			#if DEV_MODE
			timer.FinishAndLogResults();
			#endif

			EditorApplication.delayCall += ApplyRebuiltSetupDataWhenAllProvidersReady;
		}

		private static void OnDrawerProviderBecameReady(IDrawerProvider becameReady)
		{
			// UPDATE: Don't unsubscribe, to allow drawer providers to update their data
			// later on and trigger
			// becameReady.OnBecameReady -= OnDrawerProviderBecameReady;

			foreach(var drawerProvider in drawerProvidersByInspectorTypeRebuilt.Values)
			{
				if(!drawerProvider.IsReady)
				{
					return;
				}
			}

			// Trigger immediate Repaint for all active inspectors, so they'll rebuild their contents asap.
			if(InspectorUtility.ActiveManager != null)
			{
				foreach(var inspector in InspectorUtility.ActiveManager.ActiveInstances)
				{
					inspector.RefreshView();
				}
			}

			EditorApplication.delayCall -= ApplyRebuiltSetupDataWhenAllProvidersReady;
			ApplyRebuiltSetupDataWhenAllProvidersReady();
		}

		#if CSHARP_7_3_OR_NEWER
		private static Task SetupTypesInParallel(IEnumerable<Type> types)
		{
			return Task.WhenAll(types.Select(CreateSetupTypeTask));
		}
		#else
		private static void SetupTypes(IEnumerable<Type> types)
		{
			foreach(var type in types)
			{
				CreateSetupTypeTask(type);
			}
		}
		#endif

		private static Task CreateSetupTypeTask(Type type)
		{
			return Task.Run(() => SetupType(type));
		}

		private static void SetupType(Type type)
		{
			if(!typeof(IDrawerProvider).IsAssignableFrom(type))
			{
				return;
			}

			foreach(var drawerProviderFor in AttributeUtility.GetAttributes<DrawerProviderForAttribute>(type))
			{
				var inspectorType = drawerProviderFor.inspectorType;
				if(inspectorType == null)
				{
					Debug.LogError(drawerProviderFor.GetType().Name + " on class " + type.Name + " NullReferenceException - inspectorType was null!");
					return;
				}

				IDrawerProvider drawerProvider;
				if(drawerProvidersByInspectorTypeRebuilt.TryGetValue(inspectorType, out drawerProvider) && drawerProviderFor.isFallback)
				{
					continue;
				}

				bool reusedExistingInstance = false;
				foreach(var createdDrawerProvider in drawerProvidersByInspectorTypeRebuilt.Values)
				{
					if(createdDrawerProvider.GetType() == type)
					{
						drawerProvidersByInspectorTypeRebuilt[inspectorType] = createdDrawerProvider;
						reusedExistingInstance = true;
						break;
					}
				}

				if(reusedExistingInstance)
				{
					continue;
				}

				#if DEV_MODE && DEBUG_SETUP
				Debug.Log("Creating new DrawerProvider instance of type "+type.Name+" for inspector "+inspectorType.Name);
				#endif

				object instance;
				try
				{
					instance = Activator.CreateInstance(type);
				}
				#if DEV_MODE
				catch(System.Reflection.TargetInvocationException e)
				{
					Debug.LogWarning("Activator.CreateInstance(" + type.FullName + ") " + e);
				#else
				catch(System.Reflection.TargetInvocationException)
				{
				#endif
					return;
				}

				var drawerProviderInstance = (IDrawerProvider)instance;

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(drawerProviderInstance != null);
				#endif

				drawerProvidersByInspectorTypeRebuilt[inspectorType] = drawerProviderInstance;

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(drawerProvidersByInspectorTypeRebuilt[inspectorType] != null);
				#endif
			}
		}

		[CanBeNull]
		public static IDrawerProvider GetForInspector(Type inspectorType)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(IsReady, "!DefaultDrawerProviders.IsReady");
			#endif

			IDrawerProvider drawerProvider;
			if(drawerProvidersByInspectorType.TryGetValue(inspectorType, out drawerProvider))
			{
				return drawerProvider;
			}

			foreach(var provider in drawerProvidersByInspectorType)
			{
				if(provider.Key.IsAssignableFrom(inspectorType))
				{
					return provider.Value;
				}
			}

			return null;
		}
	}
}