using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class DatingRepository : IDatingRepository
    {
        private readonly DataContext _context;
        public DatingRepository(DataContext context)
        {
            _context = context;
        }
        public void Add<T>(T entity) where T : class
        {
            _context.Add(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            _context.Remove(entity);
        }

        public async Task<Like> GetLike(int userId, int recipientId)
        {
            // check if likes already exists
            return await _context.Likes.
                FirstOrDefaultAsync(u => u.LikerId == userId && u.LikeeId == recipientId);
        }

        public async Task<Photo> GetMainPhotoForUser(int userId)
        {
            return await _context.Photos.Where(u => u.UserID == userId)
                .FirstOrDefaultAsync(p => p.IsMain);
        }

        public async Task<Photo> GetPhoto(int id)
        {
            var photo = await _context.Photos.FirstOrDefaultAsync(p => p.Id == id);

            return photo;
        }

        public async Task<User> GetUser(int id)
        {
            var user = await _context.Users.Include(p => p.Photos).FirstOrDefaultAsync(u => u.Id == id);

            return user;
        }

        public async Task<PagedList<User>> GetUsers(UserParams userParams)
        {
            var users = _context.Users.Include(p => p.Photos)
                .OrderByDescending(u => u.LastActive)
                .Where(u => u.Id != userParams.UserId && u.Gender == userParams.Gender);

            if (userParams.Likers)
            {
                // get the user likers
                var userLikers = await GetUserLikes(userParams.UserId, userParams.Likers);
                // filter the user likers
                users = users.Where(u => userLikers.Any(liker => liker.LikerId == u.Id));
            }

            if (userParams.Likees)
            {
                // get the user likees
                var userLikees = await GetUserLikes(userParams.UserId, userParams.Likees);
                // filter the user likees
                users = users.Where(u => userLikees.Any(likees => likees.LikeeId == u.Id));
            }

            // check if min and max age is different 
            // from default value of age
            if (userParams.MinAge != 18 || userParams.MaxAge != 99) 
                users = users.Where(u => u.DateOfBirth.CalculateAge() >= userParams.MinAge && 
                                    u.DateOfBirth.CalculateAge() <= userParams.MaxAge);

            if (!string.IsNullOrEmpty(userParams.OrderBy)) 
            {
                switch (userParams.OrderBy)
                {   
                    case "created":
                        users = users.OrderByDescending(u => u.Created);
                        break;
                    default:
                        users = users.OrderByDescending(u => u.LastActive);
                        break;
                }
            }

            // return the paginated users
            return await PagedList<User>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);
        }

        public async Task<IEnumerable<Like>> GetUserLikes(int id, bool likers)
        {
            // get the users, users likes
            // and user likees
            var user = await _context.Users
                .Include(x => x.Liker)
                .Include(x => x.Likee)
                .FirstOrDefaultAsync(u => u.Id == id);

            // filter likers or likee
            if (likers)
            {
                return user.Liker.Where(u => u.LikerId == id);
            } 
            else
            {
                return user.Likee.Where(u => u.LikeeId == id);
            }
        }

        public async Task<bool> SaveAll()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _context.Messages.FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<PagedList<Message>> GetMessageForUser(MessageParams messageParams)
        {
            // get messages
            var message = _context.Messages
                .Include(u => u.Sender).ThenInclude(p => p.Photos)
                .Include(u => u.Recipient).ThenInclude(p => p.Photos)
                .AsQueryable();

            // switch between inbox, outbox and unread
            switch (messageParams.MessageContainer)
            {
                case "Inbox":
                    message = message.Where(u => u.RecipientId == messageParams.UserId && u.IsRead == true && u.RecipientDeleted == false);
                    break;
                case "Outbox":
                    message = message.Where(u => u.SenderId == messageParams.UserId && u.SenderDeleted == false);
                    break;
                default:
                    message = message.Where(u => u.RecipientId == messageParams.UserId && u.IsRead == false && u.RecipientDeleted == false);
                    break;
            }

            // filter newest messages first
            message = message.OrderByDescending(u => u.DateMessageSent);

            // return paged list of message
            return await PagedList<Message>.CreateAsync(message, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<Message>> GetMessageThread(int userId, int recipientId)
        {
            var messages = await _context.Messages
                .Include(u => u.Sender).ThenInclude(u => u.Photos)
                .Include(u => u.Recipient).ThenInclude(u => u.Photos)
                .Where(m => (m.SenderId == userId && m.RecipientId == recipientId && m.SenderDeleted == false) || 
                    (m.SenderId == recipientId && m.RecipientId == userId && m.RecipientDeleted == false))
                .OrderByDescending(m => m.DateMessageSent)
                .ToListAsync();

            return messages;
        }
    }
}