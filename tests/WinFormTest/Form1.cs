using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;

using SQLite;

namespace WinFormTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //open sqlite connection
            //create database * ** 
            //open database
            //insert and save 
            string dbfile = "d:\\WImageTest\\test_sqlite2.sqlite";
            using (var conn = new SQLiteConnection(dbfile, true))
            {
                string create_table = "create table if not exists \"test001\" (a int,b varchar(255))";
                conn.Execute(create_table);

                PreparedSqlLiteInsertCommand insertCmd = new PreparedSqlLiteInsertCommand(
                    "insert into test001(a,b) values(?,?)",
                    conn);
                //insert orderline
                conn.Execute("BEGIN TRANSACTION"); //faster performance with begin and end transaction
                for (int i = 0; i < 10; ++i)
                {
                    insertCmd.ExecuteNonQuery(new object[] { 1, "x" });
                }
                insertCmd.Dispose();

                conn.Execute("END TRANSACTION");
            }
            //---------
            //open db and select 
            using (var conn = new SQLiteConnection(dbfile, true))
            {
                string sql = "select * from test001";
                SQLiteCommand select = new SQLiteCommand(sql, conn);
                SQLiteDataReader reader = select.ExecuteReader();
                while (reader.Read())
                {

                }
                reader.Close();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string dbfile = "d:\\WImageTest\\test_sqlite2.sqlite";
            using (var conn = new SQLiteConnection(dbfile, true))
            {

                string create_table = "create table if not exists \"blobTest\" (a blob)";
                conn.Execute(create_table);
                PreparedSqlLiteInsertCommand insertCmd = new PreparedSqlLiteInsertCommand(conn);
                insertCmd.CommandText = "insert into blobTest(a) values(?)";
                //insert orderline
                conn.Execute("BEGIN TRANSACTION"); //faster performance with begin and end transaction
                for (int i = 0; i < 10; ++i)
                {
                    byte[] files = System.IO.File.ReadAllBytes("d:\\WImageTest\\01.png");
                    insertCmd.ExecuteNonQuery(new object[] { files });
                }
                insertCmd.Dispose();
                conn.Execute("END TRANSACTION");
            }
            //---------
            //open db and select 
            using (var conn = new SQLiteConnection(dbfile, true))
            {
                string sql = "select * from blobTest";
                SQLiteCommand select = new SQLiteCommand(conn);
                select.CommandText = sql;
                SQLiteDataReader reader = select.ExecuteReader();
                while (reader.Read())
                {
                    byte[] data = reader.GetByteArray(0);
                    //save 
                    System.IO.File.WriteAllBytes("d:\\WImageTest\\mydata1.png", data);
                }
                reader.Close();
            }
        }
    }
}
