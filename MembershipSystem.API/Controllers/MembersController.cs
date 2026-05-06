using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MembershipSystem.API.Data;
using MembershipSystem.API.Models;
using Microsoft.IdentityModel.Tokens;
using MembershipSystem.API.Services;


namespace MembershipSystem.API.Controllers
{
    [ApiController]
    [Route("api/members")]
    [Authorize]
    public class MembersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        private readonly VippsService _vippsService;

public MembersController(
    AppDbContext context,
    VippsService vippsService,
    EmailService emailService)
{
    _context = context;
    _vippsService = vippsService;
    _emailService = emailService;
}

        [HttpGet]
        public IActionResult GetMembers(
            string? status = null,
            bool deleted = false)
        {
            var query = _context.Members.AsQueryable();

            query = query.Where(x => x.IsDeleted == deleted);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(x => x.Status == status);
            }

            return Ok(query.ToList());
        }

        [AllowAnonymous]
[HttpPost]
public async Task<IActionResult> Create(CreateMemberRequest request)
{
    var member = new Member
    {
        FullName = request.FullName,
        Email = request.Email,
        PhoneNumber = request.PhoneNumber,
        Kommune = request.Kommune,
        Adresse = request.Adresse,
        Fodselsdato = request.Fodselsdato,
        StartDate = DateTime.Now,
        EndDate = DateTime.Now,
        Status = "Pending",
        IsDeleted = false
    };

    _context.Members.Add(member);
    _context.SaveChanges();

    try
    {
        var vippsLink = await _vippsService.CreatePaymentLink(member.Id);
        if (vippsLink != "ALREADY_PAID")
        {
            _emailService.SendEmail(
                member.Email,
                "Betalingslenke for medlemskap – MentorUng Agder",
                $@"<!DOCTYPE html>
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
        <tr><td style='background:#C0392B;height:4px;'></td></tr>
        <tr>
          <td style='padding:40px 40px 20px 40px;'>
            <h2 style='color:#1A2F5F;margin:0 0 16px 0;'>Hei, {member.FullName}!</h2>
            <p style='color:#444;font-size:16px;line-height:1.6;'>
              Takk for at du registrerte deg som medlem i <strong>MentorUng Agder</strong>!
            </p>
            <p style='color:#444;font-size:16px;line-height:1.6;'>
              For å fullføre registreringen, vennligst betal medlemskontingenten via Vipps.
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
            <p style='color:#ffffff;font-size:14px;margin:0;'>Mvh, <strong>MentorUng Agder</strong></p>
            <p style='color:#aabbcc;font-size:12px;margin:8px 0 0 0;'>
              <a href='https://mentorung.no' style='color:#aabbcc;'>mentorung.no</a>
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Mail error: " + ex.Message);
    }

    return Ok(member);
}

        [HttpPut("{id}")]
        public IActionResult Update(int id, CreateMemberRequest request)
        {
            var member = _context.Members.Find(id);

            if (member == null)
                return NotFound();

            member.FullName = request.FullName;
            member.Email = request.Email;
            member.PhoneNumber = request.PhoneNumber;
            member.Kommune = request.Kommune;
            member.Adresse = request.Adresse;
            member.Fodselsdato = request.Fodselsdato;

            _context.SaveChanges();

            return Ok(member);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var member = _context.Members.Find(id);

            if (member == null)
                return NotFound();

            member.IsDeleted = true;
            _context.SaveChanges();

            return Ok();
        }

        [HttpGet("expiring-soon")]
        public IActionResult ExpiringSoon()
        {
            var soon = _context.Members
                .Where(x => x.EndDate <= DateTime.Now.AddDays(7) && x.EndDate >= DateTime.Now && !x.IsDeleted)
                .ToList();

            return Ok(soon);
        }

        [AllowAnonymous]
        [HttpGet("payment-result")]
        [Produces("text/html")]
        public async Task<IActionResult> PaymentResult(int memberId, string reference)
        {
            var payment = _context.Payments
                .FirstOrDefault(p => p.PaymentReference == reference);

            if (payment == null)
                return NotFound("Payment not found");

            if (payment.PaymentDate != null)
                return Content(GetSuccessHtml(), "text/html");

            var callbackRequest = new VippsCallbackRequest { Reference = reference };

            var vippsStatus = await _vippsService.GetPaymentStatus(reference);

            if (vippsStatus == "CAPTURED" || vippsStatus == "AUTHORIZED")
            {
                payment.PaymentDate = DateTime.Now;

                var member = _context.Members
                    .FirstOrDefault(m => m.Id == memberId);

                if (member != null)
                {
                    if (member.EndDate < DateTime.Today)
                        member.EndDate = DateTime.Today.AddYears(1);
                    else
                        member.EndDate = member.EndDate.AddYears(1);

                    member.Status = "Active";
                    member.ReminderCount = 0;
                    member.LastReminderSent = null;
                }

                _context.SaveChanges();

                return Content(GetSuccessHtml(), "text/html");
            }

            return Content(GetPendingHtml(), "text/html");
        }

        private string GetSuccessHtml()
        {
            return @"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Payment Successful</title>
                <style>
                    body { font-family: Arial, sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; background: #f5f5f5; }
                    .box { background: white; padding: 40px; border-radius: 12px; text-align: center; box-shadow: 0 2px 12px rgba(0,0,0,0.1); }
                    h1 { color: #2e7d32; }
                    p { color: #555; }
                </style>
            </head>
            <body>
                <div class='box'>
                    <h1>✅ Payment Successful!</h1>
                    <p>Your membership has been renewed successfully.</p>
                    <p>You will receive a confirmation email shortly.</p>
                </div>
            </body>
            </html>";
        }

        private string GetPendingHtml()
        {
            return @"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Payment Processing</title>
                <style>
                    body { font-family: Arial, sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; background: #f5f5f5; }
                    .box { background: white; padding: 40px; border-radius: 12px; text-align: center; box-shadow: 0 2px 12px rgba(0,0,0,0.1); }
                    h1 { color: #f57c00; }
                    p { color: #555; }
                </style>
            </head>
            <body>
                <div class='box'>
                    <h1>⏳ Payment Processing...</h1>
                    <p>Your payment is being processed.</p>
                    <p>Please wait a moment and refresh the page.</p>
                </div>
            </body>
            </html>";
        }
    }
}