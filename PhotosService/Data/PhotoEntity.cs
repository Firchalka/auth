using System;
using System.ComponentModel.DataAnnotations;

namespace PhotosService.Data
{
    public class PhotoEntity
    {
        [Key] public Guid Id { get; init; }

        [Required] [MaxLength(150)] public string Title { get; set; }

        [Required] [MaxLength(200)] public string FileName { get; init; }

        [Required] [MaxLength(50)] public string OwnerId { get; init; }
    }
}