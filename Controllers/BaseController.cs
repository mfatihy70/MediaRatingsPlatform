using MediaRatingsPlatform.Data;
using MediaRatingsPlatform.Models;
using Newtonsoft.Json;
using System.IO;
using System.Net;

namespace MediaRatingsPlatform.Controllers
{
    public class BaseController
    {
        protected UserRepository _userRepo;

        public BaseController(UserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        // Return nullable T?
        protected T? Deserialize<T>(Stream stream)
        {
            using var reader = new StreamReader(stream);
            string json = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<T>(json);
        }

        protected void SendResponse(HttpListenerContext context, int statusCode, object? body = null)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            if (body != null)
            {
                string json = JsonConvert.SerializeObject(body);
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(json);
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            context.Response.Close();
        }

        // Return User?
        protected User? CheckAuth(HttpListenerContext context)
        {
            string? authHeader = context.Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ")) return null;

            string token = authHeader.Substring(7);
            return _userRepo.GetUserByToken(token);
        }
    }
}