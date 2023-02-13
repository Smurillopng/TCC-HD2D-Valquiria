using JetBrains.Annotations;

namespace Sisus
{
	public delegate void WebpageLoadFailed([NotNull]string onFailToLoadUrl);

	/// <summary>
	/// Show url in documentation window. If window is not open, open it.
	/// </summary>
	/// <param name="url"> Url to open. </param>
	/// <param name="onFailToLoadUrl"> Delegate to be called upon failure. If null then errors will be logged to console instead. </param>
	public delegate void OpenDocumentationRequest([NotNull]string url = "", WebpageLoadFailed onFailToLoadUrl = null);

	/// <summary>
	/// Show url in documentation window if it is currently open and auto-update is enabled.
	/// </summary>
	/// <param name="url"> Url to open. </param>
	/// <param name="onFailToLoadUrl"> Delegate to be called upon failure. If null then errors will be logged to console instead. </param>
	/// <returns> True if window was open and auto-updating was enabled. </returns>
	public delegate bool ShowDocumentationPageIfWindowOpenRequest([NotNull]string url, WebpageLoadFailed onFailToLoadUrl = null);

	public static class PowerInspectorDocumentation
	{
		#if !UNITY_2020_1_OR_NEWER // WebView class no longer exists in Unity 2020.1 or later
		public static OpenDocumentationRequest OnRequestingOpenWindow;
		public static ShowDocumentationPageIfWindowOpenRequest OnRequestingShowPageIfWindowOpen;
		#endif

		public const string BaseUrl = "https://docs.sisus.co/power-inspector/";
		public const string PreferencesUrl = BaseUrl + "preferences/";
		public const string FeaturesUrl = BaseUrl + "features/";
		public const string TerminologyUrl = BaseUrl + "terminology/";
		

		/// <summary>
		/// Opens the main power inspector documentation page in the Power Inspector documentation window.
		/// </summary>
		public static void Show()
		{
			OpenUrl(BaseUrl);
		}

		/// <summary>
		/// Opens the main power inspector preferences documentation page in the Power Inspector documentation window.
		/// </summary>
		public static void ShowPreferences()
		{
			ShowCategory("preferences");
		}

		/// <summary>
		/// Opens the page in the Power Inspector documentation window.
		/// 
		/// This can be used with GenericMenu.AddItem.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		public static void Show(object pageName)
		{
			Show((string)pageName);
		}

		/// <summary>
		/// Opens the page in the Power Inspector documentation window.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		public static void Show(string pageName)
		{
			OpenUrl(GetUrl(pageName));
		}

		/// <summary>
		/// Given the page name, returns full url in Power Inspector documentation.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		public static string GetUrl(string pageName)
		{
			return BaseUrl + pageName;
		}

		/// <summary>
		/// Opens the feature page in the Power Inspector documentation window.
		/// 
		/// This can be used with GenericMenu.AddItem.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		public static void ShowFeature(object pageName)
		{
			ShowFeature((string)pageName);
		}

		/// <summary>
		/// Opens the feature page in the Power Inspector documentation window.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		public static void ShowFeature(string pageName)
		{
			OpenUrl(GetFeatureUrl(pageName));
		}

		/// <summary>
		/// Given the feature page name, returns full url in Power Inspector documentation.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		public static string GetFeatureUrl(string pageName)
		{
			return FeaturesUrl + pageName;
		}

		/// <summary>
		/// Opens the attribute page in the Power Inspector documentation window.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		public static void ShowAttribute(string pageName)
		{
			OpenUrl(GetAttributeUrl(pageName));
		}

		/// <summary>
		/// Opens the terminology page in the Power Inspector documentation window.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		public static void ShowTerminology(string pageName)
		{
			OpenUrl(GetTerminologyUrl(pageName));
		}

		/// <summary>
		/// Given the terminology page name, returns full url in Power Inspector documentation.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		public static string GetTerminologyUrl(string pageName)
		{
			return TerminologyUrl + pageName;
		}

		/// <summary>
		/// Opens the drawer page in the Power Inspector documentation window.
		/// 
		/// This can be used with GenericMenu.AddItem.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		public static void ShowDrawerInfo(object pageName)
		{
			ShowDrawerInfo((string)pageName);
		}

		/// <summary>
		/// Opens the drawer page in the Power Inspector documentation window.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		public static void ShowDrawerInfo(string pageName)
		{
			OpenUrl(GetDrawerInfoUrl(pageName));
		}
		
		/// <summary>
		/// Given the drawer page name, returns full url in Power Inspector documentation.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		public static string GetDrawerInfoUrl(string pageName)
		{
			return BaseUrl + "enhanced-drawers/" + pageName;
		}

		/// <summary>
		/// Given the attribute page name, returns full url in Power Inspector documentation.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		public static string GetAttributeUrl(string pageName)
		{
			return BaseUrl + "attributes/" + pageName;
		}

		/// <summary>
		/// Opens the category page in the Power Inspector documentation window.
		/// 
		/// This can be used with GenericMenu.AddItem.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		public static void ShowCategory(object pageName)
		{
			ShowCategory((string)pageName);
		}

		/// <summary>
		/// Opens the category page in the Power Inspector documentation window.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		public static void ShowCategory(string pageName)
		{
			OpenUrl(GetCategoryUrl(pageName));
		}

		/// <summary>
		/// Given the category page name, returns full url in Power Inspector documentation.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		public static string GetCategoryUrl(string pageName)
		{
			return BaseUrl + "category/" + pageName;
		}

		/// <summary>
		/// Shows the preferences documentation page in the Power Inspector documentation window if it is already open and has auto update enabled.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		/// <param name="onFailToLoadUrl"> (Optional) Callback that is invoked if loading the url fails. </param>
		public static bool ShowPreferencesIfWindowOpen(string pageName, WebpageLoadFailed onFailToLoadUrl = null)
		{
			return ShowUrlIfWindowOpen(GetPreferencesUrl(pageName), onFailToLoadUrl);
		}

		/// <summary>
		/// Shows the feature documentation page in the Power Inspector documentation window if it is already open and has auto update enabled.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		/// <param name="onFailToLoadUrl"> (Optional) Callback that is invoked if loading the url fails. </param>
		public static bool ShowFeatureIfWindowOpen(string pageName, WebpageLoadFailed onFailToLoadUrl = null)
		{
			return ShowUrlIfWindowOpen(GetFeatureUrl(pageName), onFailToLoadUrl);
		}

		/// <summary>
		/// Show url in documentation window if it is currently open and auto-update is enabled.
		/// </summary>
		/// <param name="url"> Full url of the page. </param>
		/// <param name="onFailToLoadUrl"> (Optional) Callback that is invoked if loading the url fails. If null then errors will be logged to console instead. </param>
		/// <returns> True if window was open and auto-updating was enabled. </returns>
		public static bool ShowUrlIfWindowOpen(string url, WebpageLoadFailed onFailToLoadUrl = null)
		{
			#if !UNITY_2020_1_OR_NEWER // WebView class no longer exists in Unity 2020.1 or later
			if(OnRequestingShowPageIfWindowOpen != null)
			{
				return OnRequestingShowPageIfWindowOpen(url, onFailToLoadUrl);
			}
			#if DEV_MODE
			else { UnityEngine.Debug.LogError("PowerInspectorDocumentation OnRequestingShowPageIfWindowOpen was null - can't open documentation page"); }
			#endif
			
			#endif

			return false;
		}
		
		/// <summary>
		/// Given the preferences page name, returns full url in Power Inspector documentation.
		/// </summary>
		/// <param name="pageName"> The name of the page. </param>
		public static string GetPreferencesUrl(string pageName)
		{
			return PreferencesUrl + pageName;
		}

		/// <summary>
		/// Opens the documentation page in the Power Inspector documentation window.
		/// </summary>
		/// <param name="url"> The URL of the page in full. </param>
		/// <param name="onFailToLoadUrl"> (Optional) Callback that is invoked if loading the url fails. </param>
		public static void OpenUrl(string url, WebpageLoadFailed onFailToLoadUrl = null)
		{
			#if UNITY_2020_1_OR_NEWER // WebView class no longer exists in Unity 2020.1 or later
			ShowExternalDocumentation(url);
			#else
			if(OnRequestingOpenWindow != null)
			{
				OnRequestingOpenWindow(url, onFailToLoadUrl);
			}
			#if DEV_MODE
			else { UnityEngine.Debug.LogError("PowerInspectorDocumentation OnRequestingOpenWindow was null - can't open documentation page"); }
			#endif
			#endif
		}

		

		/// <summary>
		/// Opens documentation page that is not part of the Power Inspector documentation,
		/// for example a page in the official Unity documentation.
		/// 
		/// This can be used with GenericMenu.AddItem.
		/// </summary>
		/// <param name="fullUrl"> The URL of the page in full. </param>
		public static void ShowExternalDocumentation(object fullUrl)
		{
			ShowExternalDocumentation((string)fullUrl);
		}

		/// <summary>
		/// Opens documentation page that is not part of the Power Inspector documentation,
		/// for example a page in the official Unity documentation.
		/// </summary>
		/// <param name="fullUrl"> The URL of the page in full. </param>
		public static void ShowExternalDocumentation(string fullUrl)
		{
			UnityEngine.Application.OpenURL(fullUrl);
		}
	}
}