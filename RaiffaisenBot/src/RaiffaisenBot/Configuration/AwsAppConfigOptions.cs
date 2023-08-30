namespace RaiffaisenBot.Configuration;

public class AwsAppConfigOptions
{
    public string Application { get; set; } = null!;

    public string Environment { get; set; } = null!;

    public string ConfigurationProfile { get; set; } = null!;

    public int RequiredMinimumPollIntervalInSeconds { get; set; }
}
