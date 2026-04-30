using BaggageAPI.Dtos;
using BaggageAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BaggageAPI.Controllers;

[ApiController]
[Route("api/baggage")]
public class BaggageController(IBaggageService service) : ControllerBase
{
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] CreateBaggageDto dto)    {
        var result = await service.CreateAsync(dto);
        return Ok(result);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateBaggageStatusDto dto)    {
        var result = await service.UpdateStatusAsync(id, dto);
        return Ok(result);
    }

    [HttpGet("passenger/{passengerId}")]
    public async Task<IActionResult> GetPassengerBaggage(Guid passengerId)
    {
        var result = await service.GetByPassenger(passengerId);
        return Ok(result);
    }
}