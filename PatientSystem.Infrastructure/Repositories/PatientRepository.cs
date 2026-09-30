using Microsoft.EntityFrameworkCore;
using PatientSystem.Application.Interfaces;
using PatientSystem.Domain.Entities;
using PatientSystem.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatientSystem.Infrastructure.Repositories
{
    public class PatientRepository : IPatientRepository
    {
        private readonly PatientSystemDbContext _context;
        public PatientRepository(PatientSystemDbContext context) => _context = context;

        public async Task<(List<Patient> Items, int Total)> GetPagedAsync(int skip, int take, string? searchText)
        {
            var query = _context.Patients.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
                query = query.Where(p => p.Name.Contains(searchText) || p.Mobileno.Contains(searchText));

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (items, total);
        }

        public Task<bool> NationalIdExistsAsync(string nationalNo)
            => _context.Patients.AnyAsync(p => p.Nationalno == nationalNo);

        public Task<Patient?> GetByIdAsync(int id)
            => _context.Patients.FirstOrDefaultAsync(p => p.Id == id);

        public Task<List<Patient>> SearchAsync(string searchText)
            => _context.Patients.AsNoTracking()
                .Where(p => p.Name.Contains(searchText) || p.Mobileno.Contains(searchText))
                .ToListAsync();

        public async Task<int> AddAsync(Patient patient)
        {
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
            return patient.Id;
        }

        public async Task UpdateAsync(Patient patient)
        {
            _context.Patients.Update(patient);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Patient patient)
        {
            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAllAsync()
        {
            var all = await _context.Patients.ToListAsync();
            _context.Patients.RemoveRange(all);
            await _context.SaveChangesAsync();
        }
    }
}
