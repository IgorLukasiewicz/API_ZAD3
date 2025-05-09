using Microsoft.Data.SqlClient;
using WebApplication2.Models.DTOs;

namespace WebApplication2.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;";
    
   public async Task<List<TripDTO>> GetTrips()
{
    var trips = new List<TripDTO>();
    
    
    string command = @"
        SELECT T.IdTrip, T.Name, T.Description, T.DateFrom, T.DateTo, T.MaxPeople, C.Name AS CountryName
        FROM Trip T
        JOIN Country_Trip CT ON CT.IdTrip = T.IdTrip
        JOIN Country C ON C.IdCountry = CT.IdCountry
        ORDER BY T.IdTrip"; //wyszukuje dane o wycieczce + poprzez join'y sprawdza listę krajów w której dana wycieczka miała miejsce 

    using (SqlConnection conn = new SqlConnection(_connectionString))
    using (SqlCommand cmd = new SqlCommand(command, conn))
    {
        await conn.OpenAsync();

        using (var reader = await cmd.ExecuteReaderAsync())
        {
            TripDTO? currentTrip = null;
            int? lastTripId = null;

            while (await reader.ReadAsync())
            {
                int idTrip = reader.GetInt32(reader.GetOrdinal("IdTrip"));
                
                if (lastTripId == null || idTrip != lastTripId)
                {
                    currentTrip = new TripDTO
                    {
                        idTrip = idTrip,
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                        DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                        MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                        Countries = new List<CountryDTO>()
                    };

                    trips.Add(currentTrip);
                    lastTripId = idTrip;
                }
                
                var countryName = reader.GetString(reader.GetOrdinal("CountryName"));
                currentTrip!.Countries.Add(new CountryDTO { Name = countryName });
            }
        }
    }

    return trips;
}



public async Task<TripDTO?> GetTrip(int id)
{
    TripDTO trip = null;

    string command = @"
        SELECT T.IdTrip, T.Name, T.Description, T.DateFrom, T.DateTo, T.MaxPeople, C.Name AS CountryName
        FROM Trip T
        JOIN Country_Trip CT ON CT.IdTrip = T.IdTrip
        JOIN Country C ON C.IdCountry = CT.IdCountry
        WHERE T.IdTrip = @id";  //wyszukuje dane o wycieczce + poprzez join'y sprawdza listę krajów w której dana wycieczka miała miejsce

    using (SqlConnection conn = new SqlConnection(_connectionString))
    using (SqlCommand cmd = new SqlCommand(command, conn))
    {
        cmd.Parameters.AddWithValue("@id", id);

        await conn.OpenAsync();

        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                if (trip == null)
                {
                    trip = new TripDTO
                    {
                        idTrip = reader.GetInt32(reader.GetOrdinal("IdTrip")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                        DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                        MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                        Countries = new List<CountryDTO>()
                    };
                }

                var countryName = reader.GetString(reader.GetOrdinal("CountryName"));
                trip.Countries.Add(new CountryDTO { Name = countryName });
            }
        }
    }

    return trip;
}


    public async Task<bool> DoesTripExist(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("SELECT COUNT(1) FROM Trip WHERE IdTrip = @id", conn);  // Sprawdza czy wycieczka w ogóle isntnieje
        cmd.Parameters.AddWithValue("@id", id);

        await conn.OpenAsync();
        var result = (int)await cmd.ExecuteScalarAsync();
        return result > 0;
    }
    public async Task<List<TripDTO>> GetTripsForClient(int clientId)
{
    var trips = new List<TripDTO>();

    // Szukamy wycieczek związanych z danym klientem
    string query = @"
        SELECT T.IdTrip, T.Name, T.Description, T.DateFrom, T.DateTo, T.MaxPeople, C.Name AS CountryName, CT.Payment
        FROM Trip T
        JOIN Client_Trip CT ON CT.IdTrip = T.IdTrip
        JOIN Country_Trip CT2 ON CT2.IdTrip = T.IdTrip
        JOIN Country C ON C.IdCountry = CT2.IdCountry
        WHERE CT.IdClient = @clientId
        ORDER BY T.DateFrom";  // Zwracamy wycieczki tego klienta posortowane po dacie rozpoczęcia

    using (var conn = new SqlConnection(_connectionString))
    using (var cmd = new SqlCommand(query, conn))
    {
        cmd.Parameters.AddWithValue("@clientId", clientId);

        await conn.OpenAsync();

        using (var reader = await cmd.ExecuteReaderAsync())
        {
            TripDTO currentTrip = null;
            int lastTripId = -1;

            while (await reader.ReadAsync())
            {
                int idTrip = reader.GetInt32(reader.GetOrdinal("IdTrip"));

                if (lastTripId != idTrip) 
                {
                    currentTrip = new TripDTO
                    {
                        idTrip = idTrip,
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                        DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                        MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                        Countries = new List<CountryDTO>()
                    };

                    trips.Add(currentTrip);
                    lastTripId = idTrip;
                }

                var countryName = reader.GetString(reader.GetOrdinal("CountryName"));
                currentTrip?.Countries.Add(new CountryDTO { Name = countryName });
            }
        }
    }

    return trips;
}


}
