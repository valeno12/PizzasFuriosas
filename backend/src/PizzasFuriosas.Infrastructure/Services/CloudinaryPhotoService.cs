using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using PizzasFuriosas.Core.Interfaces;

namespace PizzasFuriosas.Infrastructure.Services;

public class CloudinaryPhotoService : IPhotoService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryPhotoService()
    {
        var account = new Account(
            Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME"),
            Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY"),
            Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
        );

        _cloudinary = new Cloudinary(account);
    }

    public async Task<PhotoResult?> UploadPhotoAsync(Stream fileStream, string fileName)
    {
        if (fileStream.Length == 0) return null;

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = "pizzas-furiosas"
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            throw new Exception(uploadResult.Error.Message);
        }

        return new PhotoResult(uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
    }

    public async Task<bool> DeletePhotoAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);
        
        return result.Result == "ok";
    }
}
