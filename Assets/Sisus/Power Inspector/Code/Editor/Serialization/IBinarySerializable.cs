using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Sisus
{
	public interface IBinarySerializable
	{
		int? DeserializationOrder { get; }
		byte[] Serialize(BinaryFormatter formatter);
		void DeserializeOverride(BinaryFormatter formatter, MemoryStream stream);
	}
}