using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace PatientSystem.Models;

public partial class Patient
{
    public int Id { get; set; }

    public string? Name { get; set; }

    //public DateTime? DOB { get; set; }

    [NotMapped]
    public string? StrDob { get; set; }
    public DateTime Dob { get; set; } 
    public string? Mobileno { get; set; } 

    public string? Nationalno { get; set; }
    
 public string? FaceImg { get; set; }
    [NotMapped]
    public DateTime LastModified { get; set; }




}
