using MediaRatingsPlatform.Models;
using Npgsql;
using System;
using System.Collections.Generic;

namespace MediaRatingsPlatform.Data
{
    public class MediaRepository
    {
        private readonly Database _db;

        public MediaRepository(Database db)
        {
            _db = db;
        }

        public List<MediaEntry> GetMediaFiltered(string? title = null, string? genre = null, string? mediaType = null,
            int? releaseYear = null, int? ageRestriction = null, double? minRating = null, string? sortBy = null)
        {
            var list = new List<MediaEntry>();
            using var conn = _db.GetConnection();
            conn.Open();

            var sql = "SELECT m.* FROM media m";
            var where = new List<string>();
            var cmd = new NpgsqlCommand();
            cmd.Connection = conn;

            if (minRating.HasValue)
            {
                // join with ratings to compute average
                sql = "SELECT m.*, COALESCE(avg_r.avg,0) as avg_score FROM media m LEFT JOIN (SELECT media_id, AVG(stars) as avg FROM ratings GROUP BY media_id) avg_r ON avg_r.media_id = m.id";
            }

            if (!string.IsNullOrEmpty(title))
            {
                where.Add("m.title ILIKE @title");
                cmd.Parameters.AddWithValue("title", $"%{title}%");
            }

            if (!string.IsNullOrEmpty(genre))
            {
                where.Add("@genre = ANY(m.genres)");
                cmd.Parameters.AddWithValue("genre", genre);
            }

            if (!string.IsNullOrEmpty(mediaType))
            {
                where.Add("m.media_type = @mediatype");
                cmd.Parameters.AddWithValue("mediatype", mediaType);
            }

            if (releaseYear.HasValue)
            {
                where.Add("m.release_year = @ry");
                cmd.Parameters.AddWithValue("ry", releaseYear.Value);
            }

            if (ageRestriction.HasValue)
            {
                where.Add("m.age_restriction <= @ar");
                cmd.Parameters.AddWithValue("ar", ageRestriction.Value);
            }

            if (minRating.HasValue)
            {
                where.Add("COALESCE(avg_r.avg,0) >= @minrating");
                cmd.Parameters.AddWithValue("minrating", minRating.Value);
            }

            if (where.Count > 0)
            {
                sql += " WHERE " + string.Join(" AND ", where);
            }

            // Sorting
            if (!string.IsNullOrEmpty(sortBy))
            {
                if (sortBy == "title") sql += " ORDER BY m.title";
                else if (sortBy == "year") sql += " ORDER BY m.release_year";
                else if (sortBy == "score") sql += " ORDER BY avg_score DESC";
            }

            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapReaderToMedia(reader));
            }
            return list;
        }

        public void CreateMedia(MediaEntry media)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO media (title, description, media_type, release_year, genres, age_restriction, creator_id)
                VALUES (@t, @d, @mt, @ry, @g, @ar, @cid)", conn);

            cmd.Parameters.AddWithValue("t", media.Title);
            cmd.Parameters.AddWithValue("d", media.Description ?? "");
            cmd.Parameters.AddWithValue("mt", media.MediaType);
            cmd.Parameters.AddWithValue("ry", media.ReleaseYear);
            cmd.Parameters.AddWithValue("g", media.Genres.ToArray());
            cmd.Parameters.AddWithValue("ar", media.AgeRestriction);
            cmd.Parameters.AddWithValue("cid", media.CreatorId);
            cmd.ExecuteNonQuery();
        }

        public List<MediaEntry> GetAllMedia()
        {
            var list = new List<MediaEntry>();
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT * FROM media", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapReaderToMedia(reader));
            }
            return list;
        }

        public MediaEntry? GetMediaById(int id)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT * FROM media WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return MapReaderToMedia(reader);
            return null;
        }

        public void UpdateMedia(MediaEntry media)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                UPDATE media SET title=@t, description=@d, media_type=@mt, release_year=@ry, genres=@g, age_restriction=@ar
                WHERE id=@id", conn);
            // Add parameters similar to Create...
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

        private MediaEntry MapReaderToMedia(NpgsqlDataReader reader)
        {
            return new MediaEntry
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                MediaType = reader.GetString(3),
                ReleaseYear = reader.GetInt32(4),
                Genres = new List<string>(reader.GetFieldValue<string[]>(5)),
                AgeRestriction = reader.GetInt32(6),
                CreatorId = reader.GetInt32(7)
            };
        }
    }
}