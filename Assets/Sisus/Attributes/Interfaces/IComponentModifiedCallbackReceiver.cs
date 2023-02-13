using JetBrains.Annotations;
using UnityEngine;

namespace Sisus.Attributes
{
	/// <summary>
	/// Interface for attributes that can be added to component classes to receive callbacks when components of a certain type are added or modified through the inspector.
	/// 
	/// OnComponentAdded is called whenever a new component of type TComponent is added to the GameObject that contains the component with this attribute.
	/// 
	/// OnComponentModified is called whenever an existing component of type TComponent is modified on the GameObject that contains the component with this attribute.
	/// </summary>
	/// <typeparam name="TComponent"> Type of the component </typeparam>
	public interface IComponentModifiedCallbackReceiver<TComponent> where TComponent : Component
	{
		/// <summary>
		/// Called when a new component is added to the GameObject that contains the component with this attribute.
		/// 
		/// Also called when attributeHolder component is first added to a GameObject, for all existing instances components of type TComponent.
		/// </summary>
		/// <param name="attributeHolder"> The component that contains the attribute. </param>
		/// <param name="addedComponent"> The component that was just added to the GameObject. </param>
		void OnComponentAdded([NotNull]Component attributeHolder, [NotNull]TComponent addedComponent);

		/// <summary>
		/// Called when an existing component is modifed on the GameObject that contains the component with this attribute.
		/// </summary>
		/// <param name="attributeHolder"> The component that contains the attribute. </param>
		/// <param name="modifiedComponent"> The component that was just modified. </param>
		void OnComponentModified([NotNull]Component attributeHolder, [NotNull]TComponent modifiedComponent);
	}
}