using BaggageAPI.Models;

namespace BaggageAPI.Dtos;

public class UpdateBaggageStatusDto
{
    public BaggageStatus Status { get; set; }
    public string Location { get; set; }
}
