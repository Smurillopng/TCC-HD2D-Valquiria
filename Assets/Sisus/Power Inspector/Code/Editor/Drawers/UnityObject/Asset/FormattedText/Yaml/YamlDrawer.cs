#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using Sisus.Attributes;

namespace Sisus
{
	[Serializable, DrawerByExtension(".yml", true), DrawerByExtension(".yaml", true)]
	public class YamlDrawer : FormattedTextAssetDrawer<YamlSyntaxFormatter>
	{
		protected override YamlSyntaxFormatter CreateSyntaxFormatter()
		{
			return YamlSyntaxFormatterPool.Pop();
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static new YamlDrawer Create(UnityEngine.Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			var result = Create<YamlDrawer>(targets, parent, inspector);
			return result;
		}
	}
}
#endif