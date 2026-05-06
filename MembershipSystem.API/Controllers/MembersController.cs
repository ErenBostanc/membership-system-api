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