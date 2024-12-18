using System;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class LikesRepository(DataContext context, IMapper mapper) : ILikesRepository
{
    public void AddLike(UserLike like)
    {
         if (context.Likes == null)
            throw new InvalidOperationException("Likes DbSet is not initialized.");
        context.Likes?.Add(like);
    }

    public void DeleteLike(UserLike like)
    {
        if (context.Likes == null)
            throw new InvalidOperationException("Likes DbSet is not initialized.");
       context.Likes?.Remove(like);
    }

    public async Task<IEnumerable<int>> GetCurrentUserLikeIds(int currentUserId)
    {
         if (context.Likes == null)
            throw new InvalidOperationException("Likes DbSet is not initialized.");
        return await context.Likes
            .Where(x => x.SourceUserId == currentUserId)
            .Select(x => x.TargetUserId)
            .ToListAsync();
    }

    public  async Task<UserLike?> GetUserLike(int sourceUserID, int targetUserId)
    {
         if (context.Likes == null)
            throw new InvalidOperationException("Likes DbSet is not initialized.");
       return await context.Likes.FindAsync(sourceUserID, targetUserId);
    }

    public async Task<PagedList<MemberDto>> GetUserLikes(LikesParams likesParams)
    {
        // Ensure the Likes DbSet is initialized properly
        if (context.Likes == null)
        {
         throw new InvalidOperationException("Likes DbSet is not initialized.");
        }

        var likes = context.Likes.AsQueryable();
        IQueryable<MemberDto> query;
        
    switch (likesParams.Predicate)
    {
        case "liked":
           query = likes
                .Where(x => x.SourceUserId == likesParams.UserId)
                .Select(x => x.TargetUser)
                .ProjectTo<MemberDto>(mapper.ConfigurationProvider);
            break;
        case "likedBy":
            query = likes
                .Where(x => x.TargetUserId == likesParams.UserId)
                .Select(x => x.SourceUser)
                .ProjectTo<MemberDto>(mapper.ConfigurationProvider);
            break;
        default:
            var likeIds = await GetCurrentUserLikeIds(likesParams.UserId); 
            query = likes
                .Where(x => x.TargetUserId == likesParams.UserId && likeIds.Contains(x.SourceUserId))
                .Select(x => x.SourceUser)
                .ProjectTo<MemberDto>(mapper.ConfigurationProvider);
            break;      
    }
    return await PagedList<MemberDto>.CreateAsync(query, likesParams.PageNumber, likesParams.PageSize);
}

}
