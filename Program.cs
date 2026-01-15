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
            // Check if we started with "test" argument
            if (args.Length > 0 && args[0] == "test")
            {
                // Run the manual test runner
                Tests.RunAllTests();
                return; // Exit after tests
            }
            Console.WriteLine("Starting MRP Server...");

            // 1. Setup DB
            // Change these credentials to match your local PostgreSQL setup
            string connString = "Host=localhost;Username=postgres;Password=admin;Database=MediaRatingsPlatform";
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
            var ratingRepo = new RatingRepository(db);
            var favRepo = new FavoritesRepository(db);

            var userController = new UserController(userRepo);
            var mediaController = new MediaController(userRepo, mediaRepo);
            var ratingController = new RatingController(userRepo, ratingRepo);
            var favoritesController = new FavoritesController(userRepo, favRepo);

            // 3. Setup Router & Server
            var router = new Router(userController, mediaController, ratingController, favoritesController);
            var server = new HttpServer(new[] { "http://localhost:8080/" }, router);

            // 4. Run
            server.Start();

            Console.WriteLine("Press Enter to stop...");
            Console.ReadLine();
        }
    }
}