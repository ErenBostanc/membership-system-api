using MembershipSystem.API.Data;
using MembershipSystem.API.Models;

namespace MembershipSystem.API.Services
{
    public class MembershipReminderService
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private readonly VippsService _vippsService;

        public MembershipReminderService(
            AppDbContext context,
            EmailService emailService,
            VippsService vippsService)
        {
            _context = context;
            _emailService = emailService;
            _vippsService = vippsService;
        }

        public void CheckExpiringMemberships()
        {
            var today = DateTime.Today;

            var members = _context.Members
                .Where(m => !m.IsDeleted)
                .ToList();

            foreach (var member in members)
            {
                var daysLeft = (member.EndDate - today).Days;

                // ödeme yapılmış mı kontrol
                var paid = _context.Payments.Any(p =>
                    p.MemberId == member.Id &&
                    p.PaymentYear == today.Year + 1 &&
                    p.PaymentDate != null);

                if (paid)
                    continue;

                var isTestUser =
                        member.Email == "eren.bstnc.eb@gmail.com";

                // 1️⃣ 30 gün kala
                if (daysLeft <= 30 && member.ReminderCount == 0 || isTestUser)
                {
                    SendReminder(member);
                    member.ReminderCount = 1;
                    member.LastReminderSent = DateTime.Now;
                }

                // 2️⃣ 7 gün kala
                else if (daysLeft <= 7 && member.ReminderCount == 1 || isTestUser)
                {
                    SendReminder(member);
                    member.ReminderCount = 2;
                    member.LastReminderSent = DateTime.Now;
                }

                // 3️⃣ Expired ise status güncelle
                if (member.EndDate < today)
                {
                    member.Status = "Expired";
                }
            }

            _context.SaveChanges();
        }

        private void SendReminder(Member member)
        {
            var vippsLink =
                _vippsService.CreatePaymentLink(member.Id).Result;

                if (vippsLink == "ALREADY_PAID")
                    return;

            _emailService.SendEmail(
                member.Email,
                "Membership Renewal Reminder",
                $@"Your membership expires soon.<br><br>

<a href='{vippsLink}' 
style='background:#ff5b24;color:white;padding:12px 20px;text-decoration:none;border-radius:6px;display:inline-block;'>
Renew Membership
</a>");
        }
    }
}