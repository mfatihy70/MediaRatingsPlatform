using Npgsql;
using System;

namespace MediaRatingsPlatform.Data
{
    public class Database
    {
        private readonly string _connectionString;

        public Database(string connectionString)
        {
            _connectionString = connectionString;
        }

        public NpgsqlConnection GetConnection()

        {
            return new NpgsqlConnection(_connectionString);
        }

        public void Initialize()
        {
            using var conn = GetConnection();
            conn.Open();

            // 1. Users Table
            using var cmd1 = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS users (
                    id SERIAL PRIMARY KEY,
                    username VARCHAR(50) UNIQUE NOT NULL,
                    password VARCHAR(200) NOT NULL,
                    token VARCHAR(100)
                )", conn);
            cmd1.ExecuteNonQuery();

            // 2. Media Table
            using var cmd2 = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS media (
                    id SERIAL PRIMARY KEY,
                    title VARCHAR(100) NOT NULL,
                    description TEXT,
                    media_type VARCHAR(20),
                    release_year INT,
                    genres TEXT[], 
                    age_restriction INT,
                    creator_id INT
                )", conn);
            cmd2.ExecuteNonQuery();

            // ensure indexing for performance
            try
            {
                using var idx1 = new NpgsqlCommand("CREATE INDEX IF NOT EXISTS idx_media_title ON media USING gin (to_tsvector('english', title))", conn);
                idx1.ExecuteNonQuery();
            }
            catch { }

            Console.WriteLine("Database initialized.");
        }
    }
}