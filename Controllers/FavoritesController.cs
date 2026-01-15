using MediaRatingsPlatform.Data;
using System.Net;
using System.Threading.Tasks;

namespace MediaRatingsPlatform.Controllers
{
    public class FavoritesController : BaseController
    {
        private readonly FavoritesRepository _favRepo;

        public FavoritesController(UserRepository userRepo, FavoritesRepository favRepo) : base(userRepo)
        {
            _favRepo = favRepo;
        }

        public Task AddFavorite(HttpListenerContext context, int mediaId)
        {
            var user = CheckAuth(context);
            if (user == null) { SendResponse(context, 401); return Task.CompletedTask; }

            try
            {
                _favRepo.AddFavorite(user.Id, mediaId);
                SendResponse(context, 200, "Added to favorites");
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "23503")
            {
                // SqlState 23503 = Foreign Key Violation (Media ID doesn't exist)
                SendResponse(context, 404, "Media ID not found");
            }
            catch (Exception ex)
            {
                SendResponse(context, 500, ex.Message);
            }
            return Task.CompletedTask;
        }

        public Task RemoveFavorite(HttpListenerContext context, int mediaId)
        {
            var user = CheckAuth(context);
            if (user == null) { SendResponse(context, 401); return Task.CompletedTask; }

            _favRepo.RemoveFavorite(user.Id, mediaId);
            SendResponse(context, 200, "Removed from favorites");
            return Task.CompletedTask;
        }

        public Task GetUserFavorites(HttpListenerContext context, int userId)
        {
            var user = CheckAuth(context);
            // Security: only show my favorites
            if (user == null || user.Id != userId) { SendResponse(context, 401); return Task.CompletedTask; }

            var list = _favRepo.GetFavorites(userId);
            SendResponse(context, 200, list);
            return Task.CompletedTask;
        }
    }
}