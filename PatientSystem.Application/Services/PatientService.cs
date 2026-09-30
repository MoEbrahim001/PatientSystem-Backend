using Microsoft.AspNetCore.Http;
using PatientSystem.Application.DTOs.Patients;
using PatientSystem.Application.Interfaces;
using PatientSystem.Domain.Entities;

namespace PatientSystem.Application.Services;

//public interface IPatientService
//{
//    Task<PatientPagedResultDto> GetPagedAsync(int skip, int take, string? searchText, string baseUrl);
//    Task<int> AddAsync(CreatePatientVM dto); // أو DTO جديد بدل VM
//    Task<List<PatientDto>> SearchAsync(string searchText, string baseUrl);
//    Task<PatientDto?> GetByIdAsync(int id, string baseUrl);
//    Task UpdateAsync(Patient updatedPatient);
//    Task<string> UploadFaceImageAsync(int patientId, IFormFile file, string contentRootPath);
//    Task DeleteAsync(int id);
//    Task DeleteAllAsync();
//}

public class PatientService : IPatientService
{
    private readonly IPatientRepository _repo;
    private readonly IFaceEncodingService _encoding;

    public PatientService(IPatientRepository repo, IFaceEncodingService encoding)
    {
        _repo = repo;
        _encoding = encoding;
    }

    public async Task<PatientPagedResultDto> GetPagedAsync(int skip, int take, string? searchText, string baseUrl)
    {
        var (items, total) = await _repo.GetPagedAsync(skip, take, searchText);

        var dtos = items.Select(p => new PatientDto
        {
            Id = p.Id,
            Dob = p.Dob,
            Mobileno = p.Mobileno,
            Name = p.Name,
            Nationalno = p.Nationalno,
            FaceImgUrl = string.IsNullOrEmpty(p.FaceImg) ? null : $"{baseUrl}/images/{p.FaceImg}"
        }).ToList();

        return new PatientPagedResultDto { Results = dtos, TotalResults = total };
    }

    public async Task<int> AddAsync(CreatePatientVM dto)
    {
        if (await _repo.NationalIdExistsAsync(dto.Nationalno))
            throw new Exception("NationalIdExists"); // خليها Custom Exception بعدين

        var patient = new Patient
        {
            Name = dto.Name,
            Dob = DateTime.Parse(dto.Dob),
            Mobileno = dto.Mobileno,
            Nationalno = dto.Nationalno,
            FaceImg = null
        };

        return await _repo.AddAsync(patient);
    }

    public async Task<List<PatientDto>> SearchAsync(string searchText, string baseUrl)
    {
        var items = await _repo.SearchAsync(searchText);
        return items.Select(p => new PatientDto
        {
            Id = p.Id,
            Name = p.Name,
            Mobileno = p.Mobileno,
            Dob = p.Dob,
            Nationalno = p.Nationalno,
            FaceImgUrl = string.IsNullOrEmpty(p.FaceImg) ? null : $"{baseUrl}/images/{p.FaceImg}"
        }).ToList();
    }

    public async Task<PatientDto?> GetByIdAsync(int id, string baseUrl)
    {
        var p = await _repo.GetByIdAsync(id);
        if (p == null) return null;

        return new PatientDto
        {
            Id = p.Id,
            Dob = p.Dob,
            Mobileno = p.Mobileno,
            Name = p.Name,
            Nationalno = p.Nationalno,
            FaceImgUrl = string.IsNullOrEmpty(p.FaceImg) ? null : $"{baseUrl}/images/{p.FaceImg}"
        };
    }

    public async Task UpdateAsync(Patient updatedPatient)
    {
        var existing = await _repo.GetByIdAsync(updatedPatient.Id);
        if (existing == null) throw new Exception("Patient not found");

        existing.Name = updatedPatient.Name;
        existing.Mobileno = updatedPatient.Mobileno;
        if (!string.IsNullOrEmpty(updatedPatient.StrDob))
            existing.Dob = DateTime.Parse(updatedPatient.StrDob);
        existing.Nationalno = updatedPatient.Nationalno;

        await _repo.UpdateAsync(existing);

        // لو عايز reload encodings بعد update (حسب منطقك)
        await _encoding.ReloadEncodingsAsync();
    }

    public async Task<string> UploadFaceImageAsync(int patientId, IFormFile file, string contentRootPath)
    {
        if (file == null || file.Length == 0)
            throw new Exception("No file uploaded");

        var patient = await _repo.GetByIdAsync(patientId);
        if (patient == null) throw new Exception("Patient not found");

        var imagesDir = Path.Combine(contentRootPath, "images");
        Directory.CreateDirectory(imagesDir);

        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(imagesDir, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        patient.FaceImg = uniqueFileName;
        await _repo.UpdateAsync(patient);

        // Generate encoding
        var encodingPath = await _encoding.GenerateEncodingAsync(patientId, filePath);
        patient.EncodingFile = encodingPath;
        await _repo.UpdateAsync(patient);

        await _encoding.ReloadEncodingsAsync();

        return uniqueFileName;
    }

    public async Task DeleteAsync(int id)
    {
        var patient = await _repo.GetByIdAsync(id);
        if (patient == null) return;

        await _repo.DeleteAsync(patient);
        await _encoding.ReloadEncodingsAsync();
    }

    public Task DeleteAllAsync() => _repo.DeleteAllAsync();
}