namespace Schichtpilot.Models.DTOs;

/// <summary>
/// Represents an address when a user is registering.
/// </summary>
public class AddressDto
{
    public required string Street { get; set; }
    public required string City { get; set; }
    public int PostalCode { get; set; }
}