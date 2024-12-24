using PatientSystem.Models;
using System;
using System.Threading.Tasks;
namespace PatientSystem.Services
{
    public class PatientService:IPatientService
    {
        private readonly IPatientService patientService;

        // Inject the repository to interact with the database
        public PatientService(IPatientService _patientService)
        {
            _patientService = patientService;
        }

        // Fetch patient data by name
        public async Task<Patient> SearchPatientByNameAsync(string patientName)
        {
            if (string.IsNullOrEmpty(patientName))
                throw new ArgumentException("Patient name cannot be null or empty.");

            // Assuming the repository can fetch data from a database or API
            var patient = await patientService.SearchPatientByNameAsync(patientName);

            if (patient == null)
            {
                throw new Exception("Patient not found.");
            }

            return patient;
        }

        // Fetch the patient's image URL or path
        public async Task<string> GetPatientImageAsync(int patientId)
        {
            if (patientId <= 0)
                throw new ArgumentException("Invalid patient ID.");

            var imagePath = await patientService.GetPatientImageAsync(patientId);

            if (string.IsNullOrEmpty(imagePath))
            {
                throw new Exception("Patient image not found.");
            }

            return imagePath;
        }
    }
}
