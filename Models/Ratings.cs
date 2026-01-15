using System;

namespace MediaRatingsPlatform.Models
{
    public class Rating
    {
        public int Id { get; set; }
        public int MediaId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty; // Useful for display
        public int Stars { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsConfirmed { get; set; }
        public int LikeCount { get; set; } // Calculated
    }
}