using Microsoft.EntityFrameworkCore;
using OrderManager.Api.Data;
using OrderManager.Api.DTOs;
using OrderManager.Api.Models;

namespace OrderManager.Api.Services;

public class ReelService
{
    private readonly AppDbContext _context;

    public ReelService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ReelFeedResponse> GetFeedAsync(int page, int pageSize, int? currentUserId)
    {
        var query = _context.Reels
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var reels = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var likedReelIds = new HashSet<int>();
        if (currentUserId.HasValue)
        {
            var reelIds = reels.Select(r => r.Id).ToList();
            likedReelIds = (await _context.ReelLikes
                .Where(rl => rl.UserId == currentUserId.Value && reelIds.Contains(rl.ReelId))
                .Select(rl => rl.ReelId)
                .ToListAsync()).ToHashSet();
        }

        return new ReelFeedResponse
        {
            Reels = reels.Select(r => MapToReelDto(r, likedReelIds.Contains(r.Id))).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            HasMore = (page * pageSize) < totalCount
        };
    }

    public async Task<ReelDto?> GetByIdAsync(int id, int? currentUserId)
    {
        var reel = await _context.Reels
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reel == null) return null;

        reel.ViewCount++;
        await _context.SaveChangesAsync();

        var isLiked = currentUserId.HasValue &&
            await _context.ReelLikes.AnyAsync(rl => rl.ReelId == id && rl.UserId == currentUserId.Value);

        return MapToReelDto(reel, isLiked);
    }

    public async Task<ReelDto> CreateAsync(CreateReelRequest request, int userId, string videoUrl, string? thumbnailUrl, int durationSeconds, long fileSizeBytes)
    {
        var reel = new Reel
        {
            Title = request.Title,
            Description = request.Description,
            VideoUrl = videoUrl,
            ThumbnailUrl = thumbnailUrl,
            DurationSeconds = durationSeconds,
            UserId = userId,
            Tags = request.Tags,
            IsDownloadable = request.IsDownloadable,
            FileSizeBytes = fileSizeBytes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reels.Add(reel);
        await _context.SaveChangesAsync();

        var reelWithUser = await _context.Reels
            .Include(r => r.User)
            .FirstAsync(r => r.Id == reel.Id);

        return MapToReelDto(reelWithUser, false);
    }

    public async Task<bool> ToggleLikeAsync(int reelId, int userId)
    {
        var existingLike = await _context.ReelLikes
            .FirstOrDefaultAsync(rl => rl.ReelId == reelId && rl.UserId == userId);

        var reel = await _context.Reels.FindAsync(reelId);
        if (reel == null) throw new InvalidOperationException("Reel not found");

        if (existingLike != null)
        {
            _context.ReelLikes.Remove(existingLike);
            reel.LikeCount = Math.Max(0, reel.LikeCount - 1);
            await _context.SaveChangesAsync();
            return false;
        }
        else
        {
            _context.ReelLikes.Add(new ReelLike
            {
                ReelId = reelId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
            reel.LikeCount++;
            await _context.SaveChangesAsync();
            return true;
        }
    }

    public async Task<List<ReelDto>> GetUserReelsAsync(int userId)
    {
        var reels = await _context.Reels
            .Include(r => r.User)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return reels.Select(r => MapToReelDto(r, false)).ToList();
    }

    public async Task<Reel?> GetReelEntityAsync(int id)
    {
        return await _context.Reels.FindAsync(id);
    }

    private static ReelDto MapToReelDto(Reel reel, bool isLiked)
    {
        return new ReelDto
        {
            Id = reel.Id,
            Title = reel.Title,
            Description = reel.Description,
            VideoUrl = reel.VideoUrl,
            ThumbnailUrl = reel.ThumbnailUrl,
            DurationSeconds = reel.DurationSeconds,
            ViewCount = reel.ViewCount,
            LikeCount = reel.LikeCount,
            Tags = reel.Tags,
            IsDownloadable = reel.IsDownloadable,
            FileSizeBytes = reel.FileSizeBytes,
            CreatedAt = reel.CreatedAt,
            Creator = AuthService.MapToUserDto(reel.User),
            IsLikedByCurrentUser = isLiked
        };
    }
}
