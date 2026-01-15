public class UserProfile
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty; // Added
    public int TotalRatings { get; set; }
    public double AverageScoreGiven { get; set; }
    public string FavoriteGenre { get; set; } = "";
}