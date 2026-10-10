using Microsoft.AspNetCore.Mvc;
using PatientSystem.Application.Interfaces;

namespace PatientSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemController : ControllerBase
    {
        private readonly IFaceEncodingService _faceEncodingService;

        public SystemController(IFaceEncodingService faceEncodingService)
        {
            _faceEncodingService = faceEncodingService;
        }

        [HttpGet("warmup")]
        [ResponseCache(
     NoStore = true,
     Location = ResponseCacheLocation.None
 )]
        public async Task<IActionResult> WarmUp()
        {
            Response.Headers["Cache-Control"] =
                "no-store, no-cache, must-revalidate";

            Response.Headers["Pragma"] =
                "no-cache";

            var pythonReady =
                await _faceEncodingService.WarmUpAsync();

            return Ok(new
            {
                status = "success",
                pythonReady,
                checkedAtUtc = DateTime.UtcNow
            });
        }
    }
}