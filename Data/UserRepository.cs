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
            using var cmd = new NpgsqlCommand("INSERT INTO users (username, password) VALUES (@u, @p)", conn);
            cmd.Parameters.AddWithValue("u", username);
            cmd.Parameters.AddWithValue("p", password);
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
    }
}