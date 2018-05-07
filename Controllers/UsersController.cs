using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
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
        public async Task<IActionResult> GetUsers()
        {
            var users = await _repo.GetUsers();
            var usersToReturn = _mapper.Map<IEnumerable<UserForListDto>>(users);

            return Ok(usersToReturn);
        }

        [HttpGet("{id}")]
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
    }
}