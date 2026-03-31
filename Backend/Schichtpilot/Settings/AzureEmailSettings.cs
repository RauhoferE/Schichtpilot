namespace Schichtpilot.Configuration;

public class AzureEmailSettings
{
    public const string SectionName = "AzureEmail";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string SenderAddress    { get; set; } = string.Empty;
}