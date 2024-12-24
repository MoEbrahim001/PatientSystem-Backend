
using System.Threading.Tasks;

using PatientSystem.Models;

namespace PatientSystem.Services
{
    public interface IPatientService
    {
        Task<Patient> SearchPatientByNameAsync(string patientName);

        // Method to fetch patient's image based on patient ID
        Task<string> GetPatientImageAsync(int patientId);
    }
}
