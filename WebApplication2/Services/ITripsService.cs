using WebApplication2.Models.DTOs;

namespace WebApplication2.Services;

public interface ITripsService
{
    Task<List<TripDTO>> GetTrips();
    Task<bool> DoesTripExist(int id);

    Task<TripDTO?> GetTrip(int id);

    Task<List<TripDTO>> GetTripsForClient(int clientId);
}