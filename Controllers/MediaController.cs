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
            var user = CheckAuth(context);
            if (user == null)
            {
                SendResponse(context, 401, "Unauthorized");
                return Task.CompletedTask;
            }

            var media = Deserialize<MediaEntry>(context.Request.InputStream);
            if (media == null)
            {
                SendResponse(context, 400, "Invalid Data");
                return Task.CompletedTask;
            }

            media.CreatorId = user.Id;
            _mediaRepo.CreateMedia(media);
            SendResponse(context, 201, "Media created");
            return Task.CompletedTask;
        }

        public Task GetMedia(HttpListenerContext context)
        {
            var user = CheckAuth(context);
            if (user == null)
            {
                SendResponse(context, 401, "Unauthorized");
                return Task.CompletedTask;
            }

            var query = context.Request.QueryString;
            string? title = query["title"];
            string? genre = query["genre"];
            string? mediaType = query["mediaType"] ?? query["mediatype"];
            int? releaseYear = null;
            if (int.TryParse(query["releaseYear"], out int ry)) releaseYear = ry;
            int? ageRestriction = null;
            if (int.TryParse(query["ageRestriction"], out int ar)) ageRestriction = ar;
            double? minRating = null;
            if (double.TryParse(query["minRating"], out double mr)) minRating = mr;
            string? sortBy = query["sortBy"];

            var list = _mediaRepo.GetMediaFiltered(title, genre, mediaType, releaseYear, ageRestriction, minRating, sortBy);
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
    }
}