using System;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class MessageRepository(DataContext context, IMapper mapper) : IMessageRepository
{
     
    private DbSet<Message> Messages => context.Messages ?? throw new InvalidOperationException("Messages DbSet is not initialized.");
    public void AddMessage(Message message)
    {
        context.Messages?.Add(message);
    }

    public void DeleteMessage(Message message)
    {
        context.Messages?.Remove(message);
    }
    

    public async Task<PagedList<MessageDto>> GetMesageForUser(MessageParams messageParams)
    {
           if (messageParams.PageNumber <= 0 || messageParams.PageSize <= 0)
            {
                throw new ArgumentException("Invalid paging parameters.");
            }
        var query = Messages    
            .OrderByDescending(x => x.MessageSent)
            .AsQueryable();

        query = messageParams.Container switch
        {
            "Inbox" => query.Where(x => x.Recipient.UserName == messageParams.UserName && x.RecipientDeleted == false),
            "Outbox" => query.Where(x => x.Sender.UserName == messageParams.UserName && x.SenderDeleted == false),
            _=> query.Where(x => x.Recipient.UserName == messageParams.UserName && x.DateRead == null && x.RecipientDeleted == false)
        };

        var messages = query.ProjectTo<MessageDto>(mapper.ConfigurationProvider);

        return await PagedList<MessageDto>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
    }

    public async Task<Message?> GetMessage(int id)
    {
        return await Messages.FindAsync(id);
    }

    public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, string recipientUsername)
    {
       var messages = await Messages
            .Include(x => x.Sender).ThenInclude(x => x.Photos)
            .Include(x => x.Recipient).ThenInclude(x => x.Photos)
            .Where(x => 
                x.RecipientUsername == currentUsername && x.RecipientDeleted == false && x.SenderUsername == recipientUsername ||
                x.SenderUsername == currentUsername && x.SenderDeleted == false && x.RecipientUsername == recipientUsername
            )
            .OrderBy(x => x.MessageSent)
            .ToListAsync();

        var unreadMessages = messages.Where( x => x.DateRead == null && x.RecipientUsername == currentUsername).ToList();

        if (unreadMessages.Count != 0)
        {
            unreadMessages.ForEach(x => x.DateRead = DateTime.UtcNow);
            await context.SaveChangesAsync();
        }

        return mapper.Map<IEnumerable<MessageDto>>(messages);
    }
 
    public async Task<bool> SaveAllAsync()
    {
        return await context.SaveChangesAsync() > 0;
    }
}
