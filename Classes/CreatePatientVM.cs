namespace PatientSystem.NewFolder
{
    public class CreatePatientVM
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        //public DateTime? DOB { get; set; }

        //[NotMapped]
        public string Dob { get; set; }
        public string? Mobileno { get; set; }


        public string? Nationalno { get; set; }

        public string? FaceImg { get; set; }

    }
}
