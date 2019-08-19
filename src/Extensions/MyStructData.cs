using System;
using System.Collections.Generic;

namespace SQLite.Extensions
{
    enum MySqlDataType : byte
    {
        // Manually extracted from mysql-5.5.23/include/mysql_com.h
        // some more info here: http://dev.mysql.com/doc/refman/5.5/en/c-api-prepared-statement-type-codes.html
        DECIMAL = 0x00, //exports.DECIMAL     = 0x00; // aka DECIMAL (http://dev.mysql.com/doc/refman/5.0/en/precision-math-decimal-changes.html)
        /// <summary>
        /// 1 byte (signed value) (-128 to 127), if unsigned => 0-255
        /// </summary>
        TINY,           //exports.TINY        = 0x01; // aka TINYINT, 1 byte
        /// <summary>
        /// 2 bytes
        /// </summary>
        SHORT,          //exports.SHORT       = 0x02; // aka SMALLINT, 2 bytes
        /// <summary>
        /// 4 bytes
        /// </summary>
        LONG,           //exports.LONG        = 0x03; // aka INT, 4 bytes
        /// <summary>
        /// 4-8 bytes
        /// </summary>
        FLOAT,          //exports.FLOAT       = 0x04; // aka FLOAT, 4-8 bytes
        /// <summary>
        /// 8 bytes
        /// </summary>
        DOUBLE,         //exports.DOUBLE      = 0x05; // aka DOUBLE, 8 bytes
        NULL,           //exports.NULL        = 0x06; // NULL (used for prepared statements, I think)
        TIMESTAMP,      //exports.TIMESTAMP   = 0x07; // aka TIMESTAMP
        /// <summary>
        /// 8 bytes
        /// </summary>
        LONGLONG,       //exports.LONGLONG    = 0x08; // aka BIGINT, 8 bytes
        /// <summary>
        /// 3 bytes
        /// </summary>
        INT24,          //exports.INT24       = 0x09; // aka MEDIUMINT, 3 bytes
        DATE,           //exports.DATE        = 0x0a; // aka DATE
        TIME,           //exports.TIME        = 0x0b; // aka TIME
        DATETIME,       //exports.DATETIME    = 0x0c; // aka DATETIME
        YEAR,           //exports.YEAR        = 0x0d; // aka YEAR, 1 byte (don't ask)
        NEWDATE,        //exports.NEWDATE     = 0x0e; // aka ?
        VARCHAR,        //exports.VARCHAR     = 0x0f; // aka VARCHAR (?)         
        BIT,            //exports.BIT         = 0x10; // aka BIT, 1-8 byte
        NEWDECIMAL = 0xf6,//exports.NEWDECIMAL= 0xf6; // aka DECIMAL
        ENUM,           //exports.ENUM        = 0xf7; // aka ENUM
        SET,            //exports.SET         = 0xf8; // aka SET
        TINY_BLOB,      //exports.TINY_BLOB   = 0xf9; // aka TINYBLOB, TINYTEXT
        MEDIUM_BLOB,    //exports.MEDIUM_BLOB = 0xfa; // aka MEDIUMBLOB, MEDIUMTEXT
        LONG_BLOB,      //exports.LONG_BLOB   = 0xfb; // aka LONGBLOG, LONGTEXT
        BLOB,           //exports.BLOB        = 0xfc; // aka BLOB, TEXT
        VAR_STRING,     //exports.VAR_STRING  = 0xfd; // aka VARCHAR, VARBINARY
        STRING,         //exports.STRING      = 0xfe; // aka CHAR, BINARY
        GEOMETRY        //exports.GEOMETRY    = 0xff; // aka GEOMETRY
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    struct MyStructData
    {
        [System.Runtime.InteropServices.FieldOffset(0)]
        public int myInt32;
        [System.Runtime.InteropServices.FieldOffset(0)]
        public uint myUInt32;
        //---------------------------------------------
        [System.Runtime.InteropServices.FieldOffset(0)]
        public long myInt64;
        [System.Runtime.InteropServices.FieldOffset(0)]
        public ulong myUInt64;
        //---------------------------------------------

        [System.Runtime.InteropServices.FieldOffset(0)]
        public double myDouble;
        //---------------------------------------------
        [System.Runtime.InteropServices.FieldOffset(0)]
        public decimal myDecimal;//16-bytes
        [System.Runtime.InteropServices.FieldOffset(0)]
        public DateTime myDateTime;
        //---------------------------------------------
        [System.Runtime.InteropServices.FieldOffset(16)]
        public byte[] myBuffer;
        [System.Runtime.InteropServices.FieldOffset(16)]
        public string myString;
        [System.Runtime.InteropServices.FieldOffset(24)]
        public MySqlDataType type; //1  byte


        public override string ToString()
        {
            switch (type)
            {
                case MySqlDataType.TIMESTAMP:
                case MySqlDataType.DATE:
                case MySqlDataType.DATETIME:
                case MySqlDataType.NEWDATE:
                    return myDateTime.ToString();
                case MySqlDataType.TINY:
                case MySqlDataType.SHORT:
                case MySqlDataType.LONG:
                case MySqlDataType.INT24:
                case MySqlDataType.YEAR:
                    return myInt32.ToString();
                case MySqlDataType.FLOAT:
                case MySqlDataType.DOUBLE:
                    return myDouble.ToString();
                case MySqlDataType.NEWDECIMAL:
                    return myDecimal.ToString();
                case MySqlDataType.LONGLONG:
                    return myInt64.ToString();
                case MySqlDataType.BIT:
                    return myBuffer.ToString();
                case MySqlDataType.STRING:
                case MySqlDataType.VAR_STRING:
                    return myString;
                case MySqlDataType.TINY_BLOB:
                case MySqlDataType.MEDIUM_BLOB:
                case MySqlDataType.LONG_BLOB:
                case MySqlDataType.BLOB:
                    return myBuffer.ToString();
                case MySqlDataType.GEOMETRY:
                default: return base.ToString();
            }
        }
    }


    public class CommandParams
    {
        Dictionary<string, MyStructData> _values = new Dictionary<string, MyStructData>(); //user bound values
        Dictionary<string, string> _sqlParts;//null at first, special  extension
        public CommandParams()
        {
        }
        public void AddWithValue(string key, string value)
        {
            var data = new MyStructData();
            if (value != null)
            {
                //replace some value
                value = value.Replace("\'", "\\\'");
                data.myString = value;
                data.type = MySqlDataType.VAR_STRING;
            }
            else
            {
                data.myString = null;
                data.type = MySqlDataType.NULL;
            }
            _values[key] = data;
        }
        public void AddWithValue(string key, byte value)
        {
            //TODO: review here
            var data = new MyStructData();
            data.myString = value.ToString();
            data.type = MySqlDataType.STRING;
            //data.myInt32 = value;
            //data.type = Types.TINY;
            _values[key] = data;
        }
        public void AddWithValue(string key, short value)
        {
            var data = new MyStructData();
            data.myInt32 = value;
            data.type = MySqlDataType.SHORT;
            _values[key] = data;
        }
        public void AddWithValue(string key, int value)
        {
            //INT 4       min        max
            //signed -2147483648 2147483647
            //unsigned     0     4294967295
            //---------------------------

            var data = new MyStructData();
            data.myInt32 = value;
            data.type = MySqlDataType.LONG;//Types.LONG = int32
            _values[key] = data;
        }
        public void AddWithValue(string key, long value)
        {
            var data = new MyStructData();
            data.myInt64 = value;
            data.type = MySqlDataType.LONGLONG;
            _values[key] = data;
        }
        public void AddWithValue(string key, float value)
        {
            var data = new MyStructData();
            data.myDouble = value;
            data.type = MySqlDataType.FLOAT;
            _values[key] = data;
        }
        public void AddWithValue(string key, double value)
        {
            var data = new MyStructData();
            data.myDouble = value;
            data.type = MySqlDataType.DOUBLE;
            _values[key] = data;
        }
        public void AddWithValue(string key, decimal value)
        {
            var data = new MyStructData();
            data.myString = value.ToString();
            data.type = MySqlDataType.STRING;
            _values[key] = data;
        }
        public void AddWithValue(string key, byte[] value)
        {
            var data = new MyStructData();
            if (value != null)
            {
                data.myBuffer = value;
                data.type = MySqlDataType.LONG_BLOB;
            }
            else
            {
                data.myBuffer = null;
                data.type = MySqlDataType.NULL;
            }
            _values[key] = data;
        }
        public void AddWithValue(string key, DateTime value)
        {
            var data = new MyStructData();
            data.myDateTime = value;
            data.type = MySqlDataType.DATETIME;
            _values[key] = data;
        }
        public void AddWithValue(string key, sbyte value)
        {
            //tiny int signed (-128 to 127)
            var data = new MyStructData();
            data.myInt32 = value;
            data.type = MySqlDataType.TINY;
            _values[key] = data;
        }
        public void AddWithValue(string key, char value)
        {
            //1 unicode char => 2 bytes store
            var data = new MyStructData();
            data.myUInt32 = value;
            data.type = MySqlDataType.LONGLONG; //TODO:?
            _values[key] = data;
        }
        public void AddWithValue(string key, ushort value)
        {
            //INT 2       min        max
            //signed      -32768    32767
            //unsigned     0     65535
            //---------------------------

            var data = new MyStructData();
            data.myString = value.ToString();
            data.type = MySqlDataType.STRING;
            //data.myUInt32 = value;
            //data.type = Types.SHORT;
            _values[key] = data;
        }
        public void AddWithValue(string key, uint value)
        {
            //INT 4       min        max
            //signed -2147483648 2147483647
            //unsigned     0     4294967295
            //---------------------------
            var data = new MyStructData();
            data.myUInt32 = value;
            data.type = MySqlDataType.LONGLONG;//** 
            _values[key] = data;
        }
        public void AddWithValue(string key, ulong value)
        {
            var data = new MyStructData();
            data.myString = value.ToString();
            data.type = MySqlDataType.STRING;
            //data.myUInt64 = value;
            //data.type = Types.LONGLONG;
            _values[key] = data;
        }
        //-------------------------------------------------------
        //user's bound data values 
        public void AddWithValue(string key, object value)
        {
            throw new NotSupportedException();

            //get type of value
            //switch (MySqlTypeConversionInfo.GetProperDataType(value))
            //{
            //    //switch proper type
            //    default:
            //    case ProperDataType.Unknown:
            //        throw new Exception("unknown data type?");
            //    case ProperDataType.Buffer:
            //        AddWithValue(key, (byte[])value);
            //        break;
            //    case ProperDataType.Bool:
            //        AddWithValue(key, (bool)value);
            //        break;
            //    case ProperDataType.Sbyte:
            //        AddWithValue(key, (sbyte)value);
            //        break;
            //    case ProperDataType.Char:
            //        AddWithValue(key, (char)value);
            //        break;
            //    case ProperDataType.Int16:
            //        AddWithValue(key, (short)value);
            //        break;
            //    case ProperDataType.UInt16:
            //        AddWithValue(key, (ushort)value);
            //        break;
            //    case ProperDataType.Int32:
            //        AddWithValue(key, (int)value);
            //        break;
            //    case ProperDataType.UInt32:
            //        AddWithValue(key, (uint)value);
            //        break;
            //    case ProperDataType.Int64:
            //        AddWithValue(key, (long)value);
            //        break;
            //    case ProperDataType.UInt64:
            //        AddWithValue(key, (ulong)value);
            //        break;
            //    case ProperDataType.DateTime:
            //        AddWithValue(key, (DateTime)value);
            //        break;
            //    case ProperDataType.Float32:
            //        AddWithValue(key, (float)value);
            //        break;
            //    case ProperDataType.Double64:
            //        AddWithValue(key, (double)value);
            //        break;
            //    case ProperDataType.Decimal:
            //        AddWithValue(key, (decimal)value);
            //        break;
            //}
        }

        internal bool TryGetData(string key, out MyStructData data)
        {
            return _values.TryGetValue(key, out data);
        }

        /// <summary>
        /// clear binding data value
        /// </summary>
        public void ClearDataValues()
        {
            _values.Clear();
        }
        /// <summary>
        /// clear binding data value
        /// </summary>
        public void Clear()
        {
            ClearDataValues();
        }

        //-------------------------------------------------------
        //sql parts : special extension 
        public void SetSqlPart(string sqlBoundKey, string sqlPart)
        {
            if (_sqlParts == null)
            {
                _sqlParts = new Dictionary<string, string>();
            }

            _sqlParts[sqlBoundKey] = "`" + sqlPart + "`";
        }
        public bool TryGetSqlPart(string sqlBoundKey, out string sqlPart)
        {
            if (_sqlParts == null)
            {
                sqlPart = null;
                return false;
            }

            return _sqlParts.TryGetValue(sqlBoundKey, out sqlPart);
        }
        public void ClearSqlParts()
        {
            if (_sqlParts != null)
            {
                _sqlParts.Clear();
            }
        }
        //-------------------------------------------------------


        public string[] GetAttachedValueKeys()
        {
            var keys = new string[_values.Count];
            int i = 0;
            foreach (string k in _values.Keys)
            {
                keys[i] = k;
                i++;
            }
            return keys;
        }
    }
}
