using BookingService.DTO;
using BookingService.Models;
using BookingService.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost()]
    public async Task<IActionResult> CreateBooking([FromBody]CreateBookingRequest booking)
    {
        var userId = "keycloak userId";
        try
        {
            var result = await _bookingService.CreateBookingAsync(booking, userId);
            if (result == null)
                return BadRequest();
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBookingByIdAsync(Guid id)
    {
        try
        {
            var result = await _bookingService.GetBookingByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetBookingsByUserIdAsync(string userId)
    {
        try
        {
            var result = await _bookingService.GetBookingsByUserIdAsync(userId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateBookingStatusAsync(Guid id, [FromBody] BookingStatus status)
    {
        try
        {
            await _bookingService.UpdateBookingStatusAsync(id, status);
            return Ok(status);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelBookingAsync(Guid id, BookingStatus status)
    {
        try
        {
            var userId = "keycloak userId";
            await _bookingService.CancelBookingAsync(id, userId);
            return Ok("Booking cancelled");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}