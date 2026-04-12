namespace Schichtpilot.Settings;

public class AzureEmailSettings
{
    public string ConnectionString { get; set; }
    public string SenderAddress { get; set; }
    public bool SendMail { get; set; }
}