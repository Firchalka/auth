using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace PhotosService.Data
{
    public static class ImageHelpers
    {
        public static string GetContentTypeByFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLower();
            return extension switch
            {
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "image/jpeg"
            };
        }

        public static string GetExtensionByBytes(byte[] content)
        {
            return GetExtensionByContentType(GetContentTypeByBytes(content));
        }

        private static string GetExtensionByContentType(string contentType)
        {
            switch (contentType)
            {
                case "image/png":
                    return "png";
                case "image/gif":
                    return "gif";
                default:
                    return "jpg";
            }
        }

        private static string GetContentTypeByBytes(byte[] content)
        {
            try
            {
                var image = Image.FromStream(new MemoryStream(content));
                var contentType = ImageCodecInfo.GetImageEncoders()
                    .FirstOrDefault(codec => codec.FormatID == image.RawFormat.Guid)?.MimeType;
                return contentType;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}