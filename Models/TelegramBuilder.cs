using System;
using System.Text;


namespace CacheService.Models
{
    public class TelegramBuilder
    {
        public class Field
        {
            protected byte[] ByteArray { get; set; }
            public uint dst_index { get; set; } = uint.MinValue;
            public uint length { get; set; } = uint.MinValue;

            public Field(string value, uint from_index, uint field_length)
            {
                if (value.Length < field_length)
                {
                    value = value.Remove((int)field_length);
                }

                ByteArray = Encoding.ASCII.GetBytes(value.PadRight((int)field_length, ' '));
                dst_index = from_index;
                length = field_length;
            }

            public byte[]? ApplyTo(byte[] arr)
            {
                if (arr == null) return arr;
                if (dst_index > arr.Length) return arr;
                if (dst_index + length > arr.Length) length = (uint)arr.Length - dst_index;


                Array.Copy(ByteArray, 0, arr, dst_index, length);
                return arr;
            }
            override public string ToString()
            {
                return Encoding.ASCII.GetString(ByteArray);
            }
        }

        protected byte[] ByteArray { get; set; } = new byte[54];
        public Dictionary<string, Field> Fields = new();

        public TelegramBuilder()
        {

        }

        public TelegramBuilder AddField(string name, string value, uint from_index, uint field_length)
        {
            if (string.IsNullOrEmpty(value)) return this;
            Fields.Add(name, new Field(value, from_index, field_length));
            return this;
        }

        protected virtual void AddPrefixSuffix() { }

        public TelegramBuilder Build()
        {
            foreach (var field in Fields)
                field.Value.ApplyTo(ByteArray);

            AddPrefixSuffix();
            return this;
        }

        public byte[] GetBytes()
        {
            return ByteArray;
        }

        public string GetString()
        {
            return Encoding.ASCII.GetString(ByteArray);
        }

        public override string ToString()
        {
            return GetString();
        }
    }
    public class Telegram54 : TelegramBuilder
    {
        private TelegramType _type = TelegramType.UNKNOWN;
        //public TelegramType Type { get {return _type;} }
        private readonly Telegram54? _parent;
        public Telegram54? Parent { get { return _parent; } }
        public enum TelegramType : short
        {
            UNKNOWN,
            SCN,
            CMD,
            SOK,
            SER,
            WDG,
            WDGA
        }
        public static readonly Dictionary<TelegramType, string> TelegramTypeStrings = new()
        {
            [TelegramType.UNKNOWN] = "    ",
            [TelegramType.SCN] = "SCN ",
            [TelegramType.CMD] = "CMD ",
            [TelegramType.SOK] = "SOK ",
            [TelegramType.SER] = "SER ",
            [TelegramType.WDG] = "WDG ",
            [TelegramType.WDGA] = "WDGA"
        };

        public Telegram54()
        {
            CreateTemplate();
        }
        public Telegram54(Telegram54 parent)
        {
            this._parent = parent;
            CreateTemplate();
        }

        public void CreateTemplate()
        {
            if (_parent is not null) { ByteArray = _parent.ByteArray; return; }

            ByteArray = new byte[54];
            Array.Fill(ByteArray, (byte)' ');
        }


        public static readonly byte prefix = (byte)'<';
        public static readonly byte suffix = (byte)'>';
        override protected void AddPrefixSuffix()
        {
            ByteArray[0] = prefix;
            ByteArray[53] = suffix;
        }

        public Telegram54 Node(string value)
        {
            AddField("node", value.PadLeft(4, '0'), 1, 4);
            return this;
        }

        public Telegram54 Type(TelegramType value)
        {
            _type = value;
            return Type(TelegramTypeStrings[_type]);
        }

        public Telegram54 Type(string value)
        {
            AddField("type", value.PadRight(4, ' '), 5, 4);
            return this;
        }

        public Telegram54 SequenceNo(int value)
        {
            AddField("sequenceNo", value.ToString().PadLeft(3, '0'), 9, 3);
            return this;
        }

        public Telegram54 Addr1(string value)
        {
            AddField("addr1", value.PadRight(4, ' '), 12, 4);
            return this;
        }

        public Telegram54 Addr2(string value)
        {
            AddField("addr2", value.PadRight(4, ' '), 16, 4);
            return this;
        }

        public Telegram54 Barcode(string value)
        {
            AddField("barcode", value.PadRight(32, ' '), 20, 32);
            return this;
        }
        
        public static Telegram54? Parse(string telegram)
        {
            if (telegram.Length < 54) return null;
            if (telegram[0] != prefix) return null;
            if (telegram[53] != suffix) return null;
            
            var t = new Telegram54();
            t.Type(TelegramType.WDG);
            t.ByteArray = Encoding.ASCII.GetBytes(telegram);
            return t;
        }
    }
}

