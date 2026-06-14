using BaggageAPI.Models;

namespace BaggageAPI.Dtos;

public class UpdateBaggageStatusDto
{
    public BaggageStatus Status { get; set; }
    public required string Location { get; set; }
}
