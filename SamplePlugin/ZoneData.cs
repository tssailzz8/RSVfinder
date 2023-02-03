using System.Collections.Generic;
using System.Text;

namespace RSVfinder
{
    public class RSV
    {
        public byte[] Key = new byte[30];
        public byte[] Value = new byte[0x404];
        public int Size = 0;

        public RSV(string key, string value, int size)
        {
            Key = Encoding.UTF8.GetBytes(key);
            Value = Encoding.UTF8.GetBytes(value);
            Size = size;
        }
    }

    public class RSF
    {
        public ulong ID = 0;
        public byte[] Data = new byte[64];

        public RSF(ulong id, byte[] data)
        {
            ID = id;
            Data = data;
        }
    }

    public class ZoneData
    {
        public List<RSV> RSVs = new();
        public List<RSF> RSFs = new();
    }
}
