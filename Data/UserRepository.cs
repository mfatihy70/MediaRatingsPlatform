using MediaRatingsPlatform.Models;
using Npgsql;
using System;
using System.Collections.Generic;
// IMPORANT: Add this using statement
using BCrypt.Net;

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
            // Hash the password before storing it
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("INSERT INTO users (username, password) VALUES (@u, @p)", conn);
            cmd.Parameters.AddWithValue("u", username);
            cmd.Parameters.AddWithValue("p", passwordHash); // Store the hash, not the plain text
            cmd.ExecuteNonQuery();
        }

        public User? GetUserByCredentials(string username, string password)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            // We only select by username first
            using var cmd = new NpgsqlCommand("SELECT id, username, password, token FROM users WHERE username = @u", conn);
            cmd.Parameters.AddWithValue("u", username);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string storedHash = reader.GetString(2);

                // Verify the provided password against the stored hash
                if (BCrypt.Net.BCrypt.Verify(password, storedHash))
                {
                    return new User
                    {
                        Id = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        // Don't put password in the User object returned to the app
                        Token = reader.IsDBNull(3) ? null : reader.GetString(3)
                    };
                }
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

        // Leaderboard and Profile methods
        public UserProfile? GetUserProfile(string username)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            // Inside UserRepository.GetUserProfile(username)
            // Replace the hardcoded line with this SQL logic:

            var sql = @"
            SELECT u.username, 
                   COUNT(r.id) as total_ratings, 
                   COALESCE(AVG(r.stars), 0) as avg_score,
                   (
                       SELECT unnest(m.genres) as g
                       FROM ratings r2 
                       JOIN media m ON r2.media_id = m.id
                       WHERE r2.user_id = u.id
                       GROUP BY g
                       ORDER BY COUNT(*) DESC LIMIT 1
                   ) as fav_genre
            FROM users u
            LEFT JOIN ratings r ON u.id = r.user_id
            WHERE u.username = @u
            GROUP BY u.id";

            // Map reader.GetString(3) to FavoriteGenre (handle DBNull check)

            var profile = new UserProfile { Username = username };

            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("u", username);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    profile.TotalRatings = reader.GetInt32(1);
                    profile.AverageScoreGiven = reader.GetDouble(2);
                }
            }
            // Simple logic for favorite genre (can be expanded)
            profile.FavoriteGenre = "Action";
            return profile;
        }

        public UserProfile? GetUserProfileById(int id)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            var sql = @"
        SELECT u.username, u.email, u.favorite_genre,
                COUNT(r.id) as total_ratings, 
                COALESCE(AVG(r.stars), 0) as avg_score
        FROM users u
        LEFT JOIN ratings r ON u.id = r.user_id
        WHERE u.id = @id
        GROUP BY u.id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new UserProfile
                {
                    Username = reader.GetString(0),
                    // Handle nulls safely
                    Email = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    FavoriteGenre = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    TotalRatings = reader.GetInt32(3),
                    AverageScoreGiven = reader.GetDouble(4)
                };
            }
            return null;
        }

        public List<UserProfile> GetLeaderboard()
        {
            var list = new List<UserProfile>();
            using var conn = _db.GetConnection();
            conn.Open();
            var sql = @"
                SELECT u.username, COUNT(r.id) as total_ratings
                FROM users u
                JOIN ratings r ON u.id = r.user_id
                GROUP BY u.id, u.username
                ORDER BY total_ratings DESC
                LIMIT 10";

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new UserProfile
                {
                    Username = reader.GetString(0),
                    TotalRatings = reader.GetInt32(1)
                });
            }
            return list;
        }
        public void UpdateUserProfile(int userId, string email, string genre)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("UPDATE users SET email=@e, favorite_genre=@g WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("e", email ?? "");
            cmd.Parameters.AddWithValue("g", genre ?? "");
            cmd.Parameters.AddWithValue("id", userId);
            cmd.ExecuteNonQuery();
        }

        public List<Rating> GetUserRatingHistory(int userId)
        {
            var list = new List<Rating>();
            using var conn = _db.GetConnection();
            conn.Open();
            // Get ratings made BY this user
            var sql = @"
                SELECT r.id, r.media_id, r.user_id, r.stars, r.comment, r.timestamp, u.username,
                       (SELECT COUNT(*) FROM rating_likes rl WHERE rl.rating_id = r.id) as like_count,
                       r.is_confirmed
                FROM ratings r
                JOIN users u ON r.user_id = u.id
                WHERE r.user_id = @uid
                ORDER BY r.timestamp DESC";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", userId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Rating
                {
                    Id = reader.GetInt32(0),
                    MediaId = reader.GetInt32(1),
                    UserId = reader.GetInt32(2),
                    Stars = reader.GetInt32(3),
                    Comment = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Timestamp = reader.GetDateTime(5),
                    Username = reader.GetString(6),
                    LikeCount = reader.GetInt32(7),
                    IsConfirmed = reader.GetBoolean(8)
                });
            }
            return list;
        }
    }
}