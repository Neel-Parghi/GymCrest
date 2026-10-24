using System.IO;
using System.Threading.Tasks;

namespace GymCrest.Application.Modules.Common.Interfaces
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves a file stream to storage and returns the relative URL
        /// </summary>
        Task<string> SaveFileAsync(Stream fileStream, string fileName, string folder);

        /// <summary>
        /// Deletes a file from storage
        /// </summary>
        Task DeleteFileAsync(string fileUrl);
    }
}
