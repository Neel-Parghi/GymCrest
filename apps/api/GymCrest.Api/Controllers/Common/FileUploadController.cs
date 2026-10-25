using GymCrest.Api.Filters;
using GymCrest.Application.Modules.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrest.Api.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FileUploadController : ControllerBase
    {
        private const long MaxUploadBytes = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
        private static readonly HashSet<string> AllowedFolders = ["general", "equipment", "logos", "products"];

        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<FileUploadController> _logger;

        public FileUploadController(IFileStorageService fileStorageService, ILogger<FileUploadController> logger)
        {
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        [HttpPost]
        [BlockDemoAccount]
        public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string folder = "general")
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            if (file.Length > MaxUploadBytes)
            {
                return BadRequest("File is too large. The limit is 5 MB.");
            }

            if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
            {
                return BadRequest("Invalid file type. Only JPG, PNG, GIF and WebP images are allowed.");
            }

            if (!AllowedFolders.Contains(folder))
            {
                return BadRequest("Invalid upload folder.");
            }

            try
            {
                using Stream stream = file.OpenReadStream();
                string? fileUrl = await _fileStorageService.SaveFileAsync(stream, file.FileName, folder);

                return Ok(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload to folder {Folder} failed", folder);
                return StatusCode(500, "Upload failed. Please try again.");
            }
        }
    }
}
