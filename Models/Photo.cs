using System;

namespace DatingApp.API.Models 
{
    public class Photo
    {
        public int Id { get; set; } 
        public string Url { get; set; } 
        public string Description { get; set; }
        public DateTime DateAdded { get; set; }
        public bool IsMain { get; set; }
        
        // property named to store the public id that we get from the cloudinary platform
        public string PublicId { get; set; }
        public User User { get; set; }
        public int UserID { get; set; }
    }
}