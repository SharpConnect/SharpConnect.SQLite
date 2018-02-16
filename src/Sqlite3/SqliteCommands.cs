
// Copyright (c) 2009-2015 Krueger Systems, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
#define NO_CONCURRENT

#if WINDOWS_PHONE && !USE_WP8_NATIVE_SQLITE
#define USE_CSHARP_SQLITE
#endif

#if NETFX_CORE
#define USE_NEW_REFLECTION_API
#endif

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

#if !USE_SQLITEPCL_RAW
using System.Runtime.InteropServices;
#endif


#if NO_CONCURRENT
using ConcurrentStringDictionary = System.Collections.Generic.Dictionary<string, object>;
//using SQLite.Extensions;
#else
using ConcurrentStringDictionary = System.Collections.Concurrent.ConcurrentDictionary<string, object>;
#endif


#if USE_CSHARP_SQLITE
using Sqlite3 = Community.CsharpSqlite.Sqlite3;
using Sqlite3DatabaseHandle = Community.CsharpSqlite.Sqlite3.sqlite3;
using Sqlite3Statement = Community.CsharpSqlite.Sqlite3.Vdbe;
#elif USE_WP8_NATIVE_SQLITE
using Sqlite3 = Sqlite.Sqlite3;
using Sqlite3DatabaseHandle = Sqlite.Database;
using Sqlite3Statement = Sqlite.Statement;
#elif USE_SQLITEPCL_RAW
using Sqlite3DatabaseHandle = SQLitePCL.sqlite3;
using Sqlite3Statement = SQLitePCL.sqlite3_stmt;
using Sqlite3 = SQLitePCL.raw;
#else
using Sqlite3DatabaseHandle = System.IntPtr;
using Sqlite3Statement = System.IntPtr;
#endif



namespace SQLite
{


    public partial class SQLiteCommand
    {
        SQLiteConnection _conn;
        private List<Binding> _bindings;
        public string CommandText { get; set; }
        public SQLiteCommand(SQLiteConnection conn)
        {
            _conn = conn;
            _bindings = new List<Binding>();
            CommandText = "";
        }
        public SQLiteCommand(string sqlCmd, SQLiteConnection conn)
        {
            _conn = conn;
            _bindings = new List<Binding>();
            CommandText = sqlCmd;
        }
        public int ExecuteNonQuery()
        {
            if (_conn.Trace)
            {
                Debug.WriteLine("Executing: " + this);
            }

            var r = SQLite3.Result.OK;
            var stmt = Prepare();
            r = SQLite3.Step(stmt);
            Finalize(stmt);
            if (r == SQLite3.Result.Done)
            {
                int rowsAffected = SQLite3.Changes(_conn.Handle);
                return rowsAffected;
            }
            else if (r == SQLite3.Result.Error)
            {
                string msg = SQLite3.GetErrmsg(_conn.Handle);
                throw SQLiteException.New(r, msg);
            }
            else if (r == SQLite3.Result.Constraint)
            {
                if (SQLite3.ExtendedErrCode(_conn.Handle) == SQLite3.ExtendedResult.ConstraintNotNull)
                {
                    throw new NotNullConstraintViolationException(r, SQLite3.GetErrmsg(_conn.Handle));
                }
            }

            throw SQLiteException.New(r, r.ToString());
        }

        public SQLiteDataReader ExecuteReader()
        {
            return new SQLiteDataReader(_conn, Prepare());
        }
        //public IEnumerable<object> ExecuteDeferredQuery()
        //{
        //    if (_conn.Trace)
        //    {
        //        Debug.WriteLine("Executing Query: " + this);
        //    }

        //    var stmt = Prepare();
        //    try
        //    {

        //        int colCount = SQLite3.ColumnCount(stmt);

        //        //for (int i = 0; i < cols.Length; i++)
        //        //{
        //        //    var name = SQLite3.ColumnName16(stmt, i);
        //        //    cols[i] = map.FindColumn(name);
        //        //}
        //        object[] reusableRow = new object[colCount];

        //        while (SQLite3.Step(stmt) == SQLite3.Result.Row)
        //        {
        //            //var obj = Activator.CreateInstance(map.MappedType);
        //            for (int i = 0; i < colCount; i++)
        //            {
        //                //if (cols[i] == null)
        //                //    continue;
        //                SQLite3.ColType colType = SQLite3.ColumnType(stmt, i);

        //                //read column as specific
        //                //***
        //                var val = ReadCol(stmt, i, colType, typeof(byte[]));
        //                reusableRow[i] = val;
        //                //cols[i].SetValue(obj, val);
        //            }
        //            //OnInstanceCreated(obj);
        //            yield return reusableRow;
        //        }
        //    }
        //    finally
        //    {
        //        SQLite3.Finalize(stmt);
        //    }
        //}

        //public T ExecuteScalar<T>()
        //{
        //    if (_conn.Trace)
        //    {
        //        Debug.WriteLine("Executing Query: " + this);
        //    }

        //    T val = default(T);

        //    var stmt = Prepare();

        //    try
        //    {
        //        var r = SQLite3.Step(stmt);
        //        if (r == SQLite3.Result.Row)
        //        {
        //            var colType = SQLite3.ColumnType(stmt, 0);
        //            val = (T)ReadCol(stmt, 0, colType, typeof(T));
        //        }
        //        else if (r == SQLite3.Result.Done)
        //        {
        //        }
        //        else
        //        {
        //            throw SQLiteException.New(r, SQLite3.GetErrmsg(_conn.Handle));
        //        }
        //    }
        //    finally
        //    {
        //        Finalize(stmt);
        //    }

        //    return val;
        //}

        public void Bind(string name, object val)
        {
            _bindings.Add(new Binding
            {
                Name = name,
                Value = val
            });
        }

        public void Bind(object val)
        {
            Bind(null, val);
        }

        public override string ToString()
        {
            var parts = new string[1 + _bindings.Count];
            parts[0] = CommandText;
            var i = 1;
            foreach (var b in _bindings)
            {
                parts[i] = string.Format("  {0}: {1}", i - 1, b.Value);
                i++;
            }
            return string.Join(Environment.NewLine, parts);
        }

        Sqlite3Statement Prepare()
        {
            var stmt = SQLite3.Prepare2(_conn.Handle, CommandText);
            BindAll(stmt);
            return stmt;
        }

        void Finalize(Sqlite3Statement stmt)
        {
            SQLite3.Finalize(stmt);
        }

        void BindAll(Sqlite3Statement stmt)
        {
            int nextIdx = 1;
            foreach (var b in _bindings)
            {
                if (b.Name != null)
                {
                    b.Index = SQLite3.BindParameterIndex(stmt, b.Name);
                }
                else
                {
                    b.Index = nextIdx++;
                }

                BindParameter(stmt, b.Index, b.Value, _conn.StoreDateTimeAsTicks);
            }
        }

        static IntPtr NegativePointer = new IntPtr(-1);

        internal static void BindParameter(Sqlite3Statement stmt, int index, object value, bool storeDateTimeAsTicks)
        {
            if (value == null)
            {
                SQLite3.BindNull(stmt, index);
            }
            else
            {
                if (value is Int32)
                {
                    SQLite3.BindInt(stmt, index, (int)value);
                }
                else if (value is String)
                {
                    SQLite3.BindText(stmt, index, (string)value, -1, NegativePointer);
                }
                else if (value is Byte || value is UInt16 || value is SByte || value is Int16)
                {
                    SQLite3.BindInt(stmt, index, Convert.ToInt32(value));
                }
                else if (value is Boolean)
                {
                    SQLite3.BindInt(stmt, index, (bool)value ? 1 : 0);
                }
                else if (value is UInt32 || value is Int64)
                {
                    SQLite3.BindInt64(stmt, index, Convert.ToInt64(value));
                }
                else if (value is Single || value is Double || value is Decimal)
                {
                    SQLite3.BindDouble(stmt, index, Convert.ToDouble(value));
                }
                else if (value is TimeSpan)
                {
                    SQLite3.BindInt64(stmt, index, ((TimeSpan)value).Ticks);
                }
                else if (value is DateTime)
                {
                    if (storeDateTimeAsTicks)
                    {
                        SQLite3.BindInt64(stmt, index, ((DateTime)value).Ticks);
                    }
                    else
                    {
                        SQLite3.BindText(stmt, index, ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss"), -1, NegativePointer);
                    }
                }
                else if (value is DateTimeOffset)
                {
                    SQLite3.BindInt64(stmt, index, ((DateTimeOffset)value).UtcTicks);
#if !USE_NEW_REFLECTION_API
                }
                else if (value.GetType().IsEnum)
                {
#else
				} else if (value.GetType().GetTypeInfo().IsEnum) {
#endif
                    SQLite3.BindInt(stmt, index, Convert.ToInt32(value));
                }
                else if (value is byte[])
                {
                    SQLite3.BindBlob(stmt, index, (byte[])value, ((byte[])value).Length, NegativePointer);
                }
                else if (value is Guid)
                {
                    SQLite3.BindText(stmt, index, ((Guid)value).ToString(), 72, NegativePointer);
                }
                else
                {
                    throw new NotSupportedException("Cannot store type: " + value.GetType());
                }
            }
        }

        class Binding
        {
            public string Name { get; set; }

            public object Value { get; set; }

            public int Index { get; set; }
        }


    }




    /// <summary>
    /// Since the insert never changed, we only need to prepare once.
    /// </summary>
    public class PreparedSqlLiteInsertCommand : IDisposable
    {
        public bool Initialized { get; private set; }

        SQLiteConnection Connection { get; set; }

        public string CommandText { get; set; }

        Sqlite3Statement Statement { get; set; }
        internal static readonly Sqlite3Statement NullStatement = default(Sqlite3Statement);

        public PreparedSqlLiteInsertCommand(SQLiteConnection conn)
        {
            Connection = conn;
        }
        public PreparedSqlLiteInsertCommand(string sqlCmd, SQLiteConnection conn)
        {
            this.CommandText = sqlCmd;
            Connection = conn;

        }
        public int ExecuteNonQuery(object[] source)
        {
            if (Connection.Trace)
            {
                Debug.WriteLine("Executing: " + CommandText);
            }

            if (!Initialized)
            {
                Statement = Prepare();
                Initialized = true;
            }

            //bind the values.
            if (source != null)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    SQLiteCommand.BindParameter(Statement, i + 1, source[i], Connection.StoreDateTimeAsTicks);
                }
            }

            SQLite3.Result r = SQLite3.Step(Statement);

            switch (r)
            {
                case SQLite3.Result.Done:
                    {
                        int rowsAffected = SQLite3.Changes(Connection.Handle);
                        SQLite3.Reset(Statement);
                        return rowsAffected;
                    }
                case SQLite3.Result.Error:
                    {
                        string msg = SQLite3.GetErrmsg(Connection.Handle);
                        SQLite3.Reset(Statement);
                        throw SQLiteException.New(r, msg);
                    }
                default:
                    {
                        if (r == SQLite3.Result.Constraint &&
                            SQLite3.ExtendedErrCode(Connection.Handle) == SQLite3.ExtendedResult.ConstraintNotNull)
                        {
                            string msg = SQLite3.GetErrmsg(Connection.Handle);
                            SQLite3.Reset(Statement);
                            throw SQLiteException.New(r, msg);
                        }
                        else
                        {
                            SQLite3.Reset(Statement);
                            throw SQLiteException.New(r, r.ToString());
                        }
                    }
            }
        }

        protected virtual Sqlite3Statement Prepare()
        {
            var stmt = SQLite3.Prepare2(Connection.Handle, CommandText);
            return stmt;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (Statement != NullStatement)
            {
                try
                {
                    SQLite3.Finalize(Statement);
                }
                finally
                {
                    Statement = NullStatement;
                    Connection = null;
                }
            }
        }

        ~PreparedSqlLiteInsertCommand()
        {
            Dispose(false);
        }
    }


    public class SQLiteDataReader : IDisposable
    {
        Sqlite3DatabaseHandle stmt;
        int colCount;
        SQLite3.ColType[] colTypes;
        string[] colNames;
        SQLiteConnection _conn;

        bool _isClosed;
        internal SQLiteDataReader(SQLiteConnection conn, Sqlite3DatabaseHandle stmt)
        {
            this._conn = conn;
            this.stmt = stmt;
            colCount = SQLite3.ColumnCount(stmt);
            colTypes = new SQLite3.ColType[colCount];
            colNames = new string[colCount];

            for (int i = 0; i < colCount; i++)
            {
                colTypes[i] = SQLite3.ColumnType(stmt, i);
                colNames[i] = SQLite3.ColumnName16(stmt, i);
            }

            //while (SQLite3.Step(stmt) == SQLite3.Result.Row)
            //{
            //    //var obj = Activator.CreateInstance(map.MappedType);
            //    for (int i = 0; i < colCount; i++)
            //    {
            //        //if (cols[i] == null)
            //        //    continue;
            //        //SQLite3.ColType colType = SQLite3.ColumnType(stmt, i);
            //        //read column as specific
            //        //***
            //        var val = ReadCol(stmt, i, colType, typeof(byte[]));
            //        reusableRow[i] = val;
            //        //cols[i].SetValue(obj, val);
            //    }
            //    //OnInstanceCreated(obj);
            //    yield return reusableRow;
            //}
        }

        public void Dispose()
        {
            if (!_isClosed)
            {
                Close();
                _isClosed = true;
            }

        }
        public void Close()
        {
            SQLite3.Finalize(stmt);
        }

        public bool Read()
        {
            return SQLite3.Step(stmt) == SQLite3.Result.Row;
        }
        public string GetString(int index)
        {
            //get data as string
            return SQLite3.ColumnString(stmt, index);
        }
        public int GetInt32(int index)
        {
            //get data as string
            return (int)SQLite3.ColumnInt(stmt, index);
        }
        public bool GetBool(int index)
        {
            return SQLite3.ColumnInt(stmt, index) == 1;
        }
        public double GetDouble(int index)
        {
            return SQLite3.ColumnDouble(stmt, index);
        }
        public float GetFloat(int index)
        {
            return (float)SQLite3.ColumnDouble(stmt, index);
        }
        public TimeSpan GetTimeSpan(int index)
        {
            return new TimeSpan(SQLite3.ColumnInt64(stmt, index));
        }

        public DateTime GetDateTime(int index)
        {
            if (_conn.StoreDateTimeAsTicks)
            {
                return new DateTime(SQLite3.ColumnInt64(stmt, index));
            }
            else
            {
                var text = SQLite3.ColumnString(stmt, index);
                return DateTime.Parse(text);
            }
        }
        public DateTimeOffset GetDateTimeOffset(int index)
        {
            return new DateTimeOffset(SQLite3.ColumnInt64(stmt, index), TimeSpan.Zero);
        }
        public Int64 GetInt64(int index)
        {
            return SQLite3.ColumnInt64(stmt, index);
        }
        public UInt32 GetUInt32(int index)
        {
            return (uint)SQLite3.ColumnInt64(stmt, index);
        }
        public decimal GetDecimal(int index)
        {
            //????
            return (decimal)SQLite3.ColumnDouble(stmt, index);
        }
        public byte GetByte(int index)
        {
            return (byte)SQLite3.ColumnInt(stmt, index);
        }
        public UInt16 GetUInt16(int index)
        {
            return (ushort)SQLite3.ColumnInt(stmt, index);
        }
        public Int16 GetInt16(int index)
        {
            return (short)SQLite3.ColumnInt(stmt, index);
        }
        public sbyte GetSByte(int index)
        {
            return (sbyte)SQLite3.ColumnInt(stmt, index);
        }
        public byte[] GetByteArray(int index)
        {
            return SQLite3.ColumnByteArray(stmt, index);
        }
        public Guid GetGuid(int index)
        {
            return new Guid(SQLite3.ColumnString(stmt, index));
        }

    }
}