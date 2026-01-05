using MediaRatingsPlatform.Models;
using Npgsql;
using System;

namespace MediaRatingsPlatform.Data
{
    public class UserRepository
    {
        private readonly Database _db;

        public UserRepository(Database db)
        {
            _db = db;
        }

        public void CreateUser(string username, string password)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            // Hash password before storing
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            using var cmd = new NpgsqlCommand("INSERT INTO users (username, password) VALUES (@u, @p)", conn);
            cmd.Parameters.AddWithValue("u", username);
            cmd.Parameters.AddWithValue("p", hash);
            cmd.ExecuteNonQuery();
        }

        // Change return type to User?
        public User? GetUserByCredentials(string username, string password)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT id, username, token FROM users WHERE username = @u AND password = @p", conn);
            cmd.Parameters.AddWithValue("u", username);
            cmd.Parameters.AddWithValue("p", password);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Token = reader.IsDBNull(2) ? null : reader.GetString(2)
                };
            }
            return null;
        }

        public void UpdateToken(int userId, string token)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("UPDATE users SET token = @t WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("t", token);
            cmd.Parameters.AddWithValue("id", userId);
            cmd.ExecuteNonQuery();
        }

        // Change return type to User?
        public User? GetUserByToken(string token)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT id, username FROM users WHERE token = @t", conn);
            cmd.Parameters.AddWithValue("t", token);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new User { Id = reader.GetInt32(0), Username = reader.GetString(1) };
            }
            return null;
        }

        public User? GetUserByUsername(string username)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT id, username FROM users WHERE username = @u", conn);
            cmd.Parameters.AddWithValue("u", username);
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return new User { Id = reader.GetInt32(0), Username = reader.GetString(1) };
            return null;
        }

        public (int totalRatings, double avgScore, string? favoriteGenre) GetUserStats(int userId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT COUNT(*) as cnt, COALESCE(AVG(stars),0) as avg
                FROM ratings WHERE user_id = @uid", conn);
            cmd.Parameters.AddWithValue("uid", userId);
            using var reader = cmd.ExecuteReader();
            int cnt = 0; double avg = 0;
            if (reader.Read()) { cnt = reader.GetInt32(0); avg = reader.IsDBNull(1) ? 0 : reader.GetDouble(1); }
            reader.Close();

            // Favorite genre: genre with most ratings by this user on media
            using var cmd2 = new NpgsqlCommand(@"
                SELECT unnest(m.genres) as genre, COUNT(*) as c
                FROM ratings r JOIN media m ON r.media_id = m.id
                WHERE r.user_id = @uid
                GROUP BY genre
                ORDER BY c DESC LIMIT 1", conn);
            cmd2.Parameters.AddWithValue("uid", userId);
            using var reader2 = cmd2.ExecuteReader();
            string? fav = null;
            if (reader2.Read()) fav = reader2.GetString(0);

            return (cnt, avg, fav);
        }

        public List<object> GetLeaderboard(int limit = 10)
        {
            var list = new List<object>();
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT u.username, COUNT(r.id) as rating_count
                FROM users u LEFT JOIN ratings r ON r.user_id = u.id
                GROUP BY u.id
                ORDER BY rating_count DESC
                LIMIT @lim", conn);
            cmd.Parameters.AddWithValue("lim", limit);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new { username = reader.GetString(0), ratings = reader.GetInt32(1) });
            }
            return list;
        }
    }
}