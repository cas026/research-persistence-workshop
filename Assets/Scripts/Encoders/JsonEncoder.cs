using System.Text;
using UnityEngine;

namespace Encoders
{
    public class JsonEncoder<T> : IEncoder<T>
    {
        public byte[] Encode(T obj)
        {
            var json = JsonUtility.ToJson(obj);
            return Encoding.UTF8.GetBytes(json);
        }

        public T Decode(byte[] encoded)
        {
            var json = Encoding.UTF8.GetString(encoded);
            return JsonUtility.FromJson<T>(json);
        }

        public string GetExtension() => ".json";
    }
}