using System;
namespace SQLite.Extensions
{
    public static class ConnectionExtensions
    {
        public static void ExecBeginTransaction(this SQLiteConnection conn)
        {
            conn.Execute("BEGIN TRANSACTION"); //faster performance with begin and end transaction
        }
        public static void ExecEndTransaction(this SQLiteConnection conn)
        {
            conn.Execute("END TRANSACTION");
        }
    }
}
