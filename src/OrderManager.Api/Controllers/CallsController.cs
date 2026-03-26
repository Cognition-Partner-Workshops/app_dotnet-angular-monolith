using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManager.Api.DTOs;
using OrderManager.Api.Services;

namespace OrderManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CallsController : ControllerBase
{
    private readonly CallService _callService;

    public CallsController(CallService callService)
    {
        _callService = callService;
    }

    [HttpPost("initiate")]
    public async Task<ActionResult<CallLogDto>> InitiateCall([FromBody] InitiateCallRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var call = await _callService.InitiateCallAsync(userId.Value, request);
            return Ok(call);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/end")]
    public async Task<ActionResult<CallLogDto>> EndCall(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var call = await _callService.EndCallAsync(id, userId.Value);
        if (call == null) return NotFound();
        return Ok(call);
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<CallLogDto>>> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 20;

        var history = await _callService.GetCallHistoryAsync(userId.Value, page, pageSize);
        return Ok(history);
    }

    [HttpGet("contacts")]
    public async Task<ActionResult<List<ContactDto>>> GetContacts()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var contacts = await _callService.GetContactsAsync(userId.Value);
        return Ok(contacts);
    }

    [HttpPost("contacts")]
    public async Task<ActionResult<ContactDto>> AddContact([FromBody] AddContactRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var contact = await _callService.AddContactAsync(userId.Value, request);
            return CreatedAtAction(nameof(GetContacts), contact);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("contacts/{id}")]
    public async Task<ActionResult> RemoveContact(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _callService.RemoveContactAsync(userId.Value, id);
        if (!result) return NotFound();
        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var userId))
            return userId;
        return null;
    }
}
