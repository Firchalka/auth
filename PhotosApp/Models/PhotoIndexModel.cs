using System.Collections.Generic;

namespace PhotosApp.Models
{
    public class PhotoIndexModel
    {
        public PhotoIndexModel(List<Photo> photos)
        {
            Photos = photos;
        }

        public IEnumerable<Photo> Photos { get; private set; }
            = new List<Photo>();
    }
}