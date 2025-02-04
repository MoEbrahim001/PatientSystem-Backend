using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatientSystem.Models;
using PatientSystem.NewFolder;
using System.Drawing; // You may need to install System.Drawing.Common
using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using PatientSystem.ResponseDTO;
using Accord.Math;
using System.Diagnostics;
using System.Text;
using PatientSystem.Classes;



[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly PatientSystemDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly FaceRecognizer _faceRecognizer;



    public PatientsController(PatientSystemDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _faceRecognizer = new LBPHFaceRecognizer(); // Use LBPH for simplicity
      
    }

    //[HttpGet]
    //public async Task<ActionResult<IEnumerable<Patient>>> GetPatients()
    //{
    //    var baseUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}";

    //    return await _context.Patients.Select(p => new Patient
    //    {
    //        Id = p.Id,
    //        Dob = p.Dob,
    //        Mobileno = p.Mobileno,
    //        Name = p.Name,
    //        Nationalno = p.Nationalno,
    //        FaceImg = $"{baseUrl}/images/{p.FaceImg}"
    //    }).ToListAsync();
    //}
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


    private bool IsBase64String(string base64String)
    {
        if (string.IsNullOrEmpty(base64String))
            return false;

        Span<byte> buffer = new Span<byte>(new byte[base64String.Length]);
        return Convert.TryFromBase64String(base64String, buffer, out _);
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










    //[HttpPost]
    //public async Task<ActionResult<Patient>> AddPatient(Patient patient)
    //{
    //    if (patient.StrDOB != "")
    //        patient.DOB = DateTime.Parse(patient.StrDOB.ToString());


    //    _context.Patients.Add(patient);
    //    await _context.SaveChangesAsync();
    //    return Ok(patient.Id);
    //}

    //[HttpPost("save")]
    //public async Task<IActionResult> SavePatientImage([FromForm] IFormFile file, [FromQuery] string patientName)
    //{
    //    // Check if the uploaded file is valid
    //    if (file == null || file.Length == 0)
    //    {
    //        return BadRequest(new { Message = "No file uploaded or the file is empty." });
    //    }

    //    // Check if the patient name is valid
    //    if (string.IsNullOrWhiteSpace(patientName))
    //    {
    //        return BadRequest(new { Message = "Patient name is missing or invalid." });
    //    }

    //    try
    //    {
    //        // Set the image file name as <patientName>.jpg
    //        var imageFileName = $"{patientName}.jpg";

    //        // Define the path where the image will be saved
    //        var specificDirectoryPath = @"C:\Users\dell\Desktop\Patient\PatientReco\Images"; // Adjust to your path
    //        var filePath = Path.Combine(specificDirectoryPath, imageFileName);

    //        // Ensure the directory exists
    //        if (!Directory.Exists(specificDirectoryPath))
    //        {
    //            Directory.CreateDirectory(specificDirectoryPath);
    //        }

    //        // Save the file to the specified directory
    //        using (var stream = new FileStream(filePath, FileMode.Create))
    //        {
    //            await file.CopyToAsync(stream);
    //        }

    //        // Add a new patient record with the image filename
    //        var newPatient = new Patient
    //        {
    //            Name = patientName,
    //            FaceImg = imageFileName // Save only the filename in the database
    //        };

    //        // Save the patient data to the database
    //        _context.Patients.Add(newPatient);
    //        await _context.SaveChangesAsync();

    //        return Ok(new { Message = "Image and patient data saved successfully." });
    //    }
    //    catch (Exception ex)
    //    {
    //        // Log the exception details for troubleshooting
    //        Console.WriteLine($"Error saving image: {ex.Message}");
    //        Console.WriteLine(ex.StackTrace); // Log stack trace for debugging

    //        // Return a server error response with the exception message
    //        return StatusCode(500, new { Message = $"Error saving image: {ex.Message}" });
    //    }
    //}
    //[HttpPost]
    //public async Task<ActionResult<Patient>> AddPatientImage([FromForm] Patient patient, [FromForm] IFormFile faceImage)
    //{
    //    if (faceImage != null && faceImage.Length > 0)
    //    {
    //        var fileName = $"{patient.Name}.jpg";
    //        var filePath = Path.Combine("C:\\Users\\dell\\Desktop\\Patient\\PatientReco\\Images", fileName);

    //        using (var stream = new FileStream(filePath, FileMode.Create))
    //        {
    //            await faceImage.CopyToAsync(stream);
    //        }

    //        patient.FaceImg = fileName;
    //    }

    //    _context.Patients.Add(patient);
    //    await _context.SaveChangesAsync();
    //    return Ok(patient.Id);
    //}



    //[HttpGet("search")]
    //public async Task<IActionResult> SearchPatients([FromQuery] string searchText)
    //{
    //    var baseUrl = $"{Request.Scheme}://{Request.Host}";

    //    var patients = await _context.Patients
    //        .Where(p => p.Name.Contains(searchText) || p.Mobileno.Contains(searchText))
    //        .Select(p => new
    //        {
    //            p.Id,
    //            p.Name,
    //            p.Mobileno,
    //            p.Dob,
    //            p.Nationalno,
    //            FaceImg = $"{baseUrl}/images/{p.FaceImg}" // Construct full URL for FaceImg
    //        })
    //        .ToListAsync(); // Use ToListAsync for asynchronous operation

    //    return Ok(patients);
    //}

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



    // PatientsController.cs

    //[HttpGet("{id}")]
    //public async Task<ActionResult<Patient>> GetPatientById(int id)
    //{
    //    var patient = await _context.Patients.FindAsync(id); // Assuming _context is your DbContext
    //    if (patient == null)
    //    {
    //        return NotFound();
    //    }
    //    return patient; // Return the patient details
    //}u

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
    //[HttpPut("{id}")]
    //public async Task<IActionResult> UpdatePatient(int id, [FromForm] Patient updatedPatient, IFormFile? file)
    //{
    //    if (id != updatedPatient.Id)
    //    {
    //        return BadRequest("Patient ID mismatch.");
    //    }

    //    var existingPatient = await _context.Patients.FindAsync(id);
    //    if (existingPatient == null)
    //    {
    //        return NotFound();
    //    }

    //    // Update properties
    //    existingPatient.Name = updatedPatient.Name;
    //    existingPatient.Mobileno = updatedPatient.Mobileno;
    //    existingPatient.Dob = updatedPatient.Dob;
    //    existingPatient.Nationalno = updatedPatient.Nationalno;

    //    // Check if a new image file was provided
    //    if (file != null && file.Length > 0)
    //    {
    //        // Here, save the file to your server (e.g., to a folder or cloud storage)
    //        // Update the patient's FaceImg path with the new image path
    //        string newFilePath = Path.Combine("C:\\Users\\dell\\source\\repos\\PatientSystem\\images", file.FileName);
    //        using (var stream = new FileStream(newFilePath, FileMode.Create))
    //        {
    //            await file.CopyToAsync(stream);
    //        }

    //        existingPatient.FaceImg = newFilePath;
    //    }

    //    await _context.SaveChangesAsync(); // Save changes to the database
    //    return Ok(existingPatient);
    //}


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





    //public async Task ReloadEncodingsAsync(int patientId, string faceImagePath)
    //{
    //    using var httpClient = new HttpClient();
    //    httpClient.Timeout = TimeSpan.FromSeconds(30); // Set a reasonable timeout

    //    // Optionally, add headers if needed by the Flask API
    //    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

    //    // Prepare data to be sent to the Python API
    //    var requestData = new
    //    {
    //        PatientId = patientId,        // Pass the patient ID
    //        ImagePath = faceImagePath     // Pass the image path
    //    };

    //    var jsonContent = new StringContent(
    //        JsonConvert.SerializeObject(requestData),
    //        Encoding.UTF8,
    //        "application/json"
    //    );

    //    // Send the request to the Python API to reload the encodings
    //    var response = await httpClient.PostAsync("http://localhost:5000/reload_encodings", jsonContent);

    //    if (!response.IsSuccessStatusCode)
    //    {
    //        // Log or handle the error
    //        var errorDetails = await response.Content.ReadAsStringAsync();
    //        throw new Exception($"Failed to update encodings: {response.ReasonPhrase}. Details: {errorDetails}");
    //    }
    //}





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

    //[HttpDelete("{id}")]
    //public IActionResult DeletePatient(int id)
    //{
    //    var patient = _context.Patients.Find(id);
    //    if (patient == null)
    //    {
    //        return NotFound(); // Return 404 if patient not found
    //    }

    //    _context.Patients.Remove(patient);
    //    _context.SaveChanges(); // Save changes to the database
    //    return Ok(); // Return 200 OK on successful deletion
    //}
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


    [HttpPost("detectAndFind")]
    public IActionResult DetectAndFind([FromForm] IFormFile file)
    {
        try
        {
            // Save the uploaded file temporarily
            var tempImagePath = Path.GetTempFileName();
            using (var fileStream = new FileStream(tempImagePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            // Path to Python executable and script
            var pythonExecutable = "python"; // Use full path if needed
            var pythonScript = @"C:\Users\dell\source\repos\PatientSystem\bin\Debug\net8.0\Script.py";
            var knownImagesFolder = @"C:\Users\dell\source\repos\PatientSystem\images";

            // Run the Python script
            var processStartInfo = new ProcessStartInfo
            {
                FileName = pythonExecutable,
                Arguments = $"-c \"import cv2; print(cv2.__version__)\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            // Capture the output
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // Handle Python script output
            if (!string.IsNullOrEmpty(error))
            {
                return StatusCode(500, new { Message = $"Error from Python script: {error}" });
            }

            // Deserialize Python script output
            var results = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(output);
            string matchedName = results?.matched_name;

            if (!string.IsNullOrEmpty(matchedName))
            {
                // Query the database for patient data
                var patient = _context.Patients.FirstOrDefault(p => p.Name == matchedName);
                if (patient != null)
                {
                    return Ok(new
                    {
                        Message = "Match found",
                        Patient = new
                        {
                            patient.Id,
                            patient.Name,
                            patient.Dob,
                            patient.Nationalno,
                            patient.FaceImg

                        }
                    });
                }
                else
                {
                    return NotFound(new { Message = "Patient record not found in the database" });
                }
            }
            else
            {
                return NotFound(new { Message = "No matching face found" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = $"Error detecting face: {ex.Message}" });
        }
    }




    //[HttpPost("detectAndFind")]
    //public IActionResult DetectAndFind([FromForm] IFormFile file)
    //{
    //    try
    //    {
    //        using var memoryStream = new MemoryStream();
    //        file.CopyTo(memoryStream);
    //        var imageData = memoryStream.ToArray();
    //        using var bitmap = new Bitmap(new MemoryStream(imageData));

    //        // Convert the Bitmap to EmguCV image
    //        var capturedImage = ConvertBitmapToImage(bitmap);

    //        // Preprocessing: Normalize lighting conditions
    //        var grayCapturedImage = capturedImage.Convert<Gray, byte>();

    //        // Apply Histogram Equalization
    //        var equalizedImage = grayCapturedImage.Clone();
    //        CvInvoke.EqualizeHist(grayCapturedImage, equalizedImage);

    //        // Apply Gaussian Blur to reduce noise
    //        var denoisedImage = new Mat();
    //        CvInvoke.GaussianBlur(equalizedImage, denoisedImage, new Size(5, 5), 0);

    //        // Face detection using Haarcascade
    //        var faceCascade = new CascadeClassifier("C:\\Users\\dell\\source\\repos\\PatientSystem\\bin\\Debug\\net8.0\\Haarcascade\\haarcascade_frontalface_default.xml");
    //        var detectedFaces = faceCascade.DetectMultiScale(denoisedImage, 1.1, 10, new Size(100, 100), new Size(500, 500));

    //        if (detectedFaces.Length == 0)
    //        {
    //            return Ok(new { isMatch = false, Message = "No face detected in the image." });
    //        }

    //        var faceRect = detectedFaces[0];
    //        var faceRegion = equalizedImage.GetSubRect(faceRect).Clone();

    //        // Load Haarcascades for face parts
    //        var eyeCascade = new CascadeClassifier("C:\\Users\\dell\\source\\repos\\PatientSystem\\bin\\Debug\\net8.0\\Haarcascade\\haarcascade_eye.xml");
    //        var noseCascade = new CascadeClassifier("C:\\Users\\dell\\source\\repos\\PatientSystem\\bin\\Debug\\net8.0\\Haarcascade\\haarcascade_mcs_nose.xml");
    //        var mouthCascade = new CascadeClassifier("C:\\Users\\dell\\source\\repos\\PatientSystem\\bin\\Debug\\net8.0\\Haarcascade\\haarcascade_mcs_mouth.xml");

    //        // Detect eyes, nose, and mouth within the face region
    //        var eyes = eyeCascade.DetectMultiScale(faceRegion, 1.1, 10, new Size(20, 20), new Size(100, 100));
    //        var noses = noseCascade.DetectMultiScale(faceRegion, 1.1, 10, new Size(20, 20), new Size(100, 100));
    //        var mouths = mouthCascade.DetectMultiScale(faceRegion, 1.1, 10, new Size(20, 20), new Size(150, 150));

    //        // Store detected parts for response
    //        var detectedParts = new
    //        {
    //            Eyes = eyes.Select(e => new { e.X, e.Y, e.Width, e.Height }).ToList(),
    //            Noses = noses.Select(n => new { n.X, n.Y, n.Width, n.Height }).ToList(),
    //            Mouths = mouths.Select(m => new { m.X, m.Y, m.Width, m.Height }).ToList()
    //        };

    //        // Match the face using existing functionality
    //        var patientImagesFolder = @"C:\Users\dell\source\repos\PatientSystem\images";
    //        var patientImageFiles = Directory.GetFiles(patientImagesFolder, "*.*")
    //                                         .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
    //                                                     f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
    //                                         .ToArray();

    //        if (patientImageFiles.Length == 0)
    //        {
    //            return Ok(new { isMatch = false, Message = "No patient images found in the directory." });
    //        }

    //        foreach (var patientImagePath in patientImageFiles)
    //        {
    //            try
    //            {
    //                using var patientImage = new Image<Gray, byte>(patientImagePath);
    //                var resizedCapturedFace = faceRegion.Resize(100, 100, Emgu.CV.CvEnum.Inter.Linear);
    //                var resizedPatientImage = patientImage.Resize(100, 100, Emgu.CV.CvEnum.Inter.Linear);

    //                var similarity = CompareImages(resizedCapturedFace, resizedPatientImage);
    //                if (similarity > 0.5)
    //                {
    //                    var FaceImg = Path.GetFileNameWithoutExtension(patientImagePath).Trim();
    //                    var matchingPatient = _context.Patients
    //                        .FirstOrDefault(p => EF.Functions.Like(p.FaceImg, $"{FaceImg}%"));

    //                    if (matchingPatient != null)
    //                    {
    //                        return Ok(new
    //                        {
    //                            isMatch = true,
    //                            detectedParts,
    //                            patient = new
    //                            {
    //                                matchingPatient.Id,
    //                                matchingPatient.Name,
    //                                matchingPatient.Dob,
    //                                matchingPatient.Mobileno,
    //                                matchingPatient.Nationalno,
    //                                matchingPatient.FaceImg
    //                            }
    //                        });
    //                    }
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                Console.WriteLine($"Error processing image {patientImagePath}: {ex.Message}");
    //            }
    //        }

    //        return Ok(new { isMatch = false, detectedParts });
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error detecting face: {ex.Message}");
    //        return StatusCode(500, new { Message = $"Error detecting face: {ex.Message}" });
    //    }
    //}









    //[HttpPost("detectAndFind")]
    //public IActionResult DetectAndFind([FromForm] IFormFile file)
    //{
    //    try
    //    {
    //        using var memoryStream = new MemoryStream();
    //        file.CopyTo(memoryStream);
    //        var imageData = memoryStream.ToArray();
    //        using var bitmap = new Bitmap(new MemoryStream(imageData));

    //        // Convert the Bitmap to EmguCV image
    //        var capturedImage = ConvertBitmapToImage(bitmap);

    //        // Perform face detection
    //        var faceCascade = new CascadeClassifier("C:\\Users\\dell\\source\\repos\\PatientSystem\\bin\\Debug\\net8.0\\Haarcascade\\haarcascade_frontalface_default.xml");
    //        var grayCapturedImage = capturedImage.Convert<Gray, byte>();
    //        var detectedFaces = faceCascade.DetectMultiScale(grayCapturedImage, 1.1, 10, new Size(100, 100), new Size(500, 500));

    //        if (detectedFaces.Length == 0)
    //        {
    //            return Ok(new { isMatch = false, Message = "No face detected in the image." });
    //        }

    //        var faceRect = detectedFaces[0];
    //        var croppedFace = grayCapturedImage.GetSubRect(faceRect).Clone();

    //        var patientImagesFolder = @"C:\Users\dell\source\repos\PatientSystem\images";

    //        // Update the file extension filter to include both .jpg and .png files
    //        var patientImageFiles = Directory.GetFiles(patientImagesFolder, "*.*")
    //                                          .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
    //                                                      f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
    //                                          .ToArray();

    //        if (patientImageFiles.Length == 0)
    //        {
    //            Console.WriteLine("No patient images found.");
    //            return Ok(new { isMatch = false, Message = "No patient images found in the directory." });
    //        }

    //        // Debug: Print all FaceImg values in database
    //        var allFaceImgs = _context.Patients.Select(p => p.FaceImg).ToList();
    //        Console.WriteLine("FaceImg values in database:");
    //        foreach (var faceImg in allFaceImgs)
    //        {
    //            Console.WriteLine(faceImg);
    //        }

    //        foreach (var patientImagePath in patientImageFiles)
    //        {
    //            try
    //            {
    //                using var patientImage = new Image<Gray, byte>(patientImagePath);

    //                // Resize both images to the same size for comparison
    //                var resizedCapturedFace = croppedFace.Resize(100, 100, Emgu.CV.CvEnum.Inter.Linear);
    //                var resizedPatientImage = patientImage.Resize(100, 100, Emgu.CV.CvEnum.Inter.Linear);

    //                // Compare images
    //                var similarity = CompareImages(resizedCapturedFace, resizedPatientImage);

    //                Console.WriteLine($"Comparing with {Path.GetFileName(patientImagePath)}: Similarity = {similarity}");

    //                if (similarity > 0.4)
    //                {
    //                    var FaceImg = Path.GetFileNameWithoutExtension(patientImagePath).Trim();

    //                    // Debug: Check the extracted FaceImg value
    //                    Console.WriteLine($"Extracted FaceImg: {FaceImg}");

    //                    var patientData = _context.Patients
    //                        .FirstOrDefault(p => p.FaceImg.Equals(FaceImg, StringComparison.OrdinalIgnoreCase));

    //                    if (patientData != null)
    //                    {
    //                        return Ok(new
    //                        {
    //                            isMatch = true,
    //                            patient = new
    //                            {
    //                                patientData.Id,
    //                                patientData.Name,
    //                                patientData.StrDob,
    //                                patientData.Mobileno,
    //                                patientData.Nationalno,
    //                                FaceImgUrl = patientData.FaceImg
    //                            }
    //                        });
    //                    }
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                Console.WriteLine($"Error processing image {patientImagePath}: {ex.Message}");
    //            }
    //        }

    //        // If no match is found, return a response indicating so
    //        return Ok(new { isMatch = false });
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error detecting face: {ex.Message}");
    //        return StatusCode(500, new { Message = $"Error detecting face: {ex.Message}" });
    //    }
    //}



    private System.Drawing.Rectangle GetFaceRectangleFromLandmarks(List<System.Drawing.Point> landmarks)
    {
        if (landmarks == null || landmarks.Count == 0)
        {
            return System.Drawing.Rectangle.Empty;
        }

        // Find the minimum and maximum X and Y coordinates of the landmarks
        var minX = landmarks.Min(p => p.X);
        var minY = landmarks.Min(p => p.Y);
        var maxX = landmarks.Max(p => p.X);
        var maxY = landmarks.Max(p => p.Y);

        // Create a rectangle from the min and max coordinates
        var rect = new System.Drawing.Rectangle(minX, minY, maxX - minX, maxY - minY);

        // Optionally, add some padding to the rectangle
        const int padding = 10;
        rect.Inflate(padding, padding);

        return rect;
    }






    private double CompareImages(Image<Gray, byte> img1, Image<Gray, byte> img2)
    {
        // Resize to the same size
        img1 = img1.Resize(100, 100, Inter.Linear);
        img2 = img2.Resize(100, 100, Inter.Linear);

        // Optional: Apply histogram equalization to enhance image features
        img1._EqualizeHist();
        img2._EqualizeHist();

        // Using template matching with normalized correlation coefficient
        using var result = new Mat();
        CvInvoke.MatchTemplate(img1, img2, result, TemplateMatchingType.CcoeffNormed);

        // Get the minimum and maximum similarity values
        result.MinMax(out _, out double[] maxValues, out _, out _);

        // Return the maximum similarity value found in the match template result
        return maxValues[0];
    }
    //private double CompareImages(Image<Gray, byte> img1, Image<Gray, byte> img2)
    //{
    //    // Resize both images to the same size
    //    img1 = img1.Resize(100, 100, Inter.Linear);
    //    img2 = img2.Resize(100, 100, Inter.Linear);

    //    // Compute SSIM using Emgu CV's Compare function
    //    using var diff = new Mat();
    //    CvInvoke.AbsDiff(img1, img2, diff); // Calculate absolute difference
    //    var meanScalar = CvInvoke.Mean(diff); // Mean of differences

    //    // Convert the mean scalar to a single difference value
    //    var difference = (meanScalar.V0 + meanScalar.V1 + meanScalar.V2) / 3;

    //    // Convert to similarity score (lower difference = higher similarity)
    //    return 1.0 - (difference / 255.0);
    //}
    //private double CompareImages(Image<Gray, byte> img1, Image<Gray, byte> img2)
    //{
    //    // Convert images to grayscale and resize
    //    img1 = img1.Resize(100, 100, Inter.Linear);
    //    img2 = img2.Resize(100, 100, Inter.Linear);

    //    // ORB detector and matcher
    //    using var orb = new ORBDetector();
    //    var keyPoints1 = new VectorOfKeyPoint();
    //    var keyPoints2 = new VectorOfKeyPoint();

    //    using var descriptors1 = new Mat();
    //    using var descriptors2 = new Mat();

    //    // Detect and compute keypoints and descriptors
    //    orb.DetectAndCompute(img1, null, keyPoints1, descriptors1, false);
    //    orb.DetectAndCompute(img2, null, keyPoints2, descriptors2, false);

    //    // Match features
    //    using var matcher = new BFMatcher(DistanceType.Hamming);
    //    var matches = matcher.Match(descriptors1, descriptors2);

    //    // Filter matches using a threshold
    //    var goodMatches = matches.Where(m => m.Distance < 30).ToList();

    //    // Calculate similarity as the ratio of good matches to total keypoints
    //    double similarity = (double)goodMatches.Count / Math.Min(keyPoints1.Size, keyPoints2.Size);
    //    return similarity;
    //}




    private Image<Bgr, byte> ConvertBitmapToImage(Bitmap bitmap)
    {
        var img = new Image<Bgr, byte>(bitmap.Width, bitmap.Height);
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                img.Data[y, x, 0] = color.B;
                img.Data[y, x, 1] = color.G;
                img.Data[y, x, 2] = color.R;
            }
        }
        return img;
    }



    //private Image<Bgr, byte> ConvertBitmapToImage(Bitmap bitmap)
    //{
    //    var img = new Image<Bgr, byte>(bitmap.Width, bitmap.Height);
    //    for (int y = 0; y < bitmap.Height; y++)
    //    {
    //        for (int x = 0; x < bitmap.Width; x++)
    //        {
    //            var color = bitmap.GetPixel(x, y);
    //            img.Data[y, x, 0] = color.B;
    //            img.Data[y, x, 1] = color.G;
    //            img.Data[y, x, 2] = color.R;
    //        }
    //    }
    //    return img;
    //}
}