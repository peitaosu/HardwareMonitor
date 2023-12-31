﻿using Microsoft.Data.Sqlite;

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
                URI varchar(32),
                Timestamp integer
            );
            ";
            create.ExecuteNonQuery();
            create.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS BIOS (
                ID integer primary key autoincrement,
                BIOS text,
                Machine integer,
                Timestamp integer,
                foreign key(Machine) references Machine(ID)
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
                Parent varchar(32),
                Machine integer,
                Timestamp integer,
                foreign key(Machine) references Machine(ID)
            );
            ";
            create.ExecuteNonQuery();
        }

        ~Database()
        {
            if (_connection.State == System.Data.ConnectionState.Open) _connection.Close();
        }

        public void Connect()
        {
            if (_connection.State == System.Data.ConnectionState.Closed && _connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open();
            }
        }

        public void Disconnect()
        {
            if (_connection.State == System.Data.ConnectionState.Open)
            {
                _connection.Close();
            }
        }
        public bool SaveMachine(string name, string URI, long timestamp)
        {
            Connect();
            var insert = _connection.CreateCommand();
            insert.CommandText =
            @"
            INSERT INTO Machine (Name, URI, Timestamp) VALUES ($Name, $URI, $Timestamp);
            ";
            insert.Parameters.AddWithValue("$Name", name);
            insert.Parameters.AddWithValue("$URI", URI);
            insert.Parameters.AddWithValue("$Timestamp", timestamp);
            return insert.ExecuteNonQuery() > 0;
        }

        public long GetMachine(string name, string URI)
        {
            Connect();
            var select = _connection.CreateCommand();
            select.CommandText =
            @"
            SELECT ID
            FROM Machine
            WHERE
            Name = $Name AND URI = $URI
            ;
            ";
            select.Parameters.AddWithValue("$Name", name);
            select.Parameters.AddWithValue("$URI", URI);
            var result = (long?)select.ExecuteScalar();
            if (result == null) return 0;
            return (long)result;
        }

        public bool SaveHardware(string hardware, string name, string identifier, string sensors, string parent, long machine, long timestamp)
        {
            Connect();
            var insert = _connection.CreateCommand();
            insert.CommandText =
            @"
            INSERT INTO Data (Hardware, Name, Identifier, Sensors, Parent, Machine, Timestamp) VALUES ($Hardware, $Name, $Identifier, $Sensors, $Parent, $Machine, $Timestamp);
            ";
            insert.Parameters.AddWithValue("$Hardware", hardware);
            insert.Parameters.AddWithValue("$Name", name);
            insert.Parameters.AddWithValue("$Identifier", identifier);
            insert.Parameters.AddWithValue("$Sensors", sensors);
            insert.Parameters.AddWithValue("$Parent", parent);
            insert.Parameters.AddWithValue("$Machine", machine);
            insert.Parameters.AddWithValue("$Timestamp", timestamp);
            return insert.ExecuteNonQuery() > 0;
        }

        public int SaveHardware(List<Tuple<string, string, string, string, string, long, long>> records)
        {
            Connect();
            using (var tx = _connection.BeginTransaction()){
                var insert = _connection.CreateCommand();
                insert.CommandText =
                @"
                    INSERT INTO Data (Hardware, Name, Identifier, Sensors, Parent, Machine, Timestamp) VALUES ($Hardware, $Name, $Identifier, $Sensors, $Parent, $Machine, $Timestamp);
                ";
                insert.Parameters.AddWithValue("$Hardware", null);
                insert.Parameters.AddWithValue("$Name", null);
                insert.Parameters.AddWithValue("$Identifier", null);
                insert.Parameters.AddWithValue("$Sensors", null);
                insert.Parameters.AddWithValue("$Parent", null);
                insert.Parameters.AddWithValue("$Machine", null);
                insert.Parameters.AddWithValue("$Timestamp", null);

                int result = 0;
                foreach (var record in records)
                {
                    insert.Parameters["$Hardware"].Value = record.Item1;
                    insert.Parameters["$Name"].Value = record.Item2;
                    insert.Parameters["$Identifier"].Value = record.Item3;
                    insert.Parameters["$Sensors"].Value = record.Item4;
                    insert.Parameters["$Parent"].Value = record.Item5;
                    insert.Parameters["$Machine"].Value = record.Item6;
                    insert.Parameters["$Timestamp"].Value = record.Item7;
                    result += insert.ExecuteNonQuery();

                }
                tx.Commit();
                return result;
            }
        }

        public bool SaveBIOS(string BIOS, long machine_id, long timestamp)
        {
            Connect();
            var insert = _connection.CreateCommand();
            insert.CommandText =
            @"
            INSERT OR REPLACE INTO BIOS (ID, BIOS, Machine, Timestamp) VALUES ((SELECT ID from BIOS where Machine = $Machine), $BIOS, $Machine, $Timestamp);
            ";
            insert.Parameters.AddWithValue("$BIOS", BIOS);
            insert.Parameters.AddWithValue("$Machine", machine_id);
            insert.Parameters.AddWithValue("$Timestamp", timestamp);
            return insert.ExecuteNonQuery() > 0;
        }

        public List<dynamic> FetchData(long machine_id, int last)
        {
            Connect();
            var select_timestamp = _connection.CreateCommand();
            select_timestamp.CommandText =
            @"SELECT DISTINCT Timestamp FROM Data WHERE Machine = $Machine ORDER BY Timestamp DESC LIMIT $Last;";
            select_timestamp.Parameters.AddWithValue("$Machine", machine_id);
            select_timestamp.Parameters.AddWithValue("$Last", last);
            SqliteDataReader reader_timestamp = select_timestamp.ExecuteReader();
            if (reader_timestamp.HasRows)
            {
                List<dynamic> data = new List<dynamic>();
                var select_data = _connection.CreateCommand();
                select_data.CommandText = @"SELECT * FROM Data WHERE Timestamp = $Timestamp";
                while (reader_timestamp.Read())
                {
                    var timestamp = reader_timestamp.GetString(0);
                    select_data.Parameters.Clear();
                    select_data.Parameters.AddWithValue("$Timestamp", timestamp);
                    using (SqliteDataReader reader_data = select_data.ExecuteReader())
                    {
                        if (reader_data.HasRows)
                        {
                            while (reader_data.Read())
                            {
                                data.Insert(0, new
                                {
                                    Hardware = reader_data.GetString(1),
                                    Name = reader_data.GetString(2),
                                    Identifier = reader_data.GetString(3),
                                    Sensors = reader_data.GetString(4),
                                    Parent = reader_data.GetString(5),
                                    Machine = reader_data.GetInt64(6),
                                    Timestamp = reader_data.GetInt64(7)
                                });
                            }
                        }
                    }

                }
                return data;
            }
            else
            {
                reader_timestamp.Close();
                return null;
            }
        }

    }
}
