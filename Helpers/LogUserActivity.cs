using System;
using System.Security.Claims;
using System.Threading.Tasks;
using DatingApp.API.Data;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace DatingApp.API.Helpers
{
    public class LogUserActivity : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // get the context
            var resultContext = await next();

            // get the id of the current user
            var userId = int.Parse(resultContext.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // get access to our repository so 
            // that we can updated the user
            // and user service locator to get 
            // the instance of our IDatingRepository
            var repo = resultContext.HttpContext.RequestServices.GetService<IDatingRepository>();
            
            // get user
            var user = await repo.GetUser(userId);
            // update last active
            user.LastActive = DateTime.Now;

            // persist changes to database
            await repo.SaveAll();
        }
    }
}