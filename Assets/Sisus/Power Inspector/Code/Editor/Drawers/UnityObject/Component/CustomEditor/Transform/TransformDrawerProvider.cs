using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// A drawer provider for the members of TransformDrawer.
	/// It works by wrapping an the default drawer provider and reusing its as the basis for drawer fetching,
	/// but it overrides the drawer returned for floats.
	/// </summary>
	[Serializable]
	public class TransformDrawerProvider : DrawerProviderModifier
	{
		public TransformDrawerProvider([NotNull]IDrawerProvider baseProvider) : base(baseProvider) { }

		/// <inheritdoc/>
		public override IFieldDrawer GetForField([CanBeNull]object value, [NotNull]Type fieldType, [CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, [CanBeNull]GUIContent label, bool readOnly, bool ignoreAttributes)
		{
			if(fieldType == Types.Float)
			{
				if(parent != null && typeof(TransformMemberBaseDrawer).IsAssignableFrom(parent.GetType()))
				{
					return base.GetForField(typeof(TransformFloatDrawer), value, fieldType, memberInfo, parent, label, readOnly);
				}
			}
			else if(fieldType == Types.Vector3)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(typeof(ITransformDrawer).IsAssignableFrom(typeof(TransformDrawer)));
				#endif

				if(parent != null && typeof(ITransformDrawer).IsAssignableFrom(parent.GetType()) && memberInfo != null)
				{
					switch(memberInfo.Name)
					{
						case "position":
							return base.GetForField(typeof(PositionDrawer), value, fieldType, memberInfo, parent, label, readOnly);
						case "rotation":
							return base.GetForField(typeof(RotationDrawer), value, fieldType, memberInfo, parent, label, readOnly);
						case "scale":
							return base.GetForField(typeof(ScaleDrawer), value, fieldType, memberInfo, parent, label, readOnly);
					}
					#if DEV_MODE
					Debug.LogError("TransformDrawerProvider failed to figure out proper drawer type for TransformDrawer member");
					#endif
				}
			}

			return base.GetForField(value, fieldType, memberInfo, parent, label, readOnly, ignoreAttributes);
		}
	}
}