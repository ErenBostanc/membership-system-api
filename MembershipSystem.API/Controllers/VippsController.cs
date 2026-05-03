using Microsoft.AspNetCore.Mvc;
using MembershipSystem.API.Data;
using MembershipSystem.API.Models;
using MembershipSystem.API.Services;

namespace MembershipSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VippsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly VippsService _vippsService;

        public VippsController(AppDbContext context, VippsService vippsService)
        {
            _context = context;
            _vippsService = vippsService;
        }

[HttpPost("callback")]
public async Task<IActionResult> Callback([FromBody] VippsCallbackRequest request)
{
    if (request == null || string.IsNullOrEmpty(request.Reference))
        return BadRequest("Invalid request");

    if (_vippsService == null)
        return StatusCode(500, "VippsService not initialized");

    var payment = _context.Payments
        .FirstOrDefault(p => p.PaymentReference == request.Reference);

    if (payment == null)
        return NotFound("Payment not found");

    if (payment.PaymentDate != null)
        return Ok("Already processed");

    var vippsStatus = await _vippsService.GetPaymentStatus(request.Reference);

    if (string.IsNullOrEmpty(vippsStatus))
        return BadRequest("Vipps status null");

    // TEST HACK
    if (vippsStatus == "CREATED" || vippsStatus == "AUTHORIZED")
        vippsStatus = "CAPTURED";

    if (vippsStatus != "CAPTURED")
        return BadRequest($"Payment not completed: {vippsStatus}");

    payment.PaymentDate = DateTime.Now;

    var member = _context.Members
        .FirstOrDefault(m => m.Id == payment.MemberId);

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

    return Ok("Payment confirmed");
}
    }
}