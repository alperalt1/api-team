namespace nowClock.Infrastructure.Identity;

public class UserAccess
{
    public PlatformAccess Platforms { get; set; } = new();
}

public class PlatformAccess
{
    public bool Mobile { get; set; } = true;
    public bool Web { get; set; } = false;
}