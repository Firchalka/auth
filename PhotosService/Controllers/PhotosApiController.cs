using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using PhotosService.Data;
using PhotosService.Models;
using PhotosService.Services;

namespace PhotosService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/photos")]
    public class PhotosApiController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IPhotosRepository _photosRepository;

        public PhotosApiController(IPhotosRepository photosRepository, IMapper mapper)
        {
            _photosRepository = photosRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetPhotos(string ownerId, JwtSecurityToken accessToken)
        {
            if (accessToken.Subject != ownerId)
                return Forbid();

            var photoEntities = (await _photosRepository.GetPhotosAsync(ownerId)).ToList();
            var photos = _mapper.Map<List<PhotoDto>>(photoEntities);
            foreach (var photo in photos)
                photo.Url = GeneratePhotoUrl(photo);
            return Ok(photos.ToList());
        }

        [HttpGet("{id}/meta")]
        public async Task<IActionResult> GetPhotoMeta(Guid id, JwtSecurityToken accessToken)
        {
            var photoEntity = await _photosRepository.GetPhotoMetaAsync(id);
            if (photoEntity == null)
                return NotFound();
            if (accessToken.Subject != photoEntity.OwnerId)
                return Forbid();

            var photo = _mapper.Map<PhotoDto>(photoEntity);
            photo.Url = GeneratePhotoUrl(photo);
            return Ok(photo);
        }

        [HttpGet("{id}/content")]
        public async Task<IActionResult> GetPhotoContent(Guid id, JwtSecurityToken accessToken)
        {
            var photoEntity = await _photosRepository.GetPhotoMetaAsync(id);
            if (photoEntity == null)
                return NotFound();
            if (accessToken.Subject != photoEntity.OwnerId)
                return Forbid();

            var photoContent = await _photosRepository.GetPhotoContentAsync(id);
            if (photoContent == null)
                return NotFound();

            return File(photoContent.Content, photoContent.ContentType, photoContent.FileName);
        }

        [AllowAnonymous]
        [HttpGet("{id}/signed-content")]
        public async Task<IActionResult> GetPhotoSignedContent(Guid id)
        {
            var currentUrl = HttpContext.Request.GetEncodedUrl();
            var check = SignedUrlHelpers.CheckSignedUrl(currentUrl);
            if (!check)
                return Forbid();

            var photoEntity = await _photosRepository.GetPhotoMetaAsync(id);
            if (photoEntity == null)
                return NotFound();

            var photoContent = await _photosRepository.GetPhotoContentAsync(id);
            if (photoContent == null)
                return NotFound();

            return File(photoContent.Content, photoContent.ContentType, photoContent.FileName);
        }

        [HttpPost]
        public async Task<IActionResult> AddPhoto(PhotoToAddDto photo,
            [FromHeader] JwtSecurityToken accessToken)
        {
            if (accessToken.Subject != photo.OwnerId)
                return Forbid();

            var content = Convert.FromBase64String(photo.Base64Content);
            var result = await _photosRepository.AddPhotoAsync(photo.Title, photo.OwnerId, content);
            if (!result)
                return Conflict();
            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePhoto(Guid id, PhotoToUpdateDto photo,
            [FromHeader] JwtSecurityToken accessToken)
        {
            var photoEntity = await _photosRepository.GetPhotoMetaAsync(id);
            if (photoEntity == null)
                return NotFound();
            if (accessToken.Subject != photoEntity.OwnerId)
                return Forbid();

            photoEntity.Title = photo.Title;
            var result = await _photosRepository.UpdatePhotoAsync(photoEntity);
            if (!result)
                return Conflict();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(Guid id, JwtSecurityToken accessToken)
        {
            var photoEntity = await _photosRepository.GetPhotoMetaAsync(id);
            if (photoEntity == null)
                return NotFound();
            if (accessToken.Subject != photoEntity.OwnerId)
                return Forbid();

            var result = await _photosRepository.DeletePhotoAsync(photoEntity);
            if (!result)
                return Conflict();
            return NoContent();
        }

        private string GeneratePhotoUrl(PhotoDto photo)
        {
            var relativeUrl = Url.Action(nameof(GetPhotoSignedContent), new
            {
                id = photo.Id
            });
            var url = "https://localhost:6001" + relativeUrl;

            var nowUtc = DateTime.UtcNow;
            var signedUrl = SignedUrlHelpers.CreateSignedUrl(url, nowUtc, nowUtc.AddMinutes(5));
            return signedUrl;
        }
    }
}