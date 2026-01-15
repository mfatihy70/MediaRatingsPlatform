using System;
using System.Collections.Generic;
using MediaRatingsPlatform.Data;
using MediaRatingsPlatform.Models;
using Npgsql;

namespace MediaRatingsPlatform {
    public static class Tests {
        private const string ConnectionString = "Host=localhost;Username=postgres;Password=admin;Database=MediaRatingsPlatform";

        public static void RunAllTests() {
            Console.WriteLine("--- STARTING 20 UNIT TESTS ---");
            int passed = 0;
            int failed = 0;

            var db = new Database(ConnectionString);
            try { db.Initialize(); } catch { /* Ignore if exists */ }

            void Test(string testName, Action<UserRepository, MediaRepository, RatingRepository, FavoritesRepository> testLogic) {
                try {
                    using var conn = db.GetConnection();
                    conn.Open();
                    // Clear data safely
                    using var cmd = new NpgsqlCommand("TRUNCATE users, media, ratings, favorites CASCADE;", conn);
                    cmd.ExecuteNonQuery();

                    var u = new UserRepository(db);
                    var m = new MediaRepository(db);
                    var r = new RatingRepository(db);
                    var f = new FavoritesRepository(db);

                    testLogic(u, m, r, f);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[PASS] {testName}");
                    passed++;
                } catch (Exception ex) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[FAIL] {testName}: {ex.Message}");
                    failed++;
                } finally {
                    Console.ResetColor();
                }
            }

            // --- TEST DEFINITIONS ---

            // 1. Auth Tests
            Test("CreateUser_HashesPassword", (u, m, r, f) => {
                u.CreateUser("test", "secret");
                using var conn = db.GetConnection(); conn.Open();
                using var cmd = new NpgsqlCommand("SELECT password FROM users", conn);
                var hash = cmd.ExecuteScalar()?.ToString(); // Null safe
                if (hash == "secret") throw new Exception("Password was stored as plain text!");
            });

            Test("Register_Duplicate_Throws", (u, m, r, f) => {
                u.CreateUser("dup", "pass");
                try { u.CreateUser("dup", "pass"); } catch (PostgresException) { return; }
                throw new Exception("Did not throw exception on duplicate user");
            });

            Test("Login_WrongPass_ReturnsNull", (u, m, r, f) => {
                u.CreateUser("user", "correct");
                if (u.GetUserByCredentials("user", "wrong") != null) throw new Exception("Logged in with wrong password");
            });

            Test("Token_Persists", (u, m, r, f) => {
                u.CreateUser("tuser", "p");
                var user = u.GetUserByCredentials("tuser", "p");
                if (user == null) throw new Exception("User creation failed");

                u.UpdateToken(user.Id, "abc-123");
                if (u.GetUserByToken("abc-123")?.Username != "tuser") throw new Exception("Token lookup failed");
            });

            // 2. Media Tests
            Test("CreateMedia_ReturnsId", (u, m, r, f) => {
                int uid = MakeUser(u);
                int id = m.CreateMedia(new MediaEntry { Title = "T", CreatorId = uid, Genres = new List<string>() });
                if (id <= 0) throw new Exception("Invalid ID returned");
            });

            Test("GetMedia_ById_Works", (u, m, r, f) => {
                int uid = MakeUser(u);
                int id = m.CreateMedia(new MediaEntry { Title = "TestMovie", CreatorId = uid, Genres = new List<string>() });
                var res = m.GetMediaById(id);
                if (res == null || res.Title != "TestMovie") throw new Exception("Title mismatch or null");
            });

            Test("UpdateMedia_ChangesDB", (u, m, r, f) => {
                int uid = MakeUser(u);
                int id = m.CreateMedia(new MediaEntry { Title = "Old", CreatorId = uid, Genres = new List<string>() });
                m.UpdateMedia(new MediaEntry { Id = id, Title = "New", CreatorId = uid, Genres = new List<string>() });
                var check = m.GetMediaById(id);
                if (check != null && check.Title != "New") throw new Exception("Update failed");
            });

            Test("DeleteMedia_RemovesRecord", (u, m, r, f) => {
                int uid = MakeUser(u);
                int id = m.CreateMedia(new MediaEntry { Title = "Del", CreatorId = uid, Genres = new List<string>() });
                m.DeleteMedia(id);
                if (m.GetMediaById(id) != null) throw new Exception("Delete failed");
            });

            Test("Filter_ByGenre", (u, m, r, f) => {
                int uid = MakeUser(u);
                m.CreateMedia(new MediaEntry { Title = "A", CreatorId = uid, Genres = new List<string> { "Horror" } });
                m.CreateMedia(new MediaEntry { Title = "B", CreatorId = uid, Genres = new List<string> { "Comedy" } });
                var res = m.GetMediaFiltered(null, "Horror", null, null, null, null, null);
                if (res.Count != 1 || res[0].Title != "A") throw new Exception("Genre filter failed");
            });

            Test("Filter_SQLInjection_Safe", (u, m, r, f) => {
                m.GetMediaFiltered("'; DROP TABLE media; --", null, null, null, null, null, null);
                if (m.GetMediaFiltered(null, null, null, null, null, null, null).Count != 0) throw new Exception("Injection might have executed or data exists");
            });

            // 3. Ratings & Logic
            Test("Rating_StartsUnconfirmed", (u, m, r, f) => {
                int uid = MakeUser(u);
                int mid = m.CreateMedia(new MediaEntry { Title = "M", CreatorId = uid, Genres = new List<string>() });
                r.CreateRating(new Rating { UserId = uid, MediaId = mid, Stars = 5, Comment = "Hi" });

                // FIXED: Call GetUserRatingHistory on 'u' (UserRepository), not 'r'
                var hist = u.GetUserRatingHistory(uid);
                if (hist[0].IsConfirmed) throw new Exception("Rating should be unconfirmed by default");
            });

            Test("ConfirmRating_MakesPublic", (u, m, r, f) => {
                int uid = MakeUser(u);
                int mid = m.CreateMedia(new MediaEntry { Title = "M", CreatorId = uid, Genres = new List<string>() });
                r.CreateRating(new Rating { UserId = uid, MediaId = mid, Stars = 5 });

                // FIXED: Call on 'u'
                int rid = u.GetUserRatingHistory(uid)[0].Id;
                r.ConfirmRating(rid, uid);

                if (r.GetRatingsForMedia(mid).Count == 0) throw new Exception("Confirmed rating not visible");
            });

            Test("AvgScore_CalculatesCorrectly", (u, m, r, f) => {
                int u1 = MakeUser(u);
                int u2 = MakeUser(u, "u2");
                int mid = m.CreateMedia(new MediaEntry { Title = "M", CreatorId = u1, Genres = new List<string>() });

                r.CreateRating(new Rating { UserId = u1, MediaId = mid, Stars = 5 });
                r.CreateRating(new Rating { UserId = u2, MediaId = mid, Stars = 3 });

                var media = m.GetMediaById(mid);
                // Null check
                if (media == null) throw new Exception("Media not found");
                if (media.AverageRating != 4.0) throw new Exception($"Avg wrong: {media.AverageRating}");
            });

            Test("DuplicateRating_Throws", (u, m, r, f) => {
                int uid = MakeUser(u);
                int mid = m.CreateMedia(new MediaEntry { Title = "M", CreatorId = uid, Genres = new List<string>() });
                r.CreateRating(new Rating { UserId = uid, MediaId = mid, Stars = 5 });
                try { r.CreateRating(new Rating { UserId = uid, MediaId = mid, Stars = 1 }); } catch { return; }
                throw new Exception("Allowed duplicate rating");
            });

            Test("EditRating_ResetsConfirmation", (u, m, r, f) => {
                int uid = MakeUser(u);
                int mid = m.CreateMedia(new MediaEntry { Title = "M", CreatorId = uid, Genres = new List<string>() });
                r.CreateRating(new Rating { UserId = uid, MediaId = mid, Stars = 5, Comment = "A" });

                // FIXED: Call on 'u'
                int rid = u.GetUserRatingHistory(uid)[0].Id;

                r.ConfirmRating(rid, uid);
                r.UpdateRating(rid, 4, "B");

                // FIXED: Call on 'u'
                if (u.GetUserRatingHistory(uid)[0].IsConfirmed) throw new Exception("Did not reset confirmation");
            });

            Test("Recs_ContentSimilarity", (u, m, r, f) => {
                int uid = MakeUser(u);
                m.CreateMedia(new MediaEntry { Title = "M1", MediaType = "Movie", AgeRestriction = 12, Genres = new List<string> { "Action" }, CreatorId = uid });
                var m1 = m.GetMediaFiltered("M1", null, null, null, null, null, null)[0];
                r.CreateRating(new Rating { UserId = uid, MediaId = m1.Id, Stars = 5 });

                m.CreateMedia(new MediaEntry { Title = "Target", MediaType = "Movie", AgeRestriction = 12, Genres = new List<string> { "Action" }, CreatorId = uid });

                var recs = m.GetRecommendations(uid, "content");
                if (recs.Count == 0 || recs[0].Title != "Target") throw new Exception("Recommendation engine failed");
            });

            // 4. Social
            Test("AddFavorite_Works", (u, m, r, f) => {
                int uid = MakeUser(u);
                int mid = m.CreateMedia(new MediaEntry { Title = "F", CreatorId = uid, Genres = new List<string>() });
                f.AddFavorite(uid, mid);
                if (f.GetFavorites(uid).Count != 1) throw new Exception("Fav add failed");
            });

            Test("RemoveFavorite_Works", (u, m, r, f) => {
                int uid = MakeUser(u);
                int mid = m.CreateMedia(new MediaEntry { Title = "F", CreatorId = uid, Genres = new List<string>() });
                f.AddFavorite(uid, mid);
                f.RemoveFavorite(uid, mid);
                if (f.GetFavorites(uid).Count != 0) throw new Exception("Fav remove failed");
            });

            Test("Leaderboard_Sorts", (u, m, r, f) => {
                u.CreateUser("Winner", "pass");
                u.CreateUser("Loser", "pass");
                int winnerId = u.GetUserByCredentials("Winner", "pass").Id;
                int mid = m.CreateMedia(new MediaEntry { Title = "M", CreatorId = winnerId, Genres = new List<string>() });
                r.CreateRating(new Rating { UserId = winnerId, MediaId = mid, Stars = 5 });
                var list = u.GetLeaderboard();
                if (list.Count == 0) throw new Exception("Leaderboard is empty");
                if (list[0].Username != "Winner") throw new Exception($"Sorting wrong. Expected 'Winner', got '{list[0].Username}'");
            });

            Test("LikeRating_Counts", (u, m, r, f) => {
                int u1 = MakeUser(u);
                int u2 = MakeUser(u, "u2");
                int mid = m.CreateMedia(new MediaEntry { Title = "M", CreatorId = u1, Genres = new List<string>() });
                r.CreateRating(new Rating { UserId = u1, MediaId = mid, Stars = 5 });
                int rid = u.GetUserRatingHistory(u1)[0].Id;
                r.ConfirmRating(rid, u1);
                r.AddLike(rid, u2);
                if (r.GetRatingsForMedia(mid)[0].LikeCount != 1) throw new Exception("Like count wrong");
            });

            Console.WriteLine($"\n--- DONE: {passed} Passed, {failed} Failed ---");
        }

        private static int MakeUser(UserRepository repo, string name = "test") {
            string uname = name + Guid.NewGuid().ToString().Substring(0, 5);
            repo.CreateUser(uname, "pass");
            var user = repo.GetUserByCredentials(uname, "pass");
            // Null check
            if (user == null) throw new Exception("Helper user failed to create");
            return user.Id;
        }
    }
}