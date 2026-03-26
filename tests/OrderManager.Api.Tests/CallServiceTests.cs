using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.DTOs;
using OrderManager.Api.Services;

namespace OrderManager.Api.Tests;

public class CallServiceTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        SeedData.Initialize(context);
        return context;
    }

    [Fact]
    public async Task InitiateCall_CreatesCallLog()
    {
        using var context = CreateContext();
        var service = new CallService(context);
        var caller = await context.AppUsers.FirstAsync(u => u.Username == "alice");
        var receiver = await context.AppUsers.FirstAsync(u => u.Username == "bob");

        var call = await service.InitiateCallAsync(caller.Id, new InitiateCallRequest
        {
            ReceiverId = receiver.Id,
            CallType = "Audio"
        });

        Assert.NotNull(call);
        Assert.Equal("Audio", call.CallType);
        Assert.Equal(caller.Id, call.Caller.Id);
        Assert.Equal(receiver.Id, call.Receiver.Id);
    }

    [Fact]
    public async Task InitiateCall_ThrowsForNonExistentReceiver()
    {
        using var context = CreateContext();
        var service = new CallService(context);
        var caller = await context.AppUsers.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.InitiateCallAsync(caller.Id, new InitiateCallRequest
            {
                ReceiverId = 99999,
                CallType = "Audio"
            }));
    }

    [Fact]
    public async Task EndCall_SetsEndTimeAndDuration()
    {
        using var context = CreateContext();
        var service = new CallService(context);
        var caller = await context.AppUsers.FirstAsync(u => u.Username == "alice");
        var receiver = await context.AppUsers.FirstAsync(u => u.Username == "bob");

        var call = await service.InitiateCallAsync(caller.Id, new InitiateCallRequest
        {
            ReceiverId = receiver.Id,
            CallType = "Video"
        });

        var ended = await service.EndCallAsync(call.Id, caller.Id);
        Assert.NotNull(ended);
        Assert.NotNull(ended!.EndedAt);
    }

    [Fact]
    public async Task GetCallHistory_ReturnsCallsForUser()
    {
        using var context = CreateContext();
        var service = new CallService(context);
        var caller = await context.AppUsers.FirstAsync(u => u.Username == "alice");
        var receiver = await context.AppUsers.FirstAsync(u => u.Username == "bob");

        await service.InitiateCallAsync(caller.Id, new InitiateCallRequest
        {
            ReceiverId = receiver.Id,
            CallType = "Audio"
        });

        var history = await service.GetCallHistoryAsync(caller.Id, 1, 20);
        Assert.NotEmpty(history);
    }

    [Fact]
    public async Task GetContacts_ReturnsSeededContacts()
    {
        using var context = CreateContext();
        var service = new CallService(context);
        var alice = await context.AppUsers.FirstAsync(u => u.Username == "alice");

        var contacts = await service.GetContactsAsync(alice.Id);
        Assert.Equal(2, contacts.Count);
    }

    [Fact]
    public async Task AddContact_CreatesNewContact()
    {
        using var context = CreateContext();
        var service = new CallService(context);
        var bob = await context.AppUsers.FirstAsync(u => u.Username == "bob");

        var contact = await service.AddContactAsync(bob.Id, new AddContactRequest
        {
            Username = "charlie",
            DisplayName = "My Friend Charlie"
        });

        Assert.NotNull(contact);
        Assert.Equal("My Friend Charlie", contact.DisplayName);
    }

    [Fact]
    public async Task AddContact_ThrowsForSelf()
    {
        using var context = CreateContext();
        var service = new CallService(context);
        var alice = await context.AppUsers.FirstAsync(u => u.Username == "alice");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddContactAsync(alice.Id, new AddContactRequest
            {
                Username = "alice"
            }));
    }

    [Fact]
    public async Task AddContact_ThrowsForDuplicate()
    {
        using var context = CreateContext();
        var service = new CallService(context);
        var alice = await context.AppUsers.FirstAsync(u => u.Username == "alice");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddContactAsync(alice.Id, new AddContactRequest
            {
                Username = "bob"
            }));
    }

    [Fact]
    public async Task RemoveContact_DeletesContact()
    {
        using var context = CreateContext();
        var service = new CallService(context);
        var alice = await context.AppUsers.FirstAsync(u => u.Username == "alice");

        var contacts = await service.GetContactsAsync(alice.Id);
        var contactToRemove = contacts.First();

        var result = await service.RemoveContactAsync(alice.Id, contactToRemove.Id);
        Assert.True(result);

        var updatedContacts = await service.GetContactsAsync(alice.Id);
        Assert.Equal(contacts.Count - 1, updatedContacts.Count);
    }
}
