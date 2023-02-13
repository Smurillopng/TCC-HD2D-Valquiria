#define SAFE_MODE

//#define DEBUG_DEFAULT_VALUE
#define DEBUG_DEFAULT_VALUE_EXCEPTIONS
//#define DEBUG_GENERATE_FILE_LIST_OF_ALL_TYPES
//#define DEBUG_GET_TYPE
//#define DEBUG_CONTAINS_OBJECT_REFERENCES
//#define DEBUG_SKIP_TYPE
//#define DEBUG_SKIP_ASSEMBLY
//#define DEBUG_GET_INSPECTOR_VIEWABLES

using System;
using System.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;
using UnityEditor;

#if UNITY_2017_3_OR_NEWER
using UnityEditor.Compilation;
using Assembly = System.Reflection.Assembly;
using AssemblyFlags = UnityEditor.Compilation.AssemblyFlags;
#endif

namespace Sisus
{
	[InitializeOnLoad]
	public static class TypeExtensions
	{
		internal static int createInstanceRecursiveCallCount = 0;
		private const int MaxRecursiveCallCount = 25;

		private static SetupPhase setupDone = SetupPhase.Unstarted;

		/// <summary>
		/// Alphabetically sorted list of all visible exported types (including abstract ones)
		/// </summary>
		private static Type[] allVisibleTypes;

		//labels for all types in the the types array, in the same order
		private static string[] allVisibleTypeLabels;

		/// <summary>
		/// List of all exported but invisible types (non-public type or a non-public nested type such that any of the enclosing types are non-public). Includes abstract types also.
		/// </summary>
		private static Type[] allInvisibleTypes;

		/// <summary>
		/// List of all visible types that are not UnityEngine.Object types
		/// </summary>
		private static Type[] allNonUnityObjectTypes;

		/// <summary>
		/// List of all visible abstract types
		/// </summary>
		private static Type[] abstractTypes;

		/// <summary>
		/// List of all invisible abstract types
		/// </summary>
		private static Type[] invisibleAbstractTypes;

		/// <summary>
		/// List of all visible and non-abstract Component types
		/// </summary>
		private static Type[] componentTypes;

		/// <summary>
		/// List of all visible and non-abstract ScriptableObject types, excluding Editor types
		/// </summary>
		private static Type[] scriptableObjectTypes;

		/// <summary>
		/// List of all visible and non-abstract Editor types
		/// </summary>
		private static Type[] editorTypes;

		/// <summary>
		/// List of all invisible and non-abstract Editor types
		/// </summary>
		private static Type[] invisibleEditorTypes;

		/// <summary>
		/// List of all visible and non-abstract UnityObject.Engine types excluding Component types
		/// </summary>
		private static Type[] otherUnityObjectTypes;

		/// <summary>
		/// List of all visible and non-abstract class types excluding UnityEngine.Object types
		/// </summary>
		private static Type[] nonUnityObjectClassTypes;

		private static Type[] invisibleNonUnityObjectClassTypes;

		/// <summary>
		/// List of all visible enum types
		/// </summary>
		private static Type[] visibleEnumTypes;

		/// <summary>
		/// List of all invisible enum types.
		/// </summary>
		private static Type[] enumTypesIncludingInvisible;

		private static readonly Dictionary<string, KeyValuePair<string, Type>[]> enumTypesByName = new Dictionary<string, KeyValuePair<string, Type>[]>(50);
		private static readonly Dictionary<string, KeyValuePair<string, Type>[]> TypesByName = new Dictionary<string, KeyValuePair<string, Type>[]>(1000);
		private static readonly Dictionary<Type, string> TypePopupMenuPaths = new Dictionary<Type, string>();
		private static readonly Dictionary<Type, string> TypeFullNames = new Dictionary<Type, string>();
		private static readonly Dictionary<Type, string> TypeNames = new Dictionary<Type, string>();
		private static readonly Dictionary<Assembly, Type[]> HiddenTypesByAssembly = new Dictionary<Assembly, Type[]>(100);
		private static readonly BiDictionary<Type, int> TypeIds = new BiDictionary<Type, int>();

		private static readonly HashSet<string> generatedFields = new HashSet<string>();
		private static readonly HashSet<string> generatedProperties = new HashSet<string>();
		private static readonly HashSet<string> generatedMethods = new HashSet<string>(); // merge all MethodInfo under one drawer? Add some kind of control to change which overload to use? Maybe number tabs [1][2][3]...?

		private static readonly object threadLock = new object();

		#if UNITY_2017_3_OR_NEWER
		private static readonly ConcurrentDictionary<string, byte> EditorOnlyAssemblyNames;
		#endif

		/// <summary>
		/// this is initialized on load due to the usage of the InitializeOnLoad attribute
		/// </summary>
		[UsedImplicitly]
		static TypeExtensions()
		{
			#if UNITY_2017_3_OR_NEWER
			EditorOnlyAssemblyNames = GetEditorOnlyAssemblyNamesNotThreadSafe();
			#endif

			EditorApplication.delayCall += Setup;
		}

		public static bool IsReady
		{
			get
			{
				lock(threadLock)
				{
					return setupDone == SetupPhase.Done;
				}
			}
		}

		/// <summary>
		/// Gets a list of all visible types (including abstract ones).
		/// </summary>
		/// <value>
		/// List of all visible types from all assemblies.
		/// </value>
		public static IEnumerable<Type> AllVisibleTypes
		{
			get
			{
				if(!IsReady)
				{
					return GetAllTypesThreadSafe(true, false, true);
				}
				return allVisibleTypes;
			}
		}

		/// <summary>
		/// Gets a list of all visible enum types.
		/// </summary>
		/// <value>
		/// All visible enum types.
		/// </value>
		public static IEnumerable<Type> VisibleEnumTypes
		{
			get
			{
				if(!IsReady)
				{
					return GetAllTypesThreadSafe(false, false, true).Where((type) => type.IsEnum);
				}
				return visibleEnumTypes;
			}
		}

		/// <summary>
		/// Gets a list of all enum types.
		/// </summary>
		/// <value>
		/// All enum types.
		/// </value>
		public static IEnumerable<Type> EnumTypesIncludingInvisible
		{
			get
			{
				if(!IsReady)
				{
					return GetAllTypesThreadSafe(false, true, true).Where((type) => type.IsEnum);
				}
				return enumTypesIncludingInvisible;
			}
		}

		/// <summary>
		/// Gets a list of all concrete and visible non-UnityEngine.Object types
		/// </summary>
		/// <value>
		/// list of all non-abstract and visible non-UnityEngine.Object types
		/// </value>
		public static IEnumerable<Type> AllNonUnityObjectTypes
		{
			get
			{
				if(!IsReady)
				{
					return GetAllTypesThreadSafe(false, false, true).Where((type) => !type.IsUnityObject());
				}
				return allNonUnityObjectTypes;
			}
		}

		public static string[] MenuLabelsForAllVisibleTypes
		{
			get
			{
				if(!IsReady)
				{
					return GetLabelsThreadSafe(GetAllTypesThreadSafe(true, false, true)).ToArray();
				}
				return allVisibleTypeLabels;
			}
		}

		public static Type[] ComponentTypes
		{
			get
			{
				if(!IsReady)
				{
					return GetAllTypesThreadSafe(typeof(Component).Assembly, false, false, true).Where((type) => type.IsComponent()).ToArray();
				}
				return componentTypes;
			}
		}


		public static HashSet<Type> GetInvalidComponentTypesThreadSafe()
		{
			var assemblies = WhereEditorOnly(GetAllAssembliesThreadSafe(typeof(Component).Assembly));
			var types = assemblies.SelectMany((assembly) => assembly.GetLoadableTypes(false).Where(IsComponent));
			return new HashSet<Type>(types);
		}

		public static bool IsInUnityNamespaceThreadSafe([NotNull] Type type)
		{
			var assemblyName = type.Assembly.GetName().Name;
			if(assemblyName.StartsWith("Unity", StringComparison.Ordinal))
			{
				string start = assemblyName.Substring(5);
				if(start.StartsWith("Engine.", StringComparison.Ordinal) || start.StartsWith("Editor.", StringComparison.Ordinal) || start.StartsWith(".", StringComparison.Ordinal) || start.Equals("Editor"))
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsUnityAssemblyThreadSafe([NotNull] Assembly assembly)
		{
			var assemblyName = assembly.GetName().Name;
			if(assemblyName.StartsWith("Unity", StringComparison.Ordinal))
			{
				string sub = assemblyName.Substring(5);
				if(sub.StartsWith(".", StringComparison.Ordinal) || sub.StartsWith("Engine.", StringComparison.Ordinal) || sub.StartsWith("Editor.", StringComparison.Ordinal) || sub.Equals("Editor"))
				{
					return true;
				}
			}
			return false;
		}

		public static IEnumerable<Assembly> GetAllAssembliesThreadSafe()
		{
			var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();

			HashSet<string> ignoredAssemblies;
			var ignoredAssembliesFilePath = new StackTrace(true).GetFrame(0).GetFileName();
			ignoredAssembliesFilePath = ignoredAssembliesFilePath.Substring(0, ignoredAssembliesFilePath.Length - ".cs".Length);
			ignoredAssembliesFilePath += ".IgnoredAssemblies.txt";
			if(File.Exists(ignoredAssembliesFilePath))
			{
				ignoredAssemblies = new HashSet<string>(File.ReadAllLines(ignoredAssembliesFilePath));
			}
			else
			{
				ignoredAssemblies = new HashSet<string>();
			}

			for(int n = allAssemblies.Length - 1; n >= 0; n--)
			{
				var assembly = allAssemblies[n];

				// skip dynamic assemblies to prevent NotSupportedException from being thrown when calling GetTypes
				#if !UNITY_2020_2_OR_NEWER && (NET_2_0 || NET_2_0_SUBSET || NET_STANDARD_2_0)
				if(assembly.ManifestModule is System.Reflection.Emit.ModuleBuilder)
				#else
				if(assembly.IsDynamic)
				#endif
				{
					continue;
				}

				if(ignoredAssemblies.Contains(assembly.GetName().Name))
				{
					continue;
				}

				yield return assembly;
			}
		}

		public static IEnumerable<Assembly> GetAllAssembliesThreadSafe(Assembly mustReferenceAssembly)
		{
			var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();

			HashSet<string> ignoredAssemblies;
			var ignoredAssembliesFilePath = new StackTrace(true).GetFrame(0).GetFileName();
			ignoredAssembliesFilePath = ignoredAssembliesFilePath.Substring(0, ignoredAssembliesFilePath.Length - ".cs".Length);
			ignoredAssembliesFilePath += ".IgnoredAssemblies.txt";
			if(File.Exists(ignoredAssembliesFilePath))
			{
				ignoredAssemblies = new HashSet<string>(File.ReadAllLines(ignoredAssembliesFilePath));
			}
			else
			{
				ignoredAssemblies = new HashSet<string>();
			}

			string mustReferenceName = mustReferenceAssembly.GetName().Name;

			for(int n = allAssemblies.Length - 1; n >= 0; n--)
			{
				var assembly = allAssemblies[n];

				// skip dynamic assemblies to prevent NotSupportedException from being thrown when calling GetTypes
				#if NET_2_0 || NET_2_0_SUBSET || NET_STANDARD_2_0
				if(assembly.ManifestModule is System.Reflection.Emit.ModuleBuilder)
				#else
				if(assembly.IsDynamic)
				#endif
				{
					continue;
				}

				if(ignoredAssemblies.Contains(assembly.GetName().Name))
				{
					continue;
				}

				if(assembly == mustReferenceAssembly)
                {
					yield return assembly;
					continue;
				}

				var referencedAssemblies = assembly.GetReferencedAssemblies();
                for(int r = referencedAssemblies.Length - 1; r >= 0; r--)
                {
					if(string.Equals(referencedAssemblies[r].Name, mustReferenceName))
                    {
						yield return assembly;
						break;
					}
                }
			}
		}

		public static IEnumerable<Type> GetAllTypesThreadSafe(bool includeAbstract = true, bool includeInvisible = false, bool includeDuplicates = true)
		{
			if(!includeDuplicates)
			{
				return UniqueTypesOnly(GetAllTypesThreadSafe(includeAbstract, includeInvisible, true));
			}

			if(includeAbstract)
			{
				if(includeInvisible)
                {
					return GetAllAssembliesThreadSafe().SelectMany((assembly) => assembly.GetLoadableTypes(false));
				}
				return GetAllAssembliesThreadSafe().SelectMany((assembly) => assembly.GetLoadableTypes(true));
			}

			if(includeInvisible)
			{
				return GetAllAssembliesThreadSafe().SelectMany((assembly) => assembly.GetLoadableTypes(false).Where(TypeIsConcrete));
			}

			return GetAllAssembliesThreadSafe().SelectMany((assembly) => assembly.GetLoadableTypes(true).Where(TypeIsConcrete));
		}

		public static IEnumerable<Type> GetAllTypesThreadSafe(Assembly mustReferenceAssembly, bool includeAbstract = true, bool includeInvisible = false, bool includeDuplicates = true)
		{
			if(!includeDuplicates)
			{
				return UniqueTypesOnly(GetAllTypesThreadSafe(mustReferenceAssembly, includeAbstract, includeInvisible, true));
			}

			if(includeAbstract)
			{
				return GetAllAssembliesThreadSafe(mustReferenceAssembly).SelectMany((assembly) => assembly.GetLoadableTypes(!includeInvisible));
			}

			return GetAllAssembliesThreadSafe(mustReferenceAssembly).SelectMany((assembly) => assembly.GetLoadableTypes(!includeInvisible).Where(TypeIsConcrete));
		}

		public static IEnumerable<Type> GetExtendingTypesThreadSafeExceptionSafeSlow(Type baseType, bool includeAbstract = true, bool includeInvisible = false, bool includeDuplicates = true)
		{
			Func<Type, bool> isSubclassOfTarget;
			if(baseType.IsGenericTypeDefinition)
			{
				isSubclassOfTarget = (t) => t.IsSubclassOfUndeclaredGeneric(baseType);
			}
			else
			{
				isSubclassOfTarget = (t) => t.IsSubclassOf(baseType);
			}

			foreach(var type in GetAllTypesThreadSafe(baseType.Assembly, includeAbstract, includeInvisible, includeDuplicates))
			{
				try
				{
					if(!isSubclassOfTarget(type))
					{
						continue;
					}
				}
				catch(Exception e)
				{
					string assemblyName = type.Assembly.GetName().Name;
					switch(assemblyName)
					{
						case "Assembly-CSharp":
							Debug.LogWarning("An exception occurred while loading type " + type.FullName + ". Please make the class doesn't have attributes with unsafe code in their constructors.\n" + e);
							break;
						case "UnityEngine":
							Debug.LogWarning("An exception occurred while loading type " + type.FullName + " in UnityEngine assembly. Please contact support via support@sisus.co.\n" + e);
							break;
						default:
							Debug.LogWarning("An exception occurred while loading type " + type.FullName + " in " + assemblyName + " assembly. Consider blacklisting the assembly by adding its name to the TypeExtensions.IgnoredAssemblies.txt file found at Assets/Sisus/Power Inspector/Code/Editor/Extensions/.\n" + e);
							break;
					}
					continue;
				}
				yield return type;
			}
		}

		public static IEnumerable<Type> UniqueTypesOnly(IEnumerable<Type> types)
		{
			var unique = new Dictionary<string, Type>(9500);

			foreach(var type in types)
			{
				var fullTypeName = type.FullName;
				if(!unique.ContainsKey(fullTypeName))
				{
					unique.Add(fullTypeName, type);
					continue;
				}

				var assemblyName = type.Assembly.GetName().Name;
				if(assemblyName.StartsWith("Unity", StringComparison.Ordinal))
				{
					string sub = assemblyName.Substring(5);
					if(sub.StartsWith(".", StringComparison.Ordinal) || sub.StartsWith("Engine.", StringComparison.Ordinal) || sub.StartsWith("Editor.", StringComparison.Ordinal) || sub.Equals("Editor"))
					{
						unique[fullTypeName] = type;
					}
					continue;
				}

				if(assemblyName.StartsWith("System", StringComparison.Ordinal) && (assemblyName.Length == 6 || assemblyName[0] == '.'))
				{
					unique[fullTypeName] = type;
				}
			}

			return unique.Values;
		}

		#if UNITY_2017_3_OR_NEWER
		public static ConcurrentDictionary<string, byte> GetEditorOnlyAssemblyNamesNotThreadSafe()
        {
			var assemblyCompilationInfos = CompilationPipeline.GetAssemblies(); // Probably not thread safe :/
			var editorOnlyAssemblies = new ConcurrentDictionary<string, byte>();
			for(int n = assemblyCompilationInfos.Length - 1; n >= 0; n--)
			{
				var assemblyInfo = assemblyCompilationInfos[n];
				if((assemblyInfo.flags & AssemblyFlags.EditorAssembly) == AssemblyFlags.EditorAssembly)
				{
					editorOnlyAssemblies[assemblyInfo.name] = default(byte);
				}
			}
			return editorOnlyAssemblies;

		}
		#endif

		private static IEnumerable<Assembly> WhereEditorOnly(IEnumerable<Assembly> assemblies)
		{
			#if UNITY_EDITOR && UNITY_2017_3_OR_NEWER
			foreach(var assembly in assemblies)
			{
				if(EditorOnlyAssemblyNames.ContainsKey(assembly.GetName().Name))
				{
					yield return assembly;
				}
			}
			#else
			return assemblies;
			#endif
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static bool TypeIsConcrete([NotNull] Type type)
		{
			return !type.IsAbstract;
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static bool IsEditorOnly([NotNull] Type type)
		{
			#if UNITY_EDITOR && UNITY_2017_3_OR_NEWER
			var assemblyCompilationInfos = CompilationPipeline.GetAssemblies();
			var editorOnlyAssemblies = new HashSet<string>();
			for(int n = assemblyCompilationInfos.Length - 1; n >= 0; n--)
			{
				var assemblyInfo = assemblyCompilationInfos[n];
				if((assemblyInfo.flags & AssemblyFlags.EditorAssembly) == AssemblyFlags.EditorAssembly)
				{
					editorOnlyAssemblies.Add(assemblyInfo.name);
					if(assemblyInfo.name.Contains(type.Assembly.GetName().Name))
					{
						return true;
					}
				}
			}
			return false;
			#else
			return true;
			#endif
		}

		private static void Setup()
		{
			if(!ApplicationUtility.IsReady())
			{
				EditorApplication.delayCall += Setup;
				return;
			}

			lock(threadLock)
			{
				if(setupDone != SetupPhase.Unstarted)
				{
					return;
				}
				setupDone = SetupPhase.InProgress;
			}

			// build syntax formatting on another thread to avoid UI slow downs when user
			// selects a large formatted text file
			ThreadPool.QueueUserWorkItem(SetupThreaded);
		}

		private static void SetupThreaded(object threadTaskId)
		{
			#if DEV_MODE
			//var timer = new ExecutionTimeLogger();
			//timer.Start("TypeExtensions.SetupThreaded");
			#endif

			var all = new List<Type>(18000);
			var allVisible = new List<Type>(9500);
			var allUnique = new Dictionary<string, Type>(9500);
			var allNotUnity = new List<Type>(7000);
			var invisibles = new List<Type>(8000);
			var abstracts = new List<Type>(1700);
			var invisibleAbstracts = new List<Type>(1000); //what is optimal value?
			var components = new List<Type>(500);
			var unityObjects = new List<Type>(150);
			var scriptableObjects = new List<Type>(150);
			#if UNITY_EDITOR
			var editors = new List<Type>(100);
			var invisibleEditors = new List<Type>(220);
			#endif
			var classes = new List<Type>(5000);
			var invisibleClasses = new List<Type>(6000);
			var values = new List<Type>(1000);
			var invisibleValues = new List<Type>(1000);
			var enums = new List<Type>(1600);
			var invisibleEnums = new List<Type>(1000);
			TypeIds.Clear();

			#if DEV_MODE && DEBUG_GENERATE_FILE_LIST_OF_ALL_TYPES
			var s = new StringBuilder(10000);
			#endif

			int nth = 0;

			foreach(var assembly in GetAllAssembliesThreadSafe())
			{
				var assemblyName = assembly.GetName().Name;
				bool isUnityAssembly = false;
				bool isSystemAssembly = false;

				// Add assemblies in namespaces starting with "Unity.", "UnityEngine." and "UnityEditor." to unitysAssemblies.
				if(assemblyName.StartsWith("Unity", StringComparison.Ordinal))
				{
					string sub = assemblyName.Substring(5);
					if(sub.StartsWith(".", StringComparison.Ordinal) || sub.StartsWith("Engine.", StringComparison.Ordinal) || sub.StartsWith("Editor.", StringComparison.Ordinal) || sub.Equals("Editor"))
					{
						isUnityAssembly = true;
					}
				}
				else if(assemblyName.StartsWith("System", StringComparison.Ordinal) && (assemblyName.Length == 6 || assemblyName[0] == '.'))
				{
					isSystemAssembly = true;
				}

				#if DEV_MODE && DEBUG_GENERATE_FILE_LIST_OF_ALL_TYPES
				s.Append("\n\n\n>>>");
				s.Append(assembly.GetName().Name);
				s.Append(":<<<\n");
				#endif

				//NOTE: important to also get invisible types, so that internal Editors such as RectTransformEditor are also returned
				var types = assembly.GetLoadableTypes(false);
				for(int m = types.Length - 1; m >= 0; m--)
				{
					var t = types[m];
					TypeIds.Add(t, nth);
					nth++;

					var name = t.Name;
					var fullName = t.FullName;

					bool hasUniqueFullName = true;

					var fullNameAndType = new KeyValuePair<string, Type>(fullName, t);
					KeyValuePair<string, Type>[] fullNamesAndTypes;
					if(TypesByName.TryGetValue(name, out fullNamesAndTypes))
					{
						int index = fullNamesAndTypes.Length;
						for(int f = index - 1; f >= 0; f--)
						{
							if(string.Equals(fullNamesAndTypes[f].Key, fullName))
							{
								hasUniqueFullName = false;
								break;
							}
						}

						Array.Resize(ref fullNamesAndTypes, index + 1);
						fullNamesAndTypes[index] = fullNameAndType;
					}
					else
					{
						fullNamesAndTypes = ArrayPool<KeyValuePair<string, Type>>.CreateWithContent(fullNameAndType);
					}
					TypesByName[name] = fullNamesAndTypes;

					all.Add(t);

					if(!t.IsVisible)
					{
						#if DEV_MODE && DEBUG_GENERATE_FILE_LIST_OF_ALL_TYPES
						s.Append("(");
						s.Append(t.FullName);
						s.Append(" )\n");
						#endif

						invisibles.Add(t);

						if(t.IsAbstract)
						{
							invisibleAbstracts.Add(t);
							continue;
						}

						if(t.IsClass)
						{
							#if UNITY_EDITOR
							if(Types.Editor.IsAssignableFrom(t))
							{
								invisibleEditors.Add(t);
								continue;
							}
							#endif

							invisibleClasses.Add(t);
							continue;
						}

						if(t.IsEnum)
						{
							invisibleEnums.Add(t);
							continue;
						}

						invisibleValues.Add(t);
						continue;
					}

					allVisible.Add(t);

					if(hasUniqueFullName)
					{
						allUnique.Add(fullName, t);
					}
					else if(isUnityAssembly || isSystemAssembly)
					{
						allUnique[fullName] = t;
					}

					#if DEV_MODE && DEBUG_GENERATE_FILE_LIST_OF_ALL_TYPES
					s.Append(fullName);
					s.Append('\n');
					#endif

					if(t.IsAbstract)
					{
						abstracts.Add(t);
						continue;
					}

					if(t.IsClass)
					{
						if(t.IsUnityObject())
						{
							if(t.IsComponent())
							{
								components.Add(t);
								continue;
							}
							#if UNITY_EDITOR
							if(Types.Editor.IsAssignableFrom(t))
							{
								editors.Add(t);
								continue;
							}
							#endif
							if(t.IsScriptableObject())
							{
								scriptableObjects.Add(t);
								continue;
							}
							unityObjects.Add(t);
							continue;
						}
						allNotUnity.Add(t);
						classes.Add(t);
						continue;
					}

					allNotUnity.Add(t);

					if(t.IsEnum)
					{
						enums.Add(t);
						if(enumTypesByName.TryGetValue(name, out fullNamesAndTypes))
						{
							int index = fullNamesAndTypes.Length;
							Array.Resize(ref fullNamesAndTypes, index + 1);
							fullNamesAndTypes[index] = fullNameAndType;
						}
						else
						{
							fullNamesAndTypes = ArrayPool<KeyValuePair<string, Type>>.CreateWithContent(fullNameAndType);
						}
						enumTypesByName[name] = fullNamesAndTypes;
						continue;
					}

					values.Add(t);
				}
			}

			//TO DO: Assert that no arrays have any duplicates of types?

			//as an optimization, make sure that some of the most common types are found near the ends of the arrays
			//(so when looped in reverse order using a for-loop, they'll be checked first)
			components.Remove(Types.Animator);
			components.Add(Types.Animator);
			components.Remove(Types.BoxCollider);
			components.Add(Types.BoxCollider);
			components.Remove(Types.RectTransform);
			components.Add(Types.RectTransform);
			components.Remove(Types.Transform);
			components.Add(Types.Transform);

			unityObjects.Remove(Types.GameObject);
			unityObjects.Add(Types.GameObject);

			values.Remove(Types.Double);
			values.Add(Types.Double);
			values.Remove(Types.Vector3);
			values.Add(Types.Vector3);
			values.Remove(Types.Int);
			values.Add(Types.Int);
			values.Remove(Types.Bool);
			values.Add(Types.Bool);
			values.Remove(Types.Float);
			values.Add(Types.Float);

			classes.Remove(Types.String);
			classes.Add(Types.String);

			allVisibleTypes = allVisible.ToArray();
			abstractTypes = abstracts.ToArray();
			invisibleAbstractTypes = invisibleAbstracts.ToArray();
			componentTypes = components.ToArray();
			scriptableObjectTypes = scriptableObjects.ToArray();
			editorTypes = editors.ToArray();
			otherUnityObjectTypes = unityObjects.ToArray();
			nonUnityObjectClassTypes = classes.ToArray();
			visibleEnumTypes = enums.ToArray();
			allNonUnityObjectTypes = allNotUnity.ToArray();
			allInvisibleTypes = ArrayPool<Type>.Create(invisibles);
			#if UNITY_EDITOR
			invisibleEditorTypes = ArrayPool<Type>.Create(invisibleEditors);
			#endif
			invisibleNonUnityObjectClassTypes = ArrayPool<Type>.Create(invisibleClasses);
			invisibleEnums.AddRange(visibleEnumTypes);
			enumTypesIncludingInvisible = ArrayPool<Type>.Create(invisibleEnums);

			int count = allVisibleTypes.Length;
			allVisibleTypeLabels = new string[count];
			var sb = new StringBuilder();
			for(int n = count - 1; n >= 0; n--)
			{
				var type = allVisibleTypes[n];

				StringUtils.ToString(type, '/', sb);
				string menuPath = sb.ToString();
				TypePopupMenuPaths[type] = menuPath;
				allVisibleTypeLabels[n] = menuPath;
				sb.Replace('/', '.');
				TypeFullNames[type] = sb.ToString();
				sb.Length = 0;
				StringUtils.ToString(type, '\0', sb);
				TypeNames[type] = sb.ToString();
				sb.Length = 0;
			}
			Array.Sort(allVisibleTypeLabels, allVisibleTypes);

			#if DEV_MODE
			//Debug.Log("allVisibleTypes.Length = " + allVisibleTypes.Length);
			//Debug.Log("allNonUnityObjectTypes.Length = " + allNonUnityObjectTypes.Length);
			//Debug.Log("abstractTypes.Length = " + abstractTypes.Length);
			//Debug.Log("componentTypes.Length = " + componentTypes.Length);
			//Debug.Log("otherUnityObjectTypes.Length = " + otherUnityObjectTypes.Length);
			//Debug.Log("scriptableObjectTypes.Length = " + scriptableObjectTypes.Length);
			//Debug.Log("editorTypes.Length = " + editorTypes.Length);
			//Debug.Log("enumTypes.Length = " + visibleEnumTypes.Length);
			//Debug.Log("nonEnumValueTypes.Length = " + nonEnumValueTypes.Length);
			//Debug.Log("nonUnityObjectClassTypes.Length = " + nonUnityObjectClassTypes.Length);
			//Debug.Log("invisibleTypes.Length = " + allInvisibleTypes.Length);
			//Debug.Log("invisibleEditorTypes.Length = " + invisibleEditorTypes.Length);
			//Debug.Log("invisibleNonEnumValueTypes.Length = " + invisibleNonEnumValueTypes.Length);
			//Debug.Log("invisibleNonUnityObjectClassTypes.Length = " + invisibleNonUnityObjectClassTypes.Length);

			#if DEBUG_GENERATE_FILE_LIST_OF_ALL_TYPES
			System.IO.File.WriteAllText(Application.dataPath + "/Testing/TypesList.txt", s.ToString());
			#endif

			//timer.FinishAndLogResults();
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(allVisibleTypeLabels.Length == allVisibleTypes.Length);
			Debug.Assert(allVisibleTypes.Distinct().Count() == allVisibleTypes.Length);
			#endif

			lock(threadLock)
			{
				setupDone = SetupPhase.Done;
			}
		}

		public static IEnumerable<string> GetLabelsThreadSafe(IEnumerable<Type> types)
		{
			var sb = new StringBuilder();
			foreach(var type in types)
			{
				StringUtils.ToString(type, '/', sb);
				yield return sb.ToString();
				sb.Length = 0;
			}
		}

		public static bool IsNumeric(this Type type)
		{
			return type == Types.Int || type == Types.Float || type == Types.Long || type == Types.Short || type == Types.UInt || type == Types.UShort || type == Types.Double || type == Types.Decimal || type == Types.ULong;
		}

		public static bool IsUnityObject(this Type type)
		{
			return Types.UnityObject.IsAssignableFrom(type);
		}

		public static bool IsComponent(this Type type)
		{
			return Types.Component.IsAssignableFrom(type);
		}

		public static bool IsScriptableObject(this Type type)
		{
			return Types.ScriptableObject.IsAssignableFrom(type);
		}

		public static bool IsGameObject(this Type type)
		{
			return type == Types.GameObject;
		}

		//finds the nth member with the given type inside the array and returns its index
		//if target did not have as many targets with said type, returns -1
		public static int IndexOfNthInstanceOfType<T>(this T[] array, Type type, int nth)
		{
			int count = array.Length;
			for(int n = 0; n < count; n++)
			{
				var test = array[n];
				if(test != null && type == test.GetType())
				{
					if(nth == 1)
					{
						return n;
					}
					nth--;
				}
			}
			return -1;
		}

		/// <summary>
		/// Finds the nth member with the given type inside the array and returns its index
		/// if target did not have as many targets with said type, returns -1
		/// </summary>
		/// <param name="array">Array whose contents will be iterated</param>
		/// <param name="nth">A number equal to or larger than one.</param>
		/// <returns>Int of nth null in array or -1 if not enough nulls were found in the array.</returns>
		public static int IndexOfNthNull<T>(this T[] array, int nth) where T : class
		{
			int count = array.Length;
			for(int n = 0; n < count; n++)
			{
				if(array[n] == null)
				{
					if(nth == 1)
					{
						return n;
					}
					nth--;
				}
			}
			return -1;
		}

		/// <summary>
		/// Return -1 if target was not found inside array. Otherwise returns value of 0 or above, based on how many objects with the same type as target precede it
		/// </summary>
		public static int CountPrecedingInstancesWithSameType<T>(this T[] array, T target)
		{
			int found = -1;
			int count = array.Length;
			var type = target.GetType();
			for(int n = 0; n < count; n++)
			{
				var check = array[n];

				if(target.Equals(check))
				{
					found++;
					return found;
				}

				if(check != null && type.Equals(check.GetType()))
				{
					found++;
				}
			}

			return -1;
		}

		public static bool IsJaggedArray([NotNull] this Type type)
		{
			if(!type.IsArray)
			{
				return false;
			}
			return type.Name.EndsWith("[][]", StringComparison.Ordinal);
		}

		public static object DefaultValue([NotNull] this Type type)
		{
			createInstanceRecursiveCallCount = 0;
			return type.DefaultValueInternal();
		}

		internal static object DefaultValueInternal([NotNull] this Type type)
		{
			if(type == Types.String)
			{
				return "";
			}

			if(type.IsArray)
			{
				string typeName = type.Name;

				var elementType = type.GetElementType();

				#if DEV_MODE && DEBUG_DEFAULT_VALUE
				Debug.Log("DefaultValue(" + StringUtils.ToString(type) + ") IsArray with ElementType="+ StringUtils.ToString(elementType));
				#endif

#if DEV_MODE && DEBUG_DEFAULT_VALUE
				if(elementType.IsAbstract)
				{
#if DEV_MODE && DEBUG_DEFAULT_VALUE
					Debug.Log("DefaultValue(" + StringUtils.ToString(type) + ") elementType " + StringUtils.ToString(elementType) + " IsAbstract: returning null");
#endif
				}

				if(elementType.IsGenericType)
				{
					var genericTypeDefinition = elementType.GetGenericTypeDefinition();
					Debug.Log("DefaultValue(" + StringUtils.ToString(type) + ") elementType " + StringUtils.ToString(elementType) + " IsGenericType with IsGenericTypeDefinition=" + elementType.IsGenericTypeDefinition + ", genericTypeDefinition=" + StringUtils.ToString(genericTypeDefinition));
				}
#endif

				// If element type is a generic type, we can't generate a default value instance, since we don't know which type to use.
				// For that we'd need a method like DefaultValue(Type type, params Type[] genericTypes).
				if(elementType.IsGenericTypeDefinition)
				{
#if DEV_MODE && DEBUG_DEFAULT_VALUE
					var genericTypeDefinition = elementType.GetGenericTypeDefinition();
					Debug.Log("DefaultValue(" + StringUtils.ToString(type) + ") elementType " + StringUtils.ToString(elementType) + " IsGenericTypeDefinition with IsGenericType="+ elementType.IsGenericType +", genericTypeDefinition=" + StringUtils.ToString(genericTypeDefinition)+": returning null");
#endif
					return null;
				}

				// If element type is a generic parameter type, we can't generate a default value instance, since we don't know which type to use.
				// For that we'd need a method like DefaultValue(Type type, params Type[] genericTypes).
				if(elementType.IsGenericParameter)
				{
#if DEV_MODE && DEBUG_DEFAULT_VALUE
					Debug.Log("DefaultValue(" + StringUtils.ToString(type) + ") elementType " + StringUtils.ToString(elementType) + " IsGenericParameter: returning null");
#endif
					return null;
				}

				switch(type.GetArrayRank())
				{
					case 1:
						if(type.IsJaggedArray())
						{
							Array.CreateInstance(elementType.MakeArrayType(), 0);
						}
						return Array.CreateInstance(elementType, 0);
					case 2:
						return Array.CreateInstance(elementType, 0, 0);
					case 3:
						return Array.CreateInstance(elementType, 0, 0, 0);
					default:
						int arrayDefinitionIndex = typeName.LastIndexOf('[');
						int jaggedIndex = typeName.IndexOf(',', arrayDefinitionIndex + 1);
						int rank;
						for(rank = 2; (jaggedIndex = typeName.IndexOf(',', jaggedIndex + 1)) != -1; rank++) { }
						var lengths = ArrayPool<int>.Create(rank);
						return Array.CreateInstance(elementType, lengths);
				}
			}

			if(type.IsGenericTypeDefinition)
			{
#if DEV_MODE && DEBUG_DEFAULT_VALUE
				Debug.Log("DefaultValue(" + StringUtils.ToString(type) + ") IsGenericTypeDefinition: returning null");
#endif
				return null;
			}

			if(type.IsGenericParameter)
			{
#if DEV_MODE && DEBUG_DEFAULT_VALUE
				Debug.Log("DefaultValue(" + StringUtils.ToString(type) + ") IsGenericParameter: returning null");
#endif
				return null;
			}

			if(type.IsAbstract)
			{
				// NEW: Create an empty list for IList, IEnumerable, ICollection. IList<T>, IEnumerable<T> and ICollection<T>.
				// Create an empty Dictionary for IDictionary, IDictionary<TValue,TKey>.
				if(type.IsGenericType)
				{
					var typeDefinition = type.GetGenericTypeDefinition();
					if(typeDefinition == typeof(IList<>) || typeDefinition == typeof(IEnumerable<>) || typeDefinition == typeof(ICollection<>))
					{
						var listType = typeof(List<>).MakeGenericType(type.GetGenericArguments());
						return listType.DefaultValueInternal();
					}
					if(typeDefinition == typeof(IDictionary<,>))
					{
						var dictionaryType = typeof(Dictionary<,>).MakeGenericType(type.GetGenericArguments());
						return dictionaryType.DefaultValueInternal();
					}
				}
				else if(type == typeof(IList) || type == typeof(IEnumerable) || type == typeof(ICollection))
				{
					return new List<object>();
				}
				else if(type == typeof(IDictionary))
				{
					return new Dictionary<object, object>();
				}

#if DEV_MODE && DEBUG_DEFAULT_VALUE
				Debug.Log("DefaultValue(" + StringUtils.ToString(type) + ") IsAbstract: returning null");
#endif
				return null;
			}

			if(type.IsUnityObject())
			{
#if DEV_MODE && DEBUG_DEFAULT_VALUE
				Debug.Log("DefaultValue(" + StringUtils.ToString(type) + ") IsUnityObject: returning null");
#endif
				return null;
			}

			if(type == Types.SystemObject)
			{
				return null;
			}

			if(Types.Type.IsAssignableFrom(type))
			{
				return typeof(void);
			}

			if(type == Types.Void || type == Types.DBNull)
			{
				return null;
			}

			if(type.IsEnum)
			{
				var underlyingType = Enum.GetUnderlyingType(type);
				return Enum.ToObject(type, underlyingType.DefaultValueInternal());
			}

			if(type.IsGenericType)
			{
				var baseType = type.GetGenericTypeDefinition();
				if(baseType == Types.KeyValuePair)
				{
#if DEV_MODE && DEBUG_DEFAULT_VALUE
					Debug.Log("type: "+StringUtils.ToString(type));
#endif
					var keyAndValueTypes = type.GetGenericArguments();
					return Activator.CreateInstance(type, keyAndValueTypes[0].DefaultValueInternal(), keyAndValueTypes[1].DefaultValueInternal());
				}

				var nullableType = Nullable.GetUnderlyingType(type);
				if(nullableType != null)
				{
#if DEV_MODE && DEBUG_DEFAULT_VALUE
					Debug.Log("return null for nullable type: "+StringUtils.ToString(type));
#endif
					return null;
				}
			}

			if(Types.MulticastDelegate.IsAssignableFrom(type))
			{
				return null;
			}

			// try to create instance using Activator.CreateInstance and FormatterServices.GetUninitializedObject
			return CreateInstanceInternal(type);
		}

		/// <summary>
		/// Tries to create an instance of type using Activator.CreateInstance and if that fails FormatterServices.GetUninitializedObject.
		/// </summary>
		/// <param name="type"> The type of the instance to create. </param>
		/// <returns> The created instance, or null if failed to create instance. </returns>
		[CanBeNull]
		public static object CreateInstance([NotNull] this Type type)
		{
			createInstanceRecursiveCallCount = 0;
			return CreateInstanceInternal(type);
		}

		/// <summary>
		/// Tries to create an instance of type using Activator.CreateInstance and if that fails FormatterServices.GetUninitializedObject.
		/// </summary>
		/// <param name="type"> The type of the instance to create. </param>
		/// <returns> The created instance, or null if failed to create instance. </returns>
		[CanBeNull]
		internal static object CreateInstanceInternal([NotNull] this Type type)
		{
#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(type != null);
			Debug.Assert(!type.IsComponent());
			Debug.Assert(!type.IsGameObject());
			Debug.Assert(!type.IsGenericTypeDefinition, "CreateInstance called for type " + type.Name + " with IsGenericTypeDefinition " + StringUtils.True);
			Debug.Assert(!type.IsAbstract, "CreateInstance called for type " + type.Name + " with IsAbstract " + StringUtils.True);
#endif

			if(createInstanceRecursiveCallCount > MaxRecursiveCallCount)
			{
				createInstanceRecursiveCallCount = 0;
#if DEV_MODE
				Debug.Log("type.CreateInstance recursive call count was 25 for type " + type.Name);
#endif
				return FormatterServices.GetUninitializedObject(type);
			}

			if(type.IsScriptableObject())
			{
#if DEV_MODE && DEBUG_DEFAULT_VALUE
				Debug.Log("ScriptableObject.CreateInstance(" + type.Name + ")");
#endif
				return ScriptableObject.CreateInstance(type);
			}

			if(type.IsGenericType && !type.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == typeof(System.Collections.ObjectModel.ReadOnlyCollection<>))
			{
				return null;
			}

			// try to create instance using parameterless constructor
			// (this can fail if class has no parameterless constructor)
			try
			{
#if DEV_MODE && DEBUG_DEFAULT_VALUE
				Debug.Log("Activator.CreateInstance(" + type.Name + ")");
#endif

				return Activator.CreateInstance(type);
			}
#if DEV_MODE && DEBUG_DEFAULT_VALUE_EXCEPTIONS
			catch(Exception e) { Debug.LogWarning("DefaultValue(" + type.Name + "): parameterless CreateInstance failed. Trying constructors with parameters.\n" + e); }
#else
			catch { }
#endif

			// manually fetch class constructors, and try and create an instance using those
			// (this can fail if constructor does not accept default parameter values and throws an exception)
			var constructors = type.GetConstructors();
			for(int c = constructors.Length - 1; c >= 0; c--)
			{
				var constructor = constructors[c];
				var parameters = constructor.GetParameters();
				int count = parameters.Length;

				// Avoid infinite loop issues with constructors where parameter type equals type of class that contains the constructor.
				// E.g. LinkedList(LinkedList previous, LinkedList next)
				bool infiniteRecursionDetected = false;
				for(int p = 0; p < count; p++)
				{
					if(parameters[p].ParameterType == type)
					{
						infiniteRecursionDetected = true;
						break;
					}
				}
				if(infiniteRecursionDetected)
				{
					continue;
				}

				bool parameterGenerationFailed = false;

				//generate default values for all parameters that the constructor requires.
				var parameterValues = ArrayPool<object>.Create(count);
				for(int p = 0; p < count; p++)
				{
					try
					{
						parameterValues[p] = parameters[p].DefaultValueInternal();

#if DEV_MODE && DEBUG_DEFAULT_VALUE
						Debug.Log(StringUtils.ToString(type)+" parameterValues["+p+"] = " + StringUtils.ToString(parameterValues[p]) +")");
#endif
					}
#if DEV_MODE && DEBUG_DEFAULT_VALUE_EXCEPTIONS
					catch(Exception e)
					{
						Debug.LogWarning("DefaultValue(" + type.Name + "): generating default value for parameter " + p + "/+" + count + " of type " + parameters[p].ParameterType + " failed.\n" + e);
						parameterGenerationFailed = true;
					}
#else
					catch
					{
						parameterGenerationFailed = true;
					}
#endif

					if(parameterGenerationFailed)
					{
						break;
					}
				}

				if(!parameterGenerationFailed)
				{
					//try to create the instance
					try
					{
						return constructor.Invoke(parameterValues);
					}
#if DEV_MODE && DEBUG_DEFAULT_VALUE_EXCEPTIONS
					catch(Exception e) { Debug.LogWarning("DefaultValue(" + type.Name + "): CreateInstance with " + count + " parameters failed. Trying (" + (c == constructors.Length - 1 ? "GetUninitializedObject next" : "next constructor") + ").\n" + e + "\nparameters:" + StringUtils.ToString(parameters)); }
#else
					catch { }
#endif
				}
			}

			// If all else fails, as a last resort, don't use a constructor at all, and
			// return an uninitialized instance of the class. This can be somewhat risky,
			// since important setup phases could get skipped because constructor was not called.
			try
			{
				return FormatterServices.GetUninitializedObject(type);
			}
#if DEV_MODE && DEBUG_DEFAULT_VALUE_EXCEPTIONS
			catch(Exception e) { Debug.LogWarning("DefaultValue: GetUninitializedObject failed. Returning null...\n" + e); }
#else
			catch { }
#endif

			// if everything failed, return null
			return null;
		}

		/// <summary>
		/// Tries to create an instance of type using Activator.CreateInstance and if that fails FormatterServices.GetUninitializedObject.
		/// </summary>
		/// <param name="type"> The type of the instance to create. </param>
		/// <returns> The created instance, or null if failed to create instance. </returns>
		[CanBeNull]
		public static object CreateInstance([NotNull] this Type type, [NotNull] Type parameterType, object parameterValue)
		{
			createInstanceRecursiveCallCount = 0;
			return CreateInstanceInternal(type, parameterType, parameterValue);
		}

		/// <summary>
		/// Tries to create an instance of type using Activator.CreateInstance and if that fails FormatterServices.GetUninitializedObject.
		/// </summary>
		/// <param name="type"> The type of the instance to create. </param>
		/// <returns> The created instance, or null if failed to create instance. </returns>
		[CanBeNull]
		internal static object CreateInstanceInternal([NotNull] this Type type, [NotNull] Type parameterType, object parameterValue)
		{
#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(type != null);
			Debug.Assert(parameterType != null);
			Debug.Assert(!type.IsUnityObject());
			if(parameterValue == null)
			{
				Debug.Assert(!parameterType.IsValueType);
			}
			else
			{
				Debug.Assert(parameterType == parameterValue.GetType());
			}
#endif

			if(createInstanceRecursiveCallCount > MaxRecursiveCallCount)
			{
				createInstanceRecursiveCallCount = 0;
#if DEV_MODE
				Debug.Log("type.CreateInstance recursive call count was 25 for type " + type.Name);
#endif
				return FormatterServices.GetUninitializedObject(type);
			}

			if(parameterType == type)
			{
				// can not generate instance using provided parameter, but try to generate it without it.
				return type.DefaultValueInternal();
			}

			// Fetch class constructors. We will then try to find one that accepts parameterValue as a parameter.
			var constructors = type.GetConstructors();

			// Best constructor candidate is one that only has one parameter that matches provided parameter type.
			int constructorCount = constructors.Length;
			for(int c = constructorCount - 1; c >= 0; c--)
			{
				var constructor = constructors[c];
				var parameters = constructor.GetParameters();
				int parameterCount = parameters.Length;
				if(parameterCount != 1)
				{
					continue;
				}

				var parameter = parameters[0];

				if(!parameter.ParameterType.IsAssignableFrom(parameterType))
				{
					continue;
				}

				var parameterValues = ArrayExtensions.TempObjectArray(parameterValue);

				//try to create the instance
				try
				{
					return constructor.Invoke(parameterValues);
				}
#if DEV_MODE && DEBUG_DEFAULT_VALUE_EXCEPTIONS
				catch(Exception e) { Debug.LogWarning("DefaultValue(" + type.Name + "): CreateInstance with " + parameterCount + " parameters failed. Trying (" + (c == constructors.Length - 1 ? "GetUninitializedObject next" : "next constructor") + ").\n" + e); }
#else
				catch { }
#endif
			}

			bool atLeastOnConstructorWithEnoughParametersFound = true;
			// Second best candidate is one that has more than one parameter, but one of them matches provided parameter type.
			// The less parameters there are, the better.
			for(int wantedParameterCount = 2; atLeastOnConstructorWithEnoughParametersFound; wantedParameterCount++)
			{
				atLeastOnConstructorWithEnoughParametersFound = false;

				for(int c = constructorCount - 1; c >= 0; c--)
				{
					var constructor = constructors[c];
					var parameters = constructor.GetParameters();
					int parameterCount = parameters.Length;

					if(parameterCount < wantedParameterCount)
					{
						continue;
					}

					atLeastOnConstructorWithEnoughParametersFound = true;

					int assignableAtIndex = -1;

					for(int p = 0; p < parameterCount; p++)
					{
						var parameter = parameters[p];

						if(!parameter.ParameterType.IsAssignableFrom(parameterType))
						{
							continue;
						}

						assignableAtIndex = p;
						break;
					}

					if(assignableAtIndex == -1)
					{
						break;
					}

					var parameterValues = ArrayPool<object>.Create(parameterCount);
					parameterValues[assignableAtIndex] = parameterValue;

					// Generate default values for all the other parameters that the constructor requires.
					bool parameterGenerationFailed = false;
					for(int p = 0; p < parameterCount; p++)
					{
						if(assignableAtIndex == p)
						{
							continue;
						}

						try
						{
							parameterValues[p] = parameters[p].DefaultValueInternal();

#if DEV_MODE && DEBUG_DEFAULT_VALUE
							Debug.Log(StringUtils.ToString(type)+" parameterValues["+p+"] = " + StringUtils.ToString(parameterValues[p]) +")");
#endif
						}
#if DEV_MODE && DEBUG_DEFAULT_VALUE_EXCEPTIONS
						catch(Exception e)
						{
							Debug.LogWarning("DefaultValue(" + type.Name + "): generating default value for parameter " + (p + 1) + "/+" + parameterCount + " of type " + parameters[p].ParameterType + " failed.\n" + e);
							parameterGenerationFailed = true;
						}
#else
						catch
						{
							parameterGenerationFailed = true;
						}
#endif

						if(parameterGenerationFailed)
						{
							break;
						}
					}

					if(!parameterGenerationFailed)
					{
						// Try to create the instance with the generated parameter values.
						try
						{
							return constructor.Invoke(parameterValues);
						}
#if DEV_MODE && DEBUG_DEFAULT_VALUE_EXCEPTIONS
						catch(Exception e) { Debug.LogWarning("DefaultValue(" + type.Name + "): CreateInstance with " + parameterCount + " parameters failed. Trying (" + (c == constructors.Length - 1 ? "GetUninitializedObject next" : "next constructor") + ").\n" + e); }
#else
						catch { }
#endif
					}
				}
			}

			// if failed to generate instance using provided parameter, try to generate it without it
			return type.DefaultValueInternal();
		}

		public static IEnumerable<Type> GetTypesAccessibleFromContext([NotNull] this Type type)
		{
			var internalTypes = type.GetOnlyInternallyAccessibleTypes();
			var result = AllVisibleTypes.Concat(internalTypes);
			return result.ToArray();
		}

		/// <summary>
		/// Gets all nested types inside type as well as all types in type's assembly
		/// that are only internally accessible
		/// </summary>
		/// <param name="type"> The type to act on. </param>
		/// <returns> An array of type. </returns>
		public static IEnumerable<Type> GetOnlyInternallyAccessibleTypes(this Type type)
		{
			var nestedTypes = type.GetNestedTypes(BindingFlags.NonPublic);
			var assemblyTypes = type.Assembly.GetOnlyInternallyAccessibleTypes();
			return assemblyTypes.Concat(nestedTypes);
		}

		public static IEnumerable<Type> GetOnlyInternallyAccessibleTypes(this Assembly assembly)
		{
			Type[] result;
			if(HiddenTypesByAssembly.TryGetValue(assembly, out result))
			{
				return result;
			}

			var allAssemblyTypes = assembly.GetLoadableTypes(false);

			var TypesList = new List<Type>(100);

			for(int n = allAssemblyTypes.Length - 1; n >= 0; n--)
			{
				var t = allAssemblyTypes[n];

				//skip all types that are visible to external assemblies
				if(t.IsVisible)
				{
					continue;
				}

				if(t.IsNested && !t.IsNestedPublic && !t.IsNestedAssembly)
				{
					//we only want to add nested public or internal types (skipping private, protected and protected internal)
					continue;
				}

				//skip some internally generated types
				var nestedClass = t.DeclaringType;
				if(nestedClass != null && nestedClass != t)
				{
					var nestedClassName = nestedClass.Name;
					if(nestedClassName.Length == 0 || nestedClassName[0] == '<')
					{
#if DEV_MODE
						Debug.Log("SKIPPING: " + t.FullName + " with nestedClass=" + nestedClass.FullName + ", Name=" + t.Name + ", Namespace=" + t.Namespace + ", Assembly=" + t.Assembly.FullName);
#endif
						continue;
					}
				}

				var name = t.Name;
				if(name.Length == 0 || name[0] == '<')
				{
#if DEV_MODE
					Debug.Log("SKIPPING: " + t.FullName + " with nestedClass=" + (nestedClass == null ? "null" : nestedClass.FullName) + ", Name=" + t.Name + ", Namespace=" + t.Namespace + ", Assembly=" + t.Assembly.FullName);
#endif
					continue;
				}

				//add internal class , nested public or nested internal type
				TypesList.Add(t);
			}

			result = TypesList.ToArray();

			HiddenTypesByAssembly.Add(assembly, result);

			return result;
		}

		[NotNull]
		public static Type[] GetLoadableTypes([NotNull] this Assembly assembly, bool exportedOnly)
		{
			try
			{
				Type[] result;
				if(exportedOnly)
				{
					result = assembly.GetExportedTypes();
				}
				else
				{
					result = assembly.GetTypes();
				}

				return result;
			}
			catch(NotSupportedException) //thrown if GetExportedTypes is called for a dynamic assembly
			{
				#if DEV_MODE
				Debug.LogWarning(assembly.GetName().Name + ".GetLoadableTypes() NotSupportedException\n" + assembly.FullName);
				#endif
				return ArrayPool<Type>.ZeroSizeArray;
			}
			catch(ReflectionTypeLoadException e)
			{
				var TypesList = new List<Type>(100);

				var exceptionTypes = e.Types;
				int count = exceptionTypes.Length;
				for(int n = count - 1; n >= 0; n--)
				{
					var type = exceptionTypes[n];
					if(type != null)
					{
						TypesList.Add(type);
					}
				}

				#if DEV_MODE
				Debug.LogWarning(assembly.GetName().Name + ".GetLoadableTypes() ReflectionTypeLoadException, salvaged: " + TypesList.Count + "\n" + assembly.FullName);
				#endif

				return TypesList.ToArray();
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogWarning(assembly.GetName().Name + ".GetLoadableTypes() " + e + "\n" + assembly.FullName);
			#else
			catch(Exception)
			{
			#endif
				return ArrayPool<Type>.ZeroSizeArray;
			}
		}

		/// <summary>
		/// Gets all non-abstract UnityEngine.Object types that extend given Type
		/// </summary>
		public static IEnumerable<Type> GetExtendingUnityObjectTypesNotThreadSafe([NotNull] this Type baseType, bool includeInvisible, bool includeAbstract = false)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(baseType != null);
			Debug.Assert(Types.UnityObject.IsAssignableFrom(baseType), "GetExtendingUnityObjectTypes was called for type " + baseType.Name + " which wasn't assignable from UnityEngine.Object.");
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(baseType.IsClass, "GetExtendingUnityObjectTypes was called for " + baseType.Name + " which wasn't a class type. Value types and interfaces don't support inheritance.");
			#endif

			#if UNITY_2019_3_OR_NEWER
			if(!baseType.IsGenericTypeDefinition)
			{
				if(!includeAbstract)
				{
					if(!includeInvisible)
					{
						return TypeCache.GetTypesDerivedFrom(baseType).Where(t => !t.IsAbstract && t.IsVisible);
					}
					return TypeCache.GetTypesDerivedFrom(baseType).Where(t => !t.IsAbstract);
				}
				if(!includeInvisible)
				{
					return TypeCache.GetTypesDerivedFrom(baseType).Where(t => t.IsVisible);
				}
				return TypeCache.GetTypesDerivedFrom(baseType);
			}
			#endif

			return baseType.GetExtendingUnityObjectTypes(includeInvisible, includeAbstract);
		}

		/// <summary>
		/// Gets all non-abstract UnityEngine.Object types that extend given Type
		/// </summary>
		public static IEnumerable<Type> GetExtendingUnityObjectTypes([NotNull] this Type baseType, bool includeInvisible, bool includeAbstract = false)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(baseType != null);
			Debug.Assert(Types.UnityObject.IsAssignableFrom(baseType), "GetExtendingUnityObjectTypes was called for type " + baseType.Name + " which wasn't assignable from UnityEngine.Object.");
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(baseType.IsClass, "GetExtendingUnityObjectTypes was called for " + baseType.Name + " which wasn't a class type. Value types and interfaces don't support inheritance.");
			#endif

			if(!IsReady)
			{
				if(baseType.IsGenericTypeDefinition)
				{
					foreach(var t in GetAllTypesThreadSafe(baseType.Assembly, includeAbstract, includeInvisible, true).Where((t) => t.IsSubclassOfUndeclaredGeneric(baseType)))
					{
						yield return t;
					}
				}
				else
				{
					foreach(var t in GetAllTypesThreadSafe(baseType.Assembly, includeAbstract, includeInvisible, true).Where((t) => t.IsSubclassOf(baseType)))
					{
						yield return t;
					}
				}
			}
			else if(baseType.IsGenericTypeDefinition)
			{
				for(int n = componentTypes.Length - 1; n >= 0; n--)
				{
					var t = componentTypes[n];
					if(t.IsSubclassOfUndeclaredGeneric(baseType))
					{
						yield return t;
					}
				}

				for(int n = scriptableObjectTypes.Length - 1; n >= 0; n--)
				{
					var t = scriptableObjectTypes[n];
					if(t.IsSubclassOfUndeclaredGeneric(baseType))
					{
						yield return t;
					}
				}

				for(int n = editorTypes.Length - 1; n >= 0; n--)
				{
					var t = editorTypes[n];
					if(t.IsSubclassOfUndeclaredGeneric(baseType))
					{
						yield return t;
					}
				}

				for(int n = otherUnityObjectTypes.Length - 1; n >= 0; n--)
				{
					var t = otherUnityObjectTypes[n];
					if(t.IsSubclassOfUndeclaredGeneric(baseType))
					{
						yield return t;
					}
				}

				if(includeInvisible)
				{
					for(int n = allInvisibleTypes.Length - 1; n >= 0; n--)
					{
						var t = allInvisibleTypes[n];
						if(t.IsSubclassOfUndeclaredGeneric(baseType))
						{
							yield return t;
						}
					}
				}

				if(includeAbstract)
				{
					for(int n = abstractTypes.Length - 1; n >= 0; n--)
					{
						var t = abstractTypes[n];
						if(t.IsSubclassOfUndeclaredGeneric(baseType))
						{
							yield return t;
						}
					}

					if(includeInvisible)
					{
						for(int n = invisibleAbstractTypes.Length - 1; n >= 0; n--)
						{
							var t = invisibleAbstractTypes[n];
							if(t.IsSubclassOfUndeclaredGeneric(baseType))
							{
								yield return t;
							}
						}
					}
				}
			}
			else
			{
				for(int n = componentTypes.Length - 1; n >= 0; n--)
				{
					var t = componentTypes[n];
					if(t.IsSubclassOf(baseType))
					{
						yield return t;
					}
				}

				for(int n = scriptableObjectTypes.Length - 1; n >= 0; n--)
				{
					var t = scriptableObjectTypes[n];
					if(t.IsSubclassOf(baseType))
					{
						yield return t;
					}
				}

				for(int n = editorTypes.Length - 1; n >= 0; n--)
				{
					var t = editorTypes[n];
					if(t.IsSubclassOf(baseType))
					{
						yield return t;
					}
				}

				for(int n = otherUnityObjectTypes.Length - 1; n >= 0; n--)
				{
					var t = otherUnityObjectTypes[n];
					if(t.IsSubclassOf(baseType))
					{
						yield return t;
					}
				}

				if(includeInvisible)
				{
					for(int n = allInvisibleTypes.Length - 1; n >= 0; n--)
					{
						var t = allInvisibleTypes[n];
						if(t.IsSubclassOf(baseType))
						{
							yield return t;
						}
					}
				}

				if(includeAbstract)
				{
					for(int n = abstractTypes.Length - 1; n >= 0; n--)
					{
						var t = abstractTypes[n];
						if(t.IsSubclassOf(baseType))
						{
							yield return t;
						}
					}

					if(includeInvisible)
					{
						for(int n = invisibleAbstractTypes.Length - 1; n >= 0; n--)
						{
							var t = invisibleAbstractTypes[n];
							if(t.IsSubclassOf(baseType))
							{
								yield return t;
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets all non-abstract UnityEngine.Object types that implement given interface
		/// </summary>
		public static IEnumerable<Type> GetImplementingUnityObjectTypesNotThreadSafe(this Type interfaceType, bool includeInvisible, bool includeAbstract = false)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(interfaceType.IsInterface, "GetImplementingUnityObjectTypes was called for type " + interfaceType.Name + " which wasn't an interface. Use GetExtendingUnityObjectTypes instead.");
			#endif

			#if UNITY_2019_3_OR_NEWER
			if(!interfaceType.IsGenericTypeDefinition)
			{
				if(!includeAbstract)
				{
					if(!includeInvisible)
					{
						return TypeCache.GetTypesDerivedFrom(interfaceType).Where(t => !t.IsAbstract && t.IsVisible);
					}
					return TypeCache.GetTypesDerivedFrom(interfaceType).Where(t => !t.IsAbstract);
				}
				if(!includeInvisible)
				{
					return TypeCache.GetTypesDerivedFrom(interfaceType).Where(t => t.IsVisible);
				}
				return TypeCache.GetTypesDerivedFrom(interfaceType);
			}
			#endif

			return interfaceType.GetImplementingUnityObjectTypes(includeInvisible, includeAbstract);
		}

		/// <summary>
		/// Gets all non-abstract UnityEngine.Object types that implement given interface
		/// </summary>
		public static IEnumerable<Type> GetImplementingUnityObjectTypes(this Type interfaceType, bool includeInvisible, bool includeAbstract = false)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(interfaceType.IsInterface, "GetImplementingUnityObjectTypes was called for type " + interfaceType.Name + " which wasn't an interface. Use GetExtendingUnityObjectTypes instead.");
			#endif

			if(!IsReady)
			{
				foreach(var t in GetAllTypesThreadSafe(interfaceType.Assembly, includeAbstract, includeInvisible, true).Where((t) => interfaceType.IsAssignableFrom(t)))
				{
					yield return t;
				}
				yield break;
			}

			for(int n = componentTypes.Length - 1; n >= 0; n--)
			{
				var t = componentTypes[n];
				if(interfaceType.IsAssignableFrom(t))
				{
					yield return t;
				}
			}

			for(int n = scriptableObjectTypes.Length - 1; n >= 0; n--)
			{
				var t = scriptableObjectTypes[n];
				if(interfaceType.IsAssignableFrom(t))
				{
					yield return t;
				}
			}

			for(int n = editorTypes.Length - 1; n >= 0; n--)
			{
				var t = editorTypes[n];
				if(interfaceType.IsAssignableFrom(t))
				{
					yield return t;
				}
			}

			for(int n = otherUnityObjectTypes.Length - 1; n >= 0; n--)
			{
				var t = otherUnityObjectTypes[n];
				if(interfaceType.IsAssignableFrom(t))
				{
					yield return t;
				}
			}

			if(includeInvisible)
			{
				for(int n = allInvisibleTypes.Length - 1; n >= 0; n--)
				{
					var t = allInvisibleTypes[n];
					if(interfaceType.IsAssignableFrom(t))
					{
						yield return t;
					}
				}
			}

			if(includeAbstract)
			{
				for(int n = abstractTypes.Length - 1; n >= 0; n--)
				{
					var t = abstractTypes[n];
					if(interfaceType.IsAssignableFrom(t))
					{
						yield return t;
					}
				}

				if(includeInvisible)
				{
					for(int n = invisibleAbstractTypes.Length - 1; n >= 0; n--)
					{
						var t = invisibleAbstractTypes[n];
						if(interfaceType.IsAssignableFrom(t))
						{
							yield return t;
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets all non-abstract Component types that extend given Type
		/// </summary>
		public static IEnumerable<Type> GetExtendingComponentTypesNotThreadSafe(this Type baseType, bool includeInvisible, bool includeAbstract = false)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(baseType.IsClass, "GetExtendingUnityObjectTypes was called for " + baseType.Name + " which wasn't a class type. Value types and interfaces don't support inheritance.");
			Debug.Assert(typeof(Component).IsAssignableFrom(baseType) || baseType.IsAssignableFrom(typeof(Component)), "GetExtendingUnityObjectTypes was called for " + baseType.Name + " which wasn't a assignable from Component or vice versa.");
			#endif

			#if UNITY_2019_3_OR_NEWER
			if(!baseType.IsGenericTypeDefinition)
			{
				// if called with type such as System.Object or UnityEngine.Object, then return all components instead.
				if(!baseType.IsComponent())
				{
					baseType = typeof(Component);
				}

				if(!includeAbstract)
				{
					if(!includeInvisible)
					{
						return TypeCache.GetTypesDerivedFrom(baseType).Where(t => !t.IsAbstract && t.IsVisible);
					}
					return TypeCache.GetTypesDerivedFrom(baseType).Where(t => !t.IsAbstract);
				}
				if(!includeInvisible)
				{
					return TypeCache.GetTypesDerivedFrom(baseType).Where(t => t.IsVisible);
				}
				return TypeCache.GetTypesDerivedFrom(baseType);
			}
			#endif

			return baseType.GetExtendingComponentTypes(includeInvisible, includeAbstract);
		}

		/// <summary>
		/// Gets all non-abstract Component types that extend given Type
		/// </summary>
		public static IEnumerable<Type> GetExtendingComponentTypes(this Type baseType, bool includeInvisible, bool includeAbstract = false)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(baseType.IsClass, "GetExtendingUnityObjectTypes was called for " + baseType.Name + " which wasn't a class type. Value types and interfaces don't support inheritance.");
			Debug.Assert(typeof(Component).IsAssignableFrom(baseType) || baseType.IsAssignableFrom(typeof(Component)), "GetExtendingUnityObjectTypes was called for " + baseType.Name + " which wasn't a assignable from Component or vice versa.");
			#endif

			// if called with type such as System.Object or UnityEngine.Object, then return all components instead.
			if(!baseType.IsComponent())
            {
				baseType = typeof(Component);
			}

			if(!IsReady)
			{
				if(baseType == typeof(Component) && !includeInvisible)
				{
					foreach(var t in ComponentTypes)
					{
						yield return t;
					}
				}

				else if(baseType.IsGenericTypeDefinition)
				{
					foreach(var t in GetAllTypesThreadSafe(typeof(Component).Assembly, includeAbstract, includeInvisible, true).Where((t) => t.IsSubclassOfUndeclaredGeneric(baseType)))
					{
						yield return t;
					}
				}
				else
				{
					foreach(var t in GetAllTypesThreadSafe(typeof(Component).Assembly, includeAbstract, includeInvisible, true).Where((t) => t.IsSubclassOf(baseType)))
					{
						yield return t;
					}
				}
			}
			else if(baseType == Types.Component && !includeInvisible)
			{
				foreach(var t in componentTypes)
                {
					yield return t;
                }
			}
			else if(baseType.IsGenericTypeDefinition)
			{
				for(int n = componentTypes.Length - 1; n >= 0; n--)
				{
					var t = componentTypes[n];
					if(t.IsSubclassOfUndeclaredGeneric(baseType))
					{
						yield return t;
					}
				}

				if(includeInvisible)
				{
					for(int n = allInvisibleTypes.Length - 1; n >= 0; n--)
					{
						var t = allInvisibleTypes[n];
						if(t.IsSubclassOfUndeclaredGeneric(baseType))
						{
							yield return t;
						}
					}
				}

				if(includeAbstract)
				{
					for(int n = abstractTypes.Length - 1; n >= 0; n--)
					{
						var t = abstractTypes[n];
						if(t.IsSubclassOfUndeclaredGeneric(baseType))
						{
							yield return t;
						}
					}

					if(includeInvisible)
					{
						for(int n = invisibleAbstractTypes.Length - 1; n >= 0; n--)
						{
							var t = invisibleAbstractTypes[n];
							if(t.IsSubclassOfUndeclaredGeneric(baseType))
							{
								yield return t;
							}
						}
					}
				}
			}
			else
			{
				for(int n = componentTypes.Length - 1; n >= 0; n--)
				{
					var t = componentTypes[n];
					if(t.IsSubclassOf(baseType))
					{
						yield return t;
					}
				}

				if(includeInvisible)
				{
					for(int n = allInvisibleTypes.Length - 1; n >= 0; n--)
					{
						var t = allInvisibleTypes[n];
						if(t.IsSubclassOf(baseType))
						{
							yield return t;
						}
					}
				}

				if(includeAbstract)
				{
					for(int n = abstractTypes.Length - 1; n >= 0; n--)
					{
						var t = abstractTypes[n];
						if(t.IsSubclassOf(baseType))
						{
							yield return t;
						}
					}

					if(includeInvisible)
					{
						for(int n = invisibleAbstractTypes.Length - 1; n >= 0; n--)
						{
							var t = invisibleAbstractTypes[n];
							if(t.IsSubclassOf(baseType))
							{
								yield return t;
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets all non-abstract Component types that implement given interface
		/// </summary>
		public static IEnumerable<Type> GetImplementingComponentTypesNotThreadSafe(this Type interfaceType, bool includeInvisible, bool includeAbstract = true)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(interfaceType.IsInterface, "GetImplementingComponentTypes was called for type " + interfaceType.Name + " which wasn't an interface. Use GetExtendingComponentTypes instead.");
			#endif

			#if UNITY_2019_3_OR_NEWER
			if(!interfaceType.IsGenericTypeDefinition)
			{
				if(!includeAbstract)
				{
					if(!includeInvisible)
					{
						return TypeCache.GetTypesDerivedFrom(interfaceType).Where(t => t.IsComponent() && !t.IsAbstract && t.IsVisible);
					}
					return TypeCache.GetTypesDerivedFrom(interfaceType).Where(t => t.IsComponent() && !t.IsAbstract);
				}
				if(!includeInvisible)
				{
					return TypeCache.GetTypesDerivedFrom(interfaceType).Where(t => t.IsComponent() && t.IsVisible);
				}
				return TypeCache.GetTypesDerivedFrom(interfaceType).Where(IsComponent);
			}
			#endif

			return interfaceType.GetImplementingComponentTypes(includeInvisible, includeAbstract);
		}

		/// <summary>
		/// Gets all non-abstract Component types that implement given interface
		/// </summary>
		public static IEnumerable<Type> GetImplementingComponentTypes(this Type interfaceType, bool includeInvisible, bool includeAbstract = true)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(interfaceType.IsInterface, "GetImplementingComponentTypes was called for type " + interfaceType.Name + " which wasn't an interface. Use GetExtendingComponentTypes instead.");
			#endif

			if(interfaceType.IsGenericTypeDefinition)
            {
				foreach(var t in GetImplementingTypesForInterfaceGenericTypeDefinition(interfaceType, includeInvisible, includeAbstract).Where(IsComponent))
                {
					yield return t;
                }
				yield break;
			}
			
			if(!IsReady)
			{
				foreach(var t in GetAllTypesThreadSafe(interfaceType.Assembly, false, includeInvisible, true).Where((t) => interfaceType.IsAssignableFrom(t)))
                {
					yield return t;
				}
			}
			else
			{
				for(int n = componentTypes.Length - 1; n >= 0; n--)
				{
					var t = componentTypes[n];
					if(interfaceType.IsAssignableFrom(t) && t != interfaceType)
					{
						yield return t;
					}
				}

				if(includeInvisible)
				{
					for(int n = allInvisibleTypes.Length - 1; n >= 0; n--)
					{
						var t = allInvisibleTypes[n];
						if(interfaceType.IsAssignableFrom(t))
						{
							yield return t;
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets all non-abstract ScriptableObject types that extend given Type
		/// </summary>
		private static IEnumerable<Type> GetExtendingEditorTypes([NotNull] this Type type, bool includeInvisible, bool includeAbstract = false)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(type != null);
			Debug.Assert(Types.Editor.IsAssignableFrom(type), "GetExtendingEditorTypes was called for type " + type.Name + " which wasn't assignable from Editor.");
			Debug.Assert(type.IsClass, "GetExtendingEditorTypes was called for " + type.Name + " which wasn't a class type. Value types and interfaces don't support inheritance.");
			#endif

			if(!IsReady)
			{
				if(type.IsGenericTypeDefinition)
				{
					return GetAllTypesThreadSafe(type.Assembly, includeAbstract, includeInvisible, true).Where((t) => t.IsSubclassOfUndeclaredGeneric(type)).ToArray();
				}
				return GetAllTypesThreadSafe(type.Assembly, includeAbstract, includeInvisible, true).Where((t) => t.IsSubclassOf(type)).ToArray();
			}

			if(type == Types.Editor)
			{
				if(includeInvisible)
				{
					return editorTypes.Join(invisibleEditorTypes);
				}
				return editorTypes;
			}

			var TypesList = new List<Type>(10);

			if(type.IsGenericTypeDefinition)
			{
				for(int n = editorTypes.Length - 1; n >= 0; n--)
				{
					var t = editorTypes[n];
					if(t.IsSubclassOfUndeclaredGeneric(type))
					{
						TypesList.Add(t);
					}
				}

				if(includeInvisible)
				{
					for(int n = invisibleEditorTypes.Length - 1; n >= 0; n--)
					{
						var t = invisibleEditorTypes[n];
						if(t.IsSubclassOfUndeclaredGeneric(type))
						{
							TypesList.Add(t);
						}
					}
				}

				if(includeAbstract)
				{
					for(int n = abstractTypes.Length - 1; n >= 0; n--)
					{
						var t = abstractTypes[n];
						if(t.IsSubclassOfUndeclaredGeneric(type))
						{
							TypesList.Add(t);
						}
					}

					if(includeInvisible)
					{
						for(int n = invisibleAbstractTypes.Length - 1; n >= 0; n--)
						{
							var t = invisibleAbstractTypes[n];
							if(t.IsSubclassOfUndeclaredGeneric(type))
							{
								TypesList.Add(t);
							}
						}
					}
				}
			}
			else
			{
				for(int n = editorTypes.Length - 1; n >= 0; n--)
				{
					var t = editorTypes[n];
					if(t.IsSubclassOf(type))
					{
						TypesList.Add(t);
					}
				}

				if(includeInvisible)
				{
					for(int n = invisibleEditorTypes.Length - 1; n >= 0; n--)
					{
						var t = invisibleEditorTypes[n];
						if(t.IsSubclassOf(type))
						{
							TypesList.Add(t);
						}
					}
				}

				if(includeAbstract)
				{
					for(int n = abstractTypes.Length - 1; n >= 0; n--)
					{
						var t = abstractTypes[n];
						if(t.IsSubclassOf(type))
						{
							TypesList.Add(t);
						}
					}

					if(includeInvisible)
					{
						for(int n = invisibleAbstractTypes.Length - 1; n >= 0; n--)
						{
							var t = invisibleAbstractTypes[n];
							if(t.IsSubclassOf(type))
							{
								TypesList.Add(t);
							}
						}
					}
				}
			}
			return TypesList.ToArray();
		}

		/// <summary>
		/// Gets all visible and non-abstract types that implement given interface
		/// </summary>
		/// <param name="baseType"> abstract, interface or other base class type to act on. </param>
		/// <param name="implementingUnityObjectTypes"> [out] List of types of the implementing unity objects. </param>
		/// <param name="implementingNonUnityObjectTypes"> [out] List of types of the implementing non unity objects. </param>
		public static void GetImplementingConcreteTypes([NotNull] this Type baseType, [NotNull] out IEnumerable<Type> implementingUnityObjectTypes, [NotNull] out IEnumerable<Type> implementingNonUnityObjectTypes)
		{
			var extendingTypes = baseType.GetImplementingTypes(true, false);

			if(baseType.IsUnityObject())
            {
				implementingUnityObjectTypes = extendingTypes;
				implementingNonUnityObjectTypes = Enumerable.Empty<Type>();
				return;
			}			
			implementingUnityObjectTypes = extendingTypes.Where(IsUnityObject);
			implementingNonUnityObjectTypes = extendingTypes.Where((t)=>!t.IsUnityObject());
		}

		/// <summary>
		/// Gets all non-abstract types that are not assignable from UnityEngine.Object and extend given Type
		/// </summary>
		private static IEnumerable<Type> GetExtendingNonUnityObjectClassTypes([NotNull] this Type type, bool includeInvisible, bool includeAbstract = false)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!Types.UnityObject.IsAssignableFrom(type), "GetExtendingEditorTypes was called for type " + type.Name + " which WAS assignable from UnityEngine.Object.");
			Debug.Assert(type.IsClass, "GetExtendingUnityObjectTypes was called for " + type.Name + " which wasn't a class type. Value types and interfaces don't support inheritance.");
			#endif

			if(!IsReady)
			{
				if(type.IsGenericTypeDefinition)
				{
					return GetAllTypesThreadSafe(type.Assembly, includeAbstract, includeInvisible, true).Where((t) => t.IsSubclassOfUndeclaredGeneric(type)).ToArray();
				}
				return GetAllTypesThreadSafe(type.Assembly, includeAbstract, includeInvisible, true).Where((t) => t.IsSubclassOf(type)).ToArray();
			}

			var TypesList = new List<Type>(10);

			if(type.IsGenericTypeDefinition)
			{
				for(int n = nonUnityObjectClassTypes.Length - 1; n >= 0; n--)
				{
					var t = nonUnityObjectClassTypes[n];
					if(t.IsSubclassOfUndeclaredGeneric(type))
					{
						TypesList.Add(t);
					}
				}

				if(includeInvisible)
				{
					for(int n = invisibleNonUnityObjectClassTypes.Length - 1; n >= 0; n--)
					{
						var t = invisibleNonUnityObjectClassTypes[n];
						if(t.IsSubclassOfUndeclaredGeneric(type))
						{
							//e.g. UnityEngine.Events.UnityEventBase
							TypesList.Add(t);
						}
					}
				}

				if(includeAbstract)
				{
					for(int n = abstractTypes.Length - 1; n >= 0; n--)
					{
						var t = abstractTypes[n];
						if(t.IsSubclassOfUndeclaredGeneric(type))
						{
							TypesList.Add(t);
						}
					}

					if(includeInvisible)
					{
						for(int n = invisibleAbstractTypes.Length - 1; n >= 0; n--)
						{
							var t = invisibleAbstractTypes[n];
							if(t.IsSubclassOfUndeclaredGeneric(type))
							{
								TypesList.Add(t);
							}
						}
					}
				}
			}
			else
			{
				for(int n = nonUnityObjectClassTypes.Length - 1; n >= 0; n--)
				{
					var t = nonUnityObjectClassTypes[n];
					if(t.IsSubclassOf(type))
					{
						TypesList.Add(t);
					}
				}

				if(includeInvisible)
				{
					for(int n = invisibleNonUnityObjectClassTypes.Length - 1; n >= 0; n--)
					{
						var t = invisibleNonUnityObjectClassTypes[n];
						if(t.IsSubclassOf(type))
						{
							//e.g. UnityEngine.Events.UnityEventBase
							TypesList.Add(t);
						}
					}
				}

				if(includeAbstract)
				{
					for(int n = abstractTypes.Length - 1; n >= 0; n--)
					{
						var t = abstractTypes[n];
						if(t.IsSubclassOf(type))
						{
							TypesList.Add(t);
						}
					}

					if(includeInvisible)
					{
						for(int n = invisibleAbstractTypes.Length - 1; n >= 0; n--)
						{
							var t = invisibleAbstractTypes[n];
							if(t.IsSubclassOf(type))
							{
								TypesList.Add(t);
							}
						}
					}
				}
			}

			return TypesList.ToArray();
		}

		/// <summary>
		/// Gets all non-abstract class types that are not assignable from UnityEngine.Object and implement given interface
		/// </summary>
		private static IEnumerable<Type> GetImplementingNonUnityObjectTypes(this Type interfaceType, bool includeInvisible)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(interfaceType.IsInterface, "GetImplementingNonUnityObjectClassTypes was called for type " + interfaceType.Name + " which wasn't an interface. Use GetExtendingNonUnityObjectClassTypes instead.");
			#endif

			return GetAllTypesThreadSafe(interfaceType.Assembly, false, includeInvisible, true).Where((t) => interfaceType.IsAssignableFrom(t) && !typeof(Object).IsAssignableFrom(t));
		}

				/// <summary>
		/// Gets all non-abstract types that extend the provided base class type
		/// </summary>
		/// <param name="type"> The type for which all extending types are returned. </param>
		/// <param name="includeInvisible"> (Optional) True to also return invisible (not publically accessible) classes. </param>
		/// <returns> An array of extending types. </returns>
		public static IEnumerable<Type> GetExtendingTypesNotThreadSafe<T>(bool includeInvisible = true, bool includeAbstract = false) where T : class
		{
			#if UNITY_2019_3_OR_NEWER
			if(!typeof(T).IsGenericTypeDefinition)
			{
				if(!includeAbstract)
				{
					if(!includeInvisible)
					{
						return TypeCache.GetTypesDerivedFrom<T>().Where(t => !t.IsAbstract && t.IsVisible);
					}
					return TypeCache.GetTypesDerivedFrom<T>().Where(t => !t.IsAbstract);
				}
				if(!includeInvisible)
				{
					return TypeCache.GetTypesDerivedFrom<T>().Where(t => t.IsVisible);
				}
				return TypeCache.GetTypesDerivedFrom<T>();
			}
			#endif
			return GetExtendingTypes<T>(includeInvisible, includeAbstract);
		}

		/// <summary>
		/// Gets all non-abstract types that extend the provided base class type
		/// </summary>
		/// <param name="type"> The type for which all extending types are returned. </param>
		/// <param name="includeInvisible"> (Optional) True to also return invisible (not publically accessible) classes. </param>
		/// <returns> An array of extending types. </returns>
		public static IEnumerable<Type> GetExtendingTypes<T>(bool includeInvisible, bool includeAbstract = false) where T : class
		{
			if(typeof(T).IsUnityObject())
			{
				if(typeof(T).IsComponent())
				{
					return GetExtendingComponentTypes(typeof(T), includeInvisible, includeAbstract);
				}

				if(Types.Editor.IsAssignableFrom(typeof(T)))
				{
					return GetExtendingEditorTypes(typeof(T), includeInvisible, includeAbstract);
				}

				return GetExtendingUnityObjectTypes(typeof(T), includeInvisible, includeAbstract);
			}

			return GetExtendingNonUnityObjectClassTypes(typeof(T), includeInvisible, includeAbstract);
		}

		/// <summary>
		/// Gets all non-abstract types that extend the provided base class type
		/// </summary>
		/// <param name="type"> The type for which all extending types are returned. </param>
		/// <param name="includeInvisible"> (Optional) True to also return invisible (not publically accessible) classes. </param>
		/// <returns> An array of extending types. </returns>
		public static IEnumerable<Type> GetExtendingTypesNotThreadSafe([NotNull] this Type type, bool includeInvisible, bool includeAbstract = false)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(type.IsClass, "GetExtendingTypes was called for " + type.Name + " which wasn't a class type. Value types and interfaces don't support inheritance.");
			#endif

			#if UNITY_2019_3_OR_NEWER
			if(!type.IsGenericTypeDefinition)
			{
				if(!includeAbstract)
				{
					if(!includeInvisible)
					{
						return TypeCache.GetTypesDerivedFrom(type).Where(t => !t.IsAbstract && t.IsVisible);
					}
					return TypeCache.GetTypesDerivedFrom(type).Where(t => !t.IsAbstract);
				}
				if(!includeInvisible)
				{
					return TypeCache.GetTypesDerivedFrom(type).Where(t => t.IsVisible);
				}
				return TypeCache.GetTypesDerivedFrom(type);
			}
			#endif
			return type.GetExtendingTypes(includeInvisible, includeAbstract);
		}

		/// <summary>
		/// Gets all non-abstract types that extend the provided base class type
		/// </summary>
		/// <param name="type"> The type for which all extending types are returned. </param>
		/// <param name="includeInvisible"> (Optional) True to also return invisible (not publically accessible) classes. </param>
		/// <returns> An array of extending types. </returns>
		public static IEnumerable<Type> GetExtendingTypes([NotNull] this Type type, bool includeInvisible, bool includeAbstract = false)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(type.IsClass, "GetExtendingTypes was called for " + type.Name + " which wasn't a class type. Value types and interfaces don't support inheritance.");
			#endif

			if(type.IsUnityObject())
			{
				if(type.IsComponent())
				{
					return GetExtendingComponentTypes(type, includeInvisible, includeAbstract);
				}

				if(Types.Editor.IsAssignableFrom(type))
				{
					return GetExtendingEditorTypes(type, includeInvisible, includeAbstract);
				}

				return GetExtendingUnityObjectTypes(type, includeInvisible, includeAbstract);
			}

			return GetExtendingNonUnityObjectClassTypes(type, includeInvisible, includeAbstract);
		}

		public static Type GetRandomEnumType()
		{
			int index = UnityEngine.Random.Range(0, GetEnumTypeCount());
			int n = 0;
			foreach(var keyValuePair in enumTypesByName)
			{
				if(n == index)
				{
					var fullNameAndtype = keyValuePair.Value;
					return fullNameAndtype[UnityEngine.Random.Range(0, fullNameAndtype.Length)].Value;
				}
				n++;
			}
			throw new IndexOutOfRangeException();
		}

		public static IEnumerable<Type> GetTypesWithAttributeNotThreadSafe<TAttribute>() where TAttribute : Attribute
		{
			#if UNITY_2019_3_OR_NEWER
			return TypeCache.GetTypesWithAttribute<TAttribute>();
			#else
			return GetTypesWithAttribute(typeof(TAttribute));
			#endif
		}

		public static IEnumerable<Type> GetTypesWithAttributeNotThreadSafe(Type attributeType)
		{
			#if UNITY_2019_3_OR_NEWER
			return TypeCache.GetTypesWithAttribute(attributeType);
			#else
			return GetTypesWithAttribute(attributeType);
			#endif
		}

		public static IEnumerable<Type> GetTypesWithAttribute(Type attributeType)
		{
			return GetAllTypesThreadSafe(attributeType.Assembly, true, true, true).Where(t => AttributeUtility.HasAttribute(t, attributeType));
		}

		private static int GetEnumTypeCount()
		{
			return enumTypesByName.Count;
		}
		
		/// <summary>
		/// Tries to find visible enum type in any assemblies.
		/// Supports finding by assembly qualified name, full name and class name
		/// </summary>
		/// <param name="name"> The assembly qualified name, full name or class name of the enum. </param>
		/// <returns>
		/// enum type matching name or null if not found
		/// </returns>
		[CanBeNull]
		public static Type GetEnumType(string name)
		{
			//UPDATE: support getting by assembly qualified name
			try
			{
				var result = Type.GetType(name);
				if(result != null)
				{
					return result;
				}
			}
			catch{}

			string fullName = null;
			int i = name.LastIndexOf('.');
			if(i != -1)
			{
				if(!IsReady)
				{
					return GetAllTypesThreadSafe(false, false, true).Where((t) => t.IsEnum && string.Equals(t.FullName, name)).SingleOrDefault();
				}

				fullName = name;
				name = name.Substring(i+1);
			}
			else if(!IsReady)
			{
				return GetAllTypesThreadSafe(false, false, true).Where((t) => t.IsEnum && string.Equals(t.Name, name)).SingleOrDefault();
			}

			KeyValuePair<string, Type>[] keyValuePairs;
			if(enumTypesByName.TryGetValue(name, out keyValuePairs))
			{
				if(fullName != null)
				{
					for(int n = keyValuePairs.Length - 1; n >= 0; n--)
					{
						var pair = keyValuePairs[n];
						if(string.Equals(pair.Key, fullName))
						{
							return pair.Value;
						}
					}
					#if DEV_MODE
					Debug.LogWarning("GetEnumType("+name+"): found Type that matches name but not namespace: "+ keyValuePairs[0].Key);
					#endif
					return null;
				}
				return keyValuePairs[0].Value;
			}
			return null;
		}

		/// <summary>
		/// Tries to find type in any assemblies.
		/// Supports finding by assembly qualified name, full name and class name
		/// </summary>
		/// <param name="name"> The assembly qualified name, full name or class name of the type. </param>
		/// <returns>
		/// type matching name or null if not found
		/// </returns>
		[CanBeNull]
		public static Type GetType(string name)
		{
			string fullName = null;
			int i = name.LastIndexOf('.');
			if(i != -1)
			{
				// assembly qualified name
				if(name.IndexOf(',') != -1)
				{
					var result = Type.GetType(name, false);
					#if DEV_MODE && DEBUG_GET_TYPE
					Debug.Log("GetType(\"" + name + "\") Type.GetType returned null for assembly qualified name \"" + name + "\".");
					#endif
					return result;
				}

				if(!IsReady)
				{
					return GetAllTypesThreadSafe(true, true, true).Where((t) => string.Equals(t.FullName, name)).SingleOrDefault();
				}

				fullName = name;
				name = name.Substring(i+1);
			}
			else if(!IsReady)
			{
				return GetAllTypesThreadSafe(true, true, true).Where((t) => string.Equals(t.Name, name)).SingleOrDefault();
			}

			KeyValuePair<string, Type>[] keyValuePairs;
			if(TypesByName.TryGetValue(name, out keyValuePairs))
			{
				if(fullName != null)
				{
					for(int n = keyValuePairs.Length - 1; n >= 0; n--)
					{
						var pair = keyValuePairs[n];
						if(string.Equals(pair.Key, fullName))
						{
							#if DEV_MODE && DEBUG_GET_TYPE
							if(pair.Value == null) { Debug.Log("GetType(\"" + name + "\") returning null via keyValuePairs["+n+"] which matched fullName \""+fullName+"\""); }
							#endif
							return pair.Value;
						}
					}
					#if DEV_MODE && DEBUG_GET_TYPE
					Debug.Log("GetType(\"" + name + "\") returning null because no match found for fullName=\"" + fullName + "\" in keyValuePairs:\n" + StringUtils.ToString(keyValuePairs, "\n"));
					#endif
					return null;
				}

				#if DEV_MODE && DEBUG_GET_TYPE
				if(keyValuePairs[0].Value == null) { Debug.Log("GetType(\"" + name + "\") returning null via keyValuePairs[0]: with fullName=null"); }
				#endif

				return keyValuePairs[0].Value;
			}

			#if DEV_MODE && DEBUG_GET_TYPE
			Debug.Log("GetType(\""+name+"\") returning null. Entry not found in among " + TypesByName.Count + " TypesByName entries.");
			#endif

			return null;
		}

		/// <summary>
		/// Tries to find visible type in any assemblies.
		/// Supports finding by assembly qualified name, full name and class name
		/// </summary>
		/// <param name="name"> The assembly qualified name, full name or class name of the type. </param>
		/// <param name="baseType"> Base type or interface of returned type. </param>
		/// <returns> Type matching name or null if not found. </returns>
		[CanBeNull]
		public static Type GetType(string name, [NotNull]Type baseType)
		{
			string fullName = null;
			int i = name.LastIndexOf('.');
			if(i != -1)
			{
				// assembly qualified name
				if(name.IndexOf(',') != -1)
				{
					try
					{
						return Type.GetType(name);
					}
					catch
					{
						return null;
					}
				}

				if(!IsReady)
				{
					return GetAllTypesThreadSafe(true, true, true).Where((t) => baseType.IsAssignableFrom(t) && string.Equals(t.FullName, name)).SingleOrDefault();
				}

				fullName = name;
				name = name.Substring(i+1);
			}
			else if(!IsReady)
			{
				return GetAllTypesThreadSafe(true, true, true).Where((t) => baseType.IsAssignableFrom(t) && string.Equals(t.Name, name)).SingleOrDefault();
			}

			KeyValuePair<string, Type>[] keyValuePairs;
			if(TypesByName.TryGetValue(name, out keyValuePairs))
			{
				if(fullName != null)
				{
					for(int n = keyValuePairs.Length - 1; n >= 0; n--)
					{
						var pair = keyValuePairs[n];
						if(string.Equals(pair.Key, fullName))
						{
							if(baseType.IsAssignableFrom(pair.Value))
							{
								return pair.Value;
							}
						}
					}
					#if DEV_MODE && DEBUG_GET_TYPE
					Debug.Log("GetType(\""+name+"\") returning null because no match with fullName=\""+fullName+ "\" and assignable from " + baseType.FullName + " found in keyValuePairs:\n" + StringUtils.ToString(keyValuePairs, "\n"));
					#endif
					return null;
				}

				for(int n = keyValuePairs.Length - 1; n >= 0; n--)
				{
					var pair = keyValuePairs[n];
					if(baseType.IsAssignableFrom(pair.Value))
					{
						return pair.Value;
					}
				}

				#if DEV_MODE && DEBUG_GET_TYPE
				Debug.Log("GetType(\""+name+"\") returning null because no type assignable from "+ baseType.FullName + " found in keyValuePairs:\n" + StringUtils.ToString(keyValuePairs, "\n"));
				#endif
				return null;
			}

			#if DEV_MODE && DEBUG_GET_TYPE
			Debug.Log("GetType(\""+name+"\", "+ baseType.FullName+") returning null. Entry not found in among " + TypesByName.Count + " TypesByName entries.");
			#endif

			return null;
		}

		/// <summary>
		/// Gets all non-abstract types that implement the provided interface type.
		/// </summary>
		/// <param name="interfaceType">
		/// The interface for which all implementing types are returned.
		/// This can not be a generic type definition.
		/// E.g. IList{int} is legal, but IList{T} is not.
		/// </param>
		/// <param name="includeInvisible"> (Optional) True to also return invisible (not publically accessible) classes. </param>
		/// <returns> Array of implementing types. </returns>
		public static IEnumerable<Type> GetImplementingTypesNotThreadSafe([NotNull]this Type interfaceType, bool includeInvisible = true, bool includeAbstract = true)
		{
			#if UNITY_2019_3_OR_NEWER
			if(!interfaceType.IsGenericTypeDefinition)
			{
				if(!includeAbstract)
				{
					if(!includeInvisible)
					{
						return TypeCache.GetTypesDerivedFrom(interfaceType).Where(t => !t.IsAbstract && t.IsVisible);
					}
					return TypeCache.GetTypesDerivedFrom(interfaceType).Where(t => !t.IsAbstract);
				}
				if(!includeInvisible)
				{
					return TypeCache.GetTypesDerivedFrom(interfaceType).Where(t => t.IsVisible);
				}
				return TypeCache.GetTypesDerivedFrom(interfaceType);
			}
			#endif

			return interfaceType.GetImplementingTypes(includeInvisible, includeAbstract);
		}

		/// <summary>
		/// Gets all non-abstract types that implement the provided interface type.
		/// </summary>
		/// <param name="interfaceType">
		/// The interface for which all implementing types are returned.
		/// This can not be a generic type definition.
		/// E.g. IList{int} is legal, but IList{T} is not.
		/// </param>
		/// <param name="includeInvisible"> (Optional) True to also return invisible (not publically accessible) classes. </param>
		/// <returns> Array of implementing types. </returns>
		public static IEnumerable<Type> GetImplementingTypes([NotNull]this Type interfaceType, bool includeInvisible = true, bool includeAbstract = true)
		{
			if(interfaceType.IsGenericTypeDefinition)
			{
				return GetImplementingTypesForInterfaceGenericTypeDefinition(interfaceType, includeInvisible, includeAbstract);
			}

			#if UNITY_2019_3_OR_NEWER
			if(!includeAbstract)
			{
				if(!includeInvisible)
				{
					return TypeCache.GetTypesDerivedFrom(interfaceType).Where(t => !t.IsAbstract && t.IsVisible);
				}
				return TypeCache.GetTypesDerivedFrom(interfaceType).Where(t => !t.IsAbstract);
			}
			if(!includeInvisible)
			{
				return TypeCache.GetTypesDerivedFrom(interfaceType).Where(t => t.IsVisible);
			}
			return TypeCache.GetTypesDerivedFrom(interfaceType);
			#else
			return GetImplementingTypesForInterfaceNotGenericTypeDefinition(interfaceType, includeInvisible, includeAbstract);
			#endif
		}

		/// <summary>
		/// Gets all non-abstract types that implement the provided interface type
		/// </summary>
		/// <param name="interfaceType"> The interfaceType target for which all implementing types are returned. </param>
		/// <param name="includeInvisible"> (Optional) True to also return invisible (not publically accessible) classes. </param>
		/// <returns> Array of implementing types. </returns>
		private static IEnumerable<Type> GetImplementingTypesForInterfaceNotGenericTypeDefinition([NotNull]this Type interfaceType, bool includeInvisible, bool includeAbstract)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(interfaceType.IsGenericTypeDefinition) { Debug.LogError("GetImplementingTypesForInterfaceNotGenericTypeDefinition called with generic type: "+interfaceType.Name); }
			if(!interfaceType.IsInterface) { Debug.LogError("GetImplementingTypesForInterfaceNotGenericTypeDefinition called with type which was not an interface: "+interfaceType.Name); }
			#endif

			foreach(var type in GetAllTypesThreadSafe(interfaceType.Assembly, includeAbstract, includeInvisible, true))
			{
				if(interfaceType.IsAssignableFrom(type))
				{
					yield return type;
				}
			}
		}
		
		/// <summary>
		/// Gets all non-abstract types that implement the given generic type definition of an interface.
		/// 
		/// E.g. If 'interfaceGenericTypeDefinition' is of type IList{T}, then returns types that implement IList{int}, IList{string}, List{float} etc.
		/// </summary>
		/// <param name="interfaceGenericTypeDefinition"> The generic type definition of an interface for which all implementing types are returned. </param>
		/// <param name="includeInvisible"> (Optional) True to also return invisible (not publically accessible) classes. </param>
		/// <returns> Array of implementing types. </returns>
		public static IEnumerable<Type> GetImplementingTypesForInterfaceGenericTypeDefinition([NotNull]this Type interfaceGenericTypeDefinition, bool includeInvisible = false, bool includeAbstract = false)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(!interfaceGenericTypeDefinition.IsGenericTypeDefinition) { Debug.LogError("GetImplementingTypesForGenericInterface called with type which was not a generic type definition: " + interfaceGenericTypeDefinition.Name); }
			if(!interfaceGenericTypeDefinition.IsInterface) { Debug.LogError("GetImplementingTypesForGenericInterface called with type which was not an interface: " + interfaceGenericTypeDefinition.Name); }
			#endif

			foreach(var type in GetAllTypesThreadSafe(interfaceGenericTypeDefinition.Assembly, includeAbstract, includeInvisible, true))
			{
				var testInterfaces = type.GetInterfaces();
				for(int i = testInterfaces.Length - 1; i >= 0; i--)
				{
					var testType = testInterfaces[i];
					do
					{
						if(testType.IsGenericType)
						{
							if(testType.IsGenericTypeDefinition)
							{
								if(testType == interfaceGenericTypeDefinition)
								{
									yield return type;
								}
							}
							else if(testType.GetGenericTypeDefinition() == interfaceGenericTypeDefinition)
							{
								yield return type;
							}
						}
						testType = testType.BaseType;
					}
					while(testType != null);
				}
			}
		}
		
		public static bool IsAssetType(this Type type)
		{
			if(Types.ScriptableObject.IsAssignableFrom(type))
			{
				return true;
			}

			if(Types.Motion.IsAssignableFrom(type))
			{
				return true;
			}

			if(type == Types.AudioClip)
			{
				return true;
			}

			if(type == typeof(TextAsset))
			{
				return true;
			}
			
			return false;
		}

		public static string GetNamespaceParent(string namespaceString)
		{
			int i = namespaceString.LastIndexOf('.');
			if(i == -1)
			{
				return namespaceString;
			}
			return namespaceString.Substring(0, i);
		}

		public static string[] GetPopupMenuLabels(IEnumerable<Type> types)
		{
			var list = types as IList<Type>;
			if(list != null)
			{
				int count = list.Count;
				var labels = new string[count];
				for(int n = count - 1; n >= 0; n--)
				{
					labels[n] = GetPopupMenuLabel(list[n]);
				}
				return labels;
			}
			else
			{
				int count = types.Count();
				var labels = new string[count];
				int i = 0;
				foreach(var type in types)
				{
					labels[i] = GetPopupMenuLabel(type);
					i++;
				}
				return labels;
			}
		}

		public static string GetPopupMenuLabel([NotNull]Type type, [NotNull]string globalNamespaceTypePrefix)
		{
			if(type.Namespace == null)
			{
				return string.Concat(globalNamespaceTypePrefix, GetPopupMenuLabel(type));
			}
			return GetPopupMenuLabel(type);
		}
		
		public static string GetPopupMenuLabel([NotNull]Type type)
		{
			string result;
			if(TypePopupMenuPaths.TryGetValue(type, out result))
			{
				return result;
			}
			
			var sb = new StringBuilder();

			StringUtils.ToString(type, '/', sb);
			var menuPath = sb.ToString();
			TypePopupMenuPaths.Add(type, menuPath);

			sb.Replace('/', '.');
			TypeFullNames.Add(type, sb.ToString());

			sb.Length = 0;
			StringUtils.ToString(type, '\0', sb);
			TypeNames.Add(type, sb.ToString());

			return menuPath;
		}

		public static string GetFullName([NotNull]Type type)
		{
			string result;
			if(TypeFullNames.TryGetValue(type, out result))
			{
				return result;
			}
			
			var sb = new StringBuilder();

			StringUtils.ToString(type, '/', sb);
			var menuPath = sb.ToString();
			TypePopupMenuPaths.Add(type, menuPath);

			sb.Replace('/', '.');
			string fullName = sb.ToString();
			TypeFullNames.Add(type, fullName);

			sb.Length = 0;
			StringUtils.ToString(type, '\0', sb);
			TypeNames.Add(type, sb.ToString());

			return fullName;
		}

		public static string GetShortName([NotNull]Type type)
		{
			#if SAFE_MODE
			if(type == null)
			{
				#if DEV_MODE
				Debug.LogError("TypeExtensions.GetShortName called with null type.");
				#endif
				return "null";
			}
			#endif

			string result;
			if(TypeNames.TryGetValue(type, out result))
			{
				return result;
			}

			var sb = new StringBuilder();
			StringUtils.ToString(type, '/', sb);
			TypePopupMenuPaths.Add(type, sb.ToString());

			sb.Replace('/', '.');
			TypeFullNames.Add(type, sb.ToString());

			sb.Length = 0;
			StringUtils.ToString(type, '\0', sb);
			string shortName = sb.ToString();
			TypeNames.Add(type, shortName);

			return shortName;
		}

		public static bool ContainsSerializedObjectReferenceFields([NotNull]this Type type, ref bool depthLimitReached, int depthLimit = 7)
		{
			if(type.IsPrimitive)
			{
				#if DEV_MODE && DEBUG_CONTAINS_OBJECT_REFERENCES
				Debug.Log("ContainsSerializedObjectReferenceFields("+type.Name+"): "+StringUtils.False + " (Primitive)");
				#endif
				return false;
			}

			if(type.IsUnityObject())
			{
				#if DEV_MODE && DEBUG_CONTAINS_OBJECT_REFERENCES
				Debug.Log("ContainsSerializedObjectReferenceFields("+type.Name+"): "+StringUtils.True + " (this.IsUnityObject)");
				#endif
				return true;
			}
			
			if(depthLimit <= 0)
			{
				#if DEV_MODE
				Debug.LogWarning("ContainsSerializedObjectReferenceFields returning false for type "+type.Name+" depth limit was reached; possible recursive self-reference");
				#endif
				depthLimitReached = true;
				return false;
			}

			if(type.IsArray)
			{
				var elementTypeContains = type.GetElementType().ContainsSerializedObjectReferenceFields(ref depthLimitReached, depthLimit - 1);
				#if DEV_MODE && DEBUG_CONTAINS_OBJECT_REFERENCES
				Debug.Log("ContainsSerializedObjectReferenceFields("+type.Name+"): "+StringUtils.ToColorizedString(elementTypeContains) + " (via array element type) with depthLimit="+StringUtils.ToColorizedString(depthLimit)+", depthLimitReached="+StringUtils.ToColorizedString(depthLimitReached));
				#endif
				return elementTypeContains;
			}

			if(type.IsGenericType)
			{
				var genericArguments = type.GetGenericArguments();
				for(int n = genericArguments.Length - 1; n >= 0; n--)
				{
					if(genericArguments[n].ContainsSerializedObjectReferenceFields(ref depthLimitReached, depthLimit - 1))
					{
						#if DEV_MODE && DEBUG_CONTAINS_OBJECT_REFERENCES
						Debug.Log("ContainsSerializedObjectReferenceFields("+type.Name+"): "+StringUtils.False + " (via generic argument "+genericArguments[n].Name+") with depthLimit="+StringUtils.ToColorizedString(depthLimit)+", depthLimitReached="+StringUtils.ToColorizedString(depthLimitReached));
						#endif
						return true;
					}
				}
			}

			var fields = type.GetFields(BindingFlags.Instance | BindingFlags.FlattenHierarchy);
			for(int n = fields.Length - 1; n >= 0; n--)
			{
				var field = fields[n];
				if(field.FieldType.ContainsSerializedObjectReferenceFields(ref depthLimitReached, depthLimit - 1))
				{
					#if DEV_MODE && DEBUG_CONTAINS_OBJECT_REFERENCES
					Debug.Log("ContainsSerializedObjectReferenceFields("+type.Name+"): "+StringUtils.True + " (via field \""+field.Name+"\" of type "+field.FieldType.Name+") with depthLimit="+StringUtils.ToColorizedString(depthLimit)+", depthLimitReached="+StringUtils.ToColorizedString(depthLimitReached));
					#endif
					return true;
				}
			}

			var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.FlattenHierarchy);
			for(int n = properties.Length - 1; n >= 0; n--)
			{
				var property = properties[n];
				if(property.PropertyType.ContainsSerializedObjectReferenceFields(ref depthLimitReached, depthLimit - 1))
				{
					#if DEV_MODE && DEBUG_CONTAINS_OBJECT_REFERENCES
					Debug.Log("ContainsSerializedObjectReferenceFields("+StringUtils.ToStringSansNamespace(type)+"): "+StringUtils.True + " (via property \""+property.Name+"\" of type "+StringUtils.ToStringSansNamespace(property.PropertyType)+") with depthLimit="+StringUtils.ToColorizedString(depthLimit)+", depthLimitReached="+StringUtils.ToColorizedString(depthLimitReached));
					#endif
					return true;
				}
			}

			#if DEV_MODE && DEBUG_CONTAINS_OBJECT_REFERENCES
			Debug.Log("ContainsSerializedObjectReferenceFields("+StringUtils.ToStringSansNamespace(type)+"): "+StringUtils.False + " (tested "+fields.Length+" fields and "+properties.Length+" properties)\n\nfields:\n"+StringUtils.ToString(fields, "\n")+"\n\nproperties:\n"+StringUtils.ToString(properties, "\n"));
			#endif

			return false;
		}
		
		public static bool NestedMemberCountExceeds([NotNull]this Type type, ref int maxCount, int maxDepth = 2, bool includeProperties = true)
		{
			if(type.IsPrimitive)
			{
				return false;
			}
			
			if(maxDepth <= 0)
			{
				#if DEV_MODE
				Debug.LogWarning("NestedMemberCountExceeds returning false for type " + type.Name+" depth limit was reached; possible recursive self-reference");
				#endif
				return true;
			}

			if(type.IsArray)
			{
				return type.GetElementType().NestedMemberCountExceeds(ref maxCount, maxDepth);
			}
			
			var fields = type.GetFields(BindingFlags.Instance | BindingFlags.FlattenHierarchy);
			int fieldCount = fields.Length;
			maxCount -= fieldCount;
			if(maxCount <= 0)
			{
				return true;
			}

			if(includeProperties)
			{
				var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.FlattenHierarchy);
				int propertyCount = properties.Length;
				maxCount -= propertyCount;
				if(maxCount <= 0)
				{
					return true;
				}
				
				for(int n = propertyCount - 1; n >= 0; n--)
				{
					if(properties[n].PropertyType.NestedMemberCountExceeds(ref maxCount, maxDepth - 1))
					{
						return true;
					}
				}
			}

			for(int n = fieldCount - 1; n >= 0; n--)
			{
				if(fields[n].FieldType.NestedMemberCountExceeds(ref maxCount, maxDepth - 1))
				{
					return true;
				}
			}

			return false;
		}
		
		/// <summary>
		/// Tests whether or not 'testType' is subclass of given undeclared generic type.
		/// </summary>
		/// <param name="testType"> The type to test. </param>
		/// <param name="undeclaredGenericType"> An undeclared generic type. </param>
		/// <returns> True if testType derives from undeclaredGenericType, false if not. </returns>
		/// <example>
		/// <code>
		/// public class SomeValue&lt;T>
		/// {
		///		public T value;
		/// }
		/// 
		/// public class IntValue : SomeValue&lt;int>
		/// {
		///		public void TestIsSubclassOfSomeValue()
		///		{
		///			Debug.Log(GetType().IsSubclassOfUndeclaredGeneric(typeof(SomeValue&lt;int>))); //prints "True"
		///		}
		/// }
		/// </code>
		/// </example>
		public static bool IsSubclassOfUndeclaredGeneric([NotNull]this Type testType, [NotNull]Type undeclaredGenericType)
		{
			#if DEV_MODE
			Debug.Assert(testType != null);
			Debug.Assert(undeclaredGenericType != null);
			Debug.Assert(undeclaredGenericType.IsGenericTypeDefinition);
			#endif

			do
			{
				var test = testType.IsGenericType ? testType.GetGenericTypeDefinition() : testType;
				if(test == undeclaredGenericType)
				{
					return true;
				}

				testType = testType.BaseType;
			}
			while(testType != Types.SystemObject && testType != null);
 
			return false;
		}

		/// <summary>
		/// If 'type' is generic type, returns its generic type definition.
		/// If any inherited type of 'type' is generic, returns generic argument of said type.
		/// If any implemented interface of type 'type' is generic, returns generic argument of said type.
		/// Otherwise returns null.
		/// </summary>
		/// <param name="type"> The type whose generic argument we are trying to get. </param>
		/// <returns> Generic argument of type, its inherited type or an interface that it implements. </returns>
		[CanBeNull]
		public static Type GetGenericArgumentOrInheritedGenericArgument([NotNull]this Type type)
		{
			var testType = type;
			do
			{
				if(testType.IsGenericType)
				{
					var genericArguments = testType.GetGenericArguments();
					if(genericArguments.Length == 1)
					{
						return genericArguments[0];
					}
				}
				testType = testType.BaseType;
			}
			while(testType != Types.SystemObject && testType != null);
 
			if(!type.IsInterface)
			{
				var interfaces = type.GetInterfaces();
				for(int n = interfaces.Length - 1; n >= 0; n--)
				{
					var genericArgument = GetGenericArgumentOrInheritedGenericArgument(interfaces[n]);
					if(genericArgument != null)
					{
						return genericArgument;
					}
				}
			}

			return null;
		}

		/// <summary> Gets all Components in hierarchy that implement the given interface type. </summary>
		/// <param name="interfaceType"> Type of the interface. </param>
		/// <param name="foundComponents"> List into which found Components are added, in no particular order. </param>
		/// <param name="foundOther"> List into which found non-Component UnityEngine.Objects are added, in no particular order. </param>
		public static void FindObjectsImplementingInterface(this Type interfaceType, [NotNull]List<Component> foundComponents, [NotNull]List<Object> foundOther)
		{
			var types = interfaceType.GetImplementingUnityObjectTypes(false);
			foreach(var type in types)
			{
				if(type.IsComponent())
				{
					#if UNITY_2023_1_OR_NEWER
					var foundObjects = Object.FindObjectsByType(type, FindObjectsSortMode.None);
					#else
					var foundObjects = Object.FindObjectsOfType(type);
					#endif

					if(foundObjects.Length > 0)
					{
						foundComponents.AddRange(foundObjects as Component[]);
					}
					continue;
				}
				
				#if UNITY_EDITOR
				var assetGuids = AssetDatabase.FindAssets("t:" + type.Name);
				for(int a = 0, assetCount = assetGuids.Length; a < assetCount; a++)
				{
					var guid = assetGuids[a];
					var path = AssetDatabase.GUIDToAssetPath(guid);
					foundOther.Add(AssetDatabase.LoadAssetAtPath(path, type));
				}
				#endif
			}
		}

		public static int GetTypeId([NotNull]Type type)
		{
			if(type.IsGenericType && !type.IsGenericTypeDefinition)
			{
				var genericTypeDefinition = type.GetGenericTypeDefinition();

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(genericTypeDefinition != null);
				Debug.Assert(genericTypeDefinition != type, "type "+type.Name+" GetGenericTypeDefinition returned " + genericTypeDefinition.Name);
				#endif

				var genericTypeDefinitionId = GetTypeId(genericTypeDefinition);
				
				int typeCount = allVisibleTypes.Length + allInvisibleTypes.Length;
				int idComponentMultiplier = typeCount;

				int result = genericTypeDefinitionId;

				var genericTypes = type.GetGenericArguments();
				int genericTypeCount = genericTypes.Length;
				for(int n = 0; n < genericTypeCount; n++)
				{
					var genericType = genericTypes[n];
					int genericTypeId = GetTypeId(genericType);

					result += genericTypeId * idComponentMultiplier;
					idComponentMultiplier *= typeCount;
				}
				return result;
			}

			int id;
			if(TypeIds.TryGet(type, out id))
			{
				return id;
			}
			
			id = TypeIds.Count;
			TypeIds.Add(type, id);

			#if DEV_MODE
			Debug.LogWarning("GetTypeUniqueId("+type.Name+") did not exist yet: "+id);
			#endif

			return id;
		}

		[CanBeNull]
		public static Type GetTypeById(int id)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(id >= 0);
			#endif

			Type type;
			if(TypeIds.TryGetBySecond(id, out type))
			{
				return type;
			}
			return null;
		}
		
		public static bool IsCollection(this Type type)
		{
			return typeof(ICollection).IsAssignableFrom(type);
		}

		public static bool TryGetGenericArgumentsFromInterface([NotNull]this Type sourceType, [NotNull]Type interfaceTypeDefinition, [NotNull]out Type[] genericArguments)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(interfaceTypeDefinition.IsInterface);
			#endif

			//handle special case where sourceType generic type definition equals interface type definition
			if(sourceType.IsInterface && sourceType.IsGenericType && sourceType.GetGenericTypeDefinition() == interfaceTypeDefinition)
			{
				genericArguments = sourceType.GetGenericArguments();
				return true;
			}
			
			// handle class types
			var allInterfaces = sourceType.GetInterfaces();
			for(int n = allInterfaces.Length - 1; n >= 0; n--)
			{
				var testInterface = allInterfaces[n];
				if(testInterface.IsGenericType && testInterface.GetGenericTypeDefinition() == interfaceTypeDefinition)
				{
					genericArguments = testInterface.GetGenericArguments();
					return true;
				}
			}

			genericArguments = ArrayPool<Type>.ZeroSizeArray;
			return false;
		}

		public static bool TryGetGenericArgumentsFromBaseClass([NotNull]this Type sourceType, [NotNull]Type baseClassTypeDefinition, [NotNull]out Type[] genericArguments)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!baseClassTypeDefinition.IsInterface);
			#endif

			for(var type = sourceType; type != null; type = type.BaseType)
			{
				if(type.IsGenericType && type.GetGenericTypeDefinition() == baseClassTypeDefinition)
				{
					genericArguments = type.GetGenericArguments();
					return true;
				}
			}
			
			genericArguments = ArrayPool<Type>.ZeroSizeArray;
			return false;
		}

		/// <summary>
		/// Is the component type one of the built-in base component types that is not marked as abstract but effectively is?
		/// </summary>
		/// <param name="componentType"></param>
		/// <returns> True if component type is base component type. </returns>
		public static bool IsBaseComponentType(this Type type)
		{
			return type == Types.MonoBehaviour
				|| type == Types.Component
				|| type == Types.Behaviour
				|| type == Types.Renderer
				|| type == Types.Collider
				|| type == typeof(Collider2D)

				|| type == typeof(Effector2D)
				#if !UNITY_2019_3_OR_NEWER
				|| type == Types.GUIElement
				#endif
				#if UNITY_2017_2_OR_NEWER
				|| type == Types.GridLayout
				#endif
				|| type == Types.Joint
				|| type == typeof(Joint2D)
				|| type == Types.AnchoredJoint2D
				|| type == typeof(PhysicsUpdateBehaviour2D)
				|| type == Types.Tree
				|| type == Types.ParticleSystemRenderer
				|| type == Types.AudioBehaviour
				;
		}

		public static void GetInspectorViewables(this Type type, ref LinkedMemberHierarchy hierarchy, [CanBeNull]LinkedMemberInfo parentMemberInfo, [NotNull]ref List<LinkedMemberInfo> results, BindingFlags bindingFlags = ParentDrawerUtility.BindingFlagsInstance, bool includeHidden = false, FieldVisibility includeNonSerializedFields = FieldVisibility.SerializedOnly, PropertyVisibility propertyVisibility = PropertyVisibility.AttributeExposedOnly, MethodVisibility methodVisibility = MethodVisibility.AttributeExposedOnly, LinkedMemberParent parentType = LinkedMemberParent.UnityObject)
		{
			bool notUnitySerialized = parentType == LinkedMemberParent.Static || hierarchy.Target == null;

			var order = InspectorUtility.Preferences.MemberDisplayOrder;
			for(int t = 0; t < 3; t++)
			{
				switch(order[t])
				{
					case Member.Field:
						var fromType = type;
						do
						{
							var fields = fromType.GetFields(bindingFlags);
							for(int n = 0, count = fields.Length; n < count; n++)
							{
								var field = fields[n];
								if(!field.IsInspectorViewable(includeHidden, includeNonSerializedFields))
								{
									continue;
								}

								#if DEV_MODE && PI_ASSERTATIONS
								Debug.Assert(!field.IsStatic || parentType == LinkedMemberParent.Static);
								#endif

								// Don't display hidden fields twice (except in debug mode).
								if(!generatedFields.Add(field.Name) && !includeHidden)
								{
									#if DEV_MODE
									Debug.Log("GetInspectorViewables: Skipping " + field.Name+" because field with same name already shown. This is probably because of polymorphism.");
									#endif
									continue;
								}

								var linkedMemberInfo = hierarchy.Get(parentMemberInfo, field, parentType, notUnitySerialized ? null : field.Name);

								// NEW TEST
								if(linkedMemberInfo.IsDuplicateReference(false))
								{
									#if DEV_MODE
									Debug.LogWarning("Won't show field with cyclical reference: " + type.Name +"."+ linkedMemberInfo);
									#endif
									continue;
								}

								if((linkedMemberInfo.HasAttribute<Attributes.InlineAttribute>(false, true) || Attribute<Attributes.InlineAttribute>.ExistsOn(field.FieldType, false)) && !field.FieldType.IsUnityObject())
								{
									field.FieldType.GetInspectorViewables(ref hierarchy, linkedMemberInfo, ref results, bindingFlags, includeHidden, includeNonSerializedFields, propertyVisibility, methodVisibility, LinkedMemberParent.LinkedMemberInfo);
								}
								else
								{
									#if DEV_MODE && DEBUG_GET_INSPECTOR_VIEWABLES
									Debug.Log("GetInspectorViewables - Adding field: "+ field);
									#endif

									results.Add(linkedMemberInfo);
								}
							}
							fromType = fromType.BaseType;
						}
						// We stop once we hit specific base types (even in debug mode this is desired, to avoid the number of results getting out of hand).
						while(fromType != Types.MonoBehaviour && fromType != Types.Component && fromType != Types.ScriptableObject && fromType != null && fromType != Types.UnityObject && fromType != Types.SystemObject);

						generatedFields.Clear();
						break;
					case Member.Property:
						fromType = type;
						do
						{
							var properties = fromType.GetProperties(bindingFlags);
							for(int n = 0, count = properties.Length; n < count; n++)
							{
								var property = properties[n];
								if(property.IsInspectorViewable(propertyVisibility, includeHidden))
								{
									#if DEV_MODE && PI_ASSERTATIONS
									Debug.Assert(!property.IsStatic() || parentType == LinkedMemberParent.Static);
									#endif

									// Don't display hidden / overridden properties twice.
									if(!generatedProperties.Add(property.Name))
									{
										continue;
									}

									var linkedMemberInfo = hierarchy.Get(parentMemberInfo, property, parentType, notUnitySerialized ? null : property.Name);

									// NEW TEST
									if(linkedMemberInfo.IsDuplicateReference(false))
									{
										#if DEV_MODE
										Debug.LogWarning("Won't show field with cyclical reference: " + type.Name +"."+ linkedMemberInfo);
										#endif
										continue;
									}

									if((linkedMemberInfo.HasAttribute<Attributes.InlineAttribute>(false, true) || Attribute<Attributes.InlineAttribute>.ExistsOn(property.PropertyType, false)) && !property.PropertyType.IsUnityObject())
									{
										property.PropertyType.GetInspectorViewables(ref hierarchy, linkedMemberInfo, ref results, bindingFlags, includeHidden, includeNonSerializedFields, propertyVisibility, methodVisibility, LinkedMemberParent.LinkedMemberInfo);
									}
									else
									{
										#if DEV_MODE && DEBUG_GET_INSPECTOR_VIEWABLES
										Debug.Log("GetInspectorViewables - Adding property: "+ property);
										#endif

										results.Add(linkedMemberInfo);
									}
								}
							}
							fromType = fromType.BaseType;
						}
						// We stop once we hit specific base types (even in debug mode this is desired, to avoid the number of results getting out of hand)
						while(fromType != Types.MonoBehaviour && fromType != Types.Component && fromType != Types.ScriptableObject && fromType != null && fromType != Types.UnityObject && fromType != Types.SystemObject);

						generatedProperties.Clear();

						break;
					case Member.Method:
						fromType = type;
						do
						{
							var methods = fromType.GetMethods(bindingFlags);
							for(int n = 0, count = methods.Length; n < count; n++)
							{
								var method = methods[n];
								if(method.IsInspectorViewable(methodVisibility, includeHidden))
								{
									#if DEV_MODE && PI_ASSERTATIONS
									Debug.Assert(!method.IsStatic() || parentType == LinkedMemberParent.Static);
									#endif

									// Don't display overridden methods twice.
									// In the future method overloads should also be merged into one drawer to reduce clutter in Debug Mode+.
									if(method.GetParameters().Length == 0 && !generatedMethods.Add(method.Name))
									{
										continue;
									}

									#if DEV_MODE && DEBUG_GET_INSPECTOR_VIEWABLES
									Debug.Log("GetInspectorViewables - Adding method: "+ method);
									#endif

									results.Add(hierarchy.Get(parentMemberInfo, method, parentType));
								}
							}
							fromType = fromType.BaseType;
						}
						// We stop once we hit specific base types (even in debug mode this is desired, to avoid the number of results getting out of hand)
						while(fromType != Types.MonoBehaviour && fromType != Types.Component && fromType != Types.ScriptableObject && fromType != null && fromType != Types.UnityObject && fromType != Types.SystemObject);

						generatedMethods.Clear();

						break;
				}
			}

			#if DEV_MODE && DEBUG_GET_INSPECTOR_VIEWABLES
			if(results.Count > 0) { Debug.Log(type.Name+".GetInspectorViewables: returning "+results.Count+" MemberInfos:\n"+StringUtils.ToString(results, "\n")); }
			#endif
		}

		public static void GetInspectorViewableFields(this Type type, ref LinkedMemberHierarchy hierarchy, [CanBeNull]LinkedMemberInfo parentMemberInfo, [NotNull]ref List<LinkedMemberInfo> results, BindingFlags bindingFlags, bool includeHidden, FieldVisibility includeNonSerializedFields, LinkedMemberParent parentType)
		{
			bool notUnitySerialized = parentType == LinkedMemberParent.Static || hierarchy.Target == null;
			do
			{
				var fields = type.GetFields(bindingFlags);

				#if DEV_MODE
				//Debug.Log(type.Name + ".GetFields("+bindingFlags+"): "+fields.Length);
				#endif

				for(int n = 0, count = fields.Length; n < count; n++)
				{
					var field = fields[n];
					if(field.IsInspectorViewable(includeHidden, includeNonSerializedFields))
					{
						// Don't display hidden fields twice (except in debug mode).
						if(!generatedFields.Add(field.Name) && !includeHidden)
						{
							continue;
						}

						LinkedMemberInfo linkedMemberInfo;
						if(field.IsStatic)
						{
							linkedMemberInfo = hierarchy.Get(parentMemberInfo, field, LinkedMemberParent.Static, null);
						}
						else
						{
							linkedMemberInfo = hierarchy.Get(parentMemberInfo, field, parentType, notUnitySerialized ? null : field.Name);
						}

						// NEW TEST
						if(linkedMemberInfo.IsDuplicateReference(false))
						{
							#if DEV_MODE
							Debug.LogWarning("Won't show field with cyclical reference: " + type.Name +"."+ linkedMemberInfo);
							#endif
							continue;
						}

						results.Add(linkedMemberInfo);
					}
				}
				type = type.BaseType;
			}
			// We stop once we hit specific base types (even in debug mode this is desired, to avoid the number of results getting out of hand)
			while(type != Types.MonoBehaviour && type != Types.Component && type != Types.ScriptableObject && type != null && type != Types.UnityObject && type != Types.SystemObject);

			generatedFields.Clear();

			#if DEV_MODE && DEBUG_GET_INSPECTOR_VIEWABLES
			if(results.Count > 0) { Debug.Log(type.Name+".GetInspectorViewables: returning "+results.Count+" MemberInfos:\n"+StringUtils.ToString(results, "\n")); }
			#endif
		}

		public static void GetInspectorViewableFieldsInStaticMode(this Type type, ref LinkedMemberHierarchy hierarchy, [CanBeNull]LinkedMemberInfo parentMemberInfo, [NotNull]ref List<LinkedMemberInfo> results, bool includeHidden, FieldVisibility fieldVisibility, PropertyVisibility propertyVisibility, MethodVisibility methodVisibility)
		{
			var bindingFlags = ParentDrawerUtility.BindingFlagsStatic;
			do
			{
				var fields = type.GetFields(bindingFlags);
				for(int n = 0, count = fields.Length; n < count; n++)
				{
					var field = fields[n];
					if(field.IsInspectorViewableInStaticMode(includeHidden))
					{
						// Don't display hidden fields twice (except in debug mode).
						if(!generatedFields.Add(field.Name) && !includeHidden)
						{
							continue;
						}

						var linkedMemberInfo = hierarchy.Get(parentMemberInfo, field, LinkedMemberParent.Static);

						// NEW TEST
						if(linkedMemberInfo.IsDuplicateReference(false))
						{
							#if DEV_MODE
							Debug.LogWarning("Won't show field with cyclical reference: " + type.Name +"." + linkedMemberInfo);
							#endif
							continue;
						}

						if((linkedMemberInfo.HasAttribute<Attributes.InlineAttribute>(false, true) || Attribute<Attributes.InlineAttribute>.ExistsOn(field.FieldType, false)) && !field.FieldType.IsUnityObject())
						{
							field.FieldType.GetInspectorViewables(ref hierarchy, linkedMemberInfo, ref results, bindingFlags, includeHidden, fieldVisibility, propertyVisibility, methodVisibility, LinkedMemberParent.LinkedMemberInfo);
						}
						else
						{
							results.Add(linkedMemberInfo);
						}
					}
				}
				type = type.BaseType;
			}
			// We stop once we hit specific base types (even in debug mode this is desired, to avoid the number of results getting out of hand)
			while(type != Types.MonoBehaviour && type != Types.Component && type != Types.ScriptableObject && type != null && type != Types.UnityObject && type != Types.SystemObject);

			generatedFields.Clear();
		}

		public static void GetInspectorViewablePropertiesInStaticMode(this Type type, ref LinkedMemberHierarchy hierarchy, [CanBeNull]LinkedMemberInfo parentMemberInfo, [NotNull]ref List<LinkedMemberInfo> results, bool includeHidden, FieldVisibility fieldVisibility, PropertyVisibility propertyVisibility, MethodVisibility methodVisibility)
		{
			var bindingFlags = ParentDrawerUtility.BindingFlagsStatic;
			do
			{
				var properties = type.GetProperties(bindingFlags);
				for(int n = 0, count = properties.Length; n < count; n++)
				{
					var property = properties[n];
					if(property.IsInspectorViewableInStaticMode(propertyVisibility, includeHidden))
					{
						// Don't display hidden / overridden properties twice.
						if(!generatedProperties.Add(property.Name))
						{
							continue;
						}

						var linkedMemberInfo = hierarchy.Get(parentMemberInfo, property, LinkedMemberParent.Static);

						// NEW TEST
						if(linkedMemberInfo.IsDuplicateReference(false))
						{
							#if DEV_MODE
							Debug.LogWarning("Won't show property with cyclical reference: " + type.Name +"." + linkedMemberInfo);
							#endif
							continue;
						}
						
						if((linkedMemberInfo.HasAttribute<Attributes.InlineAttribute>(false, true) || Attribute<Attributes.InlineAttribute>.ExistsOn(property.PropertyType, false)) && !property.PropertyType.IsUnityObject())
						{
							property.PropertyType.GetInspectorViewables(ref hierarchy, linkedMemberInfo, ref results, bindingFlags, includeHidden, fieldVisibility, propertyVisibility, methodVisibility, LinkedMemberParent.LinkedMemberInfo);
						}
						else
						{
							results.Add(linkedMemberInfo);
						}
					}
				}
				type = type.BaseType;
			}
			// We stop once we hit specific base types (even in debug mode this is desired, to avoid the number of results getting out of hand)
			while(type != Types.MonoBehaviour && type != Types.Component && type != Types.ScriptableObject && type != null && type != Types.UnityObject && type != Types.SystemObject);

			generatedProperties.Clear();
		}

		public static void GetInspectorViewableMethodsInStaticMode(this Type type, ref LinkedMemberHierarchy hierarchy, [CanBeNull]LinkedMemberInfo parentMemberInfo, [NotNull]ref List<LinkedMemberInfo> results, bool includeHidden, MethodVisibility methodVisibility)
		{
			do
			{
				var methods = type.GetMethods(ParentDrawerUtility.BindingFlagsStatic);
				for(int n = 0, count = methods.Length; n < count; n++)
				{
					var method = methods[n];
					if(method.IsInspectorViewableInStaticMode(methodVisibility, includeHidden))
					{
						// Don't display overridden methods twice.
						// In the future method overloads should also be merged into one drawer to reduce clutter in Debug Mode+.
						if(method.GetParameters().Length == 0 && !generatedMethods.Add(method.Name))
						{
							continue;
						}

						results.Add(hierarchy.Get(parentMemberInfo, method, LinkedMemberParent.Static));
					}
				}
				type = type.BaseType;
			}
			// We stop once we hit specific base types (even in debug mode this is desired, to avoid the number of results getting out of hand)
			while(type != Types.MonoBehaviour && type != Types.Component && type != Types.ScriptableObject && type != null && type != Types.UnityObject && type != Types.SystemObject);

			generatedMethods.Clear();
		}

		public static void GetInspectorViewableProperties(this Type type, ref LinkedMemberHierarchy hierarchy, [CanBeNull]LinkedMemberInfo parentMemberInfo, [NotNull]ref List<LinkedMemberInfo> results, BindingFlags bindingFlags, bool includeHidden, PropertyVisibility propertyVisibility, LinkedMemberParent parentType)
		{
			bool notUnitySerialized = parentType == LinkedMemberParent.Static || hierarchy.Target == null;
			do
			{
				var properties = type.GetProperties(bindingFlags);
				for(int n = 0, count = properties.Length; n < count; n++)
				{
					var property = properties[n];
					if(property.IsInspectorViewable(propertyVisibility, includeHidden))
					{
						// Don't display hidden / overridden properties twice.
						if(!generatedProperties.Add(property.Name))
						{
							continue;
						}

						LinkedMemberInfo linkedMemberInfo;
						if(property.IsStatic())
						{
							linkedMemberInfo = hierarchy.Get(parentMemberInfo, property, LinkedMemberParent.Static, null);
						}
						else
						{
							linkedMemberInfo = hierarchy.Get(parentMemberInfo, property, parentType, notUnitySerialized ? null : property.Name);
						}

						// NEW TEST
						if(linkedMemberInfo.IsDuplicateReference(false))
						{
							#if DEV_MODE
							Debug.LogWarning("Won't show property with cyclical reference: " + type.Name +"."+ linkedMemberInfo);
							#endif
							continue;
						}

						results.Add(linkedMemberInfo);
					}
				}
				type = type.BaseType;
			}
			// We stop once we hit specific base types (even in debug mode this is desired, to avoid the number of results getting out of hand)
			while(type != Types.MonoBehaviour && type != Types.Component && type != Types.ScriptableObject && type != null && type != Types.UnityObject && type != Types.SystemObject);

			generatedProperties.Clear();

			#if DEV_MODE && DEBUG_GET_INSPECTOR_VIEWABLES
			if(results.Count > 0) { Debug.Log(type.Name+".GetInspectorViewables: returning "+results.Count+" MemberInfos:\n"+StringUtils.ToString(results, "\n")); }
			#endif
		}

		public static void GetInspectorViewableMethods(this Type type, ref LinkedMemberHierarchy hierarchy, [CanBeNull]LinkedMemberInfo parentMemberInfo, [NotNull]ref List<LinkedMemberInfo> results, BindingFlags bindingFlags, bool includeHidden, MethodVisibility methodVisibility, LinkedMemberParent parentType)
		{
			do
			{
				var methods = type.GetMethods(bindingFlags);
				for(int n = 0, count = methods.Length; n < count; n++)
				{
					var method = methods[n];
					if(method.IsInspectorViewable(methodVisibility, includeHidden))
					{
						// Don't display overridden methods twice.
						// In the future method overloads should also be merged into one drawer to reduce clutter in Debug Mode+.
						if(method.GetParameters().Length == 0 && !generatedMethods.Add(method.Name))
						{
							continue;
						}

						results.Add(hierarchy.Get(parentMemberInfo, method, method.IsStatic() ? LinkedMemberParent.Static : parentType));
					}
				}
				type = type.BaseType;
			}
			// We stop once we hit specific base types (even in debug mode this is desired, to avoid the number of results getting out of hand)
			while(type != Types.MonoBehaviour && type != Types.Component && type != Types.ScriptableObject && type != null && type != Types.UnityObject && type != Types.SystemObject);

			generatedMethods.Clear();

			#if DEV_MODE && DEBUG_GET_INSPECTOR_VIEWABLES
			if(results.Count > 0) { Debug.Log(type.Name+".GetInspectorViewables: returning "+results.Count+" MemberInfos:\n"+StringUtils.ToString(results, "\n")); }
			#endif
		}

		public static void GetInspectorViewables(this Type type, ref LinkedMemberHierarchy hierarchy, [CanBeNull]LinkedMemberInfo parentMemberInfo, [NotNull]ref List<LinkedMemberInfo> results, BindingFlags bindingFlags, bool includeHidden, FieldVisibility fieldVisibility, PropertyVisibility propertyVisibility, MethodVisibility methodVisibility)
		{
			if(bindingFlags.HasFlag(BindingFlags.Static))
			{
				if(!bindingFlags.HasFlag(BindingFlags.Instance))
				{
					// static only...
					type.GetStaticInspectorViewables(ref hierarchy, parentMemberInfo, ref results, includeHidden, fieldVisibility, propertyVisibility, methodVisibility);
					return;
				}
			}
			else if(bindingFlags.HasFlag(BindingFlags.Instance))
			{
				// instance only...
				var parentType = parentMemberInfo != null ? LinkedMemberParent.LinkedMemberInfo : type.IsUnityObject() ? LinkedMemberParent.UnityObject : LinkedMemberParent.Missing;
				type.GetInspectorViewables(ref hierarchy, parentMemberInfo, ref results, bindingFlags, includeHidden, fieldVisibility, propertyVisibility, methodVisibility, parentType);
				return;
			}
			
			// static AND instance...

			var parentTypeIfNotStatic = parentMemberInfo != null ? LinkedMemberParent.LinkedMemberInfo : type.IsUnityObject() ? LinkedMemberParent.UnityObject : LinkedMemberParent.Missing;

			bool noSerializedProperty;
			if(hierarchy.Target == null)
			{
				noSerializedProperty = true;
			}
			else if(parentMemberInfo != null)
			{
				noSerializedProperty = parentMemberInfo.ParentChainIsBroken;
			}
			else
			{
				// if parentMemberInfo is null and parent is not UnityEngine.Object, parent chain is broken
				noSerializedProperty = !type.IsUnityObject();
			}

			var order = InspectorUtility.Preferences.MemberDisplayOrder;
			for(int t = 0; t < 3; t++)
			{
				switch(order[t])
				{
					case Member.Field:
						var fromType = type;
						do
						{
							var fields = fromType.GetFields(bindingFlags);
							for(int n = 0, count = fields.Length; n < count; n++)
							{
								var field = fields[n];
								if(field.IsInspectorViewable(includeHidden, fieldVisibility))
								{
									// Don't display hidden fields twice (except in debug mode).
									if(!generatedFields.Add(field.Name) && !includeHidden)
									{
										continue;
									}

									bool isStatic = field.IsStatic;
									var linkedMemberInfo = hierarchy.Get(parentMemberInfo, field, isStatic ? LinkedMemberParent.Static : parentTypeIfNotStatic, isStatic || noSerializedProperty ? null : field.Name);

									// NEW TEST
									if(linkedMemberInfo.IsDuplicateReference(false))
									{
										#if DEV_MODE
										Debug.LogWarning("Won't show field with cyclical reference: " + fromType.Name +"."+ linkedMemberInfo);
										#endif
										continue;
									}

									if(linkedMemberInfo.GetAttribute<Attributes.InlineAttribute>(false, true) != null || Attribute<Attributes.InlineAttribute>.ExistsOn(field.FieldType, false))
									{
										field.FieldType.GetInspectorViewables(ref hierarchy, linkedMemberInfo, ref results, bindingFlags, includeHidden, fieldVisibility, propertyVisibility, methodVisibility, LinkedMemberParent.LinkedMemberInfo);
									}
									else
									{
										results.Add(linkedMemberInfo);
									}
								}
							}
							fromType = fromType.BaseType;
						}
						// We stop once we hit specific base types (even in debug mode this is desired, to avoid the number of results getting out of hand)
						while(fromType != Types.MonoBehaviour && fromType != Types.Component && fromType != Types.ScriptableObject && fromType != null && fromType != Types.UnityObject && fromType != Types.SystemObject);

						generatedFields.Clear();

						break;
					case Member.Property:
						fromType = type;
						do
						{
							var properties = fromType.GetProperties(bindingFlags);
							for(int n = 0, count = properties.Length; n < count; n++)
							{
								var property = properties[n];
								if(property.IsInspectorViewable(propertyVisibility, includeHidden))
								{
									// Don't display hidden / overridden properties twice.
									if(!generatedProperties.Add(property.Name))
									{
										continue;
									}

									bool isStatic = property.IsStatic();

									var linkedMemberInfo = hierarchy.Get(parentMemberInfo, property, isStatic ? LinkedMemberParent.Static : parentTypeIfNotStatic, isStatic || noSerializedProperty ? null : property.Name);

									// NEW TEST
									if(linkedMemberInfo.IsDuplicateReference(false))
									{
										#if DEV_MODE
										Debug.LogWarning("Won't show property with cyclical reference: " + fromType.Name +"."+ linkedMemberInfo);
										#endif
										continue;
									}

									if(linkedMemberInfo.GetAttribute<Attributes.InlineAttribute>(false, true) != null || Attribute<Attributes.InlineAttribute>.ExistsOn(property.PropertyType, false))
									{
										property.PropertyType.GetInspectorViewables(ref hierarchy, linkedMemberInfo, ref results, bindingFlags, includeHidden, fieldVisibility, propertyVisibility, methodVisibility, LinkedMemberParent.LinkedMemberInfo);
									}
									else
									{
										results.Add(linkedMemberInfo);
									}
								}
							}
							fromType = fromType.BaseType;
						}
						// We stop once we hit specific base types (even in debug mode this is desired, to avoid the number of results getting out of hand)
						while(fromType != Types.MonoBehaviour && fromType != Types.Component && fromType != Types.ScriptableObject && fromType != null && fromType != Types.UnityObject && fromType != Types.SystemObject);

						generatedProperties.Clear();

						break;
					case Member.Method:
						fromType = type;
						do
						{
							var methods = fromType.GetMethods(bindingFlags);
							for(int n = 0, count = methods.Length; n < count; n++)
							{
								var method = methods[n];
								if(method.IsInspectorViewable(methodVisibility, includeHidden))
								{
									// Don't display overridden methods twice.
									// In the future method overloads should also be merged into one drawer to reduce clutter in Debug Mode+.
									if(method.GetParameters().Length == 0 && !generatedMethods.Add(method.Name))
									{
										continue;
									}

									results.Add(hierarchy.Get(parentMemberInfo, method, method.IsStatic ? LinkedMemberParent.Static : parentTypeIfNotStatic));
								}
							}
							fromType = fromType.BaseType;
						}
						// We stop once we hit specific base types (even in debug mode this is desired, to avoid the number of results getting out of hand)
						while(fromType != Types.MonoBehaviour && fromType != Types.Component && fromType != Types.ScriptableObject && fromType != null && fromType != Types.UnityObject && fromType != Types.SystemObject);

						generatedMethods.Clear();

						break;
				}
			}

			#if DEV_MODE && DEBUG_GET_INSPECTOR_VIEWABLES
			if(results.Count > 0) { Debug.Log(type.Name+".GetInspectorViewables: returning "+results.Count+" MemberInfos:\n"+StringUtils.ToString(results, "\n")); }
			#endif
		}

		public static void GetStaticInspectorViewables(this Type type, ref LinkedMemberHierarchy hierarchy, [CanBeNull]LinkedMemberInfo parentMemberInfo, [NotNull]ref List<LinkedMemberInfo> results, bool includeHidden, FieldVisibility fieldVisibility, PropertyVisibility propertyVisibility, MethodVisibility methodVisibility, DebugModeDisplaySettings debugModeDisplaySettings = null)
		{
			var bindingFlags = ParentDrawerUtility.BindingFlagsStatic;
			var order = InspectorUtility.Preferences.MemberDisplayOrder;
			for(int t = 0; t < 3; t++)
			{
				switch(order[t])
				{
					case Member.Field:
						if(debugModeDisplaySettings != null && !debugModeDisplaySettings.ShowFields)
						{
							break;
						}

						var fromType = type;
						do
						{
							var fields = fromType.GetFields(bindingFlags);
							for(int n = 0, count = fields.Length; n < count; n++)
							{
								var field = fields[n];
								if(field.IsInspectorViewableInStaticMode(includeHidden))
								{
									// Don't display hidden fields twice (except in debug mode).
									if(!generatedFields.Add(field.Name) && !includeHidden)
									{
										continue;
									}

									var linkedMemberInfo = hierarchy.Get(parentMemberInfo, field, LinkedMemberParent.Static);

									if((linkedMemberInfo.HasAttribute<Attributes.InlineAttribute>(false, true) || Attribute<Attributes.InlineAttribute>.ExistsOn(field.FieldType, false)) && !field.FieldType.IsUnityObject())
									{
										field.FieldType.GetInspectorViewables(ref hierarchy, linkedMemberInfo, ref results, bindingFlags, includeHidden, fieldVisibility, propertyVisibility, methodVisibility, LinkedMemberParent.LinkedMemberInfo);
									}
									else
									{
										results.Add(linkedMemberInfo);
									}
								}
							}
							fromType = fromType.BaseType;
						}
						// We stop once we hit specific base types (even in debug mode this is desired, to avoid the number of results getting out of hand)
						while(fromType != Types.MonoBehaviour && fromType != Types.Component && fromType != Types.ScriptableObject && fromType != null && fromType != Types.UnityObject && fromType != Types.SystemObject);

						generatedFields.Clear();

						break;
					case Member.Property:
						if(debugModeDisplaySettings != null && !debugModeDisplaySettings.ShowProperties)
						{
							break;
						}

						fromType = type;
						do
						{
							var properties = fromType.GetProperties(bindingFlags);
							for(int n = 0, count = properties.Length; n < count; n++)
							{
								var property = properties[n];
								if(property.IsInspectorViewableInStaticMode(propertyVisibility, includeHidden))
								{
									// Don't display hidden / overridden properties twice.
									if(!generatedProperties.Add(property.Name))
									{
										continue;
									}

									var linkedMemberInfo = hierarchy.Get(parentMemberInfo, property, LinkedMemberParent.Static);

									if((linkedMemberInfo.HasAttribute<Attributes.InlineAttribute>(false, true) || Attribute<Attributes.InlineAttribute>.ExistsOn(property.PropertyType, false)) && !property.PropertyType.IsUnityObject())
									{
										property.PropertyType.GetInspectorViewables(ref hierarchy, linkedMemberInfo, ref results, bindingFlags, includeHidden, fieldVisibility, propertyVisibility, methodVisibility, LinkedMemberParent.LinkedMemberInfo);
									}
									else
									{
										results.Add(linkedMemberInfo);
									}
								}
							}
							fromType = fromType.BaseType;
						}
						// We stop once we hit specific base types (even in debug mode this is desired, to avoid the number of results getting out of hand)
						while(fromType != Types.MonoBehaviour && fromType != Types.Component && fromType != Types.ScriptableObject && fromType != null && fromType != Types.UnityObject && fromType != Types.SystemObject);

						generatedProperties.Clear();

						break;
					case Member.Method:
						if(debugModeDisplaySettings != null && !debugModeDisplaySettings.ShowMethods)
						{
							break;
						}

						fromType = type;
						do
						{
							var methods = fromType.GetMethods(bindingFlags);
							for(int n = 0, count = methods.Length; n < count; n++)
							{
								var method = methods[n];
								if(method.IsInspectorViewableInStaticMode(methodVisibility, includeHidden))
								{
									// Don't display overridden methods twice.
									// In the future method overloads should also be merged into one drawer to reduce clutter in Debug Mode+.
									if(method.GetParameters().Length == 0 && !generatedMethods.Add(method.Name))
									{
										continue;
									}

									results.Add(hierarchy.Get(parentMemberInfo, method, LinkedMemberParent.Static));
								}
							}
							fromType = fromType.BaseType;
						}
						// We stop once we hit specific base types (even in debug mode this is desired, to avoid the number of results getting out of hand)
						while(fromType != Types.MonoBehaviour && fromType != Types.Component && fromType != Types.ScriptableObject && fromType != null && fromType != Types.UnityObject && fromType != Types.SystemObject);

						generatedMethods.Clear();

						break;
				}
			}

			#if DEV_MODE && DEBUG_GET_INSPECTOR_VIEWABLES
			if(results.Count > 0) { Debug.Log(type.Name+".GetInspectorViewables: returning "+results.Count+" MemberInfos:\n"+StringUtils.ToString(results, "\n")); }
			#endif
		}
	}
}