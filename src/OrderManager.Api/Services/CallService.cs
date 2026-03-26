using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.DTOs;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

public class CallService
{
    private readonly AppDbContext _context;

    public CallService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CallLogDto> InitiateCallAsync(int callerId, InitiateCallRequest request)
    {
        var receiver = await _context.AppUsers.FindAsync(request.ReceiverId);
        if (receiver == null)
            throw new InvalidOperationException("Receiver not found");

        var callType = Enum.TryParse<CallType>(request.CallType, true, out var ct) ? ct : CallType.Audio;

        var callLog = new CallLog
        {
            CallerId = callerId,
            ReceiverId = request.ReceiverId,
            StartedAt = DateTime.UtcNow,
            Status = receiver.IsOnline ? CallStatus.Answered : CallStatus.Queued,
            CallType = callType
        };

        _context.CallLogs.Add(callLog);
        await _context.SaveChangesAsync();

        return await MapToCallLogDto(callLog);
    }

    public async Task<CallLogDto?> EndCallAsync(int callId, int userId)
    {
        var call = await _context.CallLogs
            .FirstOrDefaultAsync(c => c.Id == callId && (c.CallerId == userId || c.ReceiverId == userId));

        if (call == null) return null;

        call.EndedAt = DateTime.UtcNow;
        call.DurationSeconds = (int)(call.EndedAt.Value - call.StartedAt).TotalSeconds;
        if (call.Status == CallStatus.Queued)
            call.Status = CallStatus.Missed;

        await _context.SaveChangesAsync();
        return await MapToCallLogDto(call);
    }

    public async Task<List<CallLogDto>> GetCallHistoryAsync(int userId, int page, int pageSize)
    {
        var calls = await _context.CallLogs
            .Include(c => c.Caller)
            .Include(c => c.Receiver)
            .Where(c => c.CallerId == userId || c.ReceiverId == userId)
            .OrderByDescending(c => c.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return calls.Select(c => new CallLogDto
        {
            Id = c.Id,
            Caller = AuthService.MapToUserDto(c.Caller),
            Receiver = AuthService.MapToUserDto(c.Receiver),
            StartedAt = c.StartedAt,
            EndedAt = c.EndedAt,
            DurationSeconds = c.DurationSeconds,
            Status = c.Status.ToString(),
            CallType = c.CallType.ToString()
        }).ToList();
    }

    public async Task<List<ContactDto>> GetContactsAsync(int userId)
    {
        var contacts = await _context.Contacts
            .Include(c => c.ContactUser)
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.DisplayName)
            .ToListAsync();

        return contacts.Select(c => new ContactDto
        {
            Id = c.Id,
            ContactUserId = c.ContactUserId,
            DisplayName = c.DisplayName ?? c.ContactUser.DisplayName,
            Username = c.ContactUser.Username,
            AvatarUrl = c.ContactUser.AvatarUrl,
            IsOnline = c.ContactUser.IsOnline,
            LastSeen = c.ContactUser.LastSeen
        }).ToList();
    }

    public async Task<ContactDto> AddContactAsync(int userId, AddContactRequest request)
    {
        var contactUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (contactUser == null)
            throw new InvalidOperationException("User not found");

        if (contactUser.Id == userId)
            throw new InvalidOperationException("Cannot add yourself as a contact");

        var existing = await _context.Contacts
            .AnyAsync(c => c.UserId == userId && c.ContactUserId == contactUser.Id);
        if (existing)
            throw new InvalidOperationException("Contact already exists");

        var contact = new Contact
        {
            UserId = userId,
            ContactUserId = contactUser.Id,
            DisplayName = request.DisplayName ?? contactUser.DisplayName,
            CreatedAt = DateTime.UtcNow
        };

        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        return new ContactDto
        {
            Id = contact.Id,
            ContactUserId = contactUser.Id,
            DisplayName = contact.DisplayName,
            Username = contactUser.Username,
            AvatarUrl = contactUser.AvatarUrl,
            IsOnline = contactUser.IsOnline,
            LastSeen = contactUser.LastSeen
        };
    }

    public async Task<bool> RemoveContactAsync(int userId, int contactId)
    {
        var contact = await _context.Contacts
            .FirstOrDefaultAsync(c => c.Id == contactId && c.UserId == userId);

        if (contact == null) return false;

        _context.Contacts.Remove(contact);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<CallLogDto> MapToCallLogDto(CallLog call)
    {
        var caller = await _context.AppUsers.FindAsync(call.CallerId);
        var receiver = await _context.AppUsers.FindAsync(call.ReceiverId);

        return new CallLogDto
        {
            Id = call.Id,
            Caller = AuthService.MapToUserDto(caller!),
            Receiver = AuthService.MapToUserDto(receiver!),
            StartedAt = call.StartedAt,
            EndedAt = call.EndedAt,
            DurationSeconds = call.DurationSeconds,
            Status = call.Status.ToString(),
            CallType = call.CallType.ToString()
        };
    }
}
