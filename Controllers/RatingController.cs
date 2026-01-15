using MediaRatingsPlatform.Data;
using MediaRatingsPlatform.Models;
using System.Net;
using System.Threading.Tasks;

namespace MediaRatingsPlatform.Controllers
{
    public class RatingController : BaseController
    {
        private readonly RatingRepository _ratingRepo;

        public RatingController(UserRepository userRepo, RatingRepository ratingRepo) : base(userRepo)
        {
            _ratingRepo = ratingRepo;
        }

        public Task CreateRating(HttpListenerContext context, int mediaId)
        {
            var user = CheckAuth(context);
            if (user == null) { SendResponse(context, 401); return Task.CompletedTask; }

            var input = Deserialize<Rating>(context.Request.InputStream);
            if (input == null || input.Stars < 1 || input.Stars > 5)
            {
                SendResponse(context, 400, "Invalid Rating");
                return Task.CompletedTask;
            }

            input.MediaId = mediaId;
            input.UserId = user.Id;

            try
            {
                _ratingRepo.CreateRating(input);
                SendResponse(context, 201, "Rating created (Draft mode - please confirm)");
            }
            catch (System.Exception) // Unique constraint violation usually
            {
                SendResponse(context, 409, "You already rated this media");
            }
            return Task.CompletedTask;
        }

        public Task ConfirmRating(HttpListenerContext context, int ratingId)
        {
            var user = CheckAuth(context);
            if (user == null) { SendResponse(context, 401); return Task.CompletedTask; }

            var rating = _ratingRepo.GetRatingById(ratingId);

            // Explicit null check
            if (rating == null)
            {
                SendResponse(context, 404);
                return Task.CompletedTask;
            }

            // Now safe to access rating.UserId
            if (rating.UserId != user.Id)
            {
                SendResponse(context, 403);
                return Task.CompletedTask;
            }

            _ratingRepo.ConfirmRating(ratingId, user.Id);
            SendResponse(context, 200, "Rating Confirmed and Public");
            return Task.CompletedTask;
        }

        public Task UpdateRating(HttpListenerContext context, int id)
        {
            var user = CheckAuth(context);
            if (user == null) { SendResponse(context, 401); return Task.CompletedTask; }

            var existing = _ratingRepo.GetRatingById(id);
            if (existing == null) { SendResponse(context, 404); return Task.CompletedTask; }
            if (existing.UserId != user.Id) { SendResponse(context, 403); return Task.CompletedTask; }

            var input = Deserialize<Rating>(context.Request.InputStream);

            if (input == null)
            {
                SendResponse(context, 400, "Invalid Body");
                return Task.CompletedTask;
            }

            _ratingRepo.UpdateRating(id, input.Stars, input.Comment);
            SendResponse(context, 200, "Rating updated (Requires Re-Confirmation)");
            return Task.CompletedTask;
        }

        public Task DeleteRating(HttpListenerContext context, int id)
        {
            var user = CheckAuth(context);
            if (user == null) { SendResponse(context, 401); return Task.CompletedTask; }

            var existing = _ratingRepo.GetRatingById(id);
            if (existing == null) { SendResponse(context, 404); return Task.CompletedTask; }
            if (existing.UserId != user.Id) { SendResponse(context, 403); return Task.CompletedTask; }

            _ratingRepo.DeleteRating(id);
            SendResponse(context, 204);
            return Task.CompletedTask;
        }

        public Task GetRatingsForMedia(HttpListenerContext context, int mediaId)
        {
            var ratings = _ratingRepo.GetRatingsForMedia(mediaId);
            SendResponse(context, 200, ratings);
            return Task.CompletedTask;
        }

        public Task LikeRating(HttpListenerContext context, int ratingId)
        {
            var user = CheckAuth(context);
            if (user == null) { SendResponse(context, 401); return Task.CompletedTask; }

            try
            {
                _ratingRepo.AddLike(ratingId, user.Id);
                SendResponse(context, 200, "Liked");
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "23503")
            {
                // Rating ID doesn't exist
                SendResponse(context, 404, "Rating ID not found");
            }
            return Task.CompletedTask;
        }

        public Task UnlikeRating(HttpListenerContext context, int ratingId)
        {
            var user = CheckAuth(context);
            if (user == null) { SendResponse(context, 401); return Task.CompletedTask; }

            _ratingRepo.RemoveLike(ratingId, user.Id);
            SendResponse(context, 200, "Unliked");
            return Task.CompletedTask;
        }
    }
}