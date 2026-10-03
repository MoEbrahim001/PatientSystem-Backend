using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatientSystem.Application.DTOs.Patients
{
    public class PatientDto
    {
        public int Id { get; set; }
        public DateTime? Dob { get; set; }
        public string? Mobileno { get; set; }
        public string? Name { get; set; }
        public string? Nationalno { get; set; }
        public string? FaceImgUrl { get; set; }
    }
}
