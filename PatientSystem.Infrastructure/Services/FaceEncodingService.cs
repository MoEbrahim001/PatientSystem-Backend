using PatientSystem.Application.Interfaces;
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

            Console.WriteLine(
                $"BaseAddress = {_http.BaseAddress}"
            );

            Console.WriteLine(
                "Endpoint = generate_encoding"
            );

            Console.WriteLine(
                $"PatientId = {patientId}"
            );

            Console.WriteLine(
                $"Image = {faceImageFullPath}"
            );

            try
            {
                using var form =
                    new MultipartFormDataContent();

                form.Add(
                    new StringContent(
                        patientId.ToString()
                    ),
                    "patientId"
                );

                await using var fileStream =
                    new FileStream(
                        faceImageFullPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read
                    );

                using var fileContent =
                    new StreamContent(fileStream);

                fileContent.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        GetContentType(
                            faceImageFullPath
                        )
                    );

                form.Add(
                    fileContent,
                    "file",
                    Path.GetFileName(
                        faceImageFullPath
                    )
                );

                var res = await _http.PostAsync(
                    "generate_encoding",
                    form
                );

                var body =
                    await res.Content
                        .ReadAsStringAsync();

                Console.WriteLine(
                    $"Status = {(int)res.StatusCode}"
                );

                Console.WriteLine(
                    $"Body = {body}"
                );

                if (!res.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"Failed generate encoding: {body}"
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
                        $"Python returned invalid encoding response: {body}"
                    );
                }

                return result.EncodingFile;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine(
                    $"Python Face API timed out: {ex.Message}"
                );

                throw new Exception(
                    "Face encoding service timed out.",
                    ex
                );
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(
                    $"Cannot connect to Python Face API: {ex.Message}"
                );

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
                var res = await _http.GetAsync(
                    "reload_encodings"
                );

                var body =
                    await res.Content
                        .ReadAsStringAsync();

                Console.WriteLine(
                    $"Reload Status = {(int)res.StatusCode}"
                );

                Console.WriteLine(
                    $"Reload Body = {body}"
                );

                if (!res.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"Failed reload encodings: {body}"
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

        private static string GetContentType(
            string filePath)
        {
            var extension =
                Path.GetExtension(filePath)
                    .ToLowerInvariant();

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
        private class GenerateEncodingResponse
        {
            public string? Status { get; set; }

            public string? EncodingFile { get; set; }

            public string? Message { get; set; }
        }
    }
}