using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Sisus
{
	public static class CollectionExtensions
	{
		public static void RemoveAt([NotNull]ref ICollection collection, int index, bool throwExceptionIfFails)
		{
			int count;
			try
			{
				count = collection.Count;
			}
			catch(Exception)
			{
				if(throwExceptionIfFails)
				{
					throw;
				}
				return;
			}
			
			if(count <= index)
			{
				if(throwExceptionIfFails)
				{
					throw new IndexOutOfRangeException("Index "+index+" was >= than collection.Count "+count);
				}
				return;
			}

			var array = collection as Array;
			if(array != null)
			{
				try
				{
					ArrayExtensions.RemoveAt(ref array, index);
				}
				catch(Exception)
				{
					if(throwExceptionIfFails)
					{
						throw;
					}
				}
				collection = array;
				return;
			}

			var list = collection as IList;
			if(list != null)
			{
				try
				{
					list.RemoveAt(index);
				}
				catch(Exception)
				{
					if(throwExceptionIfFails)
					{
						throw;
					}
				}
				return;
			}
			
			var dictionary = collection as IDictionary;
			if(dictionary != null)
			{
				dictionary.RemoveAt(index);
				return;
			}

			var removeAtMethod = collection.GetType().GetMethod("RemoveAt");
			if(removeAtMethod != null)
			{
				var parameters = removeAtMethod.GetParameters();
				if(parameters.Length == 1)
				{
					var parameter = parameters[0];
					if(parameter.ParameterType == Types.Int)
					{
						if(removeAtMethod.ReturnType == null || !typeof(ICollection).IsAssignableFrom(removeAtMethod.ReturnType))
						//if(removeAtMethod.ReturnType == Types.Void || removeAtMethod.ReturnType == Types.Bool)
						{
							try
							{
								removeAtMethod.Invoke(collection, ArrayExtensions.TempObjectArray(index));
							}
							catch(Exception)
							{
								if(throwExceptionIfFails)
								{
									throw;
								}
							}
						}
						else //if(typeof(ICollection).IsAssignableFrom(removeAtMethod.ReturnType))
						{
							try
							{
								collection = (ICollection)removeAtMethod.Invoke(collection, ArrayExtensions.TempObjectArray(index));
							}
							catch(Exception)
							{
								if(throwExceptionIfFails)
								{
									throw;
								}
							}
						}
						return;
					}
				}
			}

			if(throwExceptionIfFails)
			{
				throw new InvalidOperationException("Unable to remove item at index "+index+" from collection of type "+collection.GetType().Name+".");
			}
			return;
		}

		public static void RemoveAt(this IDictionary dictionary, int index)
		{
			int current = 0;
			foreach(DictionaryEntry dictionaryEntry in dictionary)
			{
				if(current == index)
				{
					dictionary.Remove(dictionaryEntry.Key);
					return;
				}
				current++;
			}

			throw new IndexOutOfRangeException("RemoveAt index " + index + " out of bounds with dictionary.Count " + dictionary.Count);
		}
	}
}