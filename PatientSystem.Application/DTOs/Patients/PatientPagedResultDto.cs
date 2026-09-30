using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatientSystem.Application.DTOs.Patients
{
    public class PatientPagedResultDto
    {
        public List<PatientDto> Results { get; set; } = new();
        public int TotalResults { get; set; }
    }
}
