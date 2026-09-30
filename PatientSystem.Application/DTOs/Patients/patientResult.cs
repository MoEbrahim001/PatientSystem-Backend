using PatientSystem.Domain.Entities;

namespace PatientSystem.Classes
{
    public class patientResult
    {
        public IEnumerable<Patient> results{get;set;}
        public int totalResults { get; set; }

    }
}
