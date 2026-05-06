using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MembershipSystem.API.Data;
using MembershipSystem.API.Models;
using MembershipSystem.API.Services;

namespace MembershipSystem.API.Controllers
{
    [ApiController]
    [Route("api/test")]
    [Authorize]
    public class TestController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly VippsService _vippsService;
        private readonly EmailService _emailService;

        public TestController(
            AppDbContext context,
            VippsService vippsService,
            EmailService emailService)
        {
            _context = context;
            _vippsService = vippsService;
            _emailService = emailService;
        }

        [HttpPost("create-test-member")]
        public IActionResult CreateTestMember()
        {
            var member = new Member
            {
                FullName = "Test Bruker",
                Email = "eren.bstnc.eb@gmail.com",
                PhoneNumber = "12345678",
                Kommune = "Oslo",
                Adresse = "Testgata 1",
                Fodselsdato = new DateTime(1990, 1, 1),
                EndDate = DateTime.Now.AddDays(3),
                IsDeleted = false,
                Status = "Active",
                StartDate = DateTime.Now
            };

            _context.Members.Add(member);
            _context.SaveChanges();

            return Ok("Test member created");
        }

[HttpPost("{id}/send-payment-link")]
        public async Task<IActionResult> SendPaymentLink(int id)
        {
            var member = _context.Members.FirstOrDefault(m => m.Id == id);

            if (member == null)
                return NotFound();

            var vippsLink = await _vippsService.CreatePaymentLink(member.Id);

            if (vippsLink == "ALREADY_PAID")
                return BadRequest("User already paid");

            var body = $@"
        <!DOCTYPE html>
        <html>
        <head><meta charset='utf-8'></head>
        <body style='margin:0;padding:0;background:#f4f4f4;font-family:Arial,sans-serif;'>
          <table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f4f4;padding:30px 0;'>
            <tr><td align='center'>
              <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.1);'>

                <tr>
                  <td style='background:#1A2F5F;padding:30px;text-align:center;'>
                    <img src='https://mentorung.no/wp-content/uploads/2021/01/cropped-cropped-cropped-MentorUng-LOGO.png' 
                         alt='MentorUng Agder' height='60' style='display:block;margin:0 auto;'>
                  </td>
                </tr>

                <tr>
                  <td style='background:#C0392B;height:4px;'></td>
                </tr>

                <tr>
                  <td style='padding:40px 40px 20px 40px;'>
                    <h2 style='color:#1A2F5F;margin:0 0 16px 0;'>Hei, {member.FullName}!</h2>
                    <p style='color:#444;font-size:16px;line-height:1.6;'>
                      Takk for at du er en del av <strong>MentorUng Agder</strong>!
                    </p>
                    <p style='color:#444;font-size:16px;line-height:1.6;'>
                    Her er din betalingslenke for medlemskapsfornyelse. 
                      Klikk på knappen nedenfor for å betale enkelt med Vipps.
                    </p>
                  </td>
                </tr>

                <tr>
                <td style='padding:20px 40px;text-align:center;'>
                    <a href='{vippsLink}' 
                       style='background:#C0392B;color:#ffffff;padding:14px 32px;text-decoration:none;
                              border-radius:6px;font-size:16px;font-weight:bold;display:inline-block;'>
                      Betal med Vipps
                    </a>
                  </td>
                </tr>

                <tr>
                  <td style='padding:20px 40px;'>
                    <p style='color:#888;font-size:14px;line-height:1.6;'>
                      Beløp: <strong>300 kr</strong>. 
                      Har du spørsmål? Ta kontakt med oss på 
                      <a href='mailto:kontakt@mentorung.no' style='color:#1A2F5F;'>kontakt@mentorung.no</a>.
                    </p>
                  </td>
                </tr>

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
                "Betalingslenke for medlemskap – MentorUng Agder",
                body);

            return Ok("Payment link sent");
        }
    }
}