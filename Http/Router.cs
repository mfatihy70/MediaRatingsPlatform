using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using MediaRatingsPlatform.Controllers;

namespace MediaRatingsPlatform.Http
{
    public class Router
    {
        private readonly UserController _userController;
        private readonly MediaController _mediaController;
        private readonly RatingController _ratingController;
        private readonly FavoritesController _favoritesController;

        public Router(UserController userController, MediaController mediaController,
            RatingController ratingController, FavoritesController favoritesController)
        {
            _userController = userController;
            _mediaController = mediaController;
            _ratingController = ratingController;
            _favoritesController = favoritesController;
        }

        public async Task HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            var path = request.Url?.AbsolutePath?.ToLower();
            var method = request.HttpMethod;

            Console.WriteLine($"{method} {path}");

            try
            {
                // User Routes
                if (path == "/api/users/register" && method == "POST")
                    await _userController.Register(context);
                else if (path == "/api/users/login" && method == "POST")
                    await _userController.Login(context);
                else if (path != null && path.StartsWith("/api/users/") && path.EndsWith("/profile") && method == "GET")
                {
                    var segments = path.Split('/');
                    if (segments.Length >= 4)
                    {
                        var username = segments[3];
                        await _userController.GetProfile(context, username);
                    }
                    else
                    {
                        response.StatusCode = 400;
                        response.Close();
                    }
                }
                else if (path == "/api/users/leaderboard" && method == "GET")
                    await _userController.GetLeaderboard(context);

                // Media Routes
                else if (path == "/api/media" && method == "POST")
                    await _mediaController.CreateMedia(context);
                else if (path == "/api/media" && method == "GET")
                    await _mediaController.GetMedia(context);
                else if (path != null && path.StartsWith("/api/media/") && method == "GET" && !path.Contains("/ratings") && !path.Contains("/favorite"))
                    await _mediaController.GetMediaById(context, GetIdFromPath(path));
                else if (path != null && path.StartsWith("/api/media/") && method == "PUT")
                    await _mediaController.UpdateMedia(context, GetIdFromPath(path));
                else if (path != null && path.StartsWith("/api/media/") && method == "DELETE" && !path.Contains("/ratings") && !path.Contains("/favorite"))
                    await _mediaController.DeleteMedia(context, GetIdFromPath(path));

                // Rating Routes
                else if (path != null && path.Contains("/api/media/") && path.EndsWith("/ratings") && method == "GET")
                    await _ratingController.GetRatingsForMedia(context, ExtractMediaIdFromRatingPath(path));
                else if (path != null && path.Contains("/api/media/") && path.EndsWith("/ratings") && method == "POST")
                    await _ratingController.CreateRating(context, ExtractMediaIdFromRatingPath(path));
                else if (path != null && path.StartsWith("/api/ratings/") && method == "PUT")
                    await _ratingController.UpdateRating(context, GetIdFromPath(path));
                else if (path != null && path.StartsWith("/api/ratings/") && method == "DELETE")
                    await _ratingController.DeleteRating(context, GetIdFromPath(path));
                else if (path != null && path.StartsWith("/api/ratings/") && path.EndsWith("/like") && method == "POST")
                    await _ratingController.LikeRating(context, ExtractRatingIdFromLikePath(path));
                else if (path != null && path.StartsWith("/api/ratings/") && path.EndsWith("/like") && method == "DELETE")
                    await _ratingController.UnlikeRating(context, ExtractRatingIdFromLikePath(path));

                // Favorites Routes
                else if (path == "/api/favorites" && method == "GET")
                    await _favoritesController.GetUserFavorites(context);
                else if (path != null && path.StartsWith("/api/favorites/") && method == "POST")
                    await _favoritesController.AddFavorite(context, GetIdFromPath(path));
                else if (path != null && path.StartsWith("/api/favorites/") && method == "DELETE")
                    await _favoritesController.RemoveFavorite(context, GetIdFromPath(path));

                else
                {
                    response.StatusCode = 404;
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                response.StatusCode = 500;
                using var writer = new StreamWriter(response.OutputStream);
                writer.Write(ex.Message);
            }
        }

        private int GetIdFromPath(string path)
        {
            var segments = path.Split('/');
            if (int.TryParse(segments[^1], out int id)) return id;
            return 0;
        }

        private int ExtractMediaIdFromRatingPath(string path)
        {
            // Path: /api/media/{id}/ratings
            var segments = path.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == "media" && i + 1 < segments.Length)
                {
                    if (int.TryParse(segments[i + 1], out int id)) return id;
                }
            }
            return 0;
        }

        private int ExtractRatingIdFromLikePath(string path)
        {
            // Path: /api/ratings/{id}/like
            var segments = path.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == "ratings" && i + 1 < segments.Length)
                {
                    if (int.TryParse(segments[i + 1], out int id)) return id;
                }
            }
            return 0;
        }
    }
}