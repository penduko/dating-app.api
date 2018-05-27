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

        public Task<Photo> GetMainPhotoForUser(int userId)
        {
            return _context.Photos.Where(u => u.UserID == userId)
                .FirstOrDefaultAsync(p => p.IsMain);
        }

        public Task<Photo> GetPhoto(int id)
        {
            var photo = _context.Photos.FirstOrDefaultAsync(p => p.Id == id);

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
    }
}