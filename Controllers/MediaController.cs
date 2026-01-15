using MediaRatingsPlatform.Data;
using MediaRatingsPlatform.Models;
using System.Net;
using System.Threading.Tasks;

namespace MediaRatingsPlatform.Controllers
{
    public class MediaController : BaseController
    {
        private readonly MediaRepository _mediaRepo;

        public MediaController(UserRepository userRepo, MediaRepository mediaRepo) : base(userRepo)
        {
            _mediaRepo = mediaRepo;
        }

        public Task CreateMedia(HttpListenerContext context)
        {
            // 1. Auth Check (Do this ONCE)
            var user = CheckAuth(context);
            if (media == null) { SendResponse(context, 400, "Empty Body"); return Task.CompletedTask; }
            if (string.IsNullOrWhiteSpace(media.Title)) { SendResponse(context, 400, "Title is required"); return Task.CompletedTask; }
            if (media.ReleaseYear < 1900 || media.ReleaseYear > 2100) { SendResponse(context, 400, "Invalid Year"); return Task.CompletedTask; }
            // 2. Deserialize
            var media = Deserialize<MediaEntry>(context.Request.InputStream);

            // 3. Validation (The new part)
            if (media == null) { SendResponse(context, 400, "Empty Body"); return Task.CompletedTask; }
            if (string.IsNullOrWhiteSpace(media.Title)) { SendResponse(context, 400, "Title is required"); return Task.CompletedTask; }
            if (media.ReleaseYear < 1900 || media.ReleaseYear > 2100) { SendResponse(context, 400, "Invalid Year"); return Task.CompletedTask; }

            // 4. Set Creator and Save
            media.CreatorId = user.Id;
            int newId = _mediaRepo.CreateMedia(media);

            SendResponse(context, 201, new { Message = "Media created", Id = newId });
            return Task.CompletedTask;
        }

        // Replace existing GetMedia with this one that handles query strings
        public Task GetMedia(HttpListenerContext context)
        {
            var user = CheckAuth(context);
            if (user == null) { SendResponse(context, 401); return Task.CompletedTask; }

            var query = context.Request.QueryString;

            // 1. Check if this is actually a Recommendation request masquerading as a media get
            // (This handles the case if you use query params for mode, though the Router usually handles /recommendations separately)
            if (query["mode"] == "recommendations")
            {
                string recType = query["type"] ?? "genre";
                var recs = _mediaRepo.GetRecommendations(user.Id, recType);
                SendResponse(context, 200, recs);
                return Task.CompletedTask;
            }

            // 2. Extract Query Parameters (Matching Postman Collection keys)
            string? title = query["title"];        // Postman key: title
            string? genre = query["genre"];        // Postman key: genre
            string? type = query["mediaType"];     // Postman key: mediaType
            string? sort = query["sortBy"];        // Postman key: sortBy

            // Parse numbers safely
            int? year = int.TryParse(query["releaseYear"], out int y) ? y : null;       // Postman key: releaseYear
            int? age = int.TryParse(query["ageRestriction"], out int a) ? a : null;     // Postman key: ageRestriction
            int? minRating = int.TryParse(query["rating"], out int r) ? r : null;       // Postman key: rating

            // 3. Call Repository
            var list = _mediaRepo.GetMediaFiltered(title, genre, type, year, age, minRating, sort);
            SendResponse(context, 200, list);
            return Task.CompletedTask;
        }

        public Task GetMediaById(HttpListenerContext context, int id)
        {
            var user = CheckAuth(context);
            if (user == null)
            {
                SendResponse(context, 401, "Unauthorized");
                return Task.CompletedTask;
            }

            var media = _mediaRepo.GetMediaById(id);
            if (media == null) SendResponse(context, 404, "Not Found");
            else SendResponse(context, 200, media);
            return Task.CompletedTask;
        }

        public Task UpdateMedia(HttpListenerContext context, int id)
        {
            var user = CheckAuth(context);
            if (user == null)
            {
                SendResponse(context, 401, "Unauthorized");
                return Task.CompletedTask;
            }

            var existing = _mediaRepo.GetMediaById(id);
            if (existing == null) { SendResponse(context, 404); return Task.CompletedTask; }

            if (existing.CreatorId != user.Id) { SendResponse(context, 403, "Only creator can edit"); return Task.CompletedTask; }

            var updateData = Deserialize<MediaEntry>(context.Request.InputStream);
            if (updateData != null)
            {
                updateData.Id = id;
                _mediaRepo.UpdateMedia(updateData);
                SendResponse(context, 200, "Updated");
            }
            else
            {
                SendResponse(context, 400, "Invalid Data");
            }
            return Task.CompletedTask;
        }

        public Task DeleteMedia(HttpListenerContext context, int id)
        {
            var user = CheckAuth(context);
            if (user == null)
            {
                SendResponse(context, 401, "Unauthorized");
                return Task.CompletedTask;
            }

            var existing = _mediaRepo.GetMediaById(id);
            if (existing == null) { SendResponse(context, 404); return Task.CompletedTask; }

            if (existing.CreatorId != user.Id) { SendResponse(context, 403, "Only creator can delete"); return Task.CompletedTask; }

            _mediaRepo.DeleteMedia(id);
            SendResponse(context, 204, null); // 204 No Content for Delete is standard, body is null
            return Task.CompletedTask;
        }

        public Task GetRecommendations(HttpListenerContext context, int userId)
        {
            var user = CheckAuth(context);
            // Ensure user matches requested ID
            if (user == null || user.Id != userId) { SendResponse(context, 401); return Task.CompletedTask; }

            string type = context.Request.QueryString["type"] ?? "genre"; // Default to genre
            var recs = _mediaRepo.GetRecommendations(userId, type);
            SendResponse(context, 200, recs);
            return Task.CompletedTask;
        }
    }
}