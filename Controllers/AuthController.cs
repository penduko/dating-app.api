using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public AuthController(IAuthRepository repo, 
            IConfiguration config, 
            IMapper mapper)
        {
            _config = config;
            _mapper = mapper;
            _repo = repo;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody]UserForRegisterDto userForRegisterDto)
        {
            if (!string.IsNullOrEmpty(userForRegisterDto.Username))
                userForRegisterDto.Username = userForRegisterDto.Username.ToLower();

            if (await _repo.UserExists(userForRegisterDto.Username))
                // pass the userexists custom validation in the model state
                ModelState.AddModelError("Username", "Username is already taken"); 

            // validate request
            // if the infromation pass in our userForRegisterDto does not 
            // meet our validation requirements then will pass
            // the error in model state
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            // map the user for register to actual user            
            var userToCreate = _mapper.Map<User>(userForRegisterDto);

            var createdUser = await _repo.Register(userToCreate, userForRegisterDto.Password);

            // map again the created user to userToReturned
            // so that it will not return all data of the user
            var userToReturned = _mapper.Map<UserForDetailedDto>(createdUser);

            return CreatedAtRoute("GetUser", new {controller = "Users", id = createdUser.Id}, userToReturned);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody]UserForLoginDto userForLoginDto)
        {
            var userFromRepo = await _repo.Login(userForLoginDto.Username.ToLower(), userForLoginDto.Password);

            if (userFromRepo == null)
                return Unauthorized();

            // generate our token
            var tokenHandler = new JwtSecurityTokenHandler();   // token header
            var key = Encoding.ASCII.GetBytes(_config.GetSection("AppSettings:Token").Value);  // token secret key
            var tokenDescriptor = new SecurityTokenDescriptor   // token payload
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                    new Claim(ClaimTypes.Name, userFromRepo.Username)
                }),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha512)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // take the user from repo and pass back to the client
            var user = _mapper.Map<UserForListDto>(userFromRepo);

            return Ok(new {tokenString, user});
        }
    }
}