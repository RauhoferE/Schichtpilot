namespace Schichtpilot.Settings;

/// <summary>
/// Contains the settings for the azure email service. 
/// </summary>
public class AzureEmailSettings
{
    public string ConnectionString { get; set; }
    public string SenderAddress { get; set; }
    public bool SendMail { get; set; }
}