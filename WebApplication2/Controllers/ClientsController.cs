using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebApplication2.Models.DTOs;

namespace WebApplication2.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;";

    // Endpoint tworzy nowy rekord Klienta
    [HttpPost]
    public async Task<IActionResult> NewClient([FromBody] NewClientDTO client)
    {
        // Sprawdzam czy podane dane spełniają warunki
        if (string.IsNullOrWhiteSpace(client.FirstName) || string.IsNullOrWhiteSpace(client.Pesel))
            return BadRequest("Imię i PESEL są wymagane.");

        if (!client.Email.Contains("@") || client.Email.Contains(" "))
            return BadRequest("Email musi zawierać znak '@' i nie może zawierać spacji.");

        if (client.Pesel.Length != 11 || !client.Pesel.All(char.IsDigit))
            return BadRequest("PESEL musi zawierać dokładnie 11 cyfr.");

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        // Sprawdzam czy klient z tym numerem PESEL istnieje
        using (var checkCmd = new SqlCommand("SELECT COUNT(1) FROM Client WHERE Pesel = @Pesel", conn))
        {
            checkCmd.Parameters.AddWithValue("@Pesel", client.Pesel);
            var exists = (int)await checkCmd.ExecuteScalarAsync() > 0;
            if (exists) return Conflict("Klient z takim PESEL już istnieje.");
        }

        // Nowe id generowane na zasadzie maxId + 1
        int newClientId = await GenerateClientId(conn);

        // Wstawienie nowego klienta
        using var cmd = new SqlCommand(@"
        INSERT INTO Client (IdClient, FirstName, LastName, Email, Telephone, Pesel)
        VALUES (@idClient, @firstName, @lastName, @Email, @Telephone, @Pesel)", conn);

        cmd.Parameters.AddWithValue("@idClient", newClientId);
        cmd.Parameters.AddWithValue("@firstName", client.FirstName);
        cmd.Parameters.AddWithValue("@lastName", client.LastName);
        cmd.Parameters.AddWithValue("@Email", client.Email);
        cmd.Parameters.AddWithValue("@Telephone", client.Telephone);
        cmd.Parameters.AddWithValue("@Pesel", client.Pesel);

        await cmd.ExecuteNonQueryAsync();

        return Created($"/api/clients/{newClientId}", new { id = newClientId });
    }
    
    //Generacja nowego ID / potrzebne przy tworzeniu nowego klienta
    private async Task<int> GenerateClientId(SqlConnection conn)
    {
        var cmd = new SqlCommand("SELECT ISNULL(MAX(IdClient), 0) + 1 FROM Client", conn); // szukamy max id i dodajemy 1
        var newClientId = await cmd.ExecuteScalarAsync();

        return Convert.ToInt32(newClientId);
    }
    
    
    // Endpoint dodaje klienta do konkretnej wycieczki 
[HttpPut("{id}/trips/{tripId}")]
public async Task<IActionResult> RegisterClientToTrip(int id, int tripId)
{
    await using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    // Sprawdzam czy Klient istnieje
    var checkClientCmd = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @id", conn);
    checkClientCmd.Parameters.AddWithValue("@id", id);
    var clientExists = await checkClientCmd.ExecuteScalarAsync();
    if (clientExists == null)
        return NotFound("Klient nie istnieje.");

    // Sprawdzam czy wycieczka istnieje
    var checkTripCmd = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip = @tripId", conn);
    checkTripCmd.Parameters.AddWithValue("@tripId", tripId);
    var maxPeopleObj = await checkTripCmd.ExecuteScalarAsync();
    if (maxPeopleObj == null)
        return NotFound("Wycieczka nie istnieje.");

    int maxPeople = (int)maxPeopleObj;

    // Sprawdzam ilu klientów już zapisano na wybraną wycieczkę
    var countCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @tripId", conn);
    countCmd.Parameters.AddWithValue("@tripId", tripId);
    int currentCount = (int)await countCmd.ExecuteScalarAsync();

    if (currentCount >= maxPeople)
        return Conflict("Osiągnięto maksymalną liczbę uczestników.");

    // Sprawdzam czy klien jest już zapisany na tą wycieczkę
    var checkAlreadyCmd = new SqlCommand("SELECT 1 FROM Client_Trip WHERE IdClient = @id AND IdTrip = @tripId", conn);
    checkAlreadyCmd.Parameters.AddWithValue("@id", id);
    checkAlreadyCmd.Parameters.AddWithValue("@tripId", tripId);
    var already = await checkAlreadyCmd.ExecuteScalarAsync();
    if (already != null)
        return Conflict("Klient już jest zapisany na tę wycieczkę.");
    
    
    //Nie do końca rozumiem jaką daną ma przechowywać RegisteredAt jako int
    int registeredAt = int.Parse(DateTime.Now.ToString("yyyyMMdd"));

    
    //umieszczam nowego klienta i nową wycieczkę
    var insertCmd = new SqlCommand(@"
    INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
    VALUES (@id, @tripId, @registeredAt, NULL)", conn);

    insertCmd.Parameters.AddWithValue("@id", id);
    insertCmd.Parameters.AddWithValue("@tripId", tripId);
    insertCmd.Parameters.AddWithValue("@registeredAt", registeredAt);


    await insertCmd.ExecuteNonQueryAsync();

    return Ok("Klient został zapisany na wycieczkę.");
}

// Endpoint usuwa klienta o podanym id z wycieczki
[HttpDelete("{id}/trips/{tripId}")]
public async Task<IActionResult> UnregisterClientFromTrip(int id, int tripId)
{
    await using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    // Sprawdzam czy klient jest przypisany do tej wycieczki
    var checkCmd = new SqlCommand("SELECT 1 FROM Client_Trip WHERE IdClient = @id AND IdTrip = @tripId", conn);
    checkCmd.Parameters.AddWithValue("@id", id);
    checkCmd.Parameters.AddWithValue("@tripId", tripId);

    var exists = await checkCmd.ExecuteScalarAsync();
    if (exists == null)
    {
        return NotFound("Rejestracja klienta na tę wycieczkę nie istnieje.");
    }

    // Usuwam rejestracje
    var deleteCmd = new SqlCommand("DELETE FROM Client_Trip WHERE IdClient = @id AND IdTrip = @tripId", conn);
    deleteCmd.Parameters.AddWithValue("@id", id);
    deleteCmd.Parameters.AddWithValue("@tripId", tripId);

    await deleteCmd.ExecuteNonQueryAsync();

    return Ok("Klient został usunięty z wycieczki.");
}

}