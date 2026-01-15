using MediaRatingsPlatform.Models;
using Npgsql;
using System.Collections.Generic;

namespace MediaRatingsPlatform.Data {
    public class FavoritesRepository {
        private readonly Database _db;

        public FavoritesRepository(Database db) {
            _db = db;
        }

        public void AddFavorite(int userId, int mediaId) {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("INSERT INTO favorites (user_id, media_id) VALUES (@uid, @mid) ON CONFLICT DO NOTHING", conn);
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.Parameters.AddWithValue("mid", mediaId);
            cmd.ExecuteNonQuery();
        }

        public void RemoveFavorite(int userId, int mediaId) {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM favorites WHERE user_id=@uid AND media_id=@mid", conn);
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.Parameters.AddWithValue("mid", mediaId);
            cmd.ExecuteNonQuery();
        }

        public List<MediaEntry> GetFavorites(int userId) {
            // Join Favorites with Media to return actual Media objects
            var list = new List<MediaEntry>();
            using var conn = _db.GetConnection();
            conn.Open();
            var sql = @"
                SELECT m.*, COALESCE(AVG(r.stars), 0) as avg_score, COUNT(r.id) as count_ratings
                FROM favorites f
                JOIN media m ON f.media_id = m.id
                LEFT JOIN ratings r ON m.id = r.media_id
                WHERE f.user_id = @uid
                GROUP BY m.id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", userId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                // Reuse mapping logic (duplicated for brevity, ideally in a helper)
                list.Add(new MediaEntry {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    MediaType = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    ReleaseYear = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                    Genres = reader.IsDBNull(5) ? new List<string>() : new List<string>(reader.GetFieldValue<string[]>(5)),
                    AgeRestriction = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                    CreatorId = reader.GetInt32(7),
                    AverageRating = reader.GetDouble(8),
                    RatingCount = reader.GetInt32(9)
                });
            }
            return list;
        }
    }
}