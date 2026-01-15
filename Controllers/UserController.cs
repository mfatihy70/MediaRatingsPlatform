using MediaRatingsPlatform.Data;
using MediaRatingsPlatform.Models;
using System.Net;
using System.Threading.Tasks;

namespace MediaRatingsPlatform.Controllers
{
    public class UserController : BaseController
    {
        public UserController(UserRepository repo) : base(repo) { }

        public Task Register(HttpListenerContext context)
        {
            var data = Deserialize<User>(context.Request.InputStream);

            // Check for null data
            if (data == null || string.IsNullOrEmpty(data.Username) || string.IsNullOrEmpty(data.Password))
            {
                SendResponse(context, 400, "Missing credentials");
                return Task.CompletedTask;
            }

            try
            {
                _userRepo.CreateUser(data.Username, data.Password);
                SendResponse(context, 201, "User registered");
            }
            catch
            {
                SendResponse(context, 409, "User already exists");
            }
            return Task.CompletedTask;
        }

        public Task Login(HttpListenerContext context)
        {
            var data = Deserialize<User>(context.Request.InputStream);
            if (data == null)
            {
                SendResponse(context, 400, "Invalid Body");
                return Task.CompletedTask;
            }

            var user = _userRepo.GetUserByCredentials(data.Username, data.Password);

            if (user != null)
            {
                string token = $"{user.Username}-mrpToken";
                _userRepo.UpdateToken(user.Id, token);
                SendResponse(context, 200, new { token });
            }
            else
            {
                SendResponse(context, 401, "Invalid credentials");
            }
            return Task.CompletedTask;
        }

        public Task GetProfile(HttpListenerContext context, string username)
        {
            // Auth check optional for viewing public profiles, but enforced by Router usually
            var profile = _userRepo.GetUserProfile(username);
            SendResponse(context, 200, profile);
            return Task.CompletedTask;
        }

        public Task GetLeaderboard(HttpListenerContext context)
        {
            var board = _userRepo.GetLeaderboard();
            SendResponse(context, 200, board);
            return Task.CompletedTask;
        }

        // Add/Update these methods in UserController
        public Task GetProfileById(HttpListenerContext context, int id)
        {
            var profile = _userRepo.GetUserProfileById(id);
            if (profile == null) SendResponse(context, 404, "User not found");
            else SendResponse(context, 200, profile);
            return Task.CompletedTask;
        }

        public Task UpdateProfile(HttpListenerContext context, int id)
        {
            var user = CheckAuth(context);
            if (user == null || user.Id != id) { SendResponse(context, 401); return Task.CompletedTask; }

            var data = Deserialize<UserProfileUpdateDto>(context.Request.InputStream);

            // FIX 1: Explicit null check for 'data'
            if (data == null)
            {
                SendResponse(context, 400, "Invalid Data");
                return Task.CompletedTask;
            }

            _userRepo.UpdateUserProfile(id, data.Email, data.FavoriteGenre);
            SendResponse(context, 200, "Profile updated");
            return Task.CompletedTask;
        }

        public Task GetUserRatingHistory(HttpListenerContext context, int id)
        {
            // Usually public info, but can restrict if needed.
            var history = _userRepo.GetUserRatingHistory(id);
            SendResponse(context, 200, history);
            return Task.CompletedTask;
        }

        // Helper Class (Put in User.cs or similar)
        public class UserProfileUpdateDto
        {
            public string Email { get; set; } = string.Empty;
            public string FavoriteGenre { get; set; } = string.Empty;
        }
    }
}