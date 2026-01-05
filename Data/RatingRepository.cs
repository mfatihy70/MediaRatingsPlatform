using MediaRatingsPlatform.Models;
using Npgsql;
using System;
using System.Collections.Generic;

namespace MediaRatingsPlatform.Data
{
    public class RatingRepository
    {
        private readonly Database _db;

        public RatingRepository(Database db)
        {
            _db = db;
        }

        public void CreateRating(Rating rating)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO ratings (stars, comment, comment_confirmed, user_id, media_id)
                VALUES (@stars, @comment, @comment_confirmed, @user_id, @media_id)", conn);

            cmd.Parameters.AddWithValue("stars", rating.Stars);
            cmd.Parameters.AddWithValue("comment", rating.Comment ?? "");
            cmd.Parameters.AddWithValue("comment_confirmed", rating.IsConfirmed);
            cmd.Parameters.AddWithValue("user_id", rating.UserId);
            cmd.Parameters.AddWithValue("media_id", rating.MediaId);
            cmd.ExecuteNonQuery();
        }

        public Rating? GetRatingById(int id)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT id, stars, comment, comment_confirmed, user_id, media_id, created_at FROM ratings WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return MapReaderToRating(reader);
            return null;
        }

        public List<Rating> GetRatingsByMediaId(int mediaId)
        {
            var list = new List<Rating>();
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT id, stars, comment, comment_confirmed, user_id, media_id, created_at FROM ratings WHERE media_id = @media_id ORDER BY created_at DESC", conn);
            cmd.Parameters.AddWithValue("media_id", mediaId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(MapReaderToRating(reader));
            return list;
        }

        public List<Rating> GetRatingsByUserId(int userId)
        {
            var list = new List<Rating>();
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT id, stars, comment, comment_confirmed, user_id, media_id, created_at FROM ratings WHERE user_id = @user_id ORDER BY created_at DESC", conn);
            cmd.Parameters.AddWithValue("user_id", userId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(MapReaderToRating(reader));
            return list;
        }

        public Rating? GetUserRatingForMedia(int userId, int mediaId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT id, stars, comment, comment_confirmed, user_id, media_id, created_at FROM ratings WHERE user_id = @user_id AND media_id = @media_id", conn);
            cmd.Parameters.AddWithValue("user_id", userId);
            cmd.Parameters.AddWithValue("media_id", mediaId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return MapReaderToRating(reader);
            return null;
        }

        public void UpdateRating(Rating rating)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                UPDATE ratings SET stars=@stars, comment=@comment, comment_confirmed=@comment_confirmed WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("stars", rating.Stars);
            cmd.Parameters.AddWithValue("comment", rating.Comment ?? "");
            cmd.Parameters.AddWithValue("comment_confirmed", rating.IsConfirmed);
            cmd.Parameters.AddWithValue("id", rating.Id);
            cmd.ExecuteNonQuery();
        }

        public void DeleteRating(int ratingId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM ratings WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", ratingId);
            cmd.ExecuteNonQuery();
        }

        public void LikeRating(int userId, int ratingId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("INSERT INTO rating_likes (user_id, rating_id) VALUES (@user_id, @rating_id)", conn);
            cmd.Parameters.AddWithValue("user_id", userId);
            cmd.Parameters.AddWithValue("rating_id", ratingId);
            try { cmd.ExecuteNonQuery(); } catch (PostgresException ex) when (ex.SqlState == "23505") { /* already liked */ }
        }

        public void UnlikeRating(int userId, int ratingId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM rating_likes WHERE user_id = @user_id AND rating_id = @rating_id", conn);
            cmd.Parameters.AddWithValue("user_id", userId);
            cmd.Parameters.AddWithValue("rating_id", ratingId);
            cmd.ExecuteNonQuery();
        }

        public int GetLikeCount(int ratingId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM rating_likes WHERE rating_id = @rating_id", conn);
            cmd.Parameters.AddWithValue("rating_id", ratingId);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public bool HasUserLikedRating(int userId, int ratingId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM rating_likes WHERE user_id = @user_id AND rating_id = @rating_id", conn);
            cmd.Parameters.AddWithValue("user_id", userId);
            cmd.Parameters.AddWithValue("rating_id", ratingId);
            var result = cmd.ExecuteScalar();
            return result != null && Convert.ToInt32(result) > 0;
        }

        public double GetAverageRatingForMedia(int mediaId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT AVG(stars) FROM ratings WHERE media_id = @media_id", conn);
            cmd.Parameters.AddWithValue("media_id", mediaId);
            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value ? Convert.ToDouble(result) : 0.0;
        }

        public bool UserHasRated(int userId, int mediaId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM ratings WHERE user_id=@u AND media_id=@m", conn);
            cmd.Parameters.AddWithValue("u", userId);
            cmd.Parameters.AddWithValue("m", mediaId);
            var res = cmd.ExecuteScalar();
            return res != null && Convert.ToInt32(res) > 0;
        }

        private Rating MapReaderToRating(NpgsqlDataReader reader)
        {
            return new Rating
            {
                Id = reader.GetInt32(0),
                Stars = reader.GetInt32(1),
                Comment = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                IsConfirmed = reader.GetBoolean(3),
                UserId = reader.GetInt32(4),
                MediaId = reader.GetInt32(5),
                CreatedAt = reader.GetDateTime(6)
            };
        }
    }
}
