namespace Schichtpilot.Models.DTOs;

public class AddressDto
{
    public required string Street { get; set; }
    public required string City { get; set; }
    public int PostalCode { get; set; }
}