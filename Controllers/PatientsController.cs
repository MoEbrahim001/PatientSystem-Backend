using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatientSystem.Models;
using PatientSystem.NewFolder;

using PatientSystem.ResponseDTO;

using System.Text;
using PatientSystem.Classes;



[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly PatientSystemDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;



    public PatientsController(PatientSystemDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
      
    }

   
    [HttpPost]
    public async Task<ActionResult<patientResult>> GetPatients([FromBody]  patientParams patientParams)
    {
        var baseUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}";
        // Filter patients based on the 'lastModifiedAfter' timestamp

        var query = _context.Patients.AsQueryable();
        if (!string.IsNullOrEmpty(patientParams.searchtext))
        {
            query=query.Where(p => p.Name.Contains(patientParams.searchtext) || p.Mobileno.Contains(patientParams.searchtext));

        }
        var totalPatients = await query.CountAsync();
        var patients = await query.Skip(patientParams.first).Take(patientParams.rows).Select(p => new Patient
        {
            Id = p.Id,
            Dob = p.Dob,
            Mobileno = p.Mobileno,
            Name = p.Name,
            Nationalno = p.Nationalno,
            FaceImg = $"{baseUrl}/images/{p.FaceImg}"
        }).ToListAsync();

        return new patientResult { results = patients , totalResults = totalPatients };
    }




    [HttpPost("addPatient")]
    public async Task<ActionResult<int>> AddPatient([FromBody] CreatePatientVM patientData)
    {
        try
        {
            var nationalIdExists = await _context.Patients.AnyAsync(p => p.Nationalno == patientData.Nationalno);

            if (nationalIdExists)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response()
                {
                    status = "NationalIdExists",
                    errorMsg = "National Id Already Exists",
                    errorMsgAr = "الرقم القومي موجود بالفعل"
                });
            }

            // Add patient data to the database, FaceImg can be null
            var Patient = new Patient()
            {
                Name = patientData.Name,
                Dob = DateTime.Parse(patientData.Dob),
                Mobileno = patientData.Mobileno,
                Nationalno = patientData.Nationalno,
                FaceImg = null // FaceImg will be updated later
            };

            _context.Patients.Add(Patient);
            await _context.SaveChangesAsync();

            return Patient.Id; // Return the patient ID
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving patient: {ex.Message}");
            return StatusCode(500, "Internal server error.");
        }
    }



    private async Task<string> GenerateEncodingFile(int patientId, string faceImagePath)
    {
        try
        {
            // Prepare the face image path (ensure it's the correct path to the image)
            var faceImageFilePath = Path.Combine("C:\\Users\\dell\\source\\repos\\PatientSystem\\images", faceImagePath);

            // Rename the variable to avoid conflict with ControllerBase.File
            if (!System.IO.File.Exists(faceImageFilePath))
            {
                throw new FileNotFoundException($"Face image file not found at path: {faceImageFilePath}");
            }

            // Convert backslashes to forward slashes for URL compatibility
            var faceImageFilePathForUrl = faceImageFilePath.Replace("\\", "/");

            // Call the Python service (Flask API) to generate the encoding for the face image
            var client = new HttpClient();
            var jsonContent = new StringContent($"{{\"patientId\": {patientId}, \"faceImage\": \"{faceImageFilePathForUrl}\"}}", Encoding.UTF8, "application/json");

            var response = await client.PostAsync("http://localhost:5000/generate_encoding", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                // Get the encoding file path from Flask
                var encodingFilePath = await response.Content.ReadAsStringAsync();
                return encodingFilePath;
            }
            else
            {
                // Handle error response from Flask server
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to generate encoding file. Response: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            // Log and throw exception
            throw new Exception($"Error generating encoding file: {ex.Message}");
        }
    }


    [HttpGet("search")]
    public async Task<IActionResult> SearchPatients([FromQuery] string searchText)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var patients = await _context.Patients
            .Where(p => p.Name.Contains(searchText) || p.Mobileno.Contains(searchText))
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Mobileno,
                p.Dob,
                p.Nationalno,
                FaceImg = $"{baseUrl}/images/{p.FaceImg}" // Construct full URL for FaceImg
            })
            .ToListAsync(); // Use ToListAsync for asynchronous operation

        return Ok(patients);
    }



    [HttpGet("{id}")]
    public async Task<ActionResult<Patient>> GetPatientById(int id)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient == null)
        {
            return NotFound();
        }
        patient.FaceImg = Path.Combine(Directory.GetCurrentDirectory(), "images\\") + patient.FaceImg;
        // Optionally, add base URL for image
        //patient.FaceImg = Url.Content($"~/images/{patient.FaceImg}");
        return patient;

    }
   


    [HttpPut]
    [Route("UpdatePatient")]
    public async Task<IActionResult> UpdatePatient(Patient updatedPatient)
    {
        // Find the existing patient record in the database
        var existingPatient = await _context.Patients.FindAsync(updatedPatient.Id);
        if (existingPatient == null)
        {
            return NotFound(new { message = "Patient not found" });
        }

        // Update patient properties
        existingPatient.Name = updatedPatient.Name;
        existingPatient.Mobileno = updatedPatient.Mobileno;
        if (updatedPatient.StrDob != null)
            existingPatient.Dob = DateTime.Parse(updatedPatient.StrDob.ToString());
        existingPatient.Nationalno = updatedPatient.Nationalno;

        // Save changes to the database
        await _context.SaveChangesAsync();
        Console.WriteLine($"Patient with ID {updatedPatient.Id} updated successfully.");

        // Check if the patient already has an encoding file
        var encodingFilePath = Path.Combine("C:\\Users\\dell\\source\\repos\\PatientSystem\\encodings", $"{updatedPatient.Id}_encoding.dat");
        if (!System.IO.File.Exists(encodingFilePath))
        {
            // If the encoding file does not exist, generate it
            _ = Task.Run(async () =>
            {
                try
                {
                    var generatedFilePath = await GenerateEncodingFile(updatedPatient.Id, updatedPatient.FaceImg);
                    Console.WriteLine($"Encoding file generated at: {generatedFilePath}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to generate encoding file for patient ID {updatedPatient.Id}: {ex.Message}");
                }
            });
        }

        // Trigger encodings update in the background
        _ = Task.Run(async () =>
        {
            try
            {
                await UpdateEncodingsAsync();
                Console.WriteLine("Encodings reloaded successfully.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to reload encodings: {ex.Message}");
            }
        });

        // Return the updated patient object as the response
        return Ok(existingPatient);
    }






    private async Task UpdateEncodingsAsync()
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30); // Set a reasonable timeout

        // Optionally, add headers if needed by the Flask API
        httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await httpClient.GetAsync("http://localhost:5000/reload_encodings");

        if (!response.IsSuccessStatusCode)
        {
            // Log or handle the error
            var errorDetails = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to update encodings: {response.ReasonPhrase}. Details: {errorDetails}");
        }
    }





   





    private bool PatientExists(int id)
    {
        return _context.Patients.Any(e => e.Id == id);
    }

    [HttpDelete("deleteAll")]
    public IActionResult DeleteAllPatients()
    {
        var patients = _context.Patients.ToList();
        if (!patients.Any())
        {
            return NotFound("No records found to delete.");
        }

        _context.Patients.RemoveRange(patients);
        _context.SaveChanges();

        return Ok("All records deleted successfully.");
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePatient(int id) // ID from the route
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient == null)
        {
            return NotFound(); // Return 404 if patient not found
        }

        _context.Patients.Remove(patient);
        await _context.SaveChangesAsync(); // Save changes to the database

        // Trigger encodings update in the background
        _ = Task.Run(async () =>
        {
            try
            {
                await UpdateEncodingsAsync();
                Console.WriteLine("Encodings reloaded successfully after deletion.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to reload encodings after deletion: {ex.Message}");
            }
        });

        return Ok(); // Return 200 OK on successful deletion
    }


    [HttpPost("uploadFaceImage/{patientId}")]
    public async Task<IActionResult> UploadFaceImage([FromForm] IFormFile file, int patientId)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                Console.WriteLine("File is null or empty");
                return BadRequest("No file uploaded.");
            }

            // Create the images directory if it doesn't exist
            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "images");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Generate a unique filename with a GUID
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(directoryPath, uniqueFileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            Console.WriteLine($"File uploaded successfully: {filePath}");

            // Update patient with the FaceImg path
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                return NotFound("This patientId not found");
            }

            patient.FaceImg = uniqueFileName;
            _context.Patients.Update(patient);
            await _context.SaveChangesAsync();

            // Generate encoding file for the uploaded face image
            try
            {
                var encodingFilePath = await GenerateEncodingFile(patientId, uniqueFileName);
                patient.EncodingFile = encodingFilePath;
                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to generate encoding file: {ex.Message}");
            }

            // Call UpdateEncodingsAsync to reload all encodings
            _ = Task.Run(async () =>
            {
                try
                {
                    await UpdateEncodingsAsync();
                    Console.WriteLine("Encodings reloaded successfully after image upload.");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to reload encodings after image upload: {ex.Message}");
                }
            });

            return Ok(new { filePath });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            return StatusCode(500, "Internal server error.");
        }
    }






    
}