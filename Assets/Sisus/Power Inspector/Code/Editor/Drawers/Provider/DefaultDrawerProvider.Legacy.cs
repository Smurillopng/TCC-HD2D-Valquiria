#define SAFE_MODE

#define DEBUG_SETUP_TIME
//#define DEBUG_BUILD_DICTIONARIES_FOR_FIELDS
//#define DEBUG_BUILD_DICTIONARIES_FOR_ENUM_FIELDS
//#define DEBUG_BUILD_DICTIONARIES_FOR_DELEGATE_FIELDS
//#define DEBUG_BUILD_DICTIONARIES_FOR_UNITY_OBJECT_FIELDS
//#define DEBUG_BUILD_DICTIONARIES_FOR_CUSTOM_EDITORS
//#define DEBUG_BUILD_DICTIONARIES_FOR_PROPERTY_DRAWERS
//#define DEBUG_BUILD_DICTIONARIES_FOR_DECORATOR_DRAWERS
//#define DEBUG_BUILD_DICTIONARIES_FOR_NONFALLBACK_DRAWER
//#define DEBUG_BUILD_DICTIONARIES_FOR_COMPONENTS
//#define DEBUG_BUILD_DICTIONARIES_FOR_ASSETS
//#define DEBUG_BUILD_DICTIONARIES_FOR_PLUGINS

#if !UNITY_2023_1_OR_NEWER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Sisus.Attributes;
using Sisus.Compatibility;
using UnityEditor;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Default class responsible for determining which drawer should be used for which Unity Object and class member targets in inspectors.
	/// </summary>
	[DrawerProviderFor(typeof(IInspector), true), Serializable]
	public sealed class DefaultDrawerProvider : DrawerProviderBase
	{
		[CanBeNull]
		private static DefaultDrawerProvider instance;

		private bool isReady;

		[NonSerialized]
		private ThreadSafeSetupData[] setupData;

		[NotNull]
		public static DefaultDrawerProvider Instance
		{
			get
			{
				if(instance == null)
				{
					instance = new DefaultDrawerProvider();
				}

				return instance;
			}
		}

		public override bool IsReady
		{
			get
			{
				return isReady;
			}
		}

		public DefaultDrawerProvider()
		{
			SetupOnBackgroundThread();

			CustomEditorUtility.EditorsUpdated += SetupOnBackgroundThread;

			void SetupOnBackgroundThread()
			{
				if(!CustomEditorUtility.IsReady)
				{
					EditorApplication.delayCall += SetupOnBackgroundThread;
					return;
				}

				#if CSHARP_7_3_OR_NEWER
				Task.Run(async () => await SetupThreaded());
				#else
				SetupThreaded();
				#endif
			}
		}

#if !CSHARP_7_3_OR_NEWER
		private void SetupThreaded()
#else
		private async Task SetupThreaded()
		#endif
		{
			#if DEV_MODE && DEBUG_SETUP_TIME
			var timer = new ExecutionTimeLogger();
			timer.Start("DefaultDrawerProvider.BuildDictionariesThreaded");
			#endif

			const int threadSafeSetupDataCount = ThreadSafeSetupData.UnityCustomEditorsAndDrawersIndex + 1;
			setupData = new ThreadSafeSetupData[threadSafeSetupDataCount];
			for(int n = 0; n < threadSafeSetupDataCount; n++)
			{
				setupData[n] = new ThreadSafeSetupData();
			}

			drawersFor.customEditorAssetDefault = typeof(CustomEditorAssetDrawer);
			drawersFor.customEditorComponentDefault = typeof(CustomEditorComponentDrawer);
			drawersFor.componentDefault = typeof(ComponentDrawer);
			drawersFor.assetDefault = typeof(AssetDrawer);
			drawersFor.classDefault = typeof(ClassDrawer);

			#if DEV_MODE && DEBUG_SETUP_TIME
			timer.StartInterval("Task.WhenAll(GetAllSetupTasks)");
			#endif

			try
			{
				#if !CSHARP_7_3_OR_NEWER
				Task.WhenAll(GetAllSetupTasks(GetAllDrawerTypes())).ConfigureAwait(false);
				#else
				await Task.WhenAll(GetAllSetupTasks(GetAllDrawerTypes())).ConfigureAwait(false);
				#endif
			}
			catch(Exception e)
			{
				// I think that as ThreadAbortException can occur if assembly reloading takes place while the above await is still in progress.
				if(e is ThreadAbortException)
				{
					#if DEV_MODE
					Debug.LogWarning(e);
					#endif

					// set isReady true here because ThreadAbortException automatically gets rethrown at the end of the catch for ThreadAbortExceptions
					// which means that isReady will never be true which means that DefaultDrawerProviders might get stuck waiting for this provider to become ready.
					isReady = true;
				}
				else
				{
					Debug.LogWarning(e);
				}
			}

			#if DEV_MODE && DEBUG_SETUP_TIME
			timer.FinishInterval();
			timer.StartInterval("ThreadSafeSetupData.ToDrawerProviderData");
			#endif

			ThreadSafeSetupData.ToDrawerProviderData(setupData, drawersFor);
			setupData = null;

			#if DEV_MODE && DEBUG_SETUP_TIME
			timer.FinishInterval();
			#endif

			// Make sure that GameObjects have some drawer
			if(drawersFor.gameObject == null)
			{
				drawersFor.gameObject = typeof(GameObjectDrawer);
			}

			if(drawersFor.gameObjectCategorized == null)
			{
				drawersFor.gameObjectCategorized = typeof(CategorizedGameObjectDrawer);
			}

			isReady = true;
			BroadcastOnBecameReady();

			#if DEV_MODE && DEBUG_SETUP_TIME
			timer.FinishAndLogResults();
			#endif

			#if DEV_MODE && PI_ASSERTATIONS && UNITY_EDITOR
			Debug.Assert(!drawersFor.fields.ContainsValue(null));
			Debug.Assert(!drawersFor.fields.ContainsValue(typeof(PropertyDrawerDrawer)), "drawersFor.fields contained a PropertyDrawerDrawer. They should be in drawersFor.propertyDrawersByFieldType instead.");
			Debug.Assert(!drawersFor.fields.ContainsValue(typeof(DecoratorDrawerDrawer)), "drawersFor.fields contained a DecoratorDrawerDrawer.  They should be in drawersFor.decoratorDrawers instead.");

			Debug.Assert(!drawersFor.decoratorDrawers.ContainsValue(null));
			Debug.Assert(!drawersFor.decoratorDrawers.ContainsValue(typeof(PropertyDrawerDrawer)));
			foreach(var propertyDrawerDrawerBySecondType in drawersFor.drawersByAttributeType.Values)
			{
				Debug.Assert(!propertyDrawerDrawerBySecondType.ContainsValue(null));
				Debug.Assert(!propertyDrawerDrawerBySecondType.ContainsValue(typeof(DecoratorDrawerDrawer)));
			}
			Debug.Assert(!drawersFor.propertyDrawerDrawersByFieldType.ContainsValue(null));
			Debug.Assert(!drawersFor.propertyDrawerDrawersByFieldType.ContainsValue(typeof(DecoratorDrawerDrawer)));

			Debug.Assert(!drawersFor.components.ContainsValue(null));
			Debug.Assert(!drawersFor.components.ContainsValue(typeof(CustomEditorAssetDrawer)));
			Debug.Assert(!drawersFor.components.ContainsValue(typeof(AssetDrawer)));

			Debug.Assert(!drawersFor.assets.ContainsValue(null));
			Debug.Assert(!drawersFor.assets.ContainsValue(typeof(CustomEditorComponentDrawer)));
			Debug.Assert(!drawersFor.assets.ContainsValue(typeof(ComponentDrawer)));
			Debug.Assert(!drawersFor.assetsByExtension.ContainsValue(typeof(CustomEditorComponentDrawer)));
			Debug.Assert(!drawersFor.assetsByExtension.ContainsValue(typeof(ComponentDrawer)));
			#endif
		}

		private static IEnumerable<Type> GetAllDrawerTypes()
		{
			return TypeExtensions.GetAllTypesThreadSafe(typeof(IDrawer).Assembly, false, false, true).Where(IsDrawerType);
		}
		
		private static bool IsDrawerType(Type type)
		{
			return typeof(IDrawer).IsAssignableFrom(type);
		}

		#if DEV_MODE && PI_ASSERTATIONS
		private bool AssertAllGUIInstructionTypesImplement([NotNull]IEnumerable<Type> drawerTypes, [NotNull]Type mustImplementInterface, [NotNull]DrawerFromPluginProvider provider, string methodName)
		{
			foreach(var drawerType in drawerTypes)
			{
				if(!mustImplementInterface.IsAssignableFrom(drawerType))
				{
					#if DEV_MODE
					Debug.LogError("Drawer Type "+drawerType.Name+" did not implement "+mustImplementInterface.Name+". "+provider.GetType().Name+"."+methodName+" has issues!");
					#endif
					return false;
				}
			}
			return true;
		}

		private bool AssertAllGUIInstructionTypesImplementOne(IEnumerable<Type> drawerTypes, Type mustImplementInterface1, Type mustImplementInterface2, [NotNull]DrawerFromPluginProvider provider, string methodName)
		{
			foreach(var drawerType in drawerTypes)
			{
				if(!mustImplementInterface1.IsAssignableFrom(drawerType) && !mustImplementInterface2.IsAssignableFrom(drawerType))
				{
					#if DEV_MODE
					Debug.LogError("Drawer Type "+drawerType.Name+" did not implement "+mustImplementInterface1.Name+" or "+mustImplementInterface2.Name+". "+provider.GetType().Name+"."+methodName+" has issues!");
					#endif
					return false;
				}
			}
			return true;
		}
		#endif
		
		/// <summary>
		/// Parallelizable setup task for generating drawers.
		/// PluginProviders have the highest priority and their types will be placed directly into drawersFor.
		/// Setup data from all other sources will be placed to the setupData array sorted by priority.
		/// The final drawersFor state canthen be generated by combining all the data together.
		/// </summary>
		/// <param name="drawerTypes"> List of all types that implement IDrawer in the project. </param>
		/// <returns> Parallelizable collection of tasks. </returns>
		private IEnumerable<Task> GetAllSetupTasks(IEnumerable<Type> drawerTypes)
		{
			// Priority 1
			yield return Task.Run(()=>SetupPluginProviders(drawersFor));

			// Priority 2
			yield return Task.WhenAll(drawerTypes.Select(ExactTypesNonFallbackSetupTask));

			// Priority 3
			yield return Task.WhenAll(drawerTypes.Select(InheritedTypesNonFallbackSetupTask));

			// Priority 4
			yield return Task.WhenAll(CustomEditorUtility.PropertyDrawersByType.Select(BuildDictionariesForPropertyDrawersInNonUnityNamespacesTask));
			yield return Task.WhenAll(CustomEditorUtility.DecoratorDrawersByType.Select(BuildDictionariesForDecoratorDrawersInNonUnityNamespacesTask));
			yield return Task.WhenAll(CustomEditorUtility.CustomEditorsByType.Select(BuildDictionariesForCustomEditorsInNonUnityNamespacesTask));

			// Priority 5
			yield return Task.WhenAll(drawerTypes.Select(ExactTypesFallbackSetupTask));

			// Priority 6
			yield return Task.WhenAll(drawerTypes.Select(InheritedTypesFallbackSetupTask));

			// Priority 7
			yield return Task.WhenAll(CustomEditorUtility.PropertyDrawersByType.Select(BuildDictionariesForPropertyDrawersInUnityNamespacesTask));
			yield return Task.WhenAll(CustomEditorUtility.DecoratorDrawersByType.Select(BuildDictionariesForDecoratorDrawersInUnityNamespacesTask));
			yield return Task.WhenAll(CustomEditorUtility.CustomEditorsByType.Select(BuildDictionariesForCustomEditorsInUnityNamespacesTask));
		}

		#region Priority 1
		private void SetupPluginProviders(DrawerProviderData drawersFor)
		{
			var pluginProviders = DrawerFromPluginProvider.All;
			for(int n = pluginProviders.Length - 1; n >= 0; n--)
			{
				var provider = pluginProviders[n];

				if(!provider.IsActive)
				{
					#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_PLUGINS
					Debug.Log("Skipping " + provider.GetType().Name + " because IsActive was false.");
					#endif
					continue;
				}

				try
				{
					provider.AddFieldDrawer(drawersFor.fields);

					#if DEV_MODE && PI_ASSERTATIONS
					AssertAllGUIInstructionTypesImplement(drawersFor.fields.Values, typeof(IFieldDrawer), provider, "AddFieldDrawer");
					#endif

					provider.AddDecoratorDrawerDrawer(drawersFor.decoratorDrawers);
					
					#if DEV_MODE && PI_ASSERTATIONS
					AssertAllGUIInstructionTypesImplement(drawersFor.decoratorDrawers.Values, typeof(IDecoratorDrawerDrawer), provider, "AddDecoratorDrawerDrawer");
					#endif

					provider.AddPropertyDrawerDrawer(drawersFor.drawersByAttributeType, drawersFor.propertyDrawerDrawersByFieldType);
					
					#if DEV_MODE && PI_ASSERTATIONS
					foreach(var dictionary in drawersFor.drawersByAttributeType.Values)
					{
						AssertAllGUIInstructionTypesImplement(dictionary.Values, typeof(IPropertyDrawerDrawer), provider, "AddPropertyDrawerDrawer");
					}
					AssertAllGUIInstructionTypesImplement(drawersFor.propertyDrawerDrawersByFieldType.Values, typeof(IPropertyDrawerDrawer), provider, "AddPropertyDrawerDrawer");
					#endif
					
					provider.AddComponentDrawer(drawersFor.components);
					
					#if DEV_MODE && PI_ASSERTATIONS
					AssertAllGUIInstructionTypesImplementOne(drawersFor.decoratorDrawers.Values, typeof(IEditorlessComponentDrawer), typeof(ICustomEditorComponentDrawer), provider, "AddComponentDrawer");
					#endif

					provider.AddAssetDrawer(drawersFor.assets, drawersFor.assetsByExtension);

					#if DEV_MODE && PI_ASSERTATIONS
					AssertAllGUIInstructionTypesImplementOne(drawersFor.assets.Values, typeof(IEditorlessAssetDrawer), typeof(ICustomEditorAssetDrawer), provider, "AddAssetDrawer");
					AssertAllGUIInstructionTypesImplementOne(drawersFor.assetsByExtension.Values, typeof(IEditorlessAssetDrawer), typeof(ICustomEditorAssetDrawer), provider, "AddAssetDrawer");
					#endif
				}
				catch(Exception e)
				{
					Debug.LogError(e);
				}
			}
		}
		#endregion

		#region Priority 2
		private Task ExactTypesNonFallbackSetupTask(Type type)
		{
			return Task.Run(()=> BuildDictionariesForExactTypes(setupData[ThreadSafeSetupData.ExactTypesNonFallbackIndex], type, false));
		}
		#endregion

		#region Priority 3
		private Task InheritedTypesNonFallbackSetupTask(Type type)
		{
			return Task.Run(()=> BuildDictionariesForInheritedTypes(setupData[ThreadSafeSetupData.InheritedTypesNonFallbackIndex], type, false));
		}
		#endregion

		#region Priority 4
		private Task BuildDictionariesForPropertyDrawersInNonUnityNamespacesTask(KeyValuePair<Type, Type> attributeOrFieldAndDrawerType)
		{
			return Task.Run(() => BuildDictionariesForPropertyDrawersInNonUnityNamespaces(attributeOrFieldAndDrawerType));
		}
		private void BuildDictionariesForPropertyDrawersInNonUnityNamespaces(KeyValuePair<Type, Type> attributeOrFieldAndDrawerType)
		{
			var drawerType = attributeOrFieldAndDrawerType.Value;
			var attributeOrFieldType = attributeOrFieldAndDrawerType.Key;
			if(!IsInUnityNamespace(drawerType))
			{
				AssignTypeToUsePropertyDrawerDrawer(setupData[ThreadSafeSetupData.NonUnityCustomEditorsAndDrawersIndex], attributeOrFieldType);
			}
		}

		private Task BuildDictionariesForDecoratorDrawersInNonUnityNamespacesTask(KeyValuePair<Type, Type> attributeAndDrawerType)
		{
			return Task.Run(()=> BuildDictionariesForDecoratorDrawersInNonUnityNamespaces(attributeAndDrawerType));
		}
		private void BuildDictionariesForDecoratorDrawersInNonUnityNamespaces(KeyValuePair<Type, Type> attributeAndDrawerType)
		{
			var drawerType = attributeAndDrawerType.Value;
			var attributeType = attributeAndDrawerType.Key;
			if(!IsInUnityNamespace(drawerType) && !DecoratorDrawerAttributeBlacklist.Contains(attributeType))
			{
				AssignTypeToUseDecoratorDrawerDrawer(setupData[ThreadSafeSetupData.NonUnityCustomEditorsAndDrawersIndex], attributeType);
			}
		}

		private Task BuildDictionariesForCustomEditorsInNonUnityNamespacesTask(KeyValuePair<Type, Type> unityObjectAndEditorType)
		{
			return Task.Run(() => BuildDictionariesForCustomEditorsInNonUnityNamespaces(unityObjectAndEditorType));
		}
		private void BuildDictionariesForCustomEditorsInNonUnityNamespaces(KeyValuePair<Type, Type> unityObjectAndEditorType)
		{
			Type editorType = unityObjectAndEditorType.Value;
			if(!IsInUnityNamespace(editorType))
			{
				var drawersFor = setupData[ThreadSafeSetupData.NonUnityCustomEditorsAndDrawersIndex];
				var unityObjectType = unityObjectAndEditorType.Key;
				AssignTypeToUseCustomEditorDrawer(drawersFor.components, drawersFor.assets, unityObjectType);
			}
		}
		#endregion

		#region Priority 5
		private Task ExactTypesFallbackSetupTask(Type type)
		{
			return Task.Run(() => BuildDictionariesForExactTypes(setupData[ThreadSafeSetupData.ExactTypesFallbackIndex], type, true));
		}
		#endregion

		#region Priority 6
		private Task InheritedTypesFallbackSetupTask(Type type)
		{
			return Task.Run(() => BuildDictionariesForInheritedTypes(setupData[ThreadSafeSetupData.InheritedTypesFallbackIndex], type, true));
		}
		#endregion

		#region Priority 7
		private Task BuildDictionariesForPropertyDrawersInUnityNamespacesTask(KeyValuePair<Type, Type> attributeOrFieldAndDrawerType)
		{
			return Task.Run(() => BuildDictionariesForPropertyDrawersInUnityNamespaces(attributeOrFieldAndDrawerType));
		}
		private void BuildDictionariesForPropertyDrawersInUnityNamespaces(KeyValuePair<Type, Type> attributeOrFieldAndDrawerType)
		{
			var drawerType = attributeOrFieldAndDrawerType.Value;
			if(IsInUnityNamespace(drawerType))
			{
				var attributeOrFieldType = attributeOrFieldAndDrawerType.Key;
				AssignTypeToUsePropertyDrawerDrawer(setupData[ThreadSafeSetupData.UnityCustomEditorsAndDrawersIndex], attributeOrFieldType);
			}
		}

		private Task BuildDictionariesForDecoratorDrawersInUnityNamespacesTask(KeyValuePair<Type, Type> attributeAndDrawerType)
		{
			return Task.Run(() => BuildDictionariesForDecoratorDrawersInUnityNamespaces(attributeAndDrawerType));
		}
		private void BuildDictionariesForDecoratorDrawersInUnityNamespaces(KeyValuePair<Type, Type> attributeAndDrawerType)
		{
			var drawerType = attributeAndDrawerType.Value;
			var attributeType = attributeAndDrawerType.Key;
			if(IsInUnityNamespace(drawerType))
			{
				AssignTypeToUseDecoratorDrawerDrawer(setupData[ThreadSafeSetupData.UnityCustomEditorsAndDrawersIndex], attributeType);
			}
		}

		private Task BuildDictionariesForCustomEditorsInUnityNamespacesTask(KeyValuePair<Type, Type> unityObjectAndEditorType)
		{
			return Task.Run(() => BuildDictionariesForCustomEditorsInUnityNamespaces(unityObjectAndEditorType));
		}
		private void BuildDictionariesForCustomEditorsInUnityNamespaces(KeyValuePair<Type, Type> unityObjectAndEditorType)
		{
			var editorType = unityObjectAndEditorType.Value;
			if(IsInUnityNamespace(editorType))
			{
				var drawersFor = setupData[ThreadSafeSetupData.UnityCustomEditorsAndDrawersIndex];
				var unityObjectType = unityObjectAndEditorType.Key;
				AssignTypeToUseCustomEditorDrawer(drawersFor.components, drawersFor.assets, unityObjectType);
			}
		}
		#endregion

		private void BuildDictionariesForExactTypes(ThreadSafeSetupData drawersFor, Type drawerType, bool isFallback)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!drawerType.IsAbstract);
			#endif

			IEnumerable<DrawerForBaseAttribute> attributes;
			if(!Attribute<DrawerForBaseAttribute>.TryGetAll(drawerType, false, out attributes))
			{
				return;
			}

			foreach(var attribute in attributes)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				attribute.AssertDataIsValid(drawerType);
				#endif

				if(attribute.isFallback != isFallback)
				{
					continue;
				}

				var type = attribute.Target;

				if(type != null)
				{
					#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_NONFALLBACK_DRAWER
					if(!isFallback) { Debug.Log(type.Name + " handled by " + drawerType.Name); }
					#endif

					var fieldAttribute = attribute as DrawerForFieldAttribute;
					if(fieldAttribute != null)
					{
						#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_FIELDS
						Debug.Log(type.Name + " handled by " + drawerType.Name);
						#endif
						
						// NEW
						if(typeof(IPropertyDrawerDrawer).IsAssignableFrom(drawerType) && CustomEditorUtility.HasPropertyDrawer(type))
						{
							#if DEV_MODE
							Debug.Log("propertyDrawerDrawersByFieldType["+type.Name+"] = "+drawerType.Name);
							#endif
							this.drawersFor.propertyDrawerDrawersByFieldType.Add(type, drawerType);
							continue;
						}

						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(!this.drawersFor.propertyDrawerDrawersRequiringSerializedProperty.Contains(drawerType), drawerType);
						#endif

						drawersFor.fields[type] = drawerType;
						continue;
					}
					
					var assetAttribute = attribute as DrawerForAssetAttribute;
					if(assetAttribute != null)
					{
						#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_ASSETS
						Debug.Log(type.Name + " handled by " + drawerType.Name);
						#endif

						if(type == Types.UnityObject && assetAttribute.TargetExtendingTypes && assetAttribute.isFallback)
						{
							if(typeof(ICustomEditorAssetDrawer).IsAssignableFrom(drawerType))
							{
								#if DEV_MODE
								Debug.Log("customEditorAssetDefault = " + drawerType.Name);
								#endif
								drawersFor.customEditorAssetDefault = drawerType;
							}
							else
							{
								#if DEV_MODE
								Debug.Log("assetDefault = " + drawerType.Name);
								#endif
								drawersFor.assetDefault = drawerType;
							}
						}
						else
						{
							drawersFor.assets[type] = drawerType;
						}
						continue;
					}

					var componentAttribute = attribute as DrawerForComponentAttribute;
					if(componentAttribute != null)
					{
						if((type == Types.UnityObject || type == Types.Component) && componentAttribute.TargetExtendingTypes && componentAttribute.isFallback)
						{
							if(typeof(ICustomEditorComponentDrawer).IsAssignableFrom(drawerType))
							{
								#if DEV_MODE
								Debug.Log("customEditorComponentDefault = "+drawerType.Name);
								#endif
								drawersFor.customEditorComponentDefault = drawerType;
							}
							else
							{
								#if DEV_MODE
								Debug.Log("componentDefault = " + drawerType.Name);
								#endif
								drawersFor.componentDefault = drawerType;
							}
						}
						else
						{
							#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_COMPONENTS
							Debug.Log(type.Name + " => " + drawerType.Name);
							#endif

							drawersFor.components[type] = drawerType;
						}
						continue;
					}

					var decoratorDrawerAttribute = attribute as DrawerForDecoratorAttribute;
					if(decoratorDrawerAttribute != null)
					{
						foreach(var attributeType in PluginAttributeConverterProvider.GetAttributeTypeAndEachAlias(type))
						{
							#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_DECORATOR_DRAWERS
							Debug.Log(attributeType.FullName + " handled by " + drawerType.Name);
							#endif

							drawersFor.decoratorDrawers[attributeType] = drawerType;
						}
						continue;
					}

					var propertyDrawerAttribute = attribute as DrawerForAttributeAttribute;
					if(propertyDrawerAttribute != null)
					{
						foreach(var attributeType in PluginAttributeConverterProvider.GetAttributeTypeAndEachAlias(type))
						{
							ConcurrentDictionary<Type, Type> drawerTypesByFieldType;
							if(!drawersFor.drawersByAttributeType.TryGetValue(attributeType, out drawerTypesByFieldType))
							{
								drawerTypesByFieldType = new ConcurrentDictionary<Type, Type>();
								drawersFor.drawersByAttributeType[attributeType] = drawerTypesByFieldType;
								if(!drawersFor.drawersByAttributeType.TryAdd(attributeType, drawerTypesByFieldType))
								{
									drawerTypesByFieldType = drawersFor.drawersByAttributeType[attributeType];
								}
							}

							var fieldType = propertyDrawerAttribute.valueType;
							drawerTypesByFieldType[fieldType] = drawerType;

							#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_PROPERTY_DRAWERS
							Debug.Log(attributeType.FullName + " handled by "+drawerType.Name+" with field type " + fieldType.Name);
							#endif

							#if DEV_MODE && PI_ASSERTATIONS
							Debug.Assert(drawerTypesByFieldType[fieldType] == drawerType);
							Debug.Assert(drawersFor.drawersByAttributeType.ContainsKey(attributeType));
							Debug.Assert(drawerTypesByFieldType.ContainsKey(fieldType));
							#endif
						}
						continue;
					}

					var gameObjectDrawerAttribute = attribute as DrawerForGameObjectAttribute;
					if(gameObjectDrawerAttribute != null)
					{
						if(gameObjectDrawerAttribute.requireComponentOnGameObject == null)
						{
							if(gameObjectDrawerAttribute.isCategorizedGameObjectDrawer)
							{
								if(drawersFor.gameObjectCategorized == null || !gameObjectDrawerAttribute.isFallback)
								{
									drawersFor.gameObjectCategorized = drawerType;
								}
							}
							else if(drawersFor.gameObject == null || !gameObjectDrawerAttribute.isFallback)
							{
								drawersFor.gameObject = drawerType;
							}
						}
						else
						{
							drawersFor.gameObjectByComponent[gameObjectDrawerAttribute.requireComponentOnGameObject] = drawerType;
						}
					}
					#if DEV_MODE
					else { Debug.LogError("Unrecognized DrawerForBaseAttribute type: "+attribute.GetType().Name); }
					#endif
				}
				else
				{
					var extensionAttribute = attribute as DrawerByExtensionAttribute;
					if(extensionAttribute != null)
					{
						var extension = extensionAttribute.fileExtension;

						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(!string.IsNullOrEmpty(extension), "fileExtension was null or empty on DrawerByExtension attribute of " + drawerType.Name);
						#endif

						#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_ASSETS
						Debug.Log("Asset extension \"" + extension + "\" handled by " + drawerType.Name);
						#endif

						drawersFor.assetsByExtension[extension] = drawerType;

						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(drawersFor.assetsByExtension.ContainsKey(extension));
						#endif
					}
					#if DEV_MODE
					else { Debug.LogWarning("Ignoring Attribute "+attribute.GetType().Name+" because it had a null Type"); }
					#endif
				}
			}
		}

		private void BuildDictionariesForInheritedTypes(ThreadSafeSetupData drawersFor, Type drawerType, bool isFallback)
		{
			IEnumerable<DrawerForBaseAttribute> attributes;
			if(!Attribute<DrawerForBaseAttribute>.TryGetAll(drawerType, false, out attributes))
			{
				return;
			}

			foreach(var attribute in attributes)
			{
				if(attribute.isFallback != isFallback)
				{
					continue;
				}

				if(!attribute.TargetExtendingTypes)
				{
					continue;
				}

				var type = attribute.Target;

				// If has no target type or target type is a value type then there are no inhertied types that need to be added for these drawers.
				if(type == null || type.IsValueType)
				{
					continue;
				}
				
				#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_NONFALLBACK_DRAWER
				if(!isFallback) { Debug.Log(type.Name + " inherited types handled by " + drawerType.Name); }
				#endif

				var fieldAttribute = attribute as DrawerForFieldAttribute;
				if(fieldAttribute != null)
				{
					IEnumerable<Type> subjectTypes;
					if(type == Types.Enum)
					{
						subjectTypes = TypeExtensions.EnumTypesIncludingInvisible;
					}
					else if(type.IsInterface)
					{
						subjectTypes = type.GetImplementingTypes(true, false);
					}
					else
					{
						subjectTypes = type.GetExtendingTypes(true, false);
					}

					foreach(var subjectType in subjectTypes)
					{
						if(typeof(IPropertyDrawerDrawer).IsAssignableFrom(drawerType) && CustomEditorUtility.HasPropertyDrawer(subjectType))
						{
							#if DEV_MODE
							Debug.Log("propertyDrawerDrawersByFieldType["+ subjectType.Name+"] = "+drawerType.Name);
							#endif
							base.drawersFor.propertyDrawerDrawersByFieldType.Add(subjectType, drawerType);
							continue;
						}

						#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_ENUM_FIELDS
						if(drawerType == typeof(EnumDrawer))
						{
							Debug.Log(subjectType.FullName + " handled by "+drawerType.Name+" because it's a base type of "+type.FullName);
						}
						#endif
						#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_DELEGATE_FIELDS
						if(drawerType == typeof(DelegateDrawer))
						{
							Debug.Log(subjectType.FullName + " handled by "+drawerType.Name+" because it's a base type of "+type.FullName);
						}
						#endif
						#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_UNITY_OBJECT_FIELDS
						if(drawerType == typeof(ObjectReferenceDrawer))
						{
							Debug.Log(subjectType.FullName + " handled by "+drawerType.Name+" because it's a base type of "+type.FullName);
						}
						#endif
						#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_FIELDS
						if(drawerType != typeof(EnumDrawer) && drawerType != typeof(DelegateDrawer) && drawerType != typeof(ObjectReferenceDrawer))
						{
							var sb = new System.Text.StringBuilder();
							StringUtils.ToString(subjectType, '.', sb);
							sb.Append(" handled by ");
							StringUtils.ToString(drawerType, '.', sb);
							sb.Append(" because it's a base type of ");
							StringUtils.ToString(type, '.', sb);
							Debug.Log(sb.ToString());
						}
						#endif

						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(!base.drawersFor.propertyDrawerDrawersRequiringSerializedProperty.Contains(drawerType));
						#endif

						drawersFor.fields[subjectType] = drawerType;
					}
					continue;
				}

				var decoratorDrawerAttribute = attribute as DrawerForDecoratorAttribute;
				if(decoratorDrawerAttribute != null)
				{
					foreach(var attributeType in PluginAttributeConverterProvider.GetAttributeTypeAndEachAlias(type))
					{
						foreach(var subjectType in attributeType.GetExtendingTypes(false))
						{
							#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_DECORATOR_DRAWERS
							Debug.Log(subjectType.FullName + " handled by " + drawerType.Name + " because it's a base type of " + attributeType.FullName);
							#endif

							drawersFor.decoratorDrawers[subjectType] = drawerType;
						}
					}
					continue;
				}

				var propertyDrawerAttribute = attribute as DrawerForAttributeAttribute;
				if(propertyDrawerAttribute != null)
				{
					foreach(var attributeType in PluginAttributeConverterProvider.GetAttributeTypeAndEachAlias(type))
					{
						foreach(var subjectType in attributeType.GetExtendingTypes(false))
						{
							ConcurrentDictionary<Type, Type> drawerTypesByFieldType;
							if(!drawersFor.drawersByAttributeType.TryGetValue(subjectType, out drawerTypesByFieldType))
							{
								drawerTypesByFieldType = new ConcurrentDictionary<Type, Type>();
								if(!drawersFor.drawersByAttributeType.TryAdd(subjectType, drawerTypesByFieldType))
								{
									drawerTypesByFieldType = drawersFor.drawersByAttributeType[subjectType];
								}
							}

							var fieldType = propertyDrawerAttribute.valueType;
							drawersFor.propertyDrawerDrawersByFieldType[fieldType] = drawerType;

							#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_PROPERTY_DRAWERS
							Debug.Log(subjectType.Name+" handled by "+drawerType.Name+" with field type "+fieldType.Name+" because it's a base type of "+attributeType.FullName);
							#endif

							#if DEV_MODE && PI_ASSERTATIONS
							Debug.Assert(drawerTypesByFieldType[fieldType] == drawerType);
							#endif

							#if DEV_MODE && PI_ASSERTATIONS
							Debug.Assert(drawersFor.drawersByAttributeType.ContainsKey(subjectType));
							Debug.Assert(drawerTypesByFieldType.ContainsKey(fieldType));
							#endif
						}
					}
					continue;
				}

				var componentAttribute = attribute as DrawerForComponentAttribute;
				if(componentAttribute != null)
				{
					// UPDATE: This is now handled via componentDefault and customEditorComponentDefault instead
					if((type == Types.UnityObject || type == Types.Component) && componentAttribute.TargetExtendingTypes && componentAttribute.isFallback)
					{
						continue;
					}
					
					var subjectTypes = type.IsInterface ? type.GetImplementingComponentTypes(true) : type.GetExtendingComponentTypes(true);
					foreach(var subjectType in subjectTypes)
					{
						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(subjectType.IsComponent(), "DrawerForComponent attribute subject has to be Component type: " + type.Name);
						#endif

						drawersFor.components[subjectType] = drawerType;
					}
					continue;
				}

				var assetAttribute = attribute as DrawerForAssetAttribute;
				if(assetAttribute != null)
				{
					// UPDATE: This is now handled via assetDefault and customEditorAssetDefault instead
					if(type == Types.UnityObject && assetAttribute.TargetExtendingTypes && assetAttribute.isFallback)
					{
						continue;
					}

					var subjectTypes = type.IsInterface ? type.GetImplementingUnityObjectTypes(true) : type.GetExtendingUnityObjectTypes(true);
					foreach(var subjectType in subjectTypes)
					{
						if(subjectType.IsComponent())
						{
							#if DEV_MODE && PI_ASSERTATIONS
							Debug.Assert(!type.IsInterface, "DrawerForAsset attribute subject can not be Component type: " + type.Name);
							#endif
							continue;
						}
						
						#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_ASSETS
						Debug.Log(subjectType.Name+" handled by "+drawerType.Name);
						#endif

						drawersFor.assets[subjectType] = drawerType;
					}
				}
			}
		}

		private void AssignTypeToUseCustomEditorDrawer(ConcurrentDictionary<Type, Type> componentDrawers, ConcurrentDictionary<Type, Type> assetDrawers, Type targetType)
		{
			if(targetType.IsComponent())
			{
				#if DEV_MODE && (DEBUG_BUILD_DICTIONARIES_FOR_COMPONENTS || DEBUG_BUILD_DICTIONARIES_FOR_CUSTOM_EDITORS)
				Debug.Log(targetType.Name+" => CustomEditorComponentDrawer");
				#endif

				componentDrawers[targetType] = typeof(CustomEditorComponentDrawer);
			}
			else
			{
				if(Types.TextAsset.IsAssignableFrom(targetType))
				{
					#if DEV_MODE && (DEBUG_BUILD_DICTIONARIES_FOR_ASSETS || DEBUG_BUILD_DICTIONARIES_FOR_CUSTOM_EDITORS)
					Debug.Log(targetType.Name+" => CustomEditorTextAssetDrawer");
					#endif

					assetDrawers[targetType] = typeof(CustomEditorTextAssetDrawer); 
				}
				else
				{
					#if DEV_MODE && (DEBUG_BUILD_DICTIONARIES_FOR_ASSETS || DEBUG_BUILD_DICTIONARIES_FOR_CUSTOM_EDITORS)
					Debug.Log(targetType.Name+" => CustomEditorAssetDrawer");
					#endif

					assetDrawers[targetType] = typeof(CustomEditorAssetDrawer);
				}
			}
		}

		private void AssignTypeToUseDecoratorDrawerDrawer(ThreadSafeSetupData drawersFor, Type attributeType)
		{
			#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_DECORATOR_DRAWERS
			Debug.Log(attributeType.Name+ " (DecoratorDrawer) with default field type System.Object handled by DecoratorDrawerDrawer.");
			#endif

			drawersFor.decoratorDrawers[attributeType] = typeof(DecoratorDrawerDrawer);
		}

		private void AssignTypeToUsePropertyDrawerDrawer(ThreadSafeSetupData drawersFor, Type attributeOrFieldType)
		{
			#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_PROPERTY_DRAWERS
			Debug.Log("AssignTypeToUsePropertyDrawerDrawer("+ attributeOrFieldType.Name+")...");
			#endif

			if(attributeOrFieldType.IsSubclassOf(Types.PropertyAttribute))
			{
				ConcurrentDictionary<Type, Type> drawerTypesByFieldType;
				if(drawersFor.drawersByAttributeType.TryGetValue(attributeOrFieldType, out drawerTypesByFieldType))
				{
					if(drawerTypesByFieldType.ContainsKey(Types.SystemObject))
					{
						#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_PROPERTY_DRAWERS
						Debug.LogWarning("AssignTypeToUsePropertyDrawerDrawer("+ attributeOrFieldType.Name+ ") - won't add because type already using " + drawerTypesByFieldType[Types.SystemObject].Name);
						#endif
						return;
					}
				}
				else
				{
					drawerTypesByFieldType = new ConcurrentDictionary<Type, Type>();
					drawersFor.drawersByAttributeType.TryAdd(attributeOrFieldType, drawerTypesByFieldType);
				}
				
				#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_PROPERTY_DRAWERS
				Debug.Log(attributeOrFieldType.Name+" (PropertyDrawer) default field type System.Object handled by PropertyDrawerDrawer.");
				#endif
				
				// all PropertyDrawer backed fields default to PropertyDrawerDrawer
				drawerTypesByFieldType[Types.SystemObject] = typeof(PropertyDrawerDrawer);
			}
			else if(!drawersFor.propertyDrawerDrawersByFieldType.ContainsKey(attributeOrFieldType))
			{
				#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_PROPERTY_DRAWERS
				Debug.Log("AssignTypeToUsePropertyDrawerDrawer("+ attributeOrFieldType.Name + "): adding using PropertyDrawerDrawer");
				#endif

				// NEW: If there already is a non-fallback drawer specified using DrawerForFieldAttribute for the attribute / field, then prefer that over the default PropertyDrawerDrawer.
				// This makes it possible to create drawers that replace / enhance property drawers included in the project.
				Type customDrawerType;
				if(drawersFor.fields.TryGetValue(attributeOrFieldType, out customDrawerType))
				{
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(customDrawerType != null);
					#endif

					var drawerForFieldAttributes = Attribute<DrawerForFieldAttribute>.Get(customDrawerType);
					if(drawerForFieldAttributes != null && !drawerForFieldAttributes.isFallback)
					{
						#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_PROPERTY_DRAWERS
						Debug.LogWarning("Won't use PropertyDrawer for " + attributeOrFieldType.Name + " because non-fallback drawer " + customDrawerType.Name + " is already used for the field.");
						#endif
						return;
					}
				}

				// all PropertyDrawers backed fields default to PropertyDrawerDrawer
				drawersFor.propertyDrawerDrawersByFieldType.TryAdd(attributeOrFieldType, typeof(PropertyDrawerDrawer));
			}
			#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_PROPERTY_DRAWERS
			else { Debug.Log("AssignTypeToUsePropertyDrawerDrawer("+ attributeOrFieldType.Name + ") - won't add because type already using "+ drawersFor.propertyDrawerDrawersByFieldType[attributeOrFieldType].Name); }
			#endif
		}

		private bool IsInUnityNamespace(Type type)
		{
			return TypeExtensions.IsInUnityNamespaceThreadSafe(type);
		}

		// to do: alt way to handle threaded setup would be to use just one set of dictionaries with this as key instead of Type
		public struct PrioritizedType
		{
			/// <summary>
			/// Smaller numbers are higher in priority.
			/// </summary>
			private readonly int priority;

			[NotNull]
			private readonly Type type;

			public PrioritizedType(int priority, [NotNull]Type type)
			{
				this.priority = priority;
				this.type = type;
			}

			public bool IsHigherPriority(PrioritizedType than)
			{
				return priority < than.priority;
			}

			public override int GetHashCode()
			{
				return type.GetHashCode();
			}

			public static implicit operator Type(PrioritizedType prioritizedType)
			{
				return prioritizedType.type;
			}
		}

		private class ThreadSafeSetupData
		{
			public const int ExactTypesNonFallbackIndex = 0;
			public const int InheritedTypesNonFallbackIndex = 1;
			public const int NonUnityCustomEditorsAndDrawersIndex = 2;
			public const int ExactTypesFallbackIndex = 3;
			public const int InheritedTypesFallbackIndex = 4;
			public const int UnityCustomEditorsAndDrawersIndex = 5;

			public ConcurrentDictionary<Type, Type> fields = new ConcurrentDictionary<Type, Type>();
			public ConcurrentDictionary<Type, Type> decoratorDrawers = new ConcurrentDictionary<Type, Type>();
			public ConcurrentDictionary<Type, ConcurrentDictionary<Type, Type>> drawersByAttributeType = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, Type>>();
			public ConcurrentDictionary<Type, Type> propertyDrawerDrawersByFieldType = new ConcurrentDictionary<Type, Type>();
			public ConcurrentDictionary<Type, Type> components = new ConcurrentDictionary<Type, Type>();
			public ConcurrentDictionary<Type, Type> assets = new ConcurrentDictionary<Type, Type>();
			public ConcurrentDictionary<string, Type> assetsByExtension = new ConcurrentDictionary<string, Type>();

			public volatile Type gameObject;
			public volatile Type gameObjectCategorized;
			public readonly ConcurrentDictionary<Type, Type> gameObjectByComponent = new ConcurrentDictionary<Type, Type>();
			public volatile Type componentDefault;
			public volatile Type customEditorComponentDefault;
			public volatile Type assetDefault;
			public volatile Type customEditorAssetDefault;

			public static void ToDrawerProviderData(ThreadSafeSetupData[] setupData, DrawerProviderData result)
			{
				for(int n = 0, count = setupData.Length; n < count; n++)
				{
					var data = setupData[n];

					if(result.gameObject == null)
					{
						result.gameObject = data.gameObject;
					}
					if(result.gameObjectCategorized == null)
					{
						result.gameObjectCategorized = data.gameObjectCategorized;
					}
					foreach(var add in data.gameObjectByComponent)
					{
						if(!result.gameObjectByComponent.ContainsKey(add.Key))
						{
							result.gameObjectByComponent.Add(add.Key, add.Value);
						}
					}
					if(result.componentDefault == null)
					{
						result.componentDefault = data.componentDefault;
					}
					if(result.customEditorComponentDefault == null)
					{
						result.customEditorComponentDefault = data.customEditorComponentDefault;
					}
					if(result.assetDefault == null)
					{
						result.assetDefault = data.assetDefault;
					}
					if(result.customEditorAssetDefault == null)
					{
						result.customEditorAssetDefault = data.customEditorAssetDefault;
					}

					foreach(var add in data.fields)
					{
						if(!result.fields.ContainsKey(add.Key))
						{
							result.fields.Add(add.Key, add.Value);
						}
					}
					foreach(var add in data.decoratorDrawers)
					{
						if(!result.decoratorDrawers.ContainsKey(add.Key))
						{
							result.decoratorDrawers.Add(add.Key, add.Value);
						}
					}
					foreach(var add in data.drawersByAttributeType)
					{
						Dictionary<Type, Type> drawerTypesByFieldType;
						if(!result.drawersByAttributeType.TryGetValue(add.Key, out drawerTypesByFieldType))
						{
							drawerTypesByFieldType = new Dictionary<Type, Type>();
							result.drawersByAttributeType.Add(add.Key, drawerTypesByFieldType);
						}

						foreach(var add2 in add.Value)
						{
							if(!drawerTypesByFieldType.ContainsKey(add2.Key))
							{
								if(drawerTypesByFieldType.ContainsKey(Types.SystemObject))
								{
									#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_PROPERTY_DRAWERS
									Debug.LogWarning("AssignTypeToUsePropertyDrawerDrawer("+ add2.Key.Name+ ") - won't add because type already using "+ drawerTypesByFieldType[Types.SystemObject].Name);
									#endif
									continue;
								}
								drawerTypesByFieldType.Add(add2.Key, add2.Value);
							}
						}
					}
					foreach(var add in data.propertyDrawerDrawersByFieldType)
					{
						if(!result.propertyDrawerDrawersByFieldType.ContainsKey(add.Key))
						{
							result.propertyDrawerDrawersByFieldType.Add(add.Key, add.Value);
						}
					}
					foreach(var add in data.components)
					{
						if(!result.components.ContainsKey(add.Key))
						{
							#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_COMPONENTS
							Debug.Log($"{add.Key.Name} is handled by {add.Value.Name}");
							#endif

							result.components.Add(add.Key, add.Value);
						}
						#if DEV_MODE && DEBUG_BUILD_DICTIONARIES_FOR_COMPONENTS
						else { Debug.LogWarning($"{add.Key.Name} already handled by {result.components[add.Key].Name} so won't assign to use {add.Value.Name}."); }
						#endif
					}
					foreach(var add in data.assets)
					{
						if(!result.assets.ContainsKey(add.Key))
						{
							result.assets.Add(add.Key, add.Value);
						}
					}
					foreach(var add in data.assetsByExtension)
					{
						if(!result.assetsByExtension.ContainsKey(add.Key))
						{
							result.assetsByExtension.Add(add.Key, add.Value);
						}
					}
				}
			}
		}
	}
}
#endif