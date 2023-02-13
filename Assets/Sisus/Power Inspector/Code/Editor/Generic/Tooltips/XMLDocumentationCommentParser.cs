//#define CACHE_METHODS_USING_FULL_SIGNATURE

#define DEBUG_XML_LOAD_EXCEPTIONS
//#define DEBUG_XML_LOAD_FAILED

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using JetBrains.Annotations;

namespace Sisus
{
    /// <summary>
    /// Class that handles fetching tooltips for fields, properties, methods and parameters
    /// from the XML documentation files of assembly DLL files.
    /// </summary>
    public static class XMLDocumentationCommentParser
    {
        private static readonly Dictionary<Assembly, XmlDocument> CachedXMLDocuments = new Dictionary<Assembly, XmlDocument>();
		private static readonly StringBuilder StringBuilder = new StringBuilder(100);

		/// <summary>
		/// Attempts to find XML documentation file for dll that defines the given class type, parse XML documentation comments
		/// for all members of type, and populate the tooltips Dictionary with the results.
		/// </summary>
		/// <param name="classType"> Type of the class whose members' tooltips we want. This cannot be null. </param>
		/// <param name="tooltips"> [out] The member tooltips. If no XML documentation is found, this will be null. </param>
		/// <returns> True if XML documentation file was found and loaded successfully, false if not. </returns>
		public static bool TryGetMemberTooltips([NotNull]Type classType, [CanBeNull]out Dictionary<string, string> tooltips)
		{
			var assembly = classType.Assembly;
			XmlDocument xmlDocumentation;
			if(!CachedXMLDocuments.TryGetValue(assembly, out xmlDocumentation))
			{
				xmlDocumentation = GetXMLDocument(assembly);
				CachedXMLDocuments[assembly] = xmlDocumentation;
			}

			if(xmlDocumentation == null)
			{
				tooltips = null;
				return false;
			}

			var doc = xmlDocumentation["doc"];
			if(doc == null)
			{
				#if DEV_MODE
				UnityEngine.Debug.LogWarning("XML Documentation for assembly "+classType.Assembly.GetName().Name+" had no \"doc\" section");
				#endif
				tooltips = null;
				return false;
			}

			var members = doc["members"];
			if(members == null)
			{
				#if DEV_MODE
				UnityEngine.Debug.LogWarning("XML Documentation for assembly "+classType.Assembly.GetName().Name+" had no \"members\" section under \"doc\"");
				#endif
				tooltips = null;
				return false;
			}

			tooltips = new Dictionary<string, string>();

			// For example:
			// T:UnityEngine.BoxCollider (class type)
			// T:Sisus.OdinSerializer.QueueFormatter`2 (class with two generic types)
			// F:UnityEngine.Camera.onPreCull (field)
			// P:UnityEngine.BoxCollider.size (property)
			// M:UnityEngine.AI.NavMeshAgent.CompleteOffMeshLink (method with no parameters)
			// M:UnityEngine.Collider.ClosestPoint(UnityEngine.Vector3) (method with parameters)
			// M:Namespace.ClassName`1.MethodName (method inside generic class)
			// M:Namespace.ClassName`1.MethodName(`0) (method inside generic class with parameter of class generic type)
			// M:Namespace.ClassName.#ctor (constructor with no parameters)
			// M:Namespace.ClassName.MethodName``1(``0[]) (method with generic parameter)
			// M:Namespace.ClassName.MethodName``2(System.Collections.Generic.Dictionary{``0,``1}) (method with two generic parameters)
			string match = ":" + classType.FullName + ".";

			foreach(object member in members)
			{
				// Skip non-XmlElement members to avoid exceptions
				if(!(member is XmlElement xmlElement))
				{
					continue;
				}

				// skip members without attributes to avoid exceptions
				if(!xmlElement.HasAttributes)
				{
					continue;
				}

				var attributes = xmlElement.Attributes;
				var typePrefixAndFullName = attributes["name"].InnerText;
				if(typePrefixAndFullName.IndexOf(match, StringComparison.Ordinal) == -1)
				{
					continue;
				}

				MemberCommentInfo.ParseXmlComment(xmlElement, StringBuilder);
				var comment = StringBuilder.ToString();
				StringBuilder.Length = 0;

				bool isMethod = typePrefixAndFullName[0] == 'M';
				if(isMethod)
				{
					int methodParamsStart = typePrefixAndFullName.IndexOf('(');

					// handle special case of methods with parameters
					if(methodParamsStart != -1)
					{
						int methodNameStart = typePrefixAndFullName.LastIndexOf('.', methodParamsStart - 1) + 1;
						string methodNameAndParameters = typePrefixAndFullName.Substring(methodNameStart);

						#if CACHE_METHODS_USING_FULL_SIGNATURE

						tooltips[methodNameAndParameters] = comment;

						#else

						int parametersSectionLength = typePrefixAndFullName.Length - methodParamsStart;
						int methodNameLength = methodNameAndParameters.Length - parametersSectionLength;
						string methodNameOnly = methodNameAndParameters.Substring(0, methodNameLength);

						int genericTypeStart = methodNameOnly.IndexOf('`');
						if(genericTypeStart != -1)
						{
							methodNameOnly = methodNameOnly.Substring(genericTypeStart);
						}

						tooltips[methodNameOnly] = comment;

						#endif
						continue;
					}
					#if !CACHE_METHODS_USING_FULL_SIGNATURE
					// handle special case of generic methods
					else
					{
						int methodNameStart = typePrefixAndFullName.LastIndexOf('.') + 1;
						string methodName = typePrefixAndFullName.Substring(methodNameStart);
						int genericTypeStart = methodName.IndexOf('`');
						if(genericTypeStart != -1)
						{
							methodName = methodName.Substring(genericTypeStart);
						}
						tooltips[methodName] = comment;
					}
					#endif
				}

				int memberNameStart = typePrefixAndFullName.LastIndexOf('.') + 1;
				string memberName = typePrefixAndFullName.Substring(memberNameStart);
				tooltips[memberName] = comment;
			}
			return true;
		}
		
		/// <summary> Tries to find XML Documentation file for given assembly. </summary>
		/// <param name="assembly"> The assembly whose documentation file we want. This cannot be null. </param>
		/// <returns> XmlDocument containing the documentation for the assembly. Null if no documentation was found. </returns>
		[CanBeNull]
		private static XmlDocument GetXMLDocument([NotNull]Assembly assembly)
        {
			string xmlFilePath = GetXMLDocumentationFilepath(assembly);

			if(xmlFilePath.Length == 0)
			{
				return null;
			}

			using(var streamReader = new StreamReader(xmlFilePath))
			{
				var xmlDocument = new XmlDocument();
				try
				{
					xmlDocument.Load(streamReader);
					return xmlDocument;
				}
				#if DEV_MODE && DEBUG_XML_LOAD_FAILED
				catch(Exception e)
				{
					UnityEngine.Debug.LogWarning(e);
				#else
				catch(Exception)
				{
				#endif
					return null;
				}
			}
        }

		[NotNull]
		private static string GetXMLDocumentationFilepath(Assembly assembly)
		{
			string dllPathWithFilePrefix = assembly.CodeBase;
			// convert from explicit to implicit filepath
			string dllPath = new Uri(dllPathWithFilePrefix).LocalPath;
			string directory = Path.GetDirectoryName(dllPath);
			string xmlFileName = Path.GetFileNameWithoutExtension(dllPath) + ".xml";
			string xmlFilePath = Path.Combine(directory, xmlFileName);
			
			if(File.Exists(xmlFilePath))
			{
				#if DEV_MODE && DEBUG_XML_LOAD_SUCCESS
				UnityEngine.Debug.Log("XML Found: "+ xmlFilePath);
				#endif
				return xmlFilePath;
			}

			// Support fetching xml documentation for Unity dlls
			for(directory = Path.GetDirectoryName(xmlFilePath); directory != null; directory = Path.GetDirectoryName(directory))
			{
				if(!directory.EndsWith("Data", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				#if DEV_MODE && DEBUG_XML_LOAD_STEPS
				UnityEngine.Debug.Log("directory: " + directory+"...");
				UnityEngine.Debug.Log("xmlFileName: " + xmlFileName);
				#endif

				string[] xmlFiles;
				
				try
				{
					xmlFiles = Directory.GetFiles(directory, xmlFileName, SearchOption.AllDirectories);
				}
				#if DEV_MODE && DEBUG_XML_LOAD_EXCEPTIONS
				catch(Exception e)
				{
					UnityEngine.Debug.LogWarning(e);
				#else
				catch
				{
				#endif
					continue;
				}

				if(xmlFiles.Length == 0)
				{
					break;
				}

				for(int n = xmlFiles.Length - 1; n >= 0; n--)
				{
					var xmlFile = xmlFiles[n];
					if(xmlFile.EndsWith(xmlFileName, StringComparison.OrdinalIgnoreCase))
					{
						#if DEV_MODE && DEBUG_XML_LOAD_SUCCESS
						UnityEngine.Debug.Log("XML Found: "+ xmlFile);
						#endif

						//make sure the filepath is implicit and not explicit
						string xmlPathWithoutFilePrefix = new Uri(xmlFile).LocalPath;
						return xmlPathWithoutFilePrefix;
					}
				}
			}
			
			#if DEV_MODE && DEBUG_XML_LOAD_FAILED
			UnityEngine.Debug.LogWarning("XML documentation not found @ "+ xmlFilePath, null);
			#endif
			return "";
		}
    }
}
