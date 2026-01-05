namespace MediaRatingsPlatform.Models
{
    public class UserProfile
    {
        public string Username { get; set; } = string.Empty;
        public int TotalRatings { get; set; }
        public double AverageScore { get; set; }
        public string? FavoriteGenre { get; set; }
    }
}
