namespace PizzasFuriosas.Core.Interfaces;

public record PhotoResult(string Url, string PublicId);

public interface IPhotoService
{
    Task<PhotoResult?> UploadPhotoAsync(Stream fileStream, string fileName);
    Task<bool> DeletePhotoAsync(string publicId);
}
