using MediaRatingsPlatform.Models;
using Npgsql;
using System.Collections.Generic;

namespace MediaRatingsPlatform.Data {
    public class RatingRepository {
        private readonly Database _db;

        public RatingRepository(Database db) {
            _db = db;
        }

        public void CreateRating(Rating rating) {
            using var conn = _db.GetConnection();
            conn.Open();
            // is_confirmed defaults to false in DB
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO ratings (media_id, user_id, stars, comment, is_confirmed)
                VALUES (@mid, @uid, @s, @c, false)", conn);
            cmd.Parameters.AddWithValue("mid", rating.MediaId);
            cmd.Parameters.AddWithValue("uid", rating.UserId);
            cmd.Parameters.AddWithValue("s", rating.Stars);
            cmd.Parameters.AddWithValue("c", rating.Comment ?? "");
            cmd.ExecuteNonQuery();
        }

        public void UpdateRating(int ratingId, int stars, string comment) {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                UPDATE ratings SET stars=@s, comment=@c, is_confirmed=false 
                WHERE id=@id", conn);
            // Note: Editing a rating resets confirmation (moderation logic)
            cmd.Parameters.AddWithValue("s", stars);
            cmd.Parameters.AddWithValue("c", comment);
            cmd.Parameters.AddWithValue("id", ratingId);
            cmd.ExecuteNonQuery();
        }

        // The "Moderation" confirmation
        public void ConfirmRating(int ratingId, int userId) {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("UPDATE ratings SET is_confirmed=true WHERE id=@id AND user_id=@uid", conn);
            cmd.Parameters.AddWithValue("id", ratingId);
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.ExecuteNonQuery();
        }

        public void DeleteRating(int id) {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM ratings WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);
            cmd.ExecuteNonQuery();
        }

        public Rating? GetRatingById(int id) {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT * FROM ratings WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) {
                return new Rating {
                    Id = reader.GetInt32(0),
                    MediaId = reader.GetInt32(1),
                    UserId = reader.GetInt32(2),
                    Stars = reader.GetInt32(3),
                    Comment = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    IsConfirmed = reader.GetBoolean(6)
                };
            }
            return null;
        }

        public List<Rating> GetRatingsForMedia(int mediaId) {
            var list = new List<Rating>();
            using var conn = _db.GetConnection();
            conn.Open();
            // Get ratings, join user for username, count likes
            // Only return CONFIRMED ratings (except maybe for the user themselves, but simpler to filter confirmed)
            var sql = @"
                SELECT r.id, r.media_id, r.user_id, r.stars, r.comment, r.timestamp, u.username,
                       (SELECT COUNT(*) FROM rating_likes rl WHERE rl.rating_id = r.id) as like_count
                FROM ratings r
                JOIN users u ON r.user_id = u.id
                WHERE r.media_id = @mid AND r.is_confirmed = true
                ORDER BY r.timestamp DESC";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("mid", mediaId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                list.Add(new Rating {
                    Id = reader.GetInt32(0),
                    MediaId = reader.GetInt32(1),
                    UserId = reader.GetInt32(2),
                    Stars = reader.GetInt32(3),
                    Comment = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Timestamp = reader.GetDateTime(5),
                    IsConfirmed = true,
                    Username = reader.GetString(6),
                    LikeCount = reader.GetInt32(7)
                });
            }
            return list;
        }

        public void AddLike(int ratingId, int userId) {
            using var conn = _db.GetConnection();
            conn.Open();
            // Insert ignore conflict (if already liked)
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO rating_likes (rating_id, user_id) VALUES (@rid, @uid) 
                ON CONFLICT DO NOTHING", conn);
            cmd.Parameters.AddWithValue("rid", ratingId);
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.ExecuteNonQuery();
        }

        public void RemoveLike(int ratingId, int userId) {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM rating_likes WHERE rating_id=@rid AND user_id=@uid", conn);
            cmd.Parameters.AddWithValue("rid", ratingId);
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.ExecuteNonQuery();
        }
    }
}