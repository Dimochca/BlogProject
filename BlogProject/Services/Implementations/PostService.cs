using BlogProject.Data;
using BlogProject.Models;
using BlogProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlogProject.Services.Implementations
{
    public class PostService : IPostService
    {
        private readonly AppDbContext _context;

        public PostService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Post> GetByIdAsync(int id)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Post>> GetAllAsync()
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetByAuthorIdAsync(int authorId)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
                .Where(p => p.AuthorId == authorId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> SearchAsync(string? searchText, int? tagId)
        {
            var query = _context.Posts
                .Include(p => p.Author)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.ToLower();
                query = query.Where(p => p.Title.ToLower().Contains(searchText) ||
                                         p.Content.ToLower().Contains(searchText));
            }

            if (tagId.HasValue)
            {
                query = query.Where(p => p.PostTags.Any(pt => pt.TagId == tagId.Value));
            }

            return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task CreateAsync(Post post, List<int> tagIds)
        {
            post.CreatedAt = DateTime.UtcNow;
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            if (tagIds != null && tagIds.Any())
            {
                foreach (var tagId in tagIds)
                {
                    _context.PostTags.Add(new PostTag { PostId = post.Id, TagId = tagId });
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateAsync(Post post, List<int> tagIds)
        {
            post.UpdatedAt = DateTime.UtcNow;
            _context.Posts.Update(post);

            var existingTags = _context.PostTags.Where(pt => pt.PostId == post.Id);
            _context.PostTags.RemoveRange(existingTags);

            if (tagIds != null && tagIds.Any())
            {
                foreach (var tagId in tagIds)
                {
                    _context.PostTags.Add(new PostTag { PostId = post.Id, TagId = tagId });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var post = await GetByIdAsync(id);
            if (post != null)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
        }
        public async Task RecordViewAsync(int postId, string? userIdentifier = null)
        {
            var view = new PostView
            {
                PostId = postId,
                ViewedAt = DateTime.UtcNow,
                UserIdentifier = userIdentifier
            };
            _context.PostViews.Add(view);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Post>> GetTopPostsByViewsAsync(int count, int days = 1)
        {
            var since = DateTime.UtcNow.AddDays(-days);
            var topPosts = await _context.PostViews
                .Where(pv => pv.ViewedAt >= since)
                .GroupBy(pv => pv.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(count)
                .Join(_context.Posts.Include(p => p.Author),
                      x => x.PostId,
                      p => p.Id,
                      (x, p) => p)
                .ToListAsync();
            return topPosts;
        }
    }
}