using System;
using System.Text;


namespace CacheService.Models
{
    public class TelegramBuilder
    {
        private class Field
        {
            protected byte[] ByteArray { get; set; }
            public uint dst_index { get; set; } = uint.MinValue;
            public uint length { get; set; } = uint.MinValue;

            public Field(string value, uint from_index, uint field_length)
            {
                if (value.Length < field_length)
                {
                    value = value.Remove((int) field_length);
                } 

                ByteArray = Encoding.ASCII.GetBytes(value.PadRight((int)field_length, ' '));
                dst_index = from_index;
                length = field_length;
            }

            public byte[]? Apply(byte[] arr)
            {
                if (arr == null) return arr;
                if (dst_index > arr.Length) return arr;
                if (dst_index + length > arr.Length) length = (uint) arr.Length - dst_index;


                Array.Copy(ByteArray, 0, arr, dst_index, length);
                return arr;
            }

        }

        protected byte[] ByteArray { get; set; } = new byte[54];
        private List<Field> fields = new List<Field>();

        public TelegramBuilder()
        {

        }

        public TelegramBuilder AddField(string value, uint from_index, uint field_length)
        {
            if (string.IsNullOrEmpty(value)) return this;
            fields.Add(new Field(value, from_index, field_length));
            return this;
        }

        protected virtual void AddPrefixSuffix() { }

        public TelegramBuilder Build()
        {
            for (int i = 0; i < fields.Count; i++) fields[i].Apply(ByteArray);
            
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
        private byte[] template;
        private string node = String.Empty;
        
        public enum TelegramType : short
        {
            SCN,
            CMD,
            SOK,
            SER
        }

        public Telegram54()
        {
            template = new byte[54];
            Array.Fill(template, (byte)' ');
            ByteArray = template;
        }

        override protected void AddPrefixSuffix()
        {
            ByteArray[0] = (byte)'<';
            ByteArray[53] = (byte)'>';
        }

        public Telegram54 Node(string value)
        {
            AddField(value.PadLeft(4, '0'), 1, 4);
            return this;
        }

        public Telegram54 Type(string value)
        {
            AddField(value.PadRight(4, ' '), 5, 4);
            return this;
        }

        public Telegram54 SequenceNo(int value)
        {
            AddField(value.ToString().PadLeft(3, '0'), 9, 3);
            return this;
        }

        public Telegram54 Addr1(string value)
        {
            AddField(value.PadRight(4, ' ') , 12, 4);
            return this;
        }

        public Telegram54 Addr2(string value)
        {
            AddField(value.PadRight(4, ' '), 16, 4);
            return this;
        }

        public Telegram54 Barcode(string value)
        {
            AddField(value.PadRight(32, ' '), 20, 32);
            return this;
        }
    }
}

