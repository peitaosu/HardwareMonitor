using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace HardwareMonitor
{
    internal class Database
    {
        private SqliteConnection _connection;

        public Database(string path)
        {
            _connection = new SqliteConnection("Data Source=" + path);
            _connection.Open();

            var create = _connection.CreateCommand();
            create.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Machine (
                ID integer primary key autoincrement,
                Name varchar(32),
                URI varchar(32)
            );
            ";
            create.ExecuteNonQuery();
            create.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Data (
                ID integer primary key autoincrement,
                Hardware varchar(16),
                Name varchar(32),
                Identifier varchar(32),
                Sensors text,
                Machine integer,
                foreign key(Machine) references Machine(ID)
            );
            ";
            create.ExecuteNonQuery();
            _connection.Close();
        }

        ~Database()
        {
            if (_connection.State == System.Data.ConnectionState.Open) _connection.Close();
        }

        public void Connect()
        {
            _connection.Open();
        }

        public void Disconnect()
        {
            _connection.Close();
        }
        public bool SaveMachine(string name, string URI)
        {
            var insert = _connection.CreateCommand();
            insert.CommandText =
            @"
            INSERT INTO Machine (Name, URI) VALUES ($Name, $URI);
            ";
            insert.Parameters.AddWithValue("$Name", name);
            insert.Parameters.AddWithValue("$URI", URI);
            return insert.ExecuteNonQuery() > 0;
        }

        public bool SaveData(string hardware, string name, string identifier, string sensors, int machine)
        {
            var insert = _connection.CreateCommand();
            insert.CommandText =
            @"
            INSERT INTO Data (Hardware, Name, Identifier, Sensors, Machine) VALUES ($Hardware, $Name, $Identifier, $Sensors, $Machine);
            ";
            insert.Parameters.AddWithValue("$Hardware", hardware);
            insert.Parameters.AddWithValue("$Name", name);
            insert.Parameters.AddWithValue("$Identifier", identifier);
            insert.Parameters.AddWithValue("$Sensors", sensors);
            insert.Parameters.AddWithValue("$Machine", machine);
            return insert.ExecuteNonQuery() > 0;
        }

    }
}
