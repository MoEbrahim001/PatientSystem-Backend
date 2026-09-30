using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PatientSystem.Application.DTOs;
using PatientSystem.Application.DTOs.Patients;
using PatientSystem.Application.Interfaces;
using PatientSystem.Application.Services;
using PatientSystem.Domain.Entities;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _service;
    private readonly IWebHostEnvironment _env;

    public PatientsController(IPatientService service , IWebHostEnvironment env) {
        _service = service; _env = env;
    }
    [HttpPost]
    public async Task<ActionResult> GetPatients([FromBody] patientParams patientParams)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var res = await _service.GetPagedAsync(patientParams.first, patientParams.rows, patientParams.searchtext, baseUrl);
        return Ok(res);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchPatients([FromQuery] string searchText)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var res = await _service.SearchAsync(searchText, baseUrl);
        return Ok(res);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPatientById(int id)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var res = await _service.GetByIdAsync(id, baseUrl);
        return res == null ? NotFound() : Ok(res);
    }

    [HttpPost("addPatient")]
    public async Task<ActionResult<int>> AddPatient([FromBody] CreatePatientVM patientData)
    {
        var id = await _service.AddAsync(patientData);
        return Ok(id);
    }

    [HttpPut("UpdatePatient")]
    public async Task<IActionResult> UpdatePatient([FromBody] Patient updatedPatient)
    {
        await _service.UpdateAsync(updatedPatient);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePatient(int id)
    {
        await _service.DeleteAsync(id);
        return Ok();
    }

    [HttpPost("uploadFaceImage/{patientId}")]
    public async Task<IActionResult> UploadFaceImage([FromForm] IFormFile file, int patientId)
    {
        var fileName = await _service.UploadFaceImageAsync(patientId, file, _env.ContentRootPath);
        return Ok(new { fileName });
    }
}