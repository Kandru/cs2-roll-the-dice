using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils
{
    public enum SoundEventFieldType : byte
    {
        Bool = 0,
        Int32 = 1,
        UInt32 = 2,
        UInt64 = 3,
        Float = 4,
        Float3 = 5
    }

    public static class SoundEventUtils
    {
        public static uint GenerateSoundHash(string soundEventName)
        {
            return MurmurHash2(soundEventName, 0x53524332);
        }

        internal static uint MurmurHash2(string input, uint seed)
        {
            byte[] a2 = System.Text.Encoding.ASCII.GetBytes(input.ToLower());
            if (a2 == null)
            {
                return uint.MaxValue;
            }

            long v4 = a2.Length;
            uint v6 = (uint)(v4 ^ seed);
            int offset = 0;
            if (v4 >= 4)
            {
                uint v7 = (uint)(v4 >> 2);
                v4 -= 4 * v7;
                while (v7 > 0)
                {
                    uint v8 = 1540483477 * BitConverter.ToUInt32(a2, offset);
                    v6 = (1540483477 * (v8 ^ (v8 >> 24))) ^ (1540483477 * v6);
                    offset += 4;
                    v7--;
                }
            }
            int v9 = (int)v4 - 1;
            if (v9 != 0)
            {
                int v10 = v9 - 1;
                if (v10 != 0)
                {
                    if (v10 != 1)
                    {
                        v6 ^= v6 >> 13;
                        return (1540483477 * v6) ^ ((1540483477 * v6) >> 15);
                    }
                    v6 ^= (uint)(a2[offset + 2] << 16);
                }
                v6 ^= (uint)(a2[offset + 1] << 8);
            }
            v6 = 1540483477 * (v6 ^ a2[offset]);
            v6 ^= v6 >> 13;
            return (1540483477 * v6) ^ ((1540483477 * v6) >> 15);
        }
    }

    public abstract class SoundParamsBase<T> where T : SoundParamsBase<T>
    {
        protected const uint M_CONST = 1540483477;
        protected readonly List<(string name, byte type, byte[] data)> _parameters = [];
        protected abstract uint FieldNameHashSeed { get; }

        protected T AddParameter(string name, SoundEventFieldType fieldType, byte[] data)
        {
            _parameters.Add((name, (byte)fieldType, data));
            return (T)this;
        }

        public T WithBool(string name, bool value)
        {
            return AddParameter(name, SoundEventFieldType.Bool, BitConverter.GetBytes(value));
        }

        public T WithInt32(string name, int value)
        {
            return AddParameter(name, SoundEventFieldType.Int32, BitConverter.GetBytes(value));
        }

        public T WithUInt32(string name, uint value)
        {
            return AddParameter(name, SoundEventFieldType.UInt32, BitConverter.GetBytes(value));
        }

        public T WithUInt64(string name, ulong value)
        {
            return AddParameter(name, SoundEventFieldType.UInt64, BitConverter.GetBytes(value));
        }

        public T WithFloat(string name, float value)
        {
            return AddParameter(name, SoundEventFieldType.Float, BitConverter.GetBytes(value));
        }

        public T WithFloat3(string name, Vector value)
        {
            byte[] data = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(value.X), 0, data, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(value.Y), 0, data, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(value.Z), 0, data, 8, 4);
            return AddParameter(name, SoundEventFieldType.Float3, data);
        }

        public T Volume(float volume)
        {
            return WithFloat("volume", volume);
        }

        protected byte[] BuildPackedParams()
        {
            List<byte> result = [];

            foreach ((string? name, byte type, byte[]? data) in _parameters)
            {
                uint paramNameHash = SoundEventUtils.MurmurHash2(name, FieldNameHashSeed);
                result.AddRange(BitConverter.GetBytes(paramNameHash));
                result.Add(type);
                result.Add((byte)data.Length);
                result.Add(0);
                result.AddRange(data);
            }

            return [.. result];
        }
    }

    public class SoundParams : SoundParamsBase<SoundParams>
    {
        protected override uint FieldNameHashSeed => 0xB5D9F700;

        public uint Guid { get; set; }

        public RecipientFilter? Recipients { get; set; }

        public void Send()
        {
            UserMessage message = UserMessage.FromId(210);
            message.SetUInt("soundevent_guid", Guid);
            message.SetBytes("packed_params", BuildPackedParams());
            if (Recipients != null)
            {
                message.Recipients = Recipients;
            }

            message.Send();
        }
    }
}
