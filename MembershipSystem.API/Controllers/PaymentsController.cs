using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MembershipSystem.API.Data;

namespace MembershipSystem.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetPayments(
            int? year = null,
            int? memberId = null)
        {
            var query = _context.Payments.AsQueryable();

            if (year.HasValue)
                query = query.Where(p => p.PaymentYear == year);

            if (memberId.HasValue)
                query = query.Where(p => p.MemberId == memberId);

            return Ok(query.ToList());
        }
    }
}