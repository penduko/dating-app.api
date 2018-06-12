namespace DatingApp.API.Helpers
{
    public class MessageParams
    {
        private const int MaxPageSize = 50; // maximun page size
        public int PageNumber { get; set; } = 1;    // default first page
        private int pageSize = 10;  // initial page size
        public int PageSize
        {
            get { return pageSize;}
            set { pageSize = (value > MaxPageSize) ? MaxPageSize : value;}
        }
        
        public int UserId { get; set; }
        public string MessageContainer { get; set; } = "Unread";   // represent inbox, outbox and unread messages
    }
}