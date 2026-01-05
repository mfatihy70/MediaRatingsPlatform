using Npgsql;
using System.Collections.Generic;
using System;

namespace MediaRatingsPlatform.Data
{
    public class FavoritesRepository
    {
        private readonly Database _db;

        public FavoritesRepository(Database db)
        {
            _db = db;
        }

        public void AddFavorite(int userId, int mediaId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("INSERT INTO favorites (user_id, media_id) VALUES (@user_id, @media_id)", conn);
            cmd.Parameters.AddWithValue("user_id", userId);
            cmd.Parameters.AddWithValue("media_id", mediaId);
            try { cmd.ExecuteNonQuery(); } catch (PostgresException ex) when (ex.SqlState == "23505") { }
        }

        public void RemoveFavorite(int userId, int mediaId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM favorites WHERE user_id = @user_id AND media_id = @media_id", conn);
            cmd.Parameters.AddWithValue("user_id", userId);
            cmd.Parameters.AddWithValue("media_id", mediaId);
            cmd.ExecuteNonQuery();
        }

        public bool IsFavorite(int userId, int mediaId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM favorites WHERE user_id = @user_id AND media_id = @media_id", conn);
            cmd.Parameters.AddWithValue("user_id", userId);
            cmd.Parameters.AddWithValue("media_id", mediaId);
            var result = cmd.ExecuteScalar();
            return result != null && Convert.ToInt32(result) > 0;
        }

        public List<int> GetUserFavorites(int userId)
        {
            var favorites = new List<int>();
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT media_id FROM favorites WHERE user_id = @user_id", conn);
            cmd.Parameters.AddWithValue("user_id", userId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) favorites.Add(reader.GetInt32(0));
            return favorites;
        }

        public int GetFavoritesCount(int mediaId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM favorites WHERE media_id = @media_id", conn);
            cmd.Parameters.AddWithValue("media_id", mediaId);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }
    }
}
