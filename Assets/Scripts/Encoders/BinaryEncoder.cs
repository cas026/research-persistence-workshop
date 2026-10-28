using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Encoders
{
    public class BinaryEncoder<T> : IEncoder<T>
    {
        private static FieldInfo[] GetFields(Type type)
        {
            return type
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsNotSerialized)
                .OrderBy(f => f.Name, StringComparer.Ordinal)
                .ToArray();
        }

        public byte[] Encode(T obj)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            WriteObject(writer, obj, typeof(T));

            return stream.ToArray();
        }

        public T Decode(byte[] encoded)
        {
            using var stream = new MemoryStream(encoded);
            using var reader = new BinaryReader(stream);

            return (T)ReadObject(reader, typeof(T));
        }

        private static void WriteObject(
            BinaryWriter writer, object obj, Type type)
        {
            foreach (var field in GetFields(type))
            {
                WriteValue(writer, field.FieldType, field.GetValue(obj));
            }
        }

        private static object ReadObject(
            BinaryReader reader, Type type)
        {
            object obj = Activator.CreateInstance(type);

            foreach (var field in GetFields(type))
            {
                field.SetValue(
                    obj,
                    ReadValue(reader, field.FieldType));
            }

            return obj;
        }

        private static void WriteValue(
            BinaryWriter writer, Type type, object value)
        {
            if (type == typeof(string))
                writer.Write((string)value ?? "");
            else if (type == typeof(int))
                writer.Write((int)value);
            else if (type == typeof(float))
                writer.Write((float)value);
            else if (type == typeof(double))
                writer.Write((double)value);
            else if (type == typeof(bool))
                writer.Write((bool)value);
            else if (type == typeof(long))
                writer.Write((long)value);
            else if (type == typeof(short))
                writer.Write((short)value);
            else if (type == typeof(byte))
                writer.Write((byte)value);
            else if (type.IsClass || type.IsValueType)
                WriteObject(writer, value, type);
            else
                throw new NotSupportedException(
                    $"Unsupported field type: {type}");
        }

        private static object ReadValue(
            BinaryReader reader, Type type)
        {
            if (type == typeof(string))
                return reader.ReadString();
            if (type == typeof(int))
                return reader.ReadInt32();
            if (type == typeof(float))
                return reader.ReadSingle();
            if (type == typeof(double))
                return reader.ReadDouble();
            if (type == typeof(bool))
                return reader.ReadBoolean();
            if (type == typeof(long))
                return reader.ReadInt64();
            if (type == typeof(short))
                return reader.ReadInt16();
            if (type == typeof(byte))
                return reader.ReadByte();

            if (type.IsClass || type.IsValueType)
                return ReadObject(reader, type);

            throw new NotSupportedException(
                $"Unsupported field type: {type}");
        }

        public string GetExtension() => ".bin";
    }
}