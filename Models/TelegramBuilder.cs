using System;
using System.Text;


namespace CacheService.Models;

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

public enum Type54 : short
{
    INVALID = -1,
    UNKNOWN = 0,
    SCN,
    CMD,
    SOK,
    SER,
    WDG,
    WDGA
}

public class Telegram54 : TelegramBuilder
{
    private Type54? _type;
    private string? _node;
    private int? _id;
    private string? _addr1;
    private string? _addr2;
    private string? _barcode;
    private string? _spare;
    public string NodeValue
    {
        get
        {
            if (_node is null)
            {
                _node = Parse.GetNode(this) ?? string.Empty;
            }
            return _node;
        }
    }
    public Type54? TypeValue
    {
        get
        {
            if (_type is null)
            {
                _type = Parse.GetTelegramTypeEnum(this);
            }
            return _type;
        }
    }
    public int SequenceNoValue
    {
        get
        {
            if (_id is null)
            {
                _id = Parse.GetSequenceNo(this);
            }
            return _id ?? 0;
        }
    }

    public string Addr1Value
    {
        get
        {
            if (_addr1 is null)
            {
                _addr1 = Parse.GetAddr1(this) ?? string.Empty;
            }
            return _addr1;
        }
    }

    public string Addr2Value
    {
        get
        {
            if (_addr2 is null)
            {
                _addr2 = Parse.GetAddr2(this) ?? string.Empty;
            }
            return _addr2;
        }
    }

    private readonly Telegram54? _parent;
    public Telegram54? Parent { get { return _parent; } }

    //public static readonly Dictionary<Type54, string> TelegramTypeStrings = new()
    //{
    //    [Type54.UNKNOWN] = "    ",
    //    [Type54.SCN] = "SCN ",
    //    [Type54.CMD] = "CMD ",
    //    [Type54.SOK] = "SOK ",
    //    [Type54.SER] = "SER ",
    //    [Type54.WDG] = "WDG ",
    //    [Type54.WDGA] = "WDGA"
    //};

    public Telegram54()
    {
        CreateTemplate();
    }
    public Telegram54(Telegram54 parent)
    {
        _parent = parent;
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

    public Telegram54 Type(Type54 value)
    {
        _type = value;
        return Type(Parse.TypeToStringDict[(Type54)_type]);
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

    public static class Parse
    {

        public static Telegram54? New(string telegram)
        {
            if (telegram.Length < 54) return null;
            if (telegram[0] != prefix) return null;
            if (telegram[53] != suffix) return null;

            var t = new Telegram54();

            t.ByteArray = Encoding.ASCII.GetBytes(telegram);
            return t;
        }

        public static Telegram54? New(byte[] telegram)
        {
            if (telegram.Length < 54) return null;
            if (telegram[0] != prefix) return null;
            if (telegram[53] != suffix) return null;

            var t = new Telegram54();

            t.ByteArray = telegram;
            return t;
        }


        public static readonly Dictionary<Type54, string> TypeToStringDict = new()
        {
            [Type54.UNKNOWN] = "    ",
            [Type54.SCN] = "SCN ",
            [Type54.CMD] = "CMD ",
            [Type54.SOK] = "SOK ",
            [Type54.SER] = "SER ",
            [Type54.WDG] = "WDG ",
            [Type54.WDGA] = "WDGA"
        };

        public static readonly Dictionary<string, Type54> StringToTypeDict = new()
        {
            ["    "] = Type54.UNKNOWN,
            ["SCN "] = Type54.SCN,
            ["CMD "] = Type54.CMD,
            ["SOK "] = Type54.SOK,
            ["SER "] = Type54.SER,
            ["WDG "] = Type54.WDG,
            ["WDGA"] = Type54.WDGA
        };

        public static string? GetTelegramTypeString(Type54 type)
        {
            if (!TypeToStringDict.ContainsKey(type)) return null;
            return TypeToStringDict[type];
        }

        public static Type54 GetTelegramTypeEnum(string typeString)
        {
            if (StringToTypeDict.ContainsKey(typeString)) return Type54.INVALID;
            return StringToTypeDict[typeString];
        }

        public static Type54 GetTelegramTypeEnum(Telegram54 telegram)
        {
            return GetTelegramTypeEnum(telegram.GetString().Substring(5, 4));
        }

        public static string? GetNode(Telegram54 t)
        {
            if (t is null) return null;
            return t.GetString().Substring(1, 4);
        }

        public static int? GetSequenceNo(Telegram54 t)
        {
            try
            {
                return int.Parse(t.GetString().Substring(9, 3));
            }
            catch
            {
                return null;
            }
        }

        public static string? GetAddr1(Telegram54 t)
        {
            if (t is null) return null;
            return t.GetString().Substring(12, 4);
        }

        public static string? GetAddr2(Telegram54 t)
        {
            if (t is null) return null;
            return t.GetString().Substring(16, 4);
        }

        public static string? GetBarcode(Telegram54 t)
        {
            if (t is null) return null;
            return t.GetString().Substring(20, 32).TrimEnd();
        }

        public static string? GetSpare(Telegram54 t)
        {
            if (t is null) return null;
            return t.GetString().Substring(53, 1);
        }

    }
}

