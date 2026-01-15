using System;
using System.Net;
using System.Threading.Tasks;
using MediaRatingsPlatform.Controllers;
using System.Text.RegularExpressions;

namespace MediaRatingsPlatform.Http
{
    public class Router
    {
        private readonly UserController _userController;
        private readonly MediaController _mediaController;
        private readonly RatingController _ratingController;
        private readonly FavoritesController _favoritesController;

        public Router(UserController u, MediaController m, RatingController r, FavoritesController f)
        {
            _userController = u;
            _mediaController = m;
            _ratingController = r;
            _favoritesController = f;
        }

        public async Task HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var path = request.Url?.AbsolutePath?.ToLower().TrimEnd('/') ?? string.Empty;
            var method = request.HttpMethod;

            try
            {
                // --- AUTH ---
                if (path == "/api/users/register" && method == "POST")
                    await _userController.Register(context);
                else if (path == "/api/users/login" && method == "POST")
                    await _userController.Login(context);

                // --- LEADERBOARD ---
                else if (path == "/api/leaderboard" && method == "GET")
                    await _userController.GetLeaderboard(context);

                // --- USER SPECIFIC ROUTES ---
                // Regex for /api/users/{id}/...
                else if (Regex.IsMatch(path, @"^/api/users/\d+/profile$") && method == "GET")
                    await _userController.GetProfileById(context, GetIdFromPath(path, 3));

                else if (Regex.IsMatch(path, @"^/api/users/\d+/profile$") && method == "PUT")
                    await _userController.UpdateProfile(context, GetIdFromPath(path, 3));

                else if (Regex.IsMatch(path, @"^/api/users/\d+/ratings$") && method == "GET")
                    await _userController.GetUserRatingHistory(context, GetIdFromPath(path, 3));

                else if (Regex.IsMatch(path, @"^/api/users/\d+/favorites$") && method == "GET")
                    await _favoritesController.GetUserFavorites(context, GetIdFromPath(path, 3));

                else if (Regex.IsMatch(path, @"^/api/users/\d+/recommendations$") && method == "GET")
                    await _mediaController.GetRecommendations(context, GetIdFromPath(path, 3));

                // --- MEDIA ROUTES ---
                else if (path == "/api/media" && method == "GET")
                    await _mediaController.GetMedia(context); // Search & Filter
                else if (path == "/api/media" && method == "POST")
                    await _mediaController.CreateMedia(context);

                // /api/media/{id}
                else if (Regex.IsMatch(path, @"^/api/media/\d+$") && method == "GET")
                    await _mediaController.GetMediaById(context, GetIdFromPath(path, 3));
                else if (Regex.IsMatch(path, @"^/api/media/\d+$") && method == "PUT")
                    await _mediaController.UpdateMedia(context, GetIdFromPath(path, 3));
                else if (Regex.IsMatch(path, @"^/api/media/\d+$") && method == "DELETE")
                    await _mediaController.DeleteMedia(context, GetIdFromPath(path, 3));

                // --- MEDIA ACTIONS (Rate & Favorite) ---
                // /api/media/{id}/rate
                else if (Regex.IsMatch(path, @"^/api/media/\d+/rate$") && method == "POST")
                    await _ratingController.CreateRating(context, GetIdFromPath(path, 3)); // ID is at index 3

                // /api/media/{id}/favorite
                else if (Regex.IsMatch(path, @"^/api/media/\d+/favorite$") && method == "POST")
                    await _favoritesController.AddFavorite(context, GetIdFromPath(path, 3));
                else if (Regex.IsMatch(path, @"^/api/media/\d+/favorite$") && method == "DELETE")
                    await _favoritesController.RemoveFavorite(context, GetIdFromPath(path, 3));

                // --- RATING MANAGEMENT ---
                // /api/ratings/{id}
                else if (Regex.IsMatch(path, @"^/api/ratings/\d+$") && method == "PUT")
                    await _ratingController.UpdateRating(context, GetIdFromPath(path, 3));

                // /api/ratings/{id}/like
                else if (Regex.IsMatch(path, @"^/api/ratings/\d+/like$") && method == "POST")
                    await _ratingController.LikeRating(context, GetIdFromPath(path, 3));

                // /api/ratings/{id}/confirm (Postman uses POST here)
                else if (Regex.IsMatch(path, @"^/api/ratings/\d+/confirm$") && method == "POST")
                    await _ratingController.ConfirmRating(context, GetIdFromPath(path, 3));

                else
                {
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
        }

        private int GetIdFromPath(string path, int index)
        {
            var segments = path.Split('/');
            if (segments.Length > index && int.TryParse(segments[index], out int id))
                return id;
            return 0;
        }
    }
}