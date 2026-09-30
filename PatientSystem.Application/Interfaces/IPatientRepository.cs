using PatientSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatientSystem.Application.Interfaces
{
    public interface IPatientRepository
    {
        Task<(List<Patient> Items, int Total)> GetPagedAsync(int skip, int take, string? searchText);
        Task<bool> NationalIdExistsAsync(string nationalNo);
        Task<Patient?> GetByIdAsync(int id);
        Task<List<Patient>> SearchAsync(string searchText);
        Task<int> AddAsync(Patient patient);
        Task UpdateAsync(Patient patient);
        Task DeleteAsync(Patient patient);
        Task DeleteAllAsync();
    }
}

