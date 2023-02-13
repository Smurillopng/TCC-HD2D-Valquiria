using System;

namespace Sisus
{
	/// <summary>
	/// A class where only a single instance is allowed to be created.
	/// 
	/// The Instance() method can be used to return an instance of the class.
	/// If the instance doesn't yet exist, one will be created.
	/// </summary>
	/// <typeparam name="T"> Type of the class inheriting from this Singleton class. </typeparam>
	public abstract class Singleton<T> where T : Singleton<T>, new()
	{
		public static T instance;

		protected Singleton()
		{
			if(instance == null)
			{
				instance = this as T;
			}
			else if(instance != this)
			{
				throw new InvalidOperationException("Singleton "+StringUtils.ToString(typeof(T))+" constructor was called but another instance already existed!");
			}
		}

		public static T Instance()
		{
			if(instance == null)
			{
				instance = new T();
			}
			return instance;
		}

		public static bool InstanceExists()
		{
			return instance != null;
		}
	}
}