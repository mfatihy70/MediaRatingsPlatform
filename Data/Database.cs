using Npgsql;
using System;

namespace MediaRatingsPlatform.Data
{
    public class Database
    {
        private readonly string _connectionString;

        public Database(string connectionString) { _connectionString = connectionString; }

        public NpgsqlConnection GetConnection() { return new NpgsqlConnection(_connectionString); }

        public void Initialize()
        {
            using var conn = GetConnection();
            conn.Open();

            // 1. Users Table (Added email and favorite_genre)
            using var cmd1 = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS users (
                    id SERIAL PRIMARY KEY,
                    username VARCHAR(50) UNIQUE NOT NULL,
                    password VARCHAR(100) NOT NULL,
                    token VARCHAR(100),
                    email VARCHAR(100),
                    favorite_genre VARCHAR(50)
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

            // 3. Ratings Table
            using var cmd3 = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS ratings (
                    id SERIAL PRIMARY KEY,
                    media_id INT NOT NULL REFERENCES media(id) ON DELETE CASCADE,
                    user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                    stars INT CHECK (stars >= 1 AND stars <= 5),
                    comment TEXT,
                    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    is_confirmed BOOLEAN DEFAULT FALSE, 
                    UNIQUE(media_id, user_id)
                )", conn);
            cmd3.ExecuteNonQuery();

            // 4. Rating Likes Table
            using var cmd4 = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS rating_likes (
                    rating_id INT NOT NULL REFERENCES ratings(id) ON DELETE CASCADE,
                    user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                    PRIMARY KEY (rating_id, user_id)
                )", conn);
            cmd4.ExecuteNonQuery();

            // 5. Favorites Table
            using var cmd5 = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS favorites (
                    user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                    media_id INT NOT NULL REFERENCES media(id) ON DELETE CASCADE,
                    PRIMARY KEY (user_id, media_id)
                )", conn);
            cmd5.ExecuteNonQuery();

            Console.WriteLine("Database initialized.");
        }
    }
}