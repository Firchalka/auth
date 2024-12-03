using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace PhotosApp.Data
{
    public class LocalPhotosRepository : IPhotosRepository, IDisposable
    {
        private PhotosDbContext _dbContext;

        public LocalPhotosRepository(PhotosDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<IEnumerable<PhotoEntity>> GetPhotosAsync(string ownerId)
        {
            return await _dbContext.Photos
                .Where(i => i.OwnerId == ownerId)
                .OrderBy(i => i.Title).ToListAsync();
        }

        public async Task<PhotoEntity> GetPhotoMetaAsync(Guid id)
        {
            return await _dbContext.Photos.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PhotoContent> GetPhotoContentAsync(Guid id)
        {
            var entity = await _dbContext.Photos.FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null)
                return null;

            var filePath = GetPhotoPath(entity.FileName);
            if (!File.Exists(filePath))
                return null;
            var content = await File.ReadAllBytesAsync(filePath);

            var contentType = ImageHelpers.GetContentTypeByFileName(entity.FileName);
            return new PhotoContent
            {
                FileName = entity.FileName,
                ContentType = contentType,
                Content = content
            };
        }

        public async Task<bool> AddPhotoAsync(string title, string ownerId, byte[] content)
        {
            var fileName = await SavePhotoContentAsync(content);
            var entity = new PhotoEntity
            {
                Title = title,
                OwnerId = ownerId,
                FileName = fileName
            };
            await _dbContext.Photos.AddAsync(entity);
            return await _dbContext.SaveChangesAsync() >= 0;
        }

        public async Task<bool> UpdatePhotoAsync(PhotoEntity photo)
        {
            _dbContext.Photos.Update(photo);
            return await _dbContext.SaveChangesAsync() >= 0;
        }

        public async Task<bool> DeletePhotoAsync(PhotoEntity photo)
        {
            _dbContext.Photos.Remove(photo);
            return await _dbContext.SaveChangesAsync() >= 0;
        }

        private async Task<string> SavePhotoContentAsync(byte[] content)
        {
            var fileExtension = ImageHelpers.GetExtensionByBytes(content);
            var fileName = $"{Guid.NewGuid()}.new.{fileExtension}";
            var filePath = GetPhotoPath(fileName);
            await File.WriteAllBytesAsync(filePath, content);
            return fileName;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                if (_dbContext != null)
                {
                    _dbContext.Dispose();
                    _dbContext = null;
                }
        }

        private static string GetPhotoPath(string fileName)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".photos", fileName);
        }
    }
}