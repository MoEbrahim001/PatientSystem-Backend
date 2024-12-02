using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatientSystem.Models;
using PatientSystem.NewFolder;
using Microsoft.AspNetCore.Http;
using System.Drawing; // You may need to install System.Drawing.Common
using System.IO;
using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using System.Threading.Tasks;
using Emgu.CV.CvEnum;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using PatientSystem.ResponseDTO;
using Emgu.CV.Features2D;
using Emgu.CV.Util;
using Emgu.CV.XObjdetect;
using DlibDotNet;
using DlibDotNet.Extensions;
using Tensorflow;
using Tensorflow.Keras.Models;
using Tensorflow.Keras.Layers;
using Tensorflow.Sessions;
using Tensorflow.NumPy;
using Accord.Math;
using Accord;
using Accord.Statistics;
using Accord.Imaging.ComplexFilters;
using Accord.Imaging.ColorReduction;
using Accord.Imaging.Textures;
using Accord.Imaging.Formats;
using Accord.Imaging.Filters;
using Accord.Vision.Detection;
using Accord.Vision.Detection.Cascades;



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
        TrainFaceRecognizer();
    }
    // Update the GetPatients method to return only the filename for FaceImg
    //[HttpGet]
    //public async Task<ActionResult<IEnumerable<Patient>>> GetPatients()
    //{
    //    return await _context.Patients.Select(p => new Patient()
    //    {
    //        Id = p.Id,
    //        Dob = p.Dob,
    //        Mobileno = p.Mobileno,
    //        Name = p.Name,
    //        Nationalno = p.Nationalno,
    //        FaceImg = p.FaceImg // Only save the filename in the database
    //    }).ToListAsync();
    //}

    // Get all patients
    //[HttpGet]
    //public async Task<ActionResult<IEnumerable<Patient>>> GetPatients()
    //{
    //    return await _context.Patients.Select(p => new Patient() { Id = p.Id, Dob = p.Dob, Mobileno = p.Mobileno, Name = p.Name, Nationalno = p.Nationalno, FaceImg = Path.Combine(Directory.GetCurrentDirectory(), "images\\") + p.FaceImg }).ToListAsync();
    //}
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Patient>>> GetPatients()
    {
        var baseUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}";

        return await _context.Patients.Select(p => new Patient
        {
            Id = p.Id,
            Dob = p.Dob,
            Mobileno = p.Mobileno,
            Name = p.Name,
            Nationalno = p.Nationalno,
            FaceImg = $"{baseUrl}/images/{p.FaceImg}"
        }).ToListAsync();
    }
    //[HttpPost(" PatientWithImage")]
    //public async Task<IActionResult> AddPatientWithImage([FromForm] IFormFile file, [FromForm] string name, [FromForm] string nationalNo, [FromForm] string mobileNo, [FromForm] string strDOB)
    //{
    //    // Validate the input data
    //    if (file == null || file.Length == 0)
    //    {
    //        return BadRequest(new { Message = "No file uploaded or the file is empty." });
    //    }

    //    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(nationalNo) || string.IsNullOrWhiteSpace(mobileNo))
    //    {
    //        return BadRequest(new { Message = "Name, National Number, and Mobile Number are required." });
    //    }

    //    try
    //    {
    //        // Parse DOB if provided
    //        DateTime? dob = null;
    //        if (!string.IsNullOrWhiteSpace(strDOB))
    //        {
    //            dob = DateTime.Parse(strDOB);
    //        }

    //        // Create the patient record with the details
    //        var patient = new Patient
    //        {
    //            Name = name,
    //            Nationalno = nationalNo,
    //            Mobileno = mobileNo,
    //            DOB = dob,
    //            FaceImg = $"{name}.jpg" // Set the filename for the image
    //        };

    //        // Save the patient details to the database
    //        _context.Patients.Add(patient);
    //        await _context.SaveChangesAsync();

    //        // Define the directory and path for the image
    //        var specificDirectoryPath = @"C:\Users\dell\Desktop\Patient\PatientReco\Images"; // Adjust this path as needed
    //        if (!Directory.Exists(specificDirectoryPath))
    //        {
    //            Directory.CreateDirectory(specificDirectoryPath);
    //        }

    //        var filePath = Path.Combine(specificDirectoryPath, patient.FaceImg);

    //        // Save the image file
    //        using (var stream = new FileStream(filePath, FileMode.Create))
    //        {
    //            await file.CopyToAsync(stream);
    //        }

    //        return Ok(new { Message = "Patient data and image saved successfully.", PatientId = patient.Id });
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error: {ex.Message}");
    //        return StatusCode(500, new { Message = $"Error saving data: {ex.Message}" });
    //    }
    //}

    //[HttpPost("addPatientWithImage")]
    //public async Task<IActionResult> AddPatientWithImage([FromForm] IFormFile image, [FromForm] string name, [FromForm] string dob, [FromForm] string mobileno, [FromForm] string nationalno)
    //{
    //    // Ensure the image and required data fields are present
    //    if (image == null || string.IsNullOrEmpty(name))
    //    {
    //        return BadRequest("Missing required data.");
    //    }

    //    // Set the directory path where images will be stored
    //    var imagesDirectory = Path.Combine("wwwroot", "Images");
    //    if (!Directory.Exists(imagesDirectory))
    //    {
    //        Directory.CreateDirectory(imagesDirectory);
    //    }

    //    // Save the image with the patient's name
    //    var filePath = Path.Combine(imagesDirectory, $"{name}.png");
    //    using (var stream = new FileStream(filePath, FileMode.Create))
    //    {
    //        await image.CopyToAsync(stream);
    //    }

    //    // Save patient data to the database
    //    var patient = new Patient
    //    {
    //        Name = name,
    //        Dob = dob,
    //        Mobileno = mobileno,
    //        Nationalno = nationalno,
    //        FaceImg = filePath // Store the file path in the database
    //    };

    //    _context.Patients.Add(patient);
    //    await _context.SaveChangesAsync();

    //    return Ok(patient);
    //}

    [HttpPost("addPatient")]
    public async Task<ActionResult<int>> AddPatient([FromBody] CreatePatientVM patientData)
    {
        try
        {
            var nationalIdExists = await _context.Patients.AnyAsync(p => p.Nationalno == patientData.Nationalno);

            if (nationalIdExists)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response() { status = "NationalIdExists", errorMsg = "National Id Already Exists ", errorMsgAr = "الرقم القومي موجود بالفعل" });
            }
            // Add patient data including face image path to the database
            var Patient = new Patient() { Name = patientData.Name, Dob = DateTime.Parse(patientData.Dob), Mobileno = patientData.Mobileno, Nationalno = patientData.Nationalno, FaceImg = patientData.FaceImg };
            _context.Patients.Add(Patient);
            await _context.SaveChangesAsync();

            return Patient.Id; // Return the patient data with ID
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving patient: {ex.Message}");
            return StatusCode(500, "Internal server error.");
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
        //if (id != updatedPatient.Id)
        //{
        //    return BadRequest(new { message = $"Patient ID mismatch: URL ID {updatedPatient.Id} does not match Patient ID {updatedPatient.Id}" });
        //}

        var existingPatient = await _context.Patients.FindAsync(updatedPatient.Id);
        if (existingPatient == null)
        {
            return NotFound();
        }

        // Update properties
        existingPatient.Name = updatedPatient.Name;
        existingPatient.Mobileno = updatedPatient.Mobileno;
        if (updatedPatient.StrDob != null)
            existingPatient.Dob = DateTime.Parse(updatedPatient.StrDob.ToString());
        existingPatient.Nationalno = updatedPatient.Nationalno;

        await _context.SaveChangesAsync(); // Save changes to the database
        return Ok(existingPatient);
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
    public IActionResult DeletePatient(int id)
    {
        var patient = _context.Patients.Find(id);
        if (patient == null)
        {
            return NotFound(); // Return 404 if patient not found
        }

        _context.Patients.Remove(patient);
        _context.SaveChanges(); // Save changes to the database
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
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
                return NotFound("this patientId not Found");
            patient.FaceImg = uniqueFileName;
            await _context.SaveChangesAsync();
            return Ok(new { filePath });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            return StatusCode(500, "Internal server error.");
        }
    }
    private void TrainFaceRecognizer()
    {
        var faces = _context.Patients.Select(p => new { p.FaceImg, p.Id }).ToList();
        var trainingMats = new List<Mat>();
        var labels = new List<int>();

        foreach (var face in faces)
        {
            // Assuming face.FaceImg contains only the file name, combine it with a base directory path.
            string imagePath = Path.Combine("C:\\Users\\dell\\source\\repos\\PatientSystem\\images", face.FaceImg);

            // Check if the file exists
            if (System.IO.File.Exists(imagePath))
            {
                // Load the image as a Mat
                var image = new Image<Gray, byte>(imagePath).Mat;
                trainingMats.Add(image);
                labels.Add(face.Id);
            }
            else
            {
                Console.WriteLine($"Image file not found: {imagePath}");
            }
        }

        if (trainingMats.Count > 0)
        {
            _faceRecognizer.Train(trainingMats.ToArray(), labels.ToArray());
        }
        else
        {
            Console.WriteLine("No valid training images found.");
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

    //        // Load the image using Accord
    //        var bitmap = new Bitmap(new MemoryStream(imageData));

    //        // Convert the image to grayscale
    //        var grayImage = Grayscale.CommonAlgorithms.BT709.Apply(bitmap);

    //        // Load Haar cascade
    //        var cascadePath = "haarcascade_frontalface_default.xml";
    //        var cascade = new Accord.Vision.Detection.Cascades.HaarCascade(cascadePath);

    //        // Initialize the Haar Object Detector
    //        var detector = new Accord.Vision.Detection.HaarObjectDetector(cascade)
    //        {
    //            SearchMode = Accord.Vision.Detection.ObjectDetectorSearchMode.Average,
    //            ScalingMode = Accord.Vision.Detection.ObjectDetectorScalingMode.SmallerToGreater,
    //            ScaleFactor = 1.1f,
    //            MinSize = new Size(30, 30),
    //            MaxSize = new Size(300, 300)
    //        };

    //        // Detect faces
    //        var faces = detector.ProcessFrame(grayImage);
    //        if (faces.Length == 0)
    //        {
    //            return Ok(new { isMatch = false, Message = "No face detected in the image." });
    //        }

    //        // Extract the first detected face
    //        var faceRect = faces[0];
    //        var faceBitmap = grayImage.Clone(faceRect, grayImage.PixelFormat);

    //        // Save detected parts
    //        var detectedParts = new
    //        {
    //            Faces = faces.Select(f => new { f.X, f.Y, f.Width, f.Height }).ToList()
    //        };

    //        // Perform patient matching (reuse your existing logic)
    //        var patientImagesFolder = @"C:\Users\dell\source\repos\PatientSystem\images";
    //        var patientImageFiles = Directory.GetFiles(patientImagesFolder, "*.jpg");

    //        foreach (var patientImagePath in patientImageFiles)
    //        {
    //            var patientImage = new Bitmap(patientImagePath);
    //            var resizedCapturedFace = Accord.Imaging.ResizeTools.Resize(faceBitmap, 100, 100);
    //            var resizedPatientImage = Accord.Imaging.ResizeTools.Resize(patientImage, 100, 100);

    //            // Calculate similarity
    //            var similarity = new Accord.Imaging.Metrics.SSIM()
    //                .Compare(resizedCapturedFace, resizedPatientImage);

    //            if (similarity > 0.5)
    //            {
    //                var FaceImg = Path.GetFileNameWithoutExtension(patientImagePath).Trim();
    //                var matchingPatient = _context.Patients
    //                    .FirstOrDefault(p => EF.Functions.Like(p.FaceImg, $"{FaceImg}%"));

    //                if (matchingPatient != null)
    //                {
    //                    return Ok(new
    //                    {
    //                        isMatch = true,
    //                        detectedParts,
    //                        patient = new
    //                        {
    //                            matchingPatient.Id,
    //                            matchingPatient.Name,
    //                            matchingPatient.Dob,
    //                            matchingPatient.Mobileno,
    //                            matchingPatient.Nationalno,
    //                            matchingPatient.FaceImg
    //                        }
    //                    });
    //                }
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















