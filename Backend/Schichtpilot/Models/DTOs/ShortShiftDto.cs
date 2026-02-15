namespace Schichtpilot.Models.DTOs;

public class ShortShiftDto
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    public string ColorAsHex { get; set; }
    
    public bool InSchedule { get; set; }
}