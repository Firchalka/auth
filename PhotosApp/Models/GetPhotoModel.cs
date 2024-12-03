namespace PhotosApp.Models
{
    public class GetPhotoModel
    {
        public GetPhotoModel(Photo photo)
        {
            Photo = photo;
        }

        public Photo Photo { get; private set; }
    }
}