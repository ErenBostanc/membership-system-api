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

                var paid = _context.Payments.Any(p =>
                    p.MemberId == member.Id &&
                    p.PaymentYear == today.Year + 1 &&
                    p.PaymentDate != null);

                if (paid)
                    continue;

                var isTestUser =
                        member.Email == "eren.bstnc.eb@gmail.com";

                if (daysLeft <= 30 && member.ReminderCount == 0 || isTestUser)
                {
                    SendReminder(member);
                    member.ReminderCount = 1;
                    member.LastReminderSent = DateTime.Now;
                }

                else if (daysLeft <= 7 && member.ReminderCount == 1 || isTestUser)
                {
                    SendReminder(member);
                    member.ReminderCount = 2;
                    member.LastReminderSent = DateTime.Now;
                }

                if (member.EndDate < today)
                {
                    member.Status = "Expired";
                }
            }

            _context.SaveChanges();
        }

        private void SendReminder(Member member)
        {
            var vippsLink = _vippsService.CreatePaymentLink(member.Id).Result;

            if (vippsLink == "ALREADY_PAID")
                return;

            var body = $@"
        <!DOCTYPE html>
        <html>
        <head><meta charset='utf-8'></head>
        <body style='margin:0;padding:0;background:#f4f4f4;font-family:Arial,sans-serif;'>
          <table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f4f4;padding:30px 0;'>
            <tr><td align='center'>
              <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.1);'>
        
                <!-- Header -->
                <tr>
                  <td style='background:#1A2F5F;padding:30px;text-align:center;'>
                    <img src='https://mentorung.no/wp-content/uploads/2021/01/cropped-cropped-cropped-MentorUng-LOGO.png' 
                         alt='MentorUng Agder' height='60' style='display:block;margin:0 auto;'>
                  </td>
                </tr>

                <!-- Red bar -->
                <tr>
                  <td style='background:#C0392B;height:4px;'></td>
                </tr>

                <!-- Content -->
                <tr>
                  <td style='padding:40px 40px 20px 40px;'>
                    <h2 style='color:#1A2F5F;margin:0 0 16px 0;'>Hei, {member.FullName}!</h2>
                    <p style='color:#444;font-size:16px;line-height:1.6;'>
                      Vi vil minne deg om at ditt medlemskap i <strong>MentorUng Agder</strong> 
                      utløper snart den <strong>{member.EndDate:dd.MM.yyyy}</strong>.
                    </p>
                    <p style='color:#444;font-size:16px;line-height:1.6;'>
                      For å fortsette å være en del av fellesskapet vårt, ber vi deg om å fornye 
                      medlemskapet ditt så snart som mulig.
                    </p>
                  </td>
                </tr>

                <!-- Button -->
                <tr>
                  <td style='padding:20px 40px;text-align:center;'>
                    <a href='{vippsLink}' 
                    style='background:#C0392B;color:#ffffff;padding:14px 32px;text-decoration:none;
                              border-radius:6px;font-size:16px;font-weight:bold;display:inline-block;'>
                      Forny medlemskap med Vipps
                    </a>
                  </td>
                </tr>

                <!-- Info -->
                <tr>
                  <td style='padding:20px 40px;'>
                    <p style='color:#888;font-size:14px;line-height:1.6;'>
                    Medlemskapet koster <strong>300 kr</strong> per år. 
                      Har du spørsmål? Ta kontakt med oss på 
                      <a href='mailto:kontakt@mentorung.no' style='color:#1A2F5F;'>kontakt@mentorung.no</a>.
                    </p>
                  </td>
                </tr>

                <!-- Footer -->
                <tr>
                  <td style='background:#1A2F5F;padding:24px 40px;text-align:center;'>
                    <p style='color:#ffffff;font-size:14px;margin:0;'>
                      Mvh, <strong>MentorUng Agder</strong>
                    </p>
                    <p style='color:#aabbcc;font-size:12px;margin:8px 0 0 0;'>
                      <a href='https://mentorung.no' style='color:#aabbcc;'>mentorung.no</a>
                    </p>
                  </td>
                </tr>

              </table>
            </td></tr>
          </table>
        </body>
        </html>";

            _emailService.SendEmail(
                member.Email,
                "Påminnelse om medlemskapsfornyelse – MentorUng Agder",
                body);
        }
    }
}