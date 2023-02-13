using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	public static class Clipboard
	{
		private static bool isCut;

		private static Object objectReference;
		private static List<Object> objectReferences = new List<Object>();
		private static Type copiedType = typeof(void);
		private static bool copiedTypeIsDefaultValue = true;
		private static byte[] cutMemberInfo;
		private static bool hasObjectReference;

		private static string lastSystemCopyBufferInput = "";

		private static readonly List<string> ReusedStringList = new List<string>();

		private static bool lastOperationFailed;

		public static bool LastOperationFailed
		{
			get
			{
				return lastOperationFailed;
			}
		}

		public static string Content
		{
			get
			{
				return GUIUtility.systemCopyBuffer;
			}

			set
			{
				GUIUtility.systemCopyBuffer = value;
				lastSystemCopyBufferInput = value;
				copiedTypeIsDefaultValue = false;
			}
		}

		private static bool CopiedTypeIsDefaultValue
		{
			get
			{
				HandleContentCopiedToClipboardManually();
				return copiedTypeIsDefaultValue;
			}
		}

		public static Type CopiedType
		{
			get
			{
				HandleContentCopiedToClipboardManually();
				return copiedType;
			}
		}

		public static Object ObjectReference
		{
			get
			{
				if(CopiedTypeIsDefaultValue)
				{
					var deserializedFromClipboard = PrettySerializer.DeserializeReference(Content, false);
					if(deserializedFromClipboard != null)
					{
						return deserializedFromClipboard;
					}
				}
				return objectReference;
			}

			set
			{
				objectReference = value;
				hasObjectReference = true;
			}
		}

		/// <summary>
		/// If user has copied new text to the systemCopyBuffer without doing it through this class then copy the same content.
		/// </summary>
		private static void HandleContentCopiedToClipboardManually()
		{
			// If user has copied new text to the systemCopyBuffer without doing it through this class...
			if(!string.Equals(lastSystemCopyBufferInput, GUIUtility.systemCopyBuffer))
			{
				Copy(GUIUtility.systemCopyBuffer);
				copiedTypeIsDefaultValue = true;
			}
		}
		
		public static bool HasObjectReference()
		{
			if(hasObjectReference)
			{
				#if DEV_MODE
				Debug.Log("HasObjectReference: " + StringUtils.True);
				#endif
				return true;
			}

			if(CopiedType == Types.String)
			{
				if(PrettySerializer.IsSerializedReference(Content))
				{
					#if DEV_MODE
					Debug.Log("HasObjectReference: " + StringUtils.True);
					#endif
					return true;
				}
			}
			#if DEV_MODE
			Debug.Log("HasObjectReference: " + StringUtils.False);
			#endif
			return false;
		}

		public static Object PasteObjectReference([NotNull]Type type)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(type != null, "PasteObjectReference(null)");
			Debug.Assert(type.IsUnityObject() || type.IsInterface, type.FullName);
			#endif

			lastOperationFailed = true;

			Object result;
			if(CopiedType == Types.String)
			{
				result = null;
				string content = Content;
				
				//support pasting using hierarchy path
				if(type == Types.GameObject || type == Types.UnityObject)
				{
					result = HierarchyUtility.FindByHierarchyPath(content);
				}

				if(result == null)
				{
					if(type == Types.UnityObject || type.IsComponent())
					{
						result = HierarchyUtility.FindComponentByHierarchyPath(content, type);
					}

					#if UNITY_EDITOR
					//support pasting using asset path
					if(result == null)
					{
						var asset = AssetDatabase.LoadAssetAtPath(content, type);
						if(asset != null)
						{
							result = asset;
						}
					}
					#endif
				}
			}
			else
			{
				result = objectReference;
			}

			if(result != null && !type.IsInstanceOfType(result))
			{
				var resultGameObject = result as GameObject;
				if(resultGameObject != null)
				{
					if(type.IsComponent() || type.IsInterface)
					{
						#if DEV_MODE
						Debug.Log("PasteObjectReference("+type.Name+ ") !IsInstanceOfType("+result.GetType().Name+ ") and resultGameObject.GetComponent("+type.Name+"): "+StringUtils.ToString(resultGameObject.GetComponent(type)));
						#endif

						result = resultGameObject.GetComponent(type);
					}
					else
					{
						#if DEV_MODE
						Debug.Log("PasteObjectReference("+type.Name+ ") !IsInstanceOfType("+result.GetType().Name+ ") so returning: "+StringUtils.Null);
						#endif

						result = null;
					}
				}
				else
				{
					var resultComponent = result as Component;
					if(resultComponent != null)
					{
						if(type.IsGameObject())
						{
							#if DEV_MODE
							Debug.Log("PasteObjectReference("+type.Name+ ") !IsInstanceOfType("+result.GetType().Name+ ") but returning resultComponent.gameObject: "+StringUtils.ToString(resultComponent.gameObject));
							#endif

							result = resultComponent.gameObject;
						}
						else
						{
							#if DEV_MODE
							Debug.Log("PasteObjectReference("+type.Name+ ") !IsInstanceOfType("+result.GetType().Name+ ") so returning: "+StringUtils.Null);
							#endif

							result = null;
						}
					}
					else
					{
						#if UNITY_EDITOR
						string path = AssetDatabase.GetAssetPath(result);
						if(!string.IsNullOrEmpty(path))
						{
							result = AssetDatabase.LoadAssetAtPath(path, type);
						}
						else
						{
							result = null;
						}
						#else
						result = null;
						#endif
					}
				}
			}
			#if DEV_MODE
			else { Debug.Log("PasteObjectReference(" + type.Name+ ") IsInstanceOfType("+StringUtils.ToColorizedString(result)+ ")="+StringUtils.True); }
			#endif

			if(isCut)
			{
				OnCutPasted();
			}

			lastOperationFailed = false;

			return result;
		}

		public static string CutObjectReference(Object target, Type type)
		{
			lastOperationFailed = true;

			if(!target.IsSceneObject())
			{
				throw new NotSupportedException("Clipboard.Cut is not supported for asset types to avoid data loss.");
			}
			string result = CopyObjectReference(target, type);
			isCut = true;
			
			lastOperationFailed = false;

			return result;
		}
		
		public static string Cut([CanBeNull]object target, [CanBeNull]LinkedMemberInfo memberInfo)
		{
			lastOperationFailed = true;

			if(memberInfo != null)
			{
				cutMemberInfo = SerializableMemberInfo.Serialize(memberInfo);
			}
			ObjectReference = target as Object;
			hasObjectReference = objectReference != null;
			
			if(target != null)
			{
				copiedType = target.GetType();
			}
			else if(memberInfo != null)
			{
				copiedType = memberInfo.Type;
			}
			else
			{
				throw new NullReferenceException("Clipboard.Cut both target and memberInfo were null");
			}

			Content = PrettySerializer.Serialize(target);
			isCut = true;

			lastOperationFailed = false;

			return Content;
		}

		public static string CopyObjectReference([CanBeNull]Object target, Type type)
		{
			lastOperationFailed = true;

			ClearCutData();
			
			ObjectReference = target;
			hasObjectReference = true;
			objectReferences.Clear();
			objectReferences.Add(target);

			if(target == null)
			{
				copiedType = type;
				Content = "";
				return Content;
			}

			copiedType = target.GetType();

			Content = PrettySerializer.SerializeReference(target);

			lastOperationFailed = false;

			return Content;
		}
		
		public static string CopyObjectReferences(IEnumerable<Object> targets, Type type)
		{
			lastOperationFailed = true;

			ClearCutData();
			copiedType = type;
			hasObjectReference = false;
			objectReferences.Clear();
			var ids = ReusedStringList;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(ids.Count == 0, "ReusedStringList.Count != 0!");
			#endif

			foreach(var target in targets)
			{
				if(!hasObjectReference)
				{
					hasObjectReference = true;
					ObjectReference = target;
				}

				objectReferences.Add(target);
				ids.Add(PrettySerializer.SerializeReference(target));
			}
			
			var serialize = ids.ToArray();
			ids.Clear();
			Content = PrettySerializer.Serialize(serialize);

			lastOperationFailed = false;

			return Content;
		}

		public static bool TryCopy([CanBeNull]object target, [NotNull]Type type, out string result)
		{
			try
			{
				result = Copy(target, type);
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogWarning(e.ToString());
			#else
			catch(Exception)
			{
			#endif
				lastOperationFailed = true;
				result = "";
			}
			return !lastOperationFailed;
		}

		public static bool TryCopy([CanBeNull]object target, [NotNull]Type type)
		{
			try
			{
				Copy(target, type);
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogWarning(e.ToString());
			#else
			catch(Exception)
			{
			#endif
				lastOperationFailed = true;
			}
			return !lastOperationFailed;
		}

		public static string Copy([CanBeNull]object target, [NotNull]Type type)
		{
			lastOperationFailed = true;

			ClearCutData();
			if(target == null)
			{
				copiedType = type;
				ObjectReference = null;
				hasObjectReference = type.IsUnityObject();
				objectReferences.Clear();
				if(hasObjectReference)
				{
					objectReferences.Add(null);
				}
				Content = "null";
			}
			else
			{
				#if DEV_MODE
				Debug.Assert(target.GetType() == type, "target.GetType() "+ StringUtils.TypeToString(target)+" != type "+StringUtils.ToString(type));
				#endif

				copiedType = type;
				var unityObject = target as Object;
				ObjectReference = unityObject;
				hasObjectReference = objectReference != null;
				objectReferences.Clear();
				if(hasObjectReference)
				{
					objectReferences.Add(unityObject);
				}
				Content = PrettySerializer.Serialize(target, ref objectReferences);
			}

			lastOperationFailed = false;

			return Content;
		}

		public static bool TryCopy([CanBeNull]object target, out string result)
		{
			try
			{
				result = Copy(target);
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogWarning(e.ToString());
			#else
			catch(Exception)
			{
			#endif
				lastOperationFailed = true;
				result = "";
			}
			return !lastOperationFailed;
		}

		public static bool TryCopy([CanBeNull]object target)
		{
			try
			{
				Copy(target);
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogWarning(e.ToString());
			#else
			catch(Exception)
			{
			#endif
				lastOperationFailed = true;
			}
			return !lastOperationFailed;
		}

		public static string Copy([NotNull]object target)
		{
			lastOperationFailed = true;

			ClearCutData();
			if(target == null)
			{
				copiedType = Types.Void;
				ObjectReference = target as Object;
				hasObjectReference = false;
				objectReferences.Clear();
				Content = "null";
			}
			else
			{
				copiedType = target.GetType();
				ObjectReference = target as Object;
				hasObjectReference = objectReference != null;
				objectReferences.Clear();
				Content = PrettySerializer.Serialize(target, ref objectReferences);
			}

			lastOperationFailed = false;

			return Content;
		}

		public static string Copy(string text)
		{
			lastOperationFailed = true;

			ClearCutData();
			copiedType = Types.String;
			ObjectReference = null;
			hasObjectReference = false;
			objectReferences.Clear();
			Content = text;

			lastOperationFailed = false;

			return Content;
		}

		public static bool TryPaste<T>(ref object objectToOverwrite)
		{
			if(TryPaste(typeof(T), ref objectToOverwrite))
			{
				lastOperationFailed = false;
				return true;
			}
			lastOperationFailed = true;
			return false;
		}
		
		public static bool TryPaste(Type type, ref object objectToOverwrite)
		{
			try
			{
				var unityObject = objectToOverwrite as Object;
				if(unityObject != null)
				{
					return TryPasteUnityObject(unityObject);
				}

				objectToOverwrite = Paste(type);

				if(isCut)
				{
					OnCutPasted();
				}

				lastOperationFailed = false;
				return true;
			}
            catch
			{
				lastOperationFailed = true;
				return false;
			}
		}
		
		public static bool TryPasteUnityObject(Object objectToOverwrite)
		{
			try
			{
				PrettySerializer.DeserializeUnityObject(Content, objectToOverwrite, ref objectReferences);
				lastOperationFailed = false;
				return true;
			}
			#if DEV_MODE
            catch(Exception e)
			{
				Debug.LogError("TryPasteUnityObject failed for " + objectToOverwrite.name+" of type "+ StringUtils.TypeToString(objectToOverwrite)+": "+ e);
				lastOperationFailed = true;
				return false;
			}
			#else
			catch
			{
				lastOperationFailed = true;
				return false;
			}
			#endif
		}

		public static bool CanPasteAs(Type type)
		{
			if(type.IsInterface)
			{
				var clipboardContentType = CopiedType;
				if(type.IsAssignableFrom(clipboardContentType))
				{
					#if DEV_MODE
					Debug.Log("CanPasteAs(" + type.Name + ") with CopiedType=" + clipboardContentType.Name+": " + StringUtils.True);
					#endif

					lastOperationFailed = false;
					return true;
				}

				#if DEV_MODE
				Debug.Log("CanPasteAs(" + type.Name + ") with CopiedType=" + clipboardContentType.Name + ": " + StringUtils.False);
				#endif

				lastOperationFailed = true;
				return false;
			}

			if(type.IsUnityObject())
			{
				var clipboardContentType = CopiedType;

				#if DEV_MODE
				Debug.Log("CanPasteAs("+type.Name+ ") with CopiedType="+clipboardContentType.Name);
				#endif

				if(type.IsAssignableFrom(clipboardContentType))
				{
					lastOperationFailed = false;
					return true;
				}

				if(clipboardContentType == Types.String)
				{
					string content = Content;
					
					#if UNITY_EDITOR
					//support pasting using asset path
					var assetAtPath = AssetDatabase.LoadAssetAtPath(content, type);
					if(assetAtPath != null)
					{
						lastOperationFailed = false;
						return true;
					}
					#endif

					//support pasting using hierarchy path
					if(type == Types.GameObject || type == Types.UnityObject)
					{
						if(HierarchyUtility.FindByHierarchyPath(content) != null)
						{
							lastOperationFailed = false;
							return true;
						}
					}
					if(type == Types.UnityObject || type.IsComponent())
					{
						if(HierarchyUtility.FindComponentByHierarchyPath(content, type) != null)
						{
							lastOperationFailed = false;
							return true;
						}
					}
				}

				// we can convert between Component and GameObject types
				// using Component.gameObject and gameObject.GetComponent
				if(clipboardContentType == Types.GameObject)
				{
					if(type.IsComponent())
					{
						lastOperationFailed = false;
						return true;
					}
				}
				else if(clipboardContentType.IsComponent())
				{
					if(type.IsComponent() || type.IsGameObject())
					{
						lastOperationFailed = false;
						return true;
					}
				}

				#if DEV_MODE
				Debug.Log("CanPasteAs("+type.Name+"): "+StringUtils.False);
				#endif
				lastOperationFailed = true;
				return false;
			}

			try
			{
				Paste(type, true);
				return true;
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogWarning("CanPasteAs failed for type " + StringUtils.ToString(type)+": "+e);
				return false;
			}
			#else
			catch
			{
				return false;
			}
			#endif
		}

		public static T Paste<T>()
		{
			lastOperationFailed = true;

			var result = (T)Paste(typeof(T));

			lastOperationFailed = false;

			return result;
		}

		public static object Paste(Type type)
		{
			// NEW!
			if(HasObjectReference() && (type.IsInterface || type.IsUnityObject()))
			{
				#if DEV_MODE
				Debug.Log("Clipboard.Paste("+type.FullName+") called with type that was UnityObject or interface. Returning copied Object reference.");
				#endif
				return PasteObjectReference(type);
			}

			lastOperationFailed = true;

			var result = Paste(type, false);

			lastOperationFailed = false;

			return result;
		}

		private static object Paste(Type type, bool isOnlyATest)
		{
			lastOperationFailed = true;

			string stringData = Content;
			object result = null;

			bool success;
			try
			{
				success = PrettySerializer.DeserializeOverrideNonUnityObject(stringData, type, ref result, objectReferences);
			}
			#if DEV_MODE
			catch(Exception e)
			#else
			catch
			#endif
			{
				#if DEV_MODE
				Debug.Log(e);
				#endif
				success = false;
			}

			if(!success)
			{
				#if DEV_MODE
				Debug.Log("Could not deserialize Clipboard contents as type " + type.Name + ": " + stringData);
				#endif

				if(!CopiedTypeIsDefaultValue && copiedType != type)
				{
					try
					{
						success = PrettySerializer.DeserializeOverrideNonUnityObject(stringData, copiedType, ref result, objectReferences);
					}
					#if DEV_MODE
					catch(Exception e)
					#else
					catch
					#endif
					{
						#if DEV_MODE
						Debug.Log(e);
						#endif
						success = false;
					}

					#if DEV_MODE
					Debug.Log("Could not deserialize Clipboard contents as copied type " + copiedType.Name + ": " + stringData);
					#endif
				}

				if(!success)
				{
					throw new ArgumentException("Could not deserialize Clipboard contents as type " + type.Name + " or as copied type "+copiedType.Name+": " + stringData);
				}
				
				if(!Converter.TryChangeType(ref result, type))
				{
					throw new InvalidCastException("Could not deserialize Clipboard contents as type " + type.Name + " or cast value deserialized as copied type " + copiedType.Name + " to that type: " + stringData);
				}
			}

			if(!isOnlyATest && isCut)
			{
				OnCutPasted();
			}

			lastOperationFailed = false;

			return result;
		}
		
		public static List<Object> PasteObjectReferences()
		{
			lastOperationFailed = true;

			var result = objectReferences;
			if(isCut)
			{
				OnCutPasted();
			}

			lastOperationFailed = false;

			return result;
		}

		public static void ClearClipboard()
		{
			ClearCutData();
			Content = "";
			ObjectReference = null;
			hasObjectReference = false;
			copiedType = Types.Void;
		}

		/// <summary>
		/// Sends a message to the user that gives feedback about the last copy or cut to clipboard operation.
		/// 
		/// The message is customized based on the copied type.
		/// 
		/// If the operation failed, a generic "can not" failure message will be shown.
		/// 
		/// </summary>
		/// <param name="name"> The name. </param>
		public static void SendCopyToClipboardMessage(string name)
		{
			if(lastOperationFailed)
			{
				if(isCut)
				{
					if(copiedType.IsUnityObject())
					{
						SendOperationFailedMessage("Can not cut{0} values.", name);
					}
					else if(typeof(ICollection).IsAssignableFrom(copiedType))
					{
						SendOperationFailedMessage("Can not cut{0} values.", name);
					}
					else
					{
						SendOperationFailedMessage("Can not cut{0} value.", name);
					}
				}
				else if(copiedType.IsUnityObject())
				{
					SendOperationFailedMessage("Can not copy{0} values.", name);
				}
				else if(typeof(ICollection).IsAssignableFrom(copiedType))
				{
					SendOperationFailedMessage("Can not copy{0} values.", name);
				}
				else
				{
					SendOperationFailedMessage("Can not copy{0} value.", name);
				}
			}
			else if(isCut)
			{
				if(copiedType.IsUnityObject())
				{
					SendCopyToClipboardMessage("Cut{0} values", name, "");
				}
				else if(typeof(ICollection).IsAssignableFrom(copiedType))
				{
					SendCopyToClipboardMessage("Cut{0} values", name);
				}
				else
				{
					SendCopyToClipboardMessage("Cut{0} value", name);
				}
			}
			else if(copiedType.IsUnityObject())
			{
				SendCopyToClipboardMessage("Copied{0} values", name, "");
			}
			else if(typeof(ICollection).IsAssignableFrom(copiedType))
			{
				SendCopyToClipboardMessage("Copied{0} values", name);
			}
			else
			{
				SendCopyToClipboardMessage("Copied{0} value", name);
			}
		}

		public static void SendCopyToClipboardMessage(string messageBody, string name)
		{
			if(lastOperationFailed)
			{
				if(messageBody.StartsWith("Copied", StringComparison.Ordinal))
				{
					messageBody = "Can not copy" + messageBody.Substring(6);
				}
				else if(messageBody.StartsWith("Cut", StringComparison.Ordinal))
				{
					messageBody = "Can not cut" + messageBody.Substring(3);
				}
				else if(messageBody.StartsWith("Copy", StringComparison.Ordinal))
				{
					messageBody = "Can not copy" + messageBody.Substring(4);
				}
				SendOperationFailedMessage(messageBody, name);
				return;
			}
			SendCopyToClipboardMessage(messageBody, name, hasObjectReference ? "" : Content);
		}

		/// <summary>
		/// Sends a message to user indicating that value of target by given name was copied to clipboard.
		/// </summary>
		/// <param name="messageBody">
		/// (Optional) The body of the message.
		/// 
		/// Use the text "{0}" to indicate spot where name of target preceded by a space character should be placed.
		/// 
		/// For example given the messageBody "Copied{0} value" and name "Example", the final message would equate to "Copied "Example" value."
		/// </param>
		/// <param name="name"> The name of target onto which value was pasted. This can be an empty string. </param>
		/// <param name="serializedData">
		/// The serialized data that was copied to the clipboard. This can be an empty string.
		/// 
		/// If serializedData contents are empty or the string is very long, a dot character will be placed at the end of the final message.
		/// 
		/// If serializedData is not empty and it is of short enough length, then its contents will be added to the end of the message preceded by a colon character.
		/// 
		/// The serializedData will only ever be part of the message send to the console and will never be displayed in popup notifications.
		/// </param>
		public static void SendCopyToClipboardMessage([NotNull]string messageBody, [NotNull]string name, [NotNull]string serializedData)
		{
			string message;
			if(name.Length > 0)
			{
				message = string.Format(messageBody, StringUtils.Concat(" \"", name, "\""));
			}
			else
			{
				message = string.Format(messageBody, "");
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!lastOperationFailed);
			Debug.Assert(!message.EndsWith(".", StringComparison.Ordinal));
			Debug.Assert(!message.EndsWith("!", StringComparison.Ordinal));
			Debug.Assert(!message.EndsWith(":", StringComparison.Ordinal));
			Debug.Assert(!message.EndsWith("?", StringComparison.Ordinal));
			#endif

			var messageDisplayMethod = InspectorUtility.Preferences.messageDisplayMethod;

			// Display shorter version of message as notification
			if(InspectorUtility.ActiveInspector != null && messageDisplayMethod.HasFlag(MessageDisplayMethod.Notification))
			{
				InspectorUtility.ActiveInspector.Message(MakeCopyToClipboardMessage(string.Concat(message, ".")), null, MessageType.Info, false);
			}			

			//Unity will truncate messages longer than 16300 lines from the log
			if(serializedData.Length > 0 && serializedData.Length < 300)
			{
				if(serializedData.IndexOf('\n') != -1)
				{
					message = string.Concat(message, "\n", serializedData);
				}
				else
				{
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(!message.EndsWith(" ", StringComparison.Ordinal));
					Debug.Assert(!message.EndsWith(":", StringComparison.Ordinal));
					Debug.Assert(!message.EndsWith(".", StringComparison.Ordinal));
					#endif
					message = string.Concat(message, ": ", serializedData);
				}
			}
			else
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!message.EndsWith("."));
				#endif
				message = string.Concat(message, ".");
			}

			if(messageDisplayMethod.HasFlag(MessageDisplayMethod.Console))
			{
				Debug.Log(message);
			}
		}

		public static GUIContent MakeCopyToClipboardMessage([NotNull]string text)
		{
			if(InspectorUtility.ActiveInspector != null)
			{
				return new GUIContent(text, InspectorUtility.ActiveInspector.Preferences.graphics.clipboardCopy);
			}
			return new GUIContent(text);
		}

		/// <summary>
		/// Sends a message to user indicating that value of target by given name was pasted from clipboard.
		/// 
		/// The message will be in format "Pasted value to [name]." or if no name was provided "Pasted value."
		/// 
		/// If paste operation failed, then message will be "Can not paste to [name].", or if name no name was provided "Can not paste."
		/// </summary>
		/// <param name="name"> The name of target onto which value was pasted. </param>
		public static void SendPasteFromClipboardMessage([NotNull]string name)
		{
			if(lastOperationFailed)
			{
				SendOperationFailedMessage("Can not paste{0}.", name);
				return;
			}

			if(hasObjectReference)
			{
				if(objectReferences.Count > 1)
				{
					SendPasteFromClipboardMessage("Pasted references{0}.", name);
				}
				else
				{
					SendPasteFromClipboardMessage("Pasted reference{0}.", name);
				}
			}
			else if(copiedType.IsUnityObject())
			{
				SendPasteFromClipboardMessage("Pasted value{0}.", name);
			}
			else if(typeof(ICollection).IsAssignableFrom(copiedType))
			{
				SendPasteFromClipboardMessage("Pasted value{0}.", name);
			}
			else
			{
				SendPasteFromClipboardMessage("Pasted value{0}.", name);
			}
		}

		/// <summary>
		/// Sends a message to user indicating that value of target by given name was pasted from clipboard.
		/// </summary>
		/// <param name="messageBody">
		/// The body of the message. Use the text "{0}" to indicate spot where name of target preceded by " to "  should be placed.
		/// 
		/// For example given the messageBody "Pasted value{0}." and name "Example", the final message would equate to "Pasted value to "Example"."
		/// </param>
		/// <param name="name"> The name of target onto which value was pasted. </param>
		public static void SendPasteFromClipboardMessage([NotNull]string messageBody, [NotNull]string name)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(messageBody.Length > 0);
			Debug.Assert(messageBody.IndexOf(" to ", StringComparison.Ordinal) == -1 || name.Length == 0);
			Debug.Assert(messageBody.IndexOf(" to{", StringComparison.Ordinal) == -1 || name.Length == 0);
			Debug.Assert(messageBody.EndsWith(".", StringComparison.Ordinal) || messageBody.EndsWith("!", StringComparison.Ordinal));
			Debug.Assert(!lastOperationFailed);
			#endif

			string message = name.Length > 0 ? string.Format(messageBody, StringUtils.Concat(" to \"", name, "\"")) : string.Format(messageBody, "");
			if(InspectorUtility.ActiveInspector != null)
			{
				InspectorUtility.ActiveInspector.Message(MakePasteFromClipboardMessage(message));
			}
			else
			{
				Debug.Log(message);
			}
		}
		
		public static GUIContent MakePasteFromClipboardMessage([NotNull]string text)
		{
			if(InspectorUtility.ActiveInspector != null)
			{
				return new GUIContent(text, InspectorUtility.ActiveInspector.Preferences.graphics.clipboardPaste);
			}
			return new GUIContent(text);
		}

		/// <summary>
		/// Sends a "Can not" message to the user that gives feedback about the last copy or cut operation failure.
		/// </summary>
		/// <param name="name"> The name of the target. </param>
		public static void SendOperationFailedMessage([NotNull]string name)
		{
			#if DEV_MODE
			Debug.Assert(lastOperationFailed);
			#endif

			if(isCut)
			{
				if(copiedType.IsUnityObject())
				{
					SendOperationFailedMessage("Can not cut{0} values.", name);
				}
				else if(typeof(ICollection).IsAssignableFrom(copiedType))
				{
					SendOperationFailedMessage("Can not cut{0} values.", name);
				}
				else
				{
					SendOperationFailedMessage("Can not cut{0} value.", name);
				}
			}
			else if(copiedType.IsUnityObject())
			{
				SendOperationFailedMessage("Can not copy{0} values.", name);
			}
			else if(typeof(ICollection).IsAssignableFrom(copiedType))
			{
				SendOperationFailedMessage("Can not copy{0} values.", name);
			}
			else
			{
				SendOperationFailedMessage("Can not copy{0} value.", name);
			}
		}

		public static void SendOperationFailedMessage([NotNull]string messageBody, [NotNull]string name)
		{
			string message = name.Length > 0 ? string.Format(messageBody, StringUtils.Concat(" to \"", name, "\"")) : string.Format(messageBody, "");
			if(InspectorUtility.ActiveInspector != null)
			{
				InspectorUtility.ActiveInspector.Message(MakeInvalidOperationMessage(message));
			}
			else
			{
				Debug.Log(message);
			}
		}

		public static GUIContent MakeInvalidOperationMessage([NotNull]string text)
		{
			if(InspectorUtility.ActiveInspector != null)
			{
				return new GUIContent(text, InspectorUtility.ActiveInspector.Preferences.graphics.clipboardInvalidOperation);
			}
			return new GUIContent(text);
		}

		private static void OnCutPasted()
		{
			lastOperationFailed = false;

			if(cutMemberInfo != null)
			{
				// TO DO: Handle removing member from array.
				var memberInfo = SerializableMemberInfo.Deserialize(cutMemberInfo);

				if(memberInfo == null)
				{
					#if DEV_MODE
					Debug.LogError("OnCutPasted failed to deserialized memberInfo");
					#endif
				}
				else if(memberInfo.Parent != null && memberInfo.Parent.IsCollection)
				{
					int index = memberInfo.CollectionIndex;
					var values = memberInfo.Parent.GetValues();
					for(int n = values.Length - 1; n >= 0; n--)
					{
						var value = values[n] as ICollection;
						if(value != null && value.Count > index)
						{
							try
							{
								CollectionExtensions.RemoveAt(ref value, index, true);
							}
							#if DEV_MODE
							catch(Exception e)
							{
								Debug.LogWarning(e);
							#else
							catch(Exception)
							{
							#endif
								lastOperationFailed = true;
								ClearCutData();
								return;
							}
						}
					}
					memberInfo.Parent.SetValues(values);
				}
				else if(memberInfo.CanWrite)
				{
					memberInfo.SetValue(memberInfo.DefaultValue());
				}
			}
			//if pasted target is not a field but a UnityObject
			//existing in the scene hierarchy, then destroy it
			else if(objectReference != null)
			{
				if(objectReference.IsSceneObject())
				{
					Platform.Active.Destroy(objectReference);
				}
			}

			ClearCutData();
		}

		private static void ClearCutData()
		{
			isCut = false;
			cutMemberInfo = null;
		}
	}
}
