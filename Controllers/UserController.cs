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
                SendResponse(context, 200, token);
            }
            else
            {
                SendResponse(context, 401, "Invalid credentials");
            }
            return Task.CompletedTask;
        }
    }
}