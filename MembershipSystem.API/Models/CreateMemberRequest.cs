namespace MembershipSystem.API.Models
{
    public class CreateMemberRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Kommune { get; set; }
        public string Adresse { get; set; }
        public DateTime Fodselsdato { get; set; }
    }
}