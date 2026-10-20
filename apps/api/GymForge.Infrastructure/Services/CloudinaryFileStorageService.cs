using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using GymForge.Application.Modules.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace GymForge.Infrastructure.Services
{
    public class CloudinaryFileStorageService : IFileStorageService
    {
        private const int MaxDimension = 1200;
        private const int JpegQuality = 75;
        private static readonly HashSet<string> CompressedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

        private readonly Cloudinary _cloudinary;

        public CloudinaryFileStorageService(IConfiguration configuration)
        {
            string? cloudName = configuration["Cloudinary:CloudName"];
            string? apiKey = configuration["Cloudinary:ApiKey"];
            string? apiSecret = configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                throw new ArgumentException("Cloudinary configuration is missing.");
            }

            Account account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string folder)
        {
            if (fileStream == null || fileStream.Length == 0)
                throw new ArgumentException("File stream is empty");

            string extension = Path.GetExtension(fileName).ToLowerInvariant();

            ImageUploadParams uploadParams = new()
            {
                File = new FileDescription(fileName, fileStream),
                Folder = $"gymcrest/{folder}",
                PublicId = $"{Guid.NewGuid()}_{SafeName(fileName)}"
            };

            // Photos are shrunk to fit 1200px and stored as JPEG quality 75 by Cloudinary while uploading,
            // so untrusted images are never decoded on this server. GIFs keep their animation.
            if (CompressedExtensions.Contains(extension))
            {
                if (folder == "logos")
                {
                    uploadParams.Transformation = new Transformation().Width(MaxDimension).Height(MaxDimension).Crop("limit");
                }
                else
                {
                    uploadParams.Transformation = new Transformation().Width(MaxDimension).Height(MaxDimension).Crop("limit").Quality(JpegQuality);
                    uploadParams.Format = "jpg";
                }
            }

            ImageUploadResult uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }

        private static string SafeName(string fileName)
        {
            string name = Regex.Replace(Path.GetFileNameWithoutExtension(fileName), "[^A-Za-z0-9_-]+", "-").Trim('-');
            return string.IsNullOrEmpty(name) ? "file" : name[..Math.Min(name.Length, 60)];
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;

            Uri uri = new(fileUrl);
            string? path = uri.AbsolutePath;

            string[] segments = path.Split('/');
            int uploadIndex = Array.FindIndex(segments, s => s == "upload");
            
            if (uploadIndex == -1) return;

            List<string> relevantSegments = segments.Skip(uploadIndex + 2).ToList();
            string? publicIdWithExtension = string.Join("/", relevantSegments);
            string? publicId = Path.Combine(Path.GetDirectoryName(publicIdWithExtension) ?? "", 
                                         Path.GetFileNameWithoutExtension(publicIdWithExtension))
                                         .Replace("\\", "/");

            DeletionParams deletionParams = new(publicId);
            await _cloudinary.DestroyAsync(deletionParams);
        }
    }
}
