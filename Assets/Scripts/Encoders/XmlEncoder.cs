using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Encoders
{
    public class XmlEncoder<T> : IEncoder<T>
    {
        public byte[] Encode(T obj)
        {
            var serializer = new XmlSerializer(typeof(T));

            using var stream = new MemoryStream();
            serializer.Serialize(stream, obj);
            return stream.ToArray();
        }

        public T Decode(byte[] encoded)
        {
            var serializer = new XmlSerializer(typeof(T));

            using var stream = new MemoryStream(encoded);
            return (T)serializer.Deserialize(stream);
        }

        public string GetExtension()
        {
            return ".xml";
        }
    }
}