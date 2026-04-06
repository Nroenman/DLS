using BaggageAPI.Dtos;
using BaggageAPI.Models;

namespace BaggageAPI.Interfaces;

public interface IBaggageService
{
    Task<Baggage> CreateAsync(CreateBaggageDto dto);
    Task<Baggage> UpdateStatusAsync(Guid id, UpdateBaggageStatusDto dto);
    Task<List<Baggage>> GetByPassenger(Guid passengerId);
}