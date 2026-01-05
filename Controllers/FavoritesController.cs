        private readonly FavoritesRepository _favRepo;
        private readonly MediaRepository _mediaRepo;

        public FavoritesController(UserRepository userRepo, FavoritesRepository favRepo, MediaRepository mediaRepo) : base(userRepo)
        {
            _favRepo = favRepo;
            _mediaRepo = mediaRepo;
        }

        public Task AddFavorite(HttpListenerContext context, int mediaId)
        {
            var user = CheckAuth(context);
            if (user == null) { SendResponse(context, 401, "Unauthorized"); return Task.CompletedTask; }

            var media = _mediaRepo.GetMediaById(mediaId);
            if (media == null) { SendResponse(context, 404, "Media not found"); return Task.CompletedTask; }

            _favRepo.AddFavorite(user.Id, mediaId);
            SendResponse(context, 200, "Added to favorites");
            return Task.CompletedTask;
        }

        public Task RemoveFavorite(HttpListenerContext context, int mediaId)
        {
            var user = CheckAuth(context);
            if (user == null) { SendResponse(context, 401, "Unauthorized"); return Task.CompletedTask; }

            var media = _mediaRepo.GetMediaById(mediaId);
            if (media == null) { SendResponse(context, 404, "Media not found"); return Task.CompletedTask; }

            _favRepo.RemoveFavorite(user.Id, mediaId);
            SendResponse(context, 200, "Removed from favorites");
            return Task.CompletedTask;
        }

        public Task GetUserFavorites(HttpListenerContext context)
        {
            var user = CheckAuth(context);
            if (user == null) { SendResponse(context, 401, "Unauthorized"); return Task.CompletedTask; }

            var favoriteIds = _favRepo.GetUserFavorites(user.Id);
            SendResponse(context, 200, favoriteIds);
            return Task.CompletedTask;
        }
    }
}
