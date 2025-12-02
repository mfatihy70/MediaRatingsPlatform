using MediaRatingsPlatform.Data;
using MediaRatingsPlatform.Http;
using MediaRatingsPlatform.Controllers;
using System;

namespace MediaRatingsPlatform
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting MRP Server...");

            // 1. Setup DB
            // Change these credentials to match your local PostgreSQL setup
            string connString = "Host=localhost;Username=postgres;Password=admin;Database=";
            var db = new Database(connString);

            try
            {
                db.Initialize();
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB Init Failed: " + ex.Message);
                return;
            }

            // 2. Setup Dependencies
            var userRepo = new UserRepository(db);
            var mediaRepo = new MediaRepository(db);

            var userController = new UserController(userRepo);
            var mediaController = new MediaController(userRepo, mediaRepo);

            // 3. Setup Router & Server
            var router = new Router(userController, mediaController);
            var server = new HttpServer(new[] { "http://localhost:8080/" }, router);

            // 4. Run
            server.Start();

            Console.WriteLine("Press Enter to stop...");
            Console.ReadLine();
        }
    }
}