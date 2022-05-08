
public class ByteArray
{
    static public int GetInt32(byte[] array, int offset)
    {
        int value;

        value = (array[offset] << 24) + (array[offset + 1] << 16) + (array[offset + 2] << 8) + array[offset + 3];

        return value;
    }

    static public int GetInt16(byte[] array, int offset)
    {
        int value;

        value = (array[offset] << 8) + array[offset + 1];

        return value;
    }

    static public byte GetByte(byte[] array, int offset)
    {
        return array[offset];
    }
}
