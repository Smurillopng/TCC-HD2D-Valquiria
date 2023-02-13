using System;
using JetBrains.Annotations;
using UnityEngine;
using Sisus.Attributes;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Interface for classes that are responsible for determining which Drawer should be used for which Unity Object targets and class members.
	/// </summary>
	public interface IDrawerProvider : IDrawerByNameProvider
	{
		/// <summary>
		/// Gets value indicating if drawer provider is initialized and ready to be used.
		/// </summary>
		bool IsReady { get; }

		DrawerProviderData DrawerProviderData { get; }

		Action<IDrawerProvider> OnBecameReady { get; set; }

		/// <summary>
		/// Returns a value indicating whether or not drawers have rebuilt from scratch or been deserialized from previously built data.
		/// </summary>
		bool UsingDeserializedDrawers { get; set; }

		/// <summary>
		/// "Prewarms" commonly used drawers by instantiating them and placing them in the object pool.
		/// The idea is to avoid performance spikes when selecting common targets for the first time.
		/// </summary>
		void Prewarm(IInspector inspector);

		/// <summary> Gets type of drawer for drawing data of Gameobjects inside an inspector. </summary>
		/// <param name="target"> Target GameObject. This cannot be null. </param>
		/// <returns> Type of drawer class that implements IGameObjectDrawer. This will never be null. </returns>
		[NotNull]
		Type GetDrawerTypeForGameObject([NotNull]GameObject target);


		/// <summary> Gets type of drawer for drawing Gameobject data inside an inspector. </summary>
		/// <param name="targets"> Target GameObjects. This cannot be null or empty. </param>
		/// <returns> Type of drawer class that implements IGameObjectDrawer. This will never be null. </returns>
		[NotNull]
		Type GetDrawerTypeForGameObjects([NotNullOrEmpty]GameObject[] targets);

		/// <summary> Gets drawer instance for drawing Gameobject data inside an inspector. </summary>
		/// <param name="inspector"> The inspector which will contain the created drawer. This cannot be null. </param>
		/// <param name="target"> Target GameObject. This cannot be null. </param>
		/// <param name="parent"> The parent drawer for the created drawer. This can be null. </param>
		/// <returns> Instance of drawer that implements IGameObjectDrawer. This will never be null. </returns>
		[NotNull]
		IGameObjectDrawer GetForGameObject([NotNull]IInspector inspector, [NotNull]GameObject target, [CanBeNull]IParentDrawer parent);

		/// <summary> Gets drawer instance for drawing Gameobjects' data inside an inspector. </summary>
		/// <param name="inspector"> The inspector which will contain the created drawer. This cannot be null. </param>
		/// <param name="targets"> Target GameObjects. This cannot be null or empty. </param>
		/// <param name="parent"> The parent drawer for the created drawer. This can be null. </param>
		/// <returns> Instance of drawer that implements IGameObjectDrawer. This will never be null. </returns>
		[NotNull]
		IGameObjectDrawer GetForGameObjects([NotNull]IInspector inspector, [NotNullOrEmpty]GameObject[] targets, [CanBeNull]IParentDrawer parent);

		/// <summary> Gets drawer instance for drawing Component data inside an inspector. </summary>
		/// <param name="inspector"> The inspector which will contain the created drawer. This cannot be null. </param>
		/// <param name="target"> Target Component. This cannot be null. </param>
		/// <param name="parent"> The parent drawer for the created drawer. This can be null. </param>
		/// <returns> Instance of drawer that implements IComponentDrawer. This will never be null. </returns>
		[NotNull]
		IComponentDrawer GetForComponent([NotNull]IInspector inspector, [NotNull]Component target, [CanBeNull]IParentDrawer parent);

		/// <summary> Gets drawer instance for drawing Components' data inside an inspector. </summary>
		/// <param name="inspector"> The inspector which will contain the created drawer. This cannot be null. </param>
		/// <param name="targets"> Target Components. This cannot be null. </param>
		/// <param name="parent"> The parent drawer for the created drawer. This can be null. </param>
		/// <returns> Instance of drawer that implements IComponentDrawer. This will never be null. </returns>
		[NotNull]
		IComponentDrawer GetForComponents([NotNull]IInspector inspector, [NotNull]Component[] targets, [CanBeNull]IParentDrawer parent);

		/// <summary>
		/// Gets drawer instance for drawing Components' data inside an inspector.
		/// The returned drawer will utilize other drawers for drawing its class member data.
		/// </summary>
		/// <param name="drawerType"> The type of the drawer to create. This cannot be null and the class must implement IEditorlessComponentDrawer. </param>
		/// <param name="inspector"> The inspector which will contain the created drawer. This cannot be null. </param>
		/// <param name="targets"> Target Components. This cannot be null. </param>
		/// <param name="parent"> The parent drawer for the created drawer. This can be null. </param>
		/// <returns> Instance of drawer that implements IEditorlessComponentDrawer. This will never be null. </returns>
		[NotNull]
		IEditorlessComponentDrawer GetForComponents([NotNull]Type drawerType, [NotNull]Component[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector);

		/// <summary>
		/// Gets drawer instance for drawing Components' data inside an inspector.
		/// The returned drawer will utilize an Editor for drawing its class member data.
		/// </summary>
		/// <param name="drawerType"> The type of the drawer to create. This cannot be null and the class must implement ICustomEditorComponentDrawer. </param>
		/// <param name="inspector"> The inspector which will contain the created drawer. This cannot be null. </param>
		/// <param name="targets"> Target Components. This cannot be null. </param>
		/// <param name="parent"> The parent drawer for the created drawer. This can be null. </param>
		/// <returns> Instance of drawer that implements ICustomEditorComponentDrawer. This will never be null. </returns>
		[NotNull]
		ICustomEditorComponentDrawer GetForComponents([NotNull]Type drawerType, [CanBeNull]Type customEditorType, [NotNull]Component[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector);

		/// <summary> Gets drawer type for given field type. Does not take into consideration possible property attributes. </summary>
		/// <param name="fieldType"> Type of the field for which we are trying to find the Drawer. </param>
		/// <returns> Type that implements IFieldDrawer. </returns>
		[NotNull]
		Type GetDrawerTypeForField([NotNull]Type fieldType, bool ignoreAttributes = false);

		/// <summary> Gets drawer type that should be used by default for all instances of the given class type. </summary>
		/// <param name="classType"> Type of some class for which we are trying to find the drawer. </param>
		/// <param name="ignoreAttributes"> If true attributes implementing IUseDrawer on the class won't be considered when determining drawer for class. </param>
		/// <returns> Type of drawer class that implements IDrawer. </returns>
		[NotNull]
		Type GetClassDrawerType([NotNull]Type classType, bool ignoreAttributes = false);

		/// <summary> Gets drawer type that should be used for a field, property or method of the given type. </summary>
		/// <param name="fieldType"> Type of some field, property or method for which we are trying to find the drawer. </param>
		/// <param name="ignoreAttributes"> If true attributes implementing IUseDrawer on the class won't be considered when determining drawer for class. </param>
		/// <returns> Type of drawer class that implements IDrawer. </returns>
		[NotNull]
		Type GetClassMemberDrawerType([NotNull]Type fieldType, bool ignoreAttributes = false);

		/// <summary>
		/// Gets drawer instance for drawing asset type Object's data inside an inspector.
		/// The returned drawer will utilize an Editor for drawing its class member data.
		/// </summary>
		/// <param name="inspector"> The inspector which will contain the created drawer. This cannot be null. </param>
		/// <param name="target"> Target asset type Object. This cannot be null. </param>
		/// <param name="parent"> The parent drawer for the created drawer. This can be null. </param>
		/// <returns> Instance of drawer that implements IDrawer. This will never be null. </returns>
		[NotNull]
		IDrawer GetForAsset([NotNull]IInspector inspector, [NotNull]Object target, [CanBeNull]IParentDrawer parent);

		/// <summary>
		/// Gets drawer instance for drawing data of asset type Objects inside an inspector.
		/// The returned drawer will utilize an Editor for drawing its class member data.
		/// </summary>
		/// <param name="inspector"> The inspector which will contain the created drawer. This cannot be null. </param>
		/// <param name="targets"> Target asset type Objects. This cannot be null. </param>
		/// <param name="parent"> The parent drawer for the created drawer. This can be null. </param>
		/// <returns> Instance of drawer that implements IAssetDrawer. This will never be null. </returns>
		[NotNull]
		IAssetDrawer GetForAssets([NotNull]IInspector inspector, [NotNull]Object[] targets, [CanBeNull]IParentDrawer parent);

		#if UNITY_EDITOR
		/// <summary>
		/// Gets drawer instance for drawing data of asset type classes inside an inspector.
		/// The returned drawer will utilize an Editor for drawing its class member data.
		/// </summary>
		/// <param name="drawerType"> The type of the drawer to create. This cannot be null and the class must implement ICustomEditorAssetDrawer. </param>
		/// <param name="customEditorType">
		/// The type of the Editor used for drawing class members of the drawer.
		/// This can be null, in which case default Editor type is used.
		/// This is mostly used with assets that have asset importers.
		/// </param>
		/// <param name="targets"> Target asset type Objects. This cannot be null. </param>
		/// <param name="assetImporters"> Asset importers for each of the targets. This can be null if Editor does not target asset importers. </param>
		/// <param name="parent"> The parent drawer for the created drawer. This can be null. </param>
		/// <param name="inspector"> The inspector which will contain the created drawer. This cannot be null. </param>
		/// <returns> Instance of drawer that implements ICustomEditorAssetDrawer. This will never be null. </returns>
		[NotNull]
		ICustomEditorAssetDrawer GetForAssetsWithEditor([NotNull]Type drawerType, [CanBeNull]Type customEditorType, [NotNull]Object[] targets, [CanBeNull]Object[] assetImporters, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector);
		#endif

		/// <summary>
		/// Gets drawer instance for drawing data of asset type Objects inside an inspector.
		/// The returned drawer will utilize other drawers for drawing its class member data.
		/// </summary>
		/// <param name="drawerType"> The type of the drawer to create. This cannot be null and the class must implement ICustomEditorAssetDrawer. </param>
		/// <param name="targets"> Target asset type Objects. This cannot be null. </param>
		/// <param name="parent"> The parent drawer for the created drawer. This can be null. </param>
		/// <param name="inspector"> The inspector which will contain the created drawer. This cannot be null. </param>
		/// <returns> Instance of drawer that implements IEditorlessAssetDrawer. This will never be null. </returns>
		[NotNull]
		IEditorlessAssetDrawer GetForAssetsWithoutEditor(Type drawerType, Object[] targets, IParentDrawer parent, IInspector inspector);

		/// <summary>
		/// Attempts to get drawer instance for DecoratorDrawer of given type that is responsible for drawing PropertyAttribute.
		/// </summary>
		/// <param name="fieldAttribute"> The PropertyAttribute instance. This cannot be null. </param>
		/// <param name="propertyAttributeType"> The type of the PropertyAttribute. This cannot be null. </param>
		/// <param name="parent"> The parent drawer for the created drawer. This can be null. </param>
		/// <param name="attributeTarget"> The class member that the property attribute targets. </param>
		/// <param name="result"> [out] Instance of drawer that implements IDecoratorDrawerDrawer, or null if failed to create one for the DecoratorDrawer. </param>
		/// <returns> True if succeeded in creating drawer for property drawer, false if failed. </returns>
		bool TryGetForDecoratorDrawer([NotNull]PropertyAttribute fieldAttribute, [NotNull]Type propertyAttributeType, IParentDrawer parent, LinkedMemberInfo attributeTarget, [CanBeNull]out IDecoratorDrawerDrawer result);

		/// <summary> Gets drawer for fields or properties that should be displayed just like normal fields (without getter or setter buttons). </summary>
		/// <param name="values"> The values of the drawer targets. </param>
		/// <param name="parent"> (Optional) The parent drawer of the created drawer. </param>
		/// <param name="label"> The prefix label. Can be null. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. This will never be null. </returns>
		[CanBeNull]
		IFieldDrawer GetForFields([NotNull]object[] values, IParentDrawer parent, GUIContent label = null, bool readOnly = false);

		/// <summary> Gets drawer for field or property that should be displayed just like a normal field (without getter or setter buttons). </summary>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the created drawer represents. This can not be null. </param>
		/// <param name="parent"> (Optional) The parent drawer of the created drawer. </param>
		/// <param name="label"> The prefix label. Can be null. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. This will never be null. </returns>
		[CanBeNull]
		IFieldDrawer GetForField([NotNull]LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label = null, bool readOnly = false);

		/// <summary> Gets drawer for field or property that should be displayed just like a normal field (without getter or setter buttons). </summary>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="memberInfo"> (Optional) LinkedMemberInfo for the field, property or parameter that the created drawer represents. </param>
		/// <param name="parent"> (Optional) The parent drawer of the created drawer. </param>
		/// <param name="label"> The prefix label. Can be null. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. This will never be null. </returns>
		[NotNull]
		IFieldDrawer GetForField([CanBeNull]object value, [CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, [CanBeNull]GUIContent label, bool readOnly);

		/// <summary> Gets drawer for field or property that should be displayed just like a normal field (without getter or setter buttons). </summary>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="fieldType"> The type of the field or property. </param>
		/// <param name="memberInfo"> (Optional) LinkedMemberInfo for the field, property or parameter that the created drawer represents. </param>
		/// <param name="parent"> (Optional) The parent drawer of the created drawer. </param>
		/// <param name="label"> The prefix label. Can be null. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <param name="ignoreAttributes"> If true attributes implementing IUseDrawer on the class won't be considered when determining drawer for class. </param>
		/// <returns> The drawer instance, ready to be used. This will never be null. </returns>
		[NotNull]
		IFieldDrawer GetForField([CanBeNull]object value, [NotNull]Type fieldType, [CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, [CanBeNull]GUIContent label, bool readOnly, bool ignoreAttributes = false);

		/// <summary> Gets drawer for field or property that is not drawn by a PropertyAttribute defined PropertyDrawer. </summary>
		/// <param name="drawerType"> Type of the drawer. </param>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="valueType"> Type constraint for values of the drawer. Usually same as field type if represents a field. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the class member that the created drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. Can be null. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. This will never be null. </returns>
		[NotNull]
		IFieldDrawer GetForField(Type drawerType, [CanBeNull]object value, [NotNull]Type valueType, [CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, [CanBeNull]GUIContent label, bool readOnly);

		/// <summary> Gets drawer for field or property that should be displayed just like a normal field (without getter or setter buttons). </summary>
		/// <param name="valueType"> Type constraint for values of the drawer. Usually same as field type if represents a field. </param>
		/// <param name="memberInfo"> (Optional) LinkedMemberInfo for the field, property or parameter that the created drawer represents. </param>
		/// <param name="parent"> (Optional) The parent drawer of the created drawer. </param>
		/// <param name="label"> The prefix label. Can be null. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. This will never be null. </returns>
		[NotNull]
		IFieldDrawer GetForProperty([NotNull]Type valueType, [NotNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, [CanBeNull]GUIContent label, bool readOnly);

		/// <summary> Gets drawer for field or property that should be displayed just like a normal field (without getter or setter buttons). </summary>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="valueType"> Type constraint for values of the drawer. Usually same as field type if represents a field. </param>
		/// <param name="memberInfo"> (Optional) LinkedMemberInfo for the field, property or parameter that the created drawer represents. </param>
		/// <param name="parent"> (Optional) The parent drawer of the created drawer. </param>
		/// <param name="label"> The prefix label. Can be null. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <param name="ignoreAttributes"> If true attributes implementing IUseDrawer on the class won't be considered when determining drawer for class. </param>
		/// <returns> The drawer instance, ready to be used. This will never be null. </returns>
		[NotNull]
		IFieldDrawer GetForProperty([CanBeNull]object value, [NotNull]Type valueType, [NotNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, [CanBeNull]GUIContent label, bool readOnly, bool ignoreAttributes = false);

		/// <summary>
		/// Gets drawer for field or property with a PropertyAttribute.
		/// First tries to get drawer based on PropertyAttribute type, and failing that, gets it based on field type.
		/// </summary>
		/// <param name="fieldAttribute"> The defining Attribute on the field or property. </param>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="fieldType"> The type of the field or property. </param>
		/// <param name="memberInfo"> (Optional) LinkedMemberInfo for the field, property or parameter that the created drawer represents. </param>
		/// <param name="parent"> (Optional) The parent drawer of the created drawer. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. This will never be null. </returns>
		[NotNull]
		IFieldDrawer GetForPropertyDrawer([NotNull]Attribute fieldAttribute, [CanBeNull]object value, [NotNull]Type fieldType, [CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, [CanBeNull]GUIContent label, bool readOnly);

		/// <summary> Gets drawer for field or property that is drawn by a PropertyAttribute defined PropertyDrawer. </summary>
		/// <param name="drawerType"> Type of the drawer. </param>
		/// <param name="fieldAttribute"> The defining Attribute on the field or property. </param>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the class member that the created drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. This will never be null. </returns>
		[NotNull]
		IPropertyDrawerDrawer GetForPropertyDrawer(Type drawerType, [CanBeNull]Attribute fieldAttribute, [CanBeNull]object value, [CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, [CanBeNull]GUIContent label, bool readOnly);

		/// <summary> Gets drawer for field or property that should be displayed just like a normal field (without getter or setter buttons). </summary>
		/// <param name="methodInfo"> LinkedMemberInfo for the method that the created drawer represents. </param>
		/// <param name="parent"> (Optional) The parent drawer of the created drawer. </param>
		/// <param name="label"> The prefix label. Can be null. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <param name="ignoreAttributes"> If true attributes implementing IUseDrawer on the class won't be considered when determining drawer for class. </param>
		/// <returns> The drawer instance, ready to be used. This will never be null. </returns>
		[NotNull]
		IDrawer GetForMethod(LinkedMemberInfo methodInfo, IParentDrawer parent, GUIContent label = null, bool readOnly = false, bool ignoreAttributes = false);

		/// <summary>
		/// Get instance of drawer by given type.
		/// SetupInterface and LateSetup should be called for the instance.
		/// </summary>
		/// <typeparam name="T"> Interface that the drawer implements. </typeparam>
		/// <param name="drawerType"> Type of drawer to return. </param>
		/// <returns> Drawer instance. </returns>
		[NotNull]
		T GetOrCreateInstance<T>(Type drawerType) where T : IDrawer;

		/// <summary>
		/// Goes through drawers and removes invalid entires.
		/// This can be called after deserialization.
		/// </summary>
		/// <param name="inspected">
		/// It is possible to speed up the cleanup process by only focusing on types of targets currently shown in the inspector.
		/// </param>
		void Cleanup(Object[] inspected);
	}
}