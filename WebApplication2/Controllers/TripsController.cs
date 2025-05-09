using Microsoft.AspNetCore.Mvc;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly ITripsService _tripsService;

        public TripsController(ITripsService tripsService)
        {
            _tripsService = tripsService;
        }

        // Endpoint sprawdza wszystkie dostępne wycieczki w bazie
        [HttpGet]
        public async Task<IActionResult> GetTrips()
        {
            var trips = await _tripsService.GetTrips();
            return Ok(trips);
        }
        
        // Endpoint Sprawdza wycieczki dla danego klienta
        [HttpGet("/api/clients/{id}/trips")]
        public async Task<IActionResult> GetClientTrips(int id)
        {
            var trips = await _tripsService.GetTripsForClient(id);
            if (trips == null)
                return NotFound("Client not found.");
            if (trips.Count == 0)
                return NotFound("Client has no registered trips.");
    
            return Ok(trips);
        }

    }
}