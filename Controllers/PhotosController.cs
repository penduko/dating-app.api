using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/users/{userId}/photos")]
    public class PhotosController : Controller
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;

        public PhotosController(IDatingRepository repo,
            IMapper mapper,
            IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _repo = repo;
            _mapper = mapper;
            _cloudinaryConfig = cloudinaryConfig;

            // initialize a new cloudinary account and pass our 
            // cloudinary configuration
            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            // creat a new instance of cloudinary 
            // and pass our account details
            // this will allow us to upload in 
            // cloudinary platform using our account details
            _cloudinary = new Cloudinary(acc);
        }

        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photoFromRepo = await _repo.GetPhoto(id);

            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);

            return Ok(photo);
        }

        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, PhotoForCreationDto photoDto)
        {
            // get the user from the repo
            var user = await _repo.GetUser(userId);

            // check if the user is null then return badrequest
            if (user == null)
                return BadRequest("Could not find user");

            // get the current userid
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // checked if the user is the 
            // actual current user
            if (currentUserId != user.Id)
                return Unauthorized();

            var file = photoDto.File;

            // variable to store the result of our upload to cloudinary
            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                // this will allow us to read our
                // uploaded file
                using (var stream = file.OpenReadStream())
                {   
                    // initialize cloudinary image upload parameters
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        // apply tranformation that will crop the photos 
                        // when uploading long photos and set gravity to face
                        // so that it will focus on it
                        Transformation = new Transformation()
                            .Width(500)
                            .Height(500)
                            .Crop("fill")
                            .Gravity("face")
                    };

                    // use the cloudinary upload method to 
                    // actually uploud our file in cloudinary platform
                    uploadResult = _cloudinary.Upload(uploadParams);
                }
            };

            photoDto.Url = uploadResult.Uri.ToString();
            photoDto.PublicId = uploadResult.PublicId;

            // map our photoDto to our actual 
            // photo enitity
            var photo = _mapper.Map<Photo>(photoDto);
            photo.User = user;

            // decide for main photo if theres none
            if (!user.Photos.Any(m => m.IsMain))
                photo.IsMain = true;

            user.Photos.Add(photo);
            
            // persist the changes to database
            if (await _repo.SaveAll())
            {
                var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);

                // return createdAtroute
                return CreatedAtRoute("GetPhoto", new {id = photo.Id}, photoToReturn);
            }

            return BadRequest("Could not add the photo");
        }

        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id)
        {
            // checked if the user is the 
            // actual current user
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            // check if photo is already a main photo
            var photoFromRepo = await _repo.GetPhoto(id);
            if (photoFromRepo == null)
                return NotFound();

            if (photoFromRepo.IsMain)
                return BadRequest("This is already the main photo");

            // get the current main photo from repo
            var currentMainPhoto = await _repo.GetMainPhotoForUser(userId);
            if (currentMainPhoto != null)
                // set the main photo to false
                currentMainPhoto.IsMain = false;
            
            // set the photo to main
            photoFromRepo.IsMain = true;

            if (await _repo.SaveAll())
                return NoContent();

            return BadRequest("Could not set photo to main");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id) 
        {
            // checked if the user is the 
            // actual current user
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            // check if photo is already a main photo
            var photoFromRepo = await _repo.GetPhoto(id);
            if (photoFromRepo == null)
                return NotFound();
            
            if (photoFromRepo.IsMain)
                return BadRequest("You cannot delete the main photo");

            if (photoFromRepo.PublicId != null)
            {
                // cloudinary deletion parameters that takes the public id
                var deleteParams = new DeletionParams(photoFromRepo.PublicId);
                // delete the photo in cloudinary
                var result = _cloudinary.Destroy(deleteParams);
                
                // response that comes back after
                // deleting in cloudinary
                if (result.Result == "ok")
                    // delete the photo in database
                    _repo.Delete(photoFromRepo);
            }

            if (photoFromRepo.PublicId == null)
            {
                // delete the photo in database
                _repo.Delete(photoFromRepo);
            }    

            // persist the changes in database
            if (await _repo.SaveAll())
                return Ok();

            return BadRequest("Failed to delete the photo");
        }
    }
}