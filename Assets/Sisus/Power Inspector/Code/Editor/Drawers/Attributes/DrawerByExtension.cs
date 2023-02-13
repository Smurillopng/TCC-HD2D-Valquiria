using System;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Place this attribute on a class which implements IAssetDrawer to inform DrawerProvider
	/// that the drawers are used to represent assets with the given file extension.
	/// </summary>
	public sealed class DrawerByExtensionAttribute : DrawerForBaseAttribute
	{
		/// <summary>
		/// Exact file extension of assets which IAssetDrawer class with this attribute represents.
		/// If IAssetDrawer should represent assets with different file extensions, you can place
		/// multiple instances of the DrawerByExtensionAttribute on the same class.
		/// </summary>
		[NotNull]
		public readonly string fileExtension;

		/// <inheritdoc/>
		[CanBeNull]
		public override Type Target
		{
			get
			{
				return null;
			}
		}

		/// <inheritdoc/>
		public override bool TargetExtendingTypes
		{
			get
			{
				return false;
			}
		}
		
		public DrawerByExtensionAttribute([NotNull]string setFileExtension) : base(false)
		{
			setFileExtension = setFileExtension.ToLowerInvariant();
			if(setFileExtension.Length == 0 || setFileExtension[0] != '.')
			{
				setFileExtension = "." + setFileExtension;
			}
			
			fileExtension = setFileExtension;

			#if DEV_MODE && PI_ASSERTATIONS
			AssertDataIsValid(null);
			#endif
		}

		public DrawerByExtensionAttribute([NotNull]string setFileExtension, bool setIsFallback) : base(setIsFallback)
		{
			setFileExtension = setFileExtension.ToLowerInvariant();
			if(setFileExtension.Length == 0 || setFileExtension[0] != '.')
			{
				setFileExtension = "." + setFileExtension;
			}
			
			fileExtension = setFileExtension;

			#if DEV_MODE && PI_ASSERTATIONS
			AssertDataIsValid(null);
			#endif
		}

		#if DEV_MODE && PI_ASSERTATIONS
		/// <inheritdoc/>
		public override void AssertDataIsValid(Type drawerType)
		{
			string messageBase = drawerType == null ? string.Concat("DrawerByExtension(\"", fileExtension, "\")") : StringUtils.Concat("DrawerByExtension(\"", fileExtension, "\")=>", drawerType.Name);

			if(drawerType != null)
			{
				UnityEngine.Debug.Assert(typeof(IAssetDrawer).IsAssignableFrom(drawerType), messageBase + " - class with attribute does not implement IAssetDrawer.\nDid you mean to use DrawerForComponent?");
			}
			
			string message = messageBase + " - invalid file extension.";
			UnityEngine.Debug.Assert(fileExtension.Length > 1, message);
			UnityEngine.Debug.Assert(fileExtension.LastIndexOf('.') == 0, message);
			var invalidChars = System.IO.Path.GetInvalidFileNameChars();
			UnityEngine.Debug.Assert(fileExtension.IndexOfAny(invalidChars) == -1, message);
			UnityEngine.Debug.Assert(fileExtension.IndexOf('/') == -1, message);
			UnityEngine.Debug.Assert(fileExtension.IndexOf('\\') == -1, message);
		}
		#endif
	}
}