using Models.Base;

namespace Models.Entities
{
    public class Review : BaseEntity
    {
        public int UniversityId { get; set; }
        public University University { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int Rating { get; set; }
        public string Comment { get; set; }

        public bool IsApproved { get; set; } = false;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
