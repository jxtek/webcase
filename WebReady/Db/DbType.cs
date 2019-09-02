using System;

namespace WebReady.Db
{
    public class DbType : IKeyable<uint>
    {
        // system base types
        //

        static readonly Map<uint, DbType> BASE = new Map<uint, DbType>()
        {
            new DbType(16, "BOOL")
            {
                Converter = (name, src, snk) =>
                {
                    bool v = false;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(18, "CHAR")
            {
                Converter = (name, src, snk) =>
                {
                    char v = (char) 0;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(21, "SMALLINT")
            {
                Converter = (name, src, snk) =>
                {
                    short v = 0;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(23, "INT")
            {
                Converter = (name, src, snk) =>
                {
                    int v = 0;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(20, "BIGINT")
            {
                Converter = (name, src, snk) =>
                {
                    long v = 0;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(790, "MONEY")
            {
                Converter = (name, src, snk) =>
                {
                    decimal v = 0;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(700, "FLOAT")
            {
                Converter = (name, src, snk) =>
                {
                    float v = 0;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(701, "DOUBLE")
            {
                Converter = (name, src, snk) =>
                {
                    double v = 0;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1700, "NUMERIC")
            {
                Converter = (name, src, snk) =>
                {
                    decimal v = default;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1082, "DATE")
            {
                Converter = (name, src, snk) =>
                {
                    DateTime v = default;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1083, "TIME")
            {
                Converter = (name, src, snk) =>
                {
                    DateTime v = default;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1114, "TIMESTAMP")
            {
                Converter = (name, src, snk) =>
                {
                    DateTime v = default;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1184, "TIMESTAMPTZ")
            {
                Converter = (name, src, snk) =>
                {
                    DateTime v = default;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1266, "TIMETZ")
            {
                Converter = (name, src, snk) =>
                {
                    DateTime v = default;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(2950, "UUID")
            {
                Converter = (name, src, snk) =>
                {
                    Guid v = default;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1043, "VARCHAR")
            {
                Converter = (name, src, snk) =>
                {
                    string v = null;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(25, "TEXT")
            {
                Converter = (name, src, snk) =>
                {
                    string v = null;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(114, "JSON")
            {
                Converter = (name, src, snk) =>
                {
                    string v = null;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(142, "XML")
            {
                Converter = (name, src, snk) =>
                {
                    string v = null;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(3802, "JSONB")
            {
                Converter = (name, src, snk) =>
                {
                    string v = null;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(17, "BYTEA")
            {
                Converter = (name, src, snk) =>
                {
                    byte[] v = null;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1000, "BOOL[]")
            {
                Converter = (name, src, snk) =>
                {
                    bool[] v = null;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1002, "CHAR[]")
            {
                Converter = (name, src, snk) =>
                {
                    char[] v = null;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1005, "SMALLINT[]")
            {
                Converter = (name, src, snk) =>
                {
                    short v = 0;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1007, "INT[]")
            {
                Converter = (name, src, snk) =>
                {
                    int v = 0;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1016, "BIGINT[]")
            {
                Converter = (name, src, snk) =>
                {
                    long v = 0;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(791, "MONEY[]")
            {
                Converter = (name, src, snk) =>
                {
                    decimal v = 0;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1021, "FLOAT[]")
            {
                Converter = (name, src, snk) =>
                {
                    float[] v = null;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1022, "DOUBLE[]")
            {
                Converter = (name, src, snk) =>
                {
                    double[] v = null;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
            new DbType(1015, "VARCHAR[]")
            {
                Converter = (name, src, snk) =>
                {
                    string[] v = null;
                    src.Get(name, ref v);
                    snk.Put(name, v);
                }
            },
        };

        readonly uint oid;

        readonly string name;

        internal DbType(uint oid, string name)
        {
            this.oid = oid;
            this.name = name;
        }

        public Action<string, ISource, ISink> Converter { get; private set; }

        public uint Key => oid;

        public string Name => name;

        public static DbType GetBaseType(uint oid) => BASE.GetValue(oid);
    }
}