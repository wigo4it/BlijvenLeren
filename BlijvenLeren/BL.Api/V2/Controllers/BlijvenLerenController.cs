﻿using AutoMapper;
using BL.Api.V1.Models;
using BL.DAL;
using BL.Domain.Enums;
using BL.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BL.Api.V2.Controllers
{
    [Produces("application/json")]
    [ApiVersion("2.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class BlijvenLerenController : ControllerBase
    {
        private readonly ILogger<BlijvenLerenController> _logger;
        private readonly IBlijvenLerenRepository _blijvenLerenRepository;
        private readonly IMapper _mapper;
        public BlijvenLerenController(ILogger<BlijvenLerenController> logger, IBlijvenLerenRepository blijvenLerenRepository, IMapper mapper)
        {
            _logger = logger;
            _blijvenLerenRepository = blijvenLerenRepository;
            _mapper = mapper;
        }

        [HttpPost("resource", Name = "AddResourceAsync")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateResourceAsync([FromBody] ResourceDto resource)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (resource == null)
            {
                return ValidationProblem("resource is required.");
            }

            try
            {
                var dbresource = new Resource()
                {
                    Id = Guid.NewGuid().ToString(),
                    DocType = Domain.Enums.DocumentType.Resource,
                    Title = resource.Title,
                    Description = resource.Description,
                    Url = resource.Url,
                    Comments = (resource.Comments != null && resource.Comments.Any()) ? 
                                resource.Comments.Select(c => _mapper.Map<Comment>(c)).ToList() : 
                                new List<Comment>()
                };

                var result = await _blijvenLerenRepository.CreateResourceAsync(dbresource);
                return Ok(result);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, exception.Message);
                return StatusCode(500);
            }
        }

        [HttpPut("resource", Name = "UpdateResourceAsync")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateResourceAsync([FromBody] ResourceDto resource)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (resource == null)
            {
                return ValidationProblem("resource is required.");
            }

            var dbResource = _mapper.Map<Resource>(resource);
            var updatedResource = await _blijvenLerenRepository.UpdateResourceAsync(dbResource);
            return Ok(updatedResource);
        }

        [HttpPut("{resourceid}/comment", Name = "AddOrUpdateCommentAsync")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AddOrUpdateCommentAsync([FromRoute] string resourceid, [FromBody] CommentDto comment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (comment == null)
            {
                return ValidationProblem("comment is required.");
            }

            var resource = await _blijvenLerenRepository.GetResourceByIdAsync(resourceid);
            if (resource == null)
            {
                return NotFound();
            }

            try
            {
                resource.Comments ??= new List<Comment>();

                if (comment.Id == Guid.Empty)
                {
                    comment.Id = Guid.NewGuid();
                }

                var newOrUpdatedComment = new Comment() { Id = comment.Id.ToString(), Text = comment.Text, Approved = comment.Approved };

                if (!resource.Comments.Any() || !resource.Comments.Any(c => c.Id == newOrUpdatedComment.Id))
                {
                    resource.Comments.Add(newOrUpdatedComment);
                }
                else
                {
                    resource.Comments[resource.Comments.FindIndex(c => c.Id.Equals(newOrUpdatedComment.Id))] = newOrUpdatedComment;
                }

                var result = await _blijvenLerenRepository.UpdateResourceAsync(resource);
                return Ok(result);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, exception.Message);
                return StatusCode(500);
            }
        }

        [HttpGet("documentsbytype")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetDocumentsByTypeAsync([FromQuery] DocumentType type)
        {
            try
            {
                var requestedType = EnumHelper.Enums<DocumentType>(type);

                switch (requestedType)
                {
                    case DocumentType.Resource:
                        var resourceResults = await _blijvenLerenRepository.GetDocumentsByTypeAsync<Resource>(requestedType);
                        if (resourceResults == null) return NotFound();
                        return Ok(resourceResults);
                    default:
                        return NotFound();
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, exception.Message);
                return StatusCode(500);
            }
        }
    }
}
