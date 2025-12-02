using System;

namespace MediaRatingsPlatform.Models
{
    public class Rating
    {
        public int Id { get; set; }
        public int Stars { get; set; } // 1-5
        public string Comment { get; set; } = string.Empty;
        public int UserId { get; set; }    // The user who rated
        public int MediaId { get; set; }   // The media being rated
        public bool IsConfirmed { get; set; } = false; // Moderation feature mentioned in spec
    }
}