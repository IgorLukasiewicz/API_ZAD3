namespace WebApplication2.Models.DTOs;

public class TripDTO
{
    public int idTrip { get; set; }
    public string Name { get; set; }
    
    public string Description { get; set; }
    
    public DateTime DateFrom { get; set; }
    
    public DateTime DateTo { get; set; }
    
    public int MaxPeople { get; set; }
    public List<CountryDTO> Countries { get; set; }
}
