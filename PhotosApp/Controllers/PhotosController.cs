using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhotosApp.Data;
using PhotosApp.Models;

namespace PhotosApp.Controllers
{
    [Authorize]
    public class PhotosController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IPhotosRepository _photosRepository;

        public PhotosController(IPhotosRepository photosRepository, IMapper mapper)
        {
            _photosRepository = photosRepository;
            _mapper = mapper;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var ownerId = GetOwnerId();
            var photoEntities = await _photosRepository.GetPhotosAsync(ownerId);
            var photos = _mapper.Map<IEnumerable<Photo>>(photoEntities);

            var model = new PhotoIndexModel(photos.ToList());
            return View(model);
        }

        [Authorize("MustOwnPhoto")]
        public async Task<IActionResult> GetPhoto(Guid id)
        {
            var photoEntity = await _photosRepository.GetPhotoMetaAsync(id);
            if (photoEntity == null)
                return NotFound();

            var photo = _mapper.Map<Photo>(photoEntity);

            var model = new GetPhotoModel(photo);
            return View(model);
        }

        [HttpGet("photos/{id:guid}")]
        [Authorize("MustOwnPhoto")]
        public async Task<IActionResult> GetPhotoFile(Guid id)
        {
            var photoContent = await _photosRepository.GetPhotoContentAsync(id);
            if (photoContent == null)
                return NotFound();

            return File(photoContent.Content, photoContent.ContentType, photoContent.FileName);
        }

        [Authorize(Policy = "CanAddPhoto")]
        public IActionResult AddPhoto()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanAddPhoto")]
        public async Task<IActionResult> AddPhoto(AddPhotoModel addPhotoModel)
        {
            if (addPhotoModel == null || !ModelState.IsValid)
                return View();

            var file = addPhotoModel.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
                return View();

            var title = addPhotoModel.Title;
            var ownerId = GetOwnerId();

            byte[] content;
            await using (var fileStream = file.OpenReadStream())
            {
                using (var memoryStream = new MemoryStream())
                {
                    await fileStream.CopyToAsync(memoryStream);
                    content = memoryStream.ToArray();
                }
            }

            if (!await _photosRepository.AddPhotoAsync(title, ownerId, content))
                return StatusCode(StatusCodes.Status409Conflict);

            return RedirectToAction("Index");
        }

        [Authorize(Policy = "Beta")]
        [Authorize("MustOwnPhoto")]
        public async Task<IActionResult> EditPhoto(Guid id)
        {
            var photo = await _photosRepository.GetPhotoMetaAsync(id);
            if (photo == null)
                return NotFound();

            var viewModel = new EditPhotoModel
            {
                Id = photo.Id,
                Title = photo.Title
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Beta")]
        [Authorize("MustOwnPhoto")]
        public async Task<IActionResult> EditPhoto(EditPhotoModel editPhotoModel)
        {
            if (editPhotoModel == null || !ModelState.IsValid)
                return View();

            var photoEntity = await _photosRepository.GetPhotoMetaAsync(editPhotoModel.Id);
            if (photoEntity == null)
                return NotFound();

            _mapper.Map(editPhotoModel, photoEntity);

            if (!await _photosRepository.UpdatePhotoAsync(photoEntity))
                return StatusCode(StatusCodes.Status409Conflict);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize("MustOwnPhoto")]
        public async Task<IActionResult> DeletePhoto(Guid id)
        {
            var photoEntity = await _photosRepository.GetPhotoMetaAsync(id);
            if (photoEntity == null)
                return NotFound();

            if (!await _photosRepository.DeletePhotoAsync(photoEntity))
                return StatusCode(StatusCodes.Status409Conflict);

            return RedirectToAction("Index");
        }

        private string GetOwnerId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}