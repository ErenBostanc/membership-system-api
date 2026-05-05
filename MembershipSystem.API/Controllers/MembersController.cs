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

        [HttpPost]
public IActionResult Create(CreateMemberRequest request)
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
        EndDate = DateTime.Now.AddYears(1),
        Status = "Active",
        IsDeleted = false
    };

    _context.Members.Add(member);
    _context.SaveChanges();

    return Ok(member);
}
        [HttpPut("{id}")]
        public IActionResult Update(int id, Member updatedMember)
        {
            var member = _context.Members.Find(id);

            if (member == null)
                return NotFound();

            member.FullName = updatedMember.FullName;
            member.Email = updatedMember.Email;

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

        [HttpGet("test-email")]
        public IActionResult TestEmail()
        {
            _emailService.SendEmail(
                "eren.bstnc.eb@gmail.com",  // 👈 buraya kendi mailini yaz
                "Test Mail",
                "Bu bir test mailidir"
            );

            return Ok("Test mail gönderildi");
        }

        [HttpGet("expiring-soon")]
        public IActionResult ExpiringSoon()
        {
            var soon = _context.Members
                .Where(x => x.EndDate <= DateTime.Now.AddDays(7) && x.EndDate >= DateTime.Now && !x.IsDeleted)
                .ToList();

            return Ok(soon);
        }

        [HttpPost("send-reminder-mails")]
        public IActionResult SendReminderEmails()
        {
            var members = _context.Members
                .Where(x => x.EndDate <= DateTime.Now.AddDays(7) && x.EndDate >= DateTime.Now && !x.IsDeleted)
                .ToList();

            foreach (var member in members)
            {
                var body = $@"
        Hello {member.FullName},

        Your membership will expire on {member.EndDate:dd.MM.yyyy}.

        Please renew it in time.

        Best regards,
        Mentorung
        ";

                _emailService.SendEmail(member.Email, "Membership Expiring Soon", body);
            }

            return Ok("Reminder emails sent");
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

    _emailService.SendEmail(
        member.Email,
        "Membership Payment",
        $@"Your membership payment is ready.<br><br>

<a href='{vippsLink}' 
style='background:#ff5b24;color:white;padding:12px 20px;text-decoration:none;border-radius:6px;display:inline-block;'>
Pay with Vipps
</a>");

    return Ok("Payment link sent");
}

        [HttpPost("create-test-members")]
public IActionResult CreateTestMembers()
{
    var members = new List<Member>
    {
        new Member
        {
            FullName = "Test Expiring 1",
            Email = "eren.bstnc.eb@gmail.com",
            PhoneNumber = "12345678",
            Kommune = "Oslo",
            Adresse = "Test Street 1",
            Fodselsdato = new DateTime(1990, 1, 1),
            EndDate = DateTime.Now.AddDays(3),
            IsDeleted = false,
            Status = "Active",
            StartDate = DateTime.Now
        },
    };

    _context.Members.AddRange(members);
    _context.SaveChanges();

    return Ok("Test members created");
}

[AllowAnonymous]
[HttpGet("payment-result")]
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