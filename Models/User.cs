namespace MediaRatingsPlatform.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty; // Initialize to empty
        public string Password { get; set; } = string.Empty;
        public string? Token { get; set; }    // Nullable
    }
}