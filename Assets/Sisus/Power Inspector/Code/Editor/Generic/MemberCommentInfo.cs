//#define DEBUG_COMMENT
//#define DEBUG_XML_LOAD_FAILED
//#define DEBUG_TYPE_AND_NAME

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class MemberCommentInfo : IDisposable
	{
		private static readonly StringBuilder StringBuilder = new StringBuilder();

		private static readonly char[] MemberDefinitionEndCharacters = {'(', '{', ';', ' '};
		private static readonly string[] Keywords = { "private ", "protected ", "internal ", "public ", "new ", "static ", "unsafe ", "readonly ", "volatile ", "override ", "virtual ", "abstract ", "sealed ", "const ", "event " };

		public readonly string comment = "";
		public readonly string type = "";
		public readonly string name = "";
		
		private MemberCommentInfo() { }

		public MemberCommentInfo(string setComment, string setType, string setName)
		{
			comment = setComment;
			type = setType;
			name = setName;
		}

		public MemberCommentInfo(string xmlComment, string memberDefinition)
		{
			ParseXmlComment(xmlComment, StringBuilder);
			comment = StringBuilder.ToString();
			StringBuilder.Length = 0;
			
			#if DEV_MODE && DEBUG_TYPE_AND_NAME
			string debugInput = memberDefinition;
			#endif
			
			RemoveStringsCharsAndComments(ref memberDefinition);

			#if DEV_MODE && DEBUG_TYPE_AND_NAME
			string debugWithoutCommentsStringAndChars = memberDefinition;
			#endif
			
			RemoveLeadingAttributes(ref memberDefinition);

			#if DEV_MODE && DEBUG_TYPE_AND_NAME
			string debugWithoutAttributes = memberDefinition;
			#endif

			RemoveLeadingKeywords(ref memberDefinition, Keywords);

			#if DEV_MODE && DEBUG_TYPE_AND_NAME
			string debugWithoutKeywords = memberDefinition;
			#endif
			
			int typeEnd = memberDefinition.IndexOf(' ');
			if(typeEnd == -1)
			{
				return;
			}
			type = memberDefinition.Substring(0, typeEnd).TrimStart();
			
			memberDefinition = memberDefinition.Substring(typeEnd + 1);
			memberDefinition = memberDefinition.TrimStart();
			int nameEnd = memberDefinition.IndexOfAny(MemberDefinitionEndCharacters);
			if(nameEnd != -1)
			{
				name = memberDefinition.Substring(0, nameEnd).TrimEnd();
			}
			else
			{
				name = memberDefinition;
			}

			#if DEV_MODE && DEBUG_TYPE_AND_NAME
			Debug.Log("memberDefinition input:\n\"" + debugInput + "\"\n\nwithout attributes:\n\"" + debugWithoutAttributes + "\"\n\nwithout comments, string or chars:\n\"" + debugWithoutCommentsStringAndChars + "\"\n\nwithout keywords:\n\""+ debugWithoutKeywords + "\"\n\ntype:\"" + type + "\", name:\""+name+"\"");
			#endif
		}
		
		public static void RemoveComments(ref string memberDefinition)
		{
			RemoveMultiLineComments(ref memberDefinition);
			RemoveSingleLineComments(ref memberDefinition);
		}

		public static void RemoveMultiLineComments(ref string memberDefinition)
		{
			int commentStart = memberDefinition.IndexOf("/*", StringComparison.Ordinal);
			if(commentStart != -1)
			{
				int commentEnd = memberDefinition.IndexOf("*/", StringComparison.Ordinal);
				if(commentEnd != -1)
				{
					memberDefinition = memberDefinition.Substring(0, commentStart) + " " + memberDefinition.Substring(commentEnd + 2);
					RemoveMultiLineComments(ref memberDefinition);
				}
			}
		}

		public static void RemoveSingleLineComments(ref string memberDefinition)
		{
			int commentStart = memberDefinition.IndexOf("//", StringComparison.Ordinal);
			if(commentStart == -1)
			{
				return;
			}

			int commentEnd = memberDefinition.IndexOf('\r');
			if(commentEnd != -1)
			{
				memberDefinition = memberDefinition.Substring(0, commentStart) + " " + memberDefinition.Substring(commentEnd + 1);
			}
			else
			{
				commentEnd = memberDefinition.IndexOf('\n');

				if(commentEnd != -1)
				{
					memberDefinition = memberDefinition.Substring(0, commentStart) + " " + memberDefinition.Substring(commentEnd + 1);
				}
				else
				{
					memberDefinition = memberDefinition.Substring(0, commentStart);
				}
			}

			RemoveSingleLineComments(ref memberDefinition);
		}
		
		public static void RemoveStringsCharsAndComments(ref string memberDefinition)
		{
			int contextStart = 0;
			var context = CurrentContext.Default;
			bool isEscapeSequence = false;
			for(int i = 0, count = memberDefinition.Length; i < count; i++)
			{
				if(isEscapeSequence)
				{
					isEscapeSequence = false;
					continue;
				}

				var c = memberDefinition[i];

				switch(context)
				{
					case CurrentContext.Default:
						switch(c)
						{
							case '"':
								context = CurrentContext.String;
								contextStart = i;
								break;
							case '\'':
								context = CurrentContext.Char;
								contextStart = i;
								break;
							case '/':
								int next = i + 1;
								if(next == count)
								{
									return;
								}
								if(memberDefinition[next] == '*')
								{
									context = CurrentContext.CommentBlock;
									contextStart = i;
								}
								else if(memberDefinition[next] == '/')
								{
									context = CurrentContext.CommentLine;
									contextStart = i;
								}
								break;
						}
						break;
					case CurrentContext.Char:
						if(c == '\\')
						{
							isEscapeSequence = true;
							break;
						}

						if(c == '\'')
						{
							RemoveContextBlock(out context, ref memberDefinition, ref contextStart, i + 1, ref i, "", ref count);
						}
						break;
					case CurrentContext.String:
						if(c == '\\')
						{
							isEscapeSequence = true;
							break;
						}

						if(c == '"')
						{
							RemoveContextBlock(out context, ref memberDefinition, ref contextStart, i + 1, ref i, "", ref count);
						}
						break;
					case CurrentContext.CommentLine:
						if(c == '\r' || c == '\n')
						{
							RemoveContextBlock(out context, ref memberDefinition, ref contextStart, i, ref i, "", ref count);
						}
						break;
					case CurrentContext.CommentBlock:
						if(c == '\\')
						{
							isEscapeSequence = true;
							break;
						}

						if(c == '*')
						{
							int next = i + 1;
							if(next == count)
							{
								return;
							}
							
							if(memberDefinition[next] == '/')
							{
								RemoveContextBlock(out context, ref memberDefinition, ref contextStart, i, ref i, " ", ref count);
							}
							else
							{
								i++;
							}
						}
						break;
				}
			}
		}

		private static void RemoveContextBlock(out CurrentContext context, ref string memberDefinition, ref int contextStart, int contextEnd, ref int i, string replaceChunkWith, ref int memberDefinitionCharCount)
		{
			context = CurrentContext.Default;
			memberDefinition = memberDefinition.Substring(0, contextStart) + replaceChunkWith + memberDefinition.Substring(contextEnd);
			i -= contextEnd - contextStart;
			contextStart = i;
			memberDefinitionCharCount = memberDefinition.Length;
		}

		public static void RemoveLeadingAttributes(ref string memberDefinition)
		{
			if(memberDefinition.Length > 0 && memberDefinition[0] == '[')
			{
				int attributeEnd = memberDefinition.IndexOf(']', 1);
				if(attributeEnd != -1)
				{
					memberDefinition = memberDefinition.Substring(attributeEnd + 1).TrimStart();
					RemoveLeadingAttributes(ref memberDefinition);
				}
			}
		}

		public static void RemoveLeadingKeywords(ref string memberDefinition, string[] keywords)
		{
			if(memberDefinition.Length == 0)
			{
				return;
			}
			
			for(int n = keywords.Length - 1; n >= 0; n--)
			{
				var keyword = keywords[n];
				if(memberDefinition.StartsWith(keyword, StringComparison.Ordinal))
				{
					memberDefinition = memberDefinition.Substring(keyword.Length);
					RemoveLeadingKeywords(ref memberDefinition, keywords);
					return;
				}
			}
		}

		public static void ParseXmlComment(string xmlComment, StringBuilder sb)
		{
			xmlComment = xmlComment.Trim();
			
			int charCount = xmlComment.Length;

			if(charCount == 0)
			{
				return;
			}

			if(xmlComment[0] != '<')
			{
				#if DEV_MODE && DEBUG_COMMENT
				Debug.Log("Returning whole xmlComment because first letter was not '<'\n\ninput:\n" + xmlComment);
				#endif
				sb.Append(xmlComment);
				return;
			}

			XmlDocument doc;
			if(TryLoadXmlComment(xmlComment, out doc))
			{
				#if DEV_MODE && DEBUG_COMMENT
				Debug.Log("TryLoadXmlComment success:\n" + xmlComment);
				#endif

				ParseXmlComment(doc.DocumentElement, sb);
				return;
			}

			#if DEV_MODE && DEBUG_COMMENT
			Debug.Log("TryLoadXmlComment failed:\n" + xmlComment);
			#endif
			
			int openStart = 0;
			do
			{
				int openEnd = xmlComment.IndexOf('>', openStart + 1);
				if(openEnd == -1)
				{
					#if DEV_MODE && DEBUG_COMMENT
					Debug.Log("Returning whole xmlComment because could not find '>'\n\ninput:\n" + xmlComment);
					#endif
					sb.Append(xmlComment);
					return;
				}

				int tagEnd = xmlComment.IndexOf(' ', openStart + 1);
				int from = openStart + 1;
				int fullNameLength = openEnd - from;
				int tagNameLength;
				if(tagEnd != -1 && tagEnd < openEnd)
				{
					tagNameLength = tagEnd - from;
				}
				else
				{
					tagNameLength = fullNameLength;
				}

				// tag name is part between last "<" and the following " " (if found before the next ">"
				string tagName = xmlComment.Substring(from, tagNameLength);

				string name;
				string body;

				// handle element without body like e.g. <inheritdoc cref="IDrawer.Label" />
				if(xmlComment[openEnd - 1] == '/')
				{
					#if DEV_MODE && DEBUG_COMMENT
					Debug.Log("Skipping to next '<' because element \""+xmlComment.Substring(openStart, openEnd - openStart + 1) + "\" had no body\n\ninput:\n" + xmlComment);
					#endif
					
					body = "";
					fullNameLength--;
					name = xmlComment.Substring(from, fullNameLength);
					
					openStart = xmlComment.IndexOf('<', openEnd + 1);
				}
				else
				{
					name = xmlComment.Substring(from, fullNameLength);

					string closeTag = "</" + tagName + ">";
					int closeStart = xmlComment.IndexOf(closeTag, openEnd + 1, StringComparison.Ordinal);
					if(closeStart == -1)
					{
						#if DEV_MODE
						Debug.LogWarning("ParseXmlComment: failed to find closing tag for "+tagName+" so returning whole xmlComment");
						#endif
						sb.Append(xmlComment);
						return;
					}
				
					from = openEnd + 1;
					body = TrimAllLines(xmlComment.Substring(from, closeStart - from));

					openStart = xmlComment.IndexOf('<', closeStart + closeTag.Length);
				}

				switch(tagName)
				{
					case "summary":
						name = "";
						break;
					case "param":
						if(name.StartsWith("param name=\"", StringComparison.OrdinalIgnoreCase))
						{
							name = name.Substring(12, name.Length - 13);
						}
						break;
					case "typeparam":
						if(name.StartsWith("typeparam name=\"", StringComparison.OrdinalIgnoreCase))
						{
							name = name.Substring(16, name.Length - 17);
						}
						break;
					case "exception":
						if(name.StartsWith("exception cref=\"", StringComparison.OrdinalIgnoreCase))
						{
							name = name.Substring(16, name.Length - 17);
						}
						break;
					//case "inheritdoc":
					//	if(name.StartsWith("inheritdoc cref=\"", StringComparison.OrdinalIgnoreCase))
					//	{
					//		name = name.Substring(17, name.Length - 18);
					//	}
					//	break;
				}

				AddTooltipLine(name, body, sb);
			}
			while(openStart != -1);
		}

		public static bool TryLoadXmlComment(string xmlComment, out XmlDocument doc)
		{
			doc = new XmlDocument();
			try
			{
				doc.LoadXml(xmlComment);
				return true;
			}
			#if DEV_MODE && DEBUG_XML_LOAD_FAILED
			catch(XmlException e)
			{
				Debug.LogWarning(e);
				return false;
			}
			#else
			catch(XmlException)
			{
				return false;
			}
			#endif
		}

		public static void ParseXmlComment(XmlElement xmlElement, StringBuilder sb)
		{
			if(!xmlElement.HasAttributes)
			{
				#if DEV_MODE && DEBUG_COMMENT
				Debug.Log("C \"" + xmlElement.Name + "\" (Loc=" + xmlElement.LocalName + ", Pre=" + xmlElement.Prefix + ", Val=" + xmlElement.Value + ")\nInnerXml:\n" + xmlElement.InnerXml.Replace("><", ">\r\n<") + "\n\nInnerText:\n"+ xmlElement.InnerText+"\n\nOuterXml:"+xmlElement.OuterXml.Replace("><", ">\r\n<"));
				#endif
				AddTooltipLine(xmlElement.InnerXml, sb);
				return;
			}

			#if DEV_MODE && DEBUG_COMMENT
			Debug.Log("P("+ xmlElement.Attributes.Count + ") \"" + xmlElement.Name + "\" (Loc=" + xmlElement.LocalName+ ", Pre="+ xmlElement.Prefix+ ", Val="+ xmlElement.Value+")\nInnerXml:\n" + xmlElement.InnerXml.Replace("><",">\r\n<")+ "\n\nInnerText:\n"+ xmlElement.InnerText + "\n\nOuterXml:" + xmlElement.OuterXml.Replace("><", ">\r\n<")+ "\n\nxmlElement[\"name\"]=" + (xmlElement["name"] == null ? "null" : xmlElement["name"].Name));
			#endif
			
			switch(xmlElement.Name)
			{
				case "param":
				case "paramref":
				case "typeparam":
				case "exception":
				case "see":
				case "seealso":
					var nameElement = xmlElement["name"];
					if(nameElement != null)
					{
						#if DEV_MODE
						Debug.Log(xmlElement.Name+"[\"name\"]: \""+nameElement.InnerText+"\"");
						#endif
						AddTooltipLine(nameElement.InnerText, xmlElement.InnerXml, sb);
						return;
					}

					var crefElement = xmlElement["cref"];
					if(crefElement != null)
					{
						#if DEV_MODE
						Debug.Log(xmlElement.Name+"[\"cref\"]: \""+crefElement.InnerText+"\"");
						#endif
						AddTooltipLine(crefElement.InnerText, xmlElement.InnerXml, sb);
						return;
					}
					
					var outerXml = xmlElement.OuterXml;

					string beforeName = "<" + xmlElement.Name + " name=\"";
					if(outerXml.StartsWith(beforeName, StringComparison.OrdinalIgnoreCase))
					{
						int nameStart = beforeName.Length;
						int nameEnd = outerXml.IndexOf("\">", nameStart, StringComparison.Ordinal);
						if(nameEnd != -1)
						{
							string parsedName = StringUtils.SplitPascalCaseToWords(outerXml.Substring(nameStart, nameEnd - nameStart));
							#if DEV_MODE && DEBUG_COMMENT
							Debug.Log(xmlElement.Name + " name parsed: \""+parsedName + "\"\nouterXml:\n\n" + outerXml);
							#endif
							AddTooltipLine(parsedName, xmlElement.InnerXml, sb);
							return;
						}
					}

					#if DEV_MODE
					Debug.LogWarning(xmlElement.Name+": failed to get name...\nouterXml:\n\n" + outerXml);
					#endif

					AddTooltipLine(xmlElement.InnerXml, sb);
					return;
			}
			
			foreach(var item in xmlElement)
			{
				var element = item as XmlElement;
				if(element == null)
				{
					#if DEV_MODE
					Debug.LogWarning("item "+item.GetType()+" of NodeType "+((XmlNode)item).NodeType+" was not of type XmlElement");
					#endif
					continue;
				}
				ParseXmlComment(element, sb);
			}
		}

		private static void AddTooltipLine(string line, StringBuilder sb)
		{
			if(sb.Length > 0)
			{
				sb.Append('\n');
			}

			WithoutXmlTags(line, sb);
		}

		private static void WithoutXmlTags(string line, StringBuilder sb)
		{
			int i = 0;
			int count = line.Length;

			while(i < count && char.IsWhiteSpace(line[i]))
			{
				i++;
			}

			bool tag = false;
			bool tagName = false;
			while(i < count)
			{
				var c = line[i];

				switch(c)
				{
					case '<':
						tag = true;
						tagName = false;
						break;
					case '>':
						tag = false;
						tagName = false;
						break;
					case '"':
						tagName = tag && !tagName;
						break;
					default:
						if(!tag || tagName)
						{
							sb.Append(c);
						}
						break;
				}

				i++;
			}

			while(sb.Length > 0 && char.IsWhiteSpace(sb[sb.Length - 1]))
			{
				sb.Remove(sb.Length - 1, 1);
			}
		}

		private static void AddTooltipLine(string name, string body, StringBuilder sb)
		{
			if(sb.Length > 0)
			{
				sb.Append('\n');
				sb.Append('\n'); //one or two lines?
			}

			if(name.Length == 0)
			{
				WithoutXmlTags(body, sb);
				return;
			}
			
			sb.Append(StringUtils.SplitPascalCaseToWords(name));

			if(body.Length > 0)
			{
				sb.Append(" : ");
				WithoutXmlTags(body, sb);
			}
		}

		private static string TrimAllLines(string input)
		{
			input = input.Trim();

			for(int i = input.IndexOf('\n'); i != -1; i = input.IndexOf('\n', i + 1))
			{
				input = input.Substring(0, i + 1) + input.Substring(i + 1).TrimStart();
			}

			return input;
		}
		
		public void Dispose() { }
	}
}