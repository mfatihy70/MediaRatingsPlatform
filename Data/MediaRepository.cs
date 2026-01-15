using MediaRatingsPlatform.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace MediaRatingsPlatform.Data
{
    public class MediaRepository
    {
        private readonly Database _db;

        public MediaRepository(Database db)
        {
            _db = db;
        }

        public int CreateMedia(MediaEntry media) // Return int
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO media (title, description, media_type, release_year, genres, age_restriction, creator_id)
                VALUES (@t, @d, @mt, @ry, @g, @ar, @cid)
                RETURNING id", conn); // Add RETURNING id

            cmd.Parameters.AddWithValue("t", media.Title);
            cmd.Parameters.AddWithValue("d", media.Description ?? "");
            cmd.Parameters.AddWithValue("mt", media.MediaType);
            cmd.Parameters.AddWithValue("ry", media.ReleaseYear);
            cmd.Parameters.AddWithValue("g", media.Genres.ToArray());
            cmd.Parameters.AddWithValue("ar", media.AgeRestriction);
            cmd.Parameters.AddWithValue("cid", media.CreatorId);

            // Inside CreateMedia method, replace the return line with:
            object? result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public MediaEntry? GetMediaById(int id)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            // We join with ratings to calculate average on the fly
            string sql = @"
                SELECT m.*, 
                       COALESCE(AVG(r.stars), 0) as avg_score,
                       COUNT(r.id) as count_ratings
                FROM media m
                LEFT JOIN ratings r ON m.id = r.media_id
                WHERE m.id = @id
                GROUP BY m.id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return MapReaderToMedia(reader);
            return null;
        }

        // Implementation of Filtering, Searching, and Sorting
        public List<MediaEntry> GetMediaFiltered(string? title, string? genre, string? type, int? year, int? age, int? minRating, string? sortBy)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            // Start with a base query that joins ratings to calculate the score
            var sqlBuilder = new System.Text.StringBuilder(@"
                SELECT m.*, 
                       COALESCE(AVG(r.stars), 0) as avg_score,
                       COUNT(r.id) as count_ratings
                FROM media m
                LEFT JOIN ratings r ON m.id = r.media_id
                WHERE 1=1 ");

            // Add filters only if the parameter is provided
            if (!string.IsNullOrEmpty(title)) sqlBuilder.Append(" AND LOWER(m.title) LIKE @title");
            if (!string.IsNullOrEmpty(genre)) sqlBuilder.Append(" AND @genre = ANY(m.genres)"); // Checks if genre is inside the array
            if (!string.IsNullOrEmpty(type)) sqlBuilder.Append(" AND m.media_type = @type");
            if (year.HasValue) sqlBuilder.Append(" AND m.release_year = @year");
            if (age.HasValue) sqlBuilder.Append(" AND m.age_restriction = @age");

            // Grouping is required for AVG calculation
            sqlBuilder.Append(" GROUP BY m.id");

            // "HAVING" must come after GROUP BY. This filters by the calculated average score.
            if (minRating.HasValue) sqlBuilder.Append(" HAVING COALESCE(AVG(r.stars), 0) >= @minRating");

            // Sorting logic
            switch (sortBy?.ToLower())
            {
                case "title": sqlBuilder.Append(" ORDER BY m.title ASC"); break;
                case "year": sqlBuilder.Append(" ORDER BY m.release_year DESC"); break;
                case "score": sqlBuilder.Append(" ORDER BY avg_score DESC"); break;
                default: sqlBuilder.Append(" ORDER BY m.id ASC"); break;
            }

            using var cmd = new NpgsqlCommand(sqlBuilder.ToString(), conn);

            // Bind parameters safely to prevent SQL Injection
            if (!string.IsNullOrEmpty(title)) cmd.Parameters.AddWithValue("title", $"%{title.ToLower()}%");
            if (!string.IsNullOrEmpty(genre)) cmd.Parameters.AddWithValue("genre", genre);
            if (!string.IsNullOrEmpty(type)) cmd.Parameters.AddWithValue("type", type);
            if (year.HasValue) cmd.Parameters.AddWithValue("year", year.Value);
            if (age.HasValue) cmd.Parameters.AddWithValue("age", age.Value);
            if (minRating.HasValue) cmd.Parameters.AddWithValue("minRating", (double)minRating.Value);

            var list = new List<MediaEntry>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapReaderToMedia(reader));
            }
            return list;
        }

        public void UpdateMedia(MediaEntry media)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                UPDATE media SET title=@t, description=@d, media_type=@mt, release_year=@ry, genres=@g, age_restriction=@ar
                WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("t", media.Title);
            cmd.Parameters.AddWithValue("d", media.Description);
            cmd.Parameters.AddWithValue("mt", media.MediaType);
            cmd.Parameters.AddWithValue("ry", media.ReleaseYear);
            cmd.Parameters.AddWithValue("g", media.Genres.ToArray());
            cmd.Parameters.AddWithValue("ar", media.AgeRestriction);
            cmd.Parameters.AddWithValue("id", media.Id);
            cmd.ExecuteNonQuery();
        }

        public void DeleteMedia(int id)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM media WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);
            cmd.ExecuteNonQuery();
        }

        // RECOMMENDATION LOGIC
        public List<MediaEntry> GetRecommendations(int userId, string type)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            if (type == "genre")
            {
                // 1. Get user's preferred genre (from Profile OR calculation)
                // Let's use calculation from high rated movies
                string genreSql = @"
                    SELECT unnest(m.genres) as g, COUNT(*) as c
                    FROM ratings r
                    JOIN media m ON r.media_id = m.id
                    WHERE r.user_id = @uid AND r.stars >= 4
                    GROUP BY g
                    ORDER BY c DESC LIMIT 1";

                string favGenre = "";
                using (var cmd = new NpgsqlCommand(genreSql, conn))
                {
                    cmd.Parameters.AddWithValue("uid", userId);
                    var res = cmd.ExecuteScalar();
                    if (res != null) favGenre = res.ToString() ?? "";
                }

                if (string.IsNullOrEmpty(favGenre)) return new List<MediaEntry>();

                // Recommend unrated movies with this genre
                string recSql = @"
                    SELECT m.*, COALESCE(AVG(r.stars), 0) as avg_score, COUNT(r.id) as count_ratings
                    FROM media m
                    LEFT JOIN ratings r ON m.id = r.media_id
                    WHERE @g = ANY(m.genres)
                    AND m.id NOT IN (SELECT media_id FROM ratings WHERE user_id = @uid)
                    GROUP BY m.id
                    ORDER BY avg_score DESC LIMIT 5";

                // Execute and map (omitted for brevity, same as existing GetMediaFiltered logic)
                // ...
                return ExecuteMediaListQuery(conn, recSql, new[] {
            new NpgsqlParameter("g", favGenre),
            new NpgsqlParameter("uid", userId)
        });
            }
            else if (type == "content")
            {
                // Content similarity: Recommend same MediaType and AgeRestriction as user's favorites
                // Simplified: Find most watched MediaType
                string typeSql = @"
                    SELECT m.media_type, COUNT(*) as c
                    FROM ratings r
                    JOIN media m ON r.media_id = m.id
                    WHERE r.user_id = @uid
                    GROUP BY m.media_type
                    ORDER BY c DESC LIMIT 1";

                string favType = "";
                using (var cmd = new NpgsqlCommand(typeSql, conn))
                {
                    cmd.Parameters.AddWithValue("uid", userId);
                    var res = cmd.ExecuteScalar();
                    if (res != null) favType = res.ToString() ?? "";
                }

                string recSql = @"
                    SELECT m.*, COALESCE(AVG(r.stars), 0) as avg_score, COUNT(r.id) as count_ratings
                    FROM media m
                    LEFT JOIN ratings r ON m.id = r.media_id
                    WHERE m.media_type = @mt
                    AND m.id NOT IN (SELECT media_id FROM ratings WHERE user_id = @uid)
                    GROUP BY m.id
                    ORDER BY avg_score DESC LIMIT 5";

                return ExecuteMediaListQuery(conn, recSql, new[] {
            new NpgsqlParameter("mt", favType),
            new NpgsqlParameter("uid", userId)
        });
            }

            return new List<MediaEntry>();
        }

        // Helper to avoid repeated code
        private List<MediaEntry> ExecuteMediaListQuery(NpgsqlConnection conn, string sql, NpgsqlParameter[] parameters)
        {
            var list = new List<MediaEntry>();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                // MapReaderToMedia implementation (same as before)
                list.Add(MapReaderToMedia(reader));
            }
            return list;
        }
        private MediaEntry MapReaderToMedia(NpgsqlDataReader reader)
        {
            return new MediaEntry
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                MediaType = reader.IsDBNull(3) ? "" : reader.GetString(3),
                ReleaseYear = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                Genres = reader.IsDBNull(5) ? new List<string>() : new List<string>(reader.GetFieldValue<string[]>(5)),
                AgeRestriction = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                CreatorId = reader.GetInt32(7),
                // Calculated columns appear after the main table columns
                AverageRating = reader.IsDBNull(8) ? 0.0 : reader.GetDouble(8),
                RatingCount = reader.IsDBNull(9) ? 0 : reader.GetInt32(9)
            };
        }
    }
}