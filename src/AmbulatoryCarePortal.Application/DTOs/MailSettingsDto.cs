namespace AmbulatoryCarePortal.Application.DTOs;

public class MailSettingsDto
{
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool? EnableSsl { get; set; }
    public string? FromAddress { get; set; }
    public string? FromName { get; set; }
    public string? TestRecipientEmail { get; set; }
}
