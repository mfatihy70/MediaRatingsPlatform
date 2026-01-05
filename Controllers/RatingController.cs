using MediaRatingsPlatform.Data;
using MediaRatingsPlatform.Models;
using System.Net;
using System.Threading.Tasks;

namespace MediaRatingsPlatform.Controllers
{
    public class RatingController : BaseController
    {
        private readonly RatingRepository _ratingRepo;
        private readonly MediaRepository _mediaRepo;
        private readonly FavoritesRepository _favRepo;

        public RatingController(UserRepository userRepo, RatingRepository ratingRepo, MediaRepository mediaRepo, FavoritesRepository favRepo) : base(userRepo)
        {
            _ratingRepo = ratingRepo;
            _mediaRepo = mediaRepo;
            _favRepo = favRepo;
        }

        public Task CreateRating(HttpListenerContext context, int mediaId)
        {
            var user = CheckAuth(context);
            if (user == null)
            {
                SendResponse(context, 401, "Unauthorized");
                return Task.CompletedTask;
            }

            var media = _mediaRepo.GetMediaById(mediaId);
            if (media == null)
            {
                SendResponse(context, 404, "Media not found");
                return Task.CompletedTask;
            }

            var data = Deserialize<Rating>(context.Request.InputStream);
            if (data == null || data.Stars < 1 || data.Stars > 5)
            {
                SendResponse(context, 400, "Invalid rating data. Stars must be 1-5");
                return Task.CompletedTask;
            }

            if (_ratingRepo.UserHasRated(user.Id, mediaId))
            {
                SendResponse(context, 409, "You have already rated this media");
                return Task.CompletedTask;
            }

            data.UserId = user.Id;
            data.MediaId = mediaId;
            data.IsConfirmed = string.IsNullOrEmpty(data.Comment);

            _ratingRepo.CreateRating(data);
            SendResponse(context, 201, "Rating created");
            return Task.CompletedTask;
        }

        public Task GetRatingsForMedia(HttpListenerContext context, int mediaId)
        {
            var user = CheckAuth(context);
            if (user == null) { SendResponse(context, 401, "Unauthorized"); return Task.CompletedTask; }

            var media = _mediaRepo.GetMediaById(mediaId);
            if (media == null) { SendResponse(context, 404, "Media not found"); return Task.CompletedTask; }

            var ratings = _ratingRepo.GetRatingsByMediaId(mediaId);
            var avgScore = _ratingRepo.GetAverageRatingForMedia(mediaId);
            var response = new { ratings, averageScore = avgScore };
            SendResponse(context, 200, response);
            return Task.CompletedTask;
        }

        public Task UpdateRating(HttpListenerContext context, int ratingId)
        {
            var user = CheckAuth(context);
            if (user == null) { SendResponse(context, 401, "Unauthorized"); return Task.CompletedTask; }

            var existing = _ratingRepo.GetRatingById(ratingId);
            if (existing == null) { SendResponse(context, 404, "Rating not found"); return Task.CompletedTask; }
            if (existing.UserId != user.Id) { SendResponse(context, 403, "Only the author can edit this rating"); return Task.CompletedTask; }

            var updateData = Deserialize<Rating>(context.Request.InputStream);
            if (updateData == null || updateData.Stars < 1 || updateData.Stars > 5) { SendResponse(context, 400, "Invalid rating data. Stars must be 1-5"); return Task.CompletedTask; }

            updateData.Id = ratingId;
            updateData.UserId = existing.UserId;
            updateData.MediaId = existing.MediaId;
            updateData.IsConfirmed = string.IsNullOrEmpty(updateData.Comment);

            _ratingRepo.UpdateRating(updateData);
            SendResponse(context, 200, "Rating updated");
            return Task.CompletedTask;
        }

        public Task DeleteRating(HttpListenerContext context, int ratingId)
        {
            var user = CheckAuth(context);
            if (user == null) { SendResponse(context, 401, "Unauthorized"); return Task.CompletedTask; }

            var existing = _ratingRepo.GetRatingById(ratingId);
            if (existing == null) { SendResponse(context, 404, "Rating not found"); return Task.CompletedTask; }
            if (existing.UserId != user.Id) { SendResponse(context, 403, "Only the author can delete this rating"); return Task.CompletedTask; }

            _ratingRepo.DeleteRating(ratingId);
            SendResponse(context, 204, null);
            return Task.CompletedTask;
        }

        public Task LikeRating(HttpListenerContext context, int ratingId)
        {
            var user = CheckAuth(context);
            if (user == null) { SendResponse(context, 401, "Unauthorized"); return Task.CompletedTask; }

            var rating = _ratingRepo.GetRatingById(ratingId);
            if (rating == null) { SendResponse(context, 404, "Rating not found"); return Task.CompletedTask; }

            _ratingRepo.LikeRating(user.Id, ratingId);
            SendResponse(context, 200, "Liked");
            return Task.CompletedTask;
        }

        public Task UnlikeRating(HttpListenerContext context, int ratingId)
        {
            var user = CheckAuth(context);
            if (user == null) { SendResponse(context, 401, "Unauthorized"); return Task.CompletedTask; }

            var rating = _ratingRepo.GetRatingById(ratingId);
            if (rating == null) { SendResponse(context, 404, "Rating not found"); return Task.CompletedTask; }

            _ratingRepo.UnlikeRating(user.Id, ratingId);
            SendResponse(context, 200, "Unliked");
            return Task.CompletedTask;
        }
    }
}
