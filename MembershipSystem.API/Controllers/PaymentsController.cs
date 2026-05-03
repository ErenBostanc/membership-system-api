using Microsoft.AspNetCore.Mvc;
using MembershipSystem.API.Data;
using MembershipSystem.API.Models;

namespace MembershipSystem.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult CreatePayment(Payment payment)
        {
            var alreadyPaid = _context.Payments.Any(p =>
                p.MemberId == payment.MemberId &&
                p.PaymentYear == payment.PaymentYear &&
                p.PaymentDate != null);

            if(alreadyPaid)
            {
                return BadRequest("Membership fee already paid for this year.");
            }

            var member = _context.Members
                .FirstOrDefault(m => m.Id == payment.MemberId);

            if(member == null)
            {
                return NotFound("Member not found.");
            }

            if(payment.PaymentDate != null)
            {
                member.EndDate = member.EndDate.AddYears(1);
            }

            _context.Payments.Add(payment);

            _context.SaveChanges();

            return Ok(payment);
        }

        [HttpGet]
        public IActionResult GetPayments(
            int? year = null,
            int? memberId = null)
        {
            var query = _context.Payments.AsQueryable();

            if(year.HasValue)
                query = query.Where(p=>p.PaymentYear == year);

            if(memberId.HasValue)
                query = query.Where(p=>p.MemberId == memberId);

            return Ok(query.ToList());
        }
    }
}