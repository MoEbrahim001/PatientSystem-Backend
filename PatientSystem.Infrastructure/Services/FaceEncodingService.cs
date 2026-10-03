using Microsoft.AspNetCore.Http;
using PatientSystem.Application.Interfaces;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PatientSystem.Infrastructure.Services
{
    public class FaceEncodingService : IFaceEncodingService
    {
        private readonly HttpClient _http;

        public FaceEncodingService(HttpClient http)
        {
            _http = http;
        }

        public async Task<string> GenerateEncodingAsync(
            int patientId,
            string faceImageFullPath)
        {
            if (string.IsNullOrWhiteSpace(faceImageFullPath))
            {
                throw new ArgumentException(
                    "Face image path is required.",
                    nameof(faceImageFullPath)
                );
            }

            if (!File.Exists(faceImageFullPath))
            {
                throw new FileNotFoundException(
                    "Face image was not found.",
                    faceImageFullPath
                );
            }

            try
            {
                using var form = new MultipartFormDataContent();

                form.Add(
                    new StringContent(patientId.ToString()),
                    "patientId"
                );

                await using var fileStream = new FileStream(
                    faceImageFullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read
                );

                using var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        GetContentType(faceImageFullPath)
                    );

                form.Add(
                    fileContent,
                    "file",
                    Path.GetFileName(faceImageFullPath)
                );

                using var response = await _http.PostAsync(
                    "generate_encoding",
                    form
                );

                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"Python generate_encoding failed ({(int)response.StatusCode}): {body}"
                    );
                }

                var result = JsonSerializer.Deserialize<GenerateEncodingResponse>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );

                if (result == null ||
                    string.IsNullOrWhiteSpace(result.EncodingFile))
                {
                    throw new Exception(
                        $"Python returned an invalid generate_encoding response: {body}"
                    );
                }

                return result.EncodingFile;
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception(
                    "Face encoding service timed out.",
                    ex
                );
            }
            catch (HttpRequestException ex)
            {
                throw new Exception(
                    "Could not connect to Face Recognition service.",
                    ex
                );
            }
        }

        public async Task<string> DetectAndFindAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException(
                    "Face image file is required.",
                    nameof(file)
                );
            }

            try
            {
                using var form = new MultipartFormDataContent();
                await using var stream = file.OpenReadStream();
                using var fileContent = new StreamContent(stream);

                fileContent.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        string.IsNullOrWhiteSpace(file.ContentType)
                            ? GetContentType(file.FileName)
                            : file.ContentType
                    );

                form.Add(
                    fileContent,
                    "file",
                    Path.GetFileName(file.FileName)
                );

                using var response = await _http.PostAsync(
                    "detectAndFind",
                    form
                );

                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"Python detectAndFind failed ({(int)response.StatusCode}): {body}"
                    );
                }

                return body;
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception(
                    "Face recognition service timed out.",
                    ex
                );
            }
            catch (HttpRequestException ex)
            {
                throw new Exception(
                    "Could not connect to Face Recognition service.",
                    ex
                );
            }
        }

        public async Task ReloadEncodingsAsync()
        {
            try
            {
                using var response = await _http.GetAsync(
                    "reload_encodings"
                );

                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"Failed to reload encodings: {body}"
                    );
                }
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception(
                    "Face encoding reload timed out.",
                    ex
                );
            }
            catch (HttpRequestException ex)
            {
                throw new Exception(
                    "Could not connect to Face Recognition service.",
                    ex
                );
            }
        }

        private static string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };
        }
        public async Task<bool> WarmUpAsync()
        {
            try
            {
                var response = await _http.GetAsync("health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        private sealed class GenerateEncodingResponse
        {
            public string? Status { get; set; }
            public string? EncodingFile { get; set; }
            public string? Message { get; set; }
        }
    }
}
