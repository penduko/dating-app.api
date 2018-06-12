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
    [Authorize]
    // log user activity when users comes in 
    // in this controller
    [ServiceFilter(typeof(LogUserActivity))]
    [Route("api/users/{userId}/[controller]")]
    public class MessagesController : Controller
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        public MessagesController(IDatingRepository repo, IMapper mapper)
        {
            _mapper = mapper;
            _repo = repo;
        }

        [HttpGet("{id}", Name = "GetMessage")]
        public async Task<IActionResult> GetMessage(int userId, int id)
        {
            // check if the user is the current user
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var messageFromRepo = await _repo.GetMessage(id);

            // check the message
            if (messageFromRepo == null)
                return NotFound();

            return Ok(messageFromRepo);
        }

        [HttpGet("thread/{recipientId}")]
        public async Task<IActionResult> GetMessageThread(int userId, int recipientId)
        {
            // check if the user is the current user
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            // get the messages from repo
            var messageFromRepo = await _repo.GetMessageThread(userId, recipientId);

            // map the result from repo
            var messagesThread = _mapper.Map<IEnumerable<MessageToReturnDto>>(messageFromRepo);

            return Ok(messagesThread);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessageForUser(int userId, [FromQuery] MessageParams messageParams)
        {
            // check if the user is the current user
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            // set our parameter filter for user id
            messageParams.UserId = userId;
            var messageFromRepo = await _repo.GetMessageForUser(messageParams);

            // map the ienumerable mesasages from repo
            var message = _mapper.Map<IEnumerable<MessageToReturnDto>>(messageFromRepo);

            // add pagination headers
            Response.AddPagination(messageFromRepo.CurrentPage, 
                messageFromRepo.PageSize, messageFromRepo.TotalCount, messageFromRepo.TotalPages);
            
            return Ok(message);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage(int userId, 
            [FromBody] MessageForCreationDto messageForCreationDto)
        {
            // check if the user is the current user
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            // set the sender id
            messageForCreationDto.SenderId = userId;

            // get the sender information
            // so that we can return the sender information 
            // along with the message
            var sender = _repo.GetUser(messageForCreationDto.SenderId);

            // get the recipient and check if exists in our database
            var recipient = _repo.GetUser(messageForCreationDto.RecipientId);
            if (recipient == null)
                return BadRequest("Could find the user");

            // map 
            var message = _mapper.Map<Message>(messageForCreationDto);

            _repo.Add(message);

            // map the return message
            var messageToReturn = _mapper.Map<MessageToReturnDto>(message);

            if (await _repo.SaveAll())
                // because we're creating resource we 
                // CreateaAtRoute method
                return CreatedAtRoute("GetMessage", new {id = message.Id}, messageToReturn);

            throw new Exception("Creating the message failed on save");
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> DeleteMessage(int userId, int id)
        {
            // check if the user is the current user
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            // get message from repository
            var messageFromRepo = await _repo.GetMessage(id);

            // check if sender is current user
            if (messageFromRepo.SenderId == userId)
                messageFromRepo.SenderDeleted = true;

            // check if the recipient is the current user
            if (messageFromRepo.RecipientId == userId)
                messageFromRepo.RecipientDeleted = true;

            // if sender and recipient both delete the message 
            // delete the message from database
            if (messageFromRepo.SenderDeleted == true 
                && messageFromRepo.RecipientDeleted == true)
                _repo.Delete(messageFromRepo);

            // persist the changes to database
            if (await _repo.SaveAll())
                return NoContent();

            throw new Exception("Error deleting the message");
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkMessageAsRead(int userId, int id)
        {
            // check if the user is the current user
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var messageFromRepo = await _repo.GetMessage(id);

            // check the recipient is not uqual to the current user
            if (messageFromRepo.RecipientId != userId)
                return BadRequest("Failed to mark message as read");

            // mark message read
            messageFromRepo.IsRead = true;
            messageFromRepo.DateRead = DateTime.Now;
            
            await _repo.SaveAll();

            return NoContent();
        }
    }
}