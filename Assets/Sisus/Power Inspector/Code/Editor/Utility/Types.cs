using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
#if UNITY_2020_2_OR_NEWER
using AssetImporterEditor = UnityEditor.AssetImporters.AssetImporterEditor;
#elif UNITY_2017_2_OR_NEWER
using AssetImporterEditor = UnityEditor.Experimental.AssetImporters.AssetImporterEditor;
#endif
using UnityEditor;
#endif

namespace Sisus
{
	/// <summary>
	/// Caches some often-used types and assemblies for faster performance and can return internal Unity types. 
	/// </summary>
	public static class Types
	{
		public static readonly Assembly UnityAssembly = Assembly.GetAssembly(typeof(Object));
		#if UNITY_EDITOR
		public static readonly Assembly EditorAssembly = Assembly.GetAssembly(typeof(Editor));
		public static readonly Assembly EditorInternalAssembly = Assembly.GetAssembly(typeof(UnityEditorInternal.InternalEditorUtility));
		#endif
	
		public static readonly Type[] None = new Type[0];

		public static readonly Type Void = typeof(void);
		public static readonly Type DBNull = typeof(DBNull);
		
		public static readonly Type Type = typeof(Type);
		public static readonly Type Enum = typeof(Enum);

		public static readonly Type Bool = typeof(bool);

		public static readonly Type Short = typeof(short);	//aka Int16
		public static readonly Type Int = typeof(int);		//aka Int32
		public static readonly Type Long = typeof(long);	//aka Int64
		public static readonly Type UShort = typeof(ushort);//aka UInt16
		public static readonly Type UInt = typeof(uint);	//aka UInt32
		public static readonly Type ULong = typeof(ulong);	//aka UInt64
		public static readonly Type Float = typeof(float);
		public static readonly Type Double = typeof(double);
		public static readonly Type Decimal = typeof(decimal);
		public static readonly Type SByte = typeof(sbyte);
		public static readonly Type Char = typeof(char);

		public static readonly Type String = typeof(string);

		public static readonly Type DateTime = typeof(DateTime);
		public static readonly Type TimeSpan = typeof(TimeSpan);
		

		public static readonly Type Vector2 = typeof(Vector2);
		public static readonly Type Vector3 = typeof(Vector3);
		public static readonly Type Vector4 = typeof(Vector4);
		public static readonly Type Rect = typeof(Rect);
		public static readonly Type RectOffset = typeof(RectOffset);
		public static readonly Type Color = typeof(Color);
		public static readonly Type Color32 = typeof(Color32);
		public static readonly Type AnimationCurve = typeof(AnimationCurve);
		public static readonly Type Gradient = typeof(Gradient);
		public static readonly Type GUIStyle = typeof(GUIStyle);
		public static readonly Type GUIStyleState = typeof(GUIStyleState);
		public static readonly Type GUIContent = typeof(GUIContent);
		public static readonly Type Quaternion = typeof(Quaternion);
		public static readonly Type IEnumerator = typeof(IEnumerator);

		public static readonly Type List = typeof(List<>);
		public static readonly Type HashSet = typeof(HashSet<>);
		public static readonly Type IList = typeof(IList);
		public static readonly Type IEnumerableGeneric = typeof(IEnumerable<>);
		public static readonly Type Dictionary = typeof(Dictionary<,>);
		public static readonly Type KeyValuePair = typeof(KeyValuePair<,>);
		public static readonly Type DictionaryEntry = typeof(DictionaryEntry);
		public static readonly Type MulticastDelegate = typeof(MulticastDelegate);
		public static readonly Type Delegate = typeof(Delegate);

		public static readonly Type UnityObject = typeof(Object);
		public static readonly Type SystemObject = typeof(object);

		public static readonly Type MonoBehaviour = typeof(MonoBehaviour);
		public static readonly Type Behaviour = typeof(Behaviour);
		public static readonly Type Component = typeof(Component);
		public static readonly Type ScriptableObject = typeof(ScriptableObject);
		public static readonly Type GameObject = typeof(GameObject);
		public static readonly Type Transform = typeof(Transform);
		public static readonly Type RectTransform = typeof(RectTransform);

		public static readonly Type AddComponentMenu = typeof(AddComponentMenu);
		public static readonly Type DisallowMultipleComponent = typeof(DisallowMultipleComponent);
		
		public static readonly Type Texture = typeof(Texture);
		public static readonly Type Texture2D = typeof(Texture2D);
		public static readonly Type Material = typeof(Material);
		public static readonly Type MeshFilter = typeof(MeshFilter);
		public static readonly Type TextMesh = typeof(TextMesh);
		public static readonly Type MeshRenderer = typeof(MeshRenderer);
		public static readonly Type SkinnedMeshRenderer = typeof(SkinnedMeshRenderer);
		
		public static readonly Type ParticleSystem = typeof(ParticleSystem);
		public static readonly Type TrailRenderer = typeof(TrailRenderer);
		public static readonly Type Animator = typeof(Animator);
		public static readonly Type Rigidbody = typeof(Rigidbody);
		public static readonly Type Collider = typeof(Collider);
		public static readonly Type BoxCollider = typeof(BoxCollider);
		public static readonly Type ConstantForce = typeof(ConstantForce);

		public static readonly Type Collider2D = typeof(Collider2D);
		public static readonly Type Rigidbody2D = typeof(Rigidbody2D);
		public static readonly Type Effector2D = typeof(Effector2D);
		public static readonly Type Motion = typeof(Motion);
		public static readonly Type AudioClip = typeof(AudioClip);
		public static readonly Type AudioListener = typeof(AudioListener);
		public static readonly Type Camera = typeof(Camera);
		public static readonly Type FlareLayer = typeof(FlareLayer);
		public static readonly Type Light = typeof(Light);
		
		public static readonly Type SpriteRenderer = typeof(SpriteRenderer);
		public static readonly Type Renderer = typeof(Renderer);
		
		public static readonly Type Joint = typeof(Joint);
		public static readonly Type Joint2D = typeof(Joint2D);
		public static readonly Type AnchoredJoint2D = typeof(AnchoredJoint2D);
		public static readonly Type PhysicsUpdateBehaviour2D = typeof(PhysicsUpdateBehaviour2D);
		public static readonly Type Tree = typeof(Tree);
		public static readonly Type ParticleSystemRenderer = typeof(ParticleSystemRenderer);
		public static readonly Type AudioBehaviour = typeof(AudioBehaviour);
		
		public static readonly Type PropertyAttribute = typeof(PropertyAttribute);
		public static readonly Type TooltipAttribute = typeof(TooltipAttribute);
		public static readonly Type ObsoleteAttribute = typeof(ObsoleteAttribute);
		public static readonly Type SerializeField = typeof(SerializeField);
		public static readonly Type FlagsAttribute = typeof(FlagsAttribute);
		
		public static readonly Type ContextMenu = typeof(ContextMenu);
		
		#if !UNITY_2019_3_OR_NEWER
		public static readonly Type GUIElement = typeof(GUIElement);
		#endif

		#if UNITY_2018_1_OR_NEWER
		public static readonly Type ExcludeFromPresetAttribute = typeof(ExcludeFromPresetAttribute);
		#if UNITY_EDITOR
		public static readonly Type Preset = typeof(UnityEditor.Presets.Preset);
		#endif
		#endif

		#if UNITY_2017_2_OR_NEWER
		public static readonly Type Vector2Int = typeof(Vector2Int);
		public static readonly Type Vector3Int = typeof(Vector3Int);
		public static readonly Type GridLayout = typeof(GridLayout);
		#endif

		public static readonly Type TextAsset = typeof(TextAsset);

		#if UNITY_EDITOR
		public static readonly Type Editor = typeof(Editor);
		public static readonly Type EditorWindow = typeof(EditorWindow);
		public static readonly Type MonoScript = typeof(MonoScript);
		public static readonly Type DefaultAsset = typeof(DefaultAsset);
		#if UNITY_2017_2_OR_NEWER
		public static readonly Type AssetImporterEditor = typeof(AssetImporterEditor);
		#endif
		#endif
		
		public static Type GetInternalType(string typeFullName)
		{
			return UnityAssembly.GetType(typeFullName);
		}

		#if UNITY_EDITOR
		/// <summary>
		/// Gets internal type from the UnityEditor namespace with the given full type name.
		/// </summary>
		/// <param name="typeFullName">
		/// Full name of the type writting in the form "UnityEditor.*". </param>
		/// <returns>
		/// The internal editor type.
		/// </returns>
		public static Type GetInternalEditorType(string typeFullName)
		{
			#if DEV_MODE
			Debug.Assert(typeFullName.StartsWith("UnityEditor.", StringComparison.Ordinal));
			#endif

			return EditorAssembly.GetType(typeFullName);
		}

		public static Type GetInternalEditorInternalType(string typeFullName)
		{
			return EditorInternalAssembly.GetType(typeFullName);
		}
		#endif
	}
}