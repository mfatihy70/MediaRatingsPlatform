using System.Collections.Generic;

namespace MediaRatingsPlatform.Models
{
    public class MediaEntry
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public int ReleaseYear { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
        public int AgeRestriction { get; set; }
        public int CreatorId { get; set; }

        // Calculated fields (not stored directly in media table, but fetched via SQL)
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
    }
}