using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
    // log user activity when users comes in 
    // in this controller
    [ServiceFilter(typeof(LogUserActivity))]
    [Authorize]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        public UsersController(IDatingRepository repo, IMapper mapper)
        {
            _mapper = mapper;
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] UserParams userParams)
        {
            // get the id of the user that sent request to the api
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            // get the current user from repo
            var userFromRepo = await _repo.GetUser(currentUserId);
        
            // set our parameter filter for user id
            userParams.UserId = currentUserId;
            if (string.IsNullOrEmpty(userParams.Gender)) 
                userParams.Gender = userFromRepo.Gender == "male" ? "female" : "male";

            var users = await _repo.GetUsers(userParams);
            var usersToReturn = _mapper.Map<IEnumerable<UserForListDto>>(users);
            
            // add pagination headers
            Response.AddPagination(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);
            
            return Ok(usersToReturn);
        }

        [HttpGet("{id}", Name = "GetUser")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _repo.GetUser(id);
            var userToReturn = _mapper.Map<UserForDetailedDto>(user);

            return Ok(userToReturn);      
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserForUpdateDto userForUpdateDto)
        {
            // checked if the modelstate if valid
            if(!ModelState.IsValid)
                return BadRequest(ModelState);

            // get the id of the user that sent request to the api
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // get the user from repo
            var userFromRepo = await _repo.GetUser(id);

            if(userFromRepo == null)
                return NotFound($"Could not find user of ID of {id}");
            
            // compare the currentuserid with userfromrepo id to make user it match
            // so that the only current loggin user can update their profile
            if(currentUserId != userFromRepo.Id)
                return Unauthorized();

            // map
            _mapper.Map(userForUpdateDto, userFromRepo);

            if (await _repo.SaveAll())
                // success return no content with the status code of 204
                return NoContent();

            
            throw new Exception($"Updating user {id} failed on save");
        }

        [HttpPost("{id}/like/{recipientId}")]
        public async Task<IActionResult> LikeUser(int id, int recipientId)
        {   
            // check if the user is the current user
            if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            // check if the user and recipient
            // already like each other
            var like = await _repo.GetLike(id, recipientId);
            if (like != null)
                return BadRequest("You already like this user");

            // check if the recipient is in our database
            if (await _repo.GetUser(recipientId) == null)
                return NotFound();

            // create new entity if Like
            like = new Like 
            {
                LikerId = id,
                LikeeId = recipientId
            };
            
            // add the entity
            _repo.Add<Like>(like);

            if (await _repo.SaveAll())
                // return empty object as ok response
                return Ok(new {});

            return BadRequest("Failed to like the user");
        }
    }
}