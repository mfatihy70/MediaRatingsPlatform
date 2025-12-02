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

        public Router(UserController userController, MediaController mediaController)
        {
            _userController = userController;
            _mediaController = mediaController;
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
                if (path == "/api/users/register" && method == "POST")
                    await _userController.Register(context);
                else if (path == "/api/users/login" && method == "POST")
                    await _userController.Login(context);

                // Media Routes
                else if (path == "/api/media" && method == "POST")
                    await _mediaController.CreateMedia(context);
                else if (path == "/api/media" && method == "GET")
                    await _mediaController.GetMedia(context);
                else if (path != null && path.StartsWith("/api/media/") && method == "GET")
                    await _mediaController.GetMediaById(context, GetIdFromPath(path));
                else if (path != null && path.StartsWith("/api/media/") && method == "PUT")
                    await _mediaController.UpdateMedia(context, GetIdFromPath(path));
                else if (path != null && path.StartsWith("/api/media/") && method == "DELETE")
                    await _mediaController.DeleteMedia(context, GetIdFromPath(path));
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
    }
}