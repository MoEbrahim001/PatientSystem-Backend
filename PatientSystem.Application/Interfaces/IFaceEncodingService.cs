using Microsoft.AspNetCore.Http;

namespace PatientSystem.Application.Interfaces
{
    public interface IFaceEncodingService
    {
        Task<string> GenerateEncodingAsync(
            int patientId,
            string faceImageFullPath
        );

        Task<string> DetectAndFindAsync(
            IFormFile file
        );

        Task ReloadEncodingsAsync();
        Task<bool> WarmUpAsync();
    }
}
