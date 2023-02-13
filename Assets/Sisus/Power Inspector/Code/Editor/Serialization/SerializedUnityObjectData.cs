using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus.Serialization
{
	[Serializable]
    public class SerializedUnityObjectData
    {
        [SerializeField, HideInInspector]
        private List<SerializedFieldData> serializedFields;

		public SerializedUnityObjectData() { }

		public SerializedUnityObjectData(Object target)
		{
			serializedFields = SerializedFieldData.Get(target);
		}
 
        public void Apply(Object target)
        {
			SerializedFieldData.Apply(serializedFields, target);
		}
    }
}