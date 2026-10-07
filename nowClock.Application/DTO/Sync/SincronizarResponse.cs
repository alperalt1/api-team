namespace nowClock.Application.DTO.Sync;

public class SincronizarResponse
{
    public long UnixTimestampMs { get; set; }
    public string UtcIso { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "America/Guayaquil";
}