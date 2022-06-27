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
                ByteArray = Encoding.ASCII.GetBytes(value.PadRight((int)field_length, ' '));
                dst_index = from_index;
                length = field_length;
            }

            public byte[] Apply(byte[] arr)
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
            fields.Add(new Field(value, from_index, field_length));
            return this;
        }

        protected virtual void AddPrefixSuffix() { }

        public byte[] Build()
        {
            foreach (Field field in fields)
            {
                field.Apply(ByteArray);
            }
            AddPrefixSuffix();
            return ByteArray;
        }


    }
    public class Telegram54 : TelegramBuilder
    {
        private byte[] template;

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
    }
}

