using System.Text.Json.Serialization;

namespace MembershipSystem.API.Models
{
    public class Payment
    {
        public int Id { get; set; }

        public int MemberId { get; set; }

        public decimal Amount { get; set; }

        public int PaymentYear { get; set; }

        public DateTime? PaymentDate { get; set; }

        public string PaymentReference { get; set; }

        public string Provider { get; set; } = "Vipps";
        
        [JsonIgnore]
        public Member? Member { get; set; }
    }
}