namespace Schichtpilot.Settings;

/// <summary>
/// Contains the settings for the azure email service. 
/// </summary>
public class AzureEmailSettings
{
    public required string ConnectionString { get; set; }
    public required string SenderAddress { get; set; }
    public bool SendMail { get; set; }
}