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
        public async Task<IActionResult> WarmUp()
        {
            var pythonReady = await _faceEncodingService.WarmUpAsync();

            return Ok(new
            {
                status = "success",
                pythonReady
            });
        }
    }
}