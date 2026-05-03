namespace MembershipSystem.API.Models
{
    public class Member
    {
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? LastReminderSent { get; set; }
        public int ReminderCount { get; set; }
    }
}