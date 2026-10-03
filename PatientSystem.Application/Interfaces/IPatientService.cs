using Microsoft.AspNetCore.Http;
using PatientSystem.Application.DTOs.Patients;
using PatientSystem.Domain.Entities;

namespace PatientSystem.Application.Interfaces
{
    public interface IPatientService
    {
        Task<PatientPagedResultDto> GetPagedAsync(
            int skip,
            int take,
            string? searchText,
            string baseUrl
        );

        Task<int> AddAsync(CreatePatientVM dto);

        Task<List<PatientDto>> SearchAsync(
            string searchText,
            string baseUrl
        );

        Task<PatientDto?> GetByIdAsync(
            int id,
            string baseUrl
        );

        Task UpdateAsync(Patient updatedPatient);

        Task<string> UploadFaceImageAsync(
            int patientId,
            IFormFile file,
            string contentRootPath
        );

        Task<string> DetectAndFindAsync(IFormFile file);

        Task DeleteAsync(int id);

        Task DeleteAllAsync();
    }
}
