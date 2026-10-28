public interface IEncoder<T>
{
    byte[] Encode(T obj);
    T Decode (byte[] encoded);
    string GetExtension();
}