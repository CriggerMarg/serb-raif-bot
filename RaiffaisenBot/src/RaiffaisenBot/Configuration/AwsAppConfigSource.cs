using Amazon.AppConfigData;
using Amazon.AppConfigData.Model;
using Amazon.Extensions.Configuration.SystemsManager.Internal;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.Extensions.Primitives;

namespace RaiffaisenBot.Configuration;

public class AwsAppConfigSource : IConfigurationSource
{
    public AwsAppConfigSource(string applicationId, string environmentId, string configProfileId, int pollIntervalInSeconds, AWSOptions options)
    {
        ApplicationId = applicationId;
        EnvironmentId = environmentId;
        ConfigProfileId = configProfileId;
        PollIntervalInSeconds = pollIntervalInSeconds;
        Options = options;
    }

    public string ApplicationId { get; }

    public string EnvironmentId { get; }

    public string ConfigProfileId { get; }

    public int PollIntervalInSeconds { get; }

    public AWSOptions Options { get; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new AwsAppConfigProvider(this);
    }
}


public class AwsAppConfigProvider : ConfigurationProvider
{
    private readonly AwsAppConfigProcessor _processor;
    private readonly TimeSpan _reloadAfter;
    private readonly ManualResetEvent _reloadTaskEvent = new(initialState: true);

    public AwsAppConfigProvider(AwsAppConfigSource source)
    {
        _ = source ?? throw new ArgumentNullException(nameof(source));
        _processor = new AwsAppConfigProcessor(source);
        _reloadAfter = TimeSpan.FromSeconds(source.PollIntervalInSeconds);

        ChangeToken.OnChange(() => new CancellationChangeToken(new CancellationTokenSource(_reloadAfter).Token),
            async delegate
            {
                _reloadTaskEvent.Reset();
                try
                {
                    await LoadAsync().ConfigureAwait(continueOnCapturedContext: false);
                }
                finally
                {
                    _reloadTaskEvent.Set();
                }
            });
    }

    public override void Load()
    {
        LoadAsync().ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();
    }

    private async Task LoadAsync()
    {
        IDictionary<string, string> dictionary = await _processor.GetDataAsync().ConfigureAwait(continueOnCapturedContext: false) ?? new Dictionary<string, string>();
        if (!Data.EquivalentTo(dictionary))
        {
            Data = dictionary;
            OnReload();
        }
    }
}



public class AwsAppConfigProcessor
{
    private readonly SemaphoreSlim _lastConfigLock = new SemaphoreSlim(1, 1);

    private const int _lastConfigLockTimeout = 3000;

    private readonly AwsAppConfigSource _source;

    private readonly AmazonAppConfigDataClient _appConfigDataClient;

    private IDictionary<string, string> _lastConfig;

    private string? _pollConfigurationToken;

    private DateTime _nextAllowedPollTime;

    public AwsAppConfigProcessor(AwsAppConfigSource source)
    {
        if (source.ApplicationId == null)
        {
            throw new ArgumentNullException(nameof(source.ApplicationId));
        }

        if (source.EnvironmentId == null)
        {
            throw new ArgumentNullException(nameof(source.EnvironmentId));
        }

        if (source.ConfigProfileId == null)
        {
            throw new ArgumentNullException(nameof(source.ConfigProfileId));
        }

        _lastConfig = new Dictionary<string, string>();
        _source = source;
        _appConfigDataClient = new AmazonAppConfigDataClient(source.Options.Credentials, source.Options.Region);
        _appConfigDataClient.BeforeRequestEvent += ServiceClientAppender.ServiceClientBeforeRequestEvent;
    }

    public async Task<IDictionary<string, string>> GetDataAsync()
    {
        return await GetDataFromServiceAsync().ConfigureAwait(continueOnCapturedContext: false);
    }

    private async Task<IDictionary<string, string>> GetDataFromServiceAsync()
    {
        if (!await _lastConfigLock.WaitAsync(_lastConfigLockTimeout).ConfigureAwait(continueOnCapturedContext: false))
        {
            return _lastConfig;
        }

        try
        {
            if (DateTime.UtcNow <= _nextAllowedPollTime)
            {
                return _lastConfig;
            }

            if (string.IsNullOrEmpty(_pollConfigurationToken))
            {
                _pollConfigurationToken = await GetInitialConfigurationTokenAsync(_appConfigDataClient).ConfigureAwait(continueOnCapturedContext: false);
            }

            GetLatestConfigurationRequest request = new()
            {
                ConfigurationToken = _pollConfigurationToken
            };
            GetLatestConfigurationResponse getLatestConfigurationResponse = await _appConfigDataClient.GetLatestConfigurationAsync(request).ConfigureAwait(continueOnCapturedContext: false);
            _pollConfigurationToken = getLatestConfigurationResponse.NextPollConfigurationToken;
            _nextAllowedPollTime = DateTime.UtcNow.AddSeconds(getLatestConfigurationResponse.NextPollIntervalInSeconds);
            if (getLatestConfigurationResponse.ContentLength > 0)
            {
                _lastConfig = ParseConfig(getLatestConfigurationResponse.ContentType, getLatestConfigurationResponse.Configuration);
            }
        }
        finally
        {
            _lastConfigLock.Release();
        }

        return _lastConfig;
    }

    private async Task<string> GetInitialConfigurationTokenAsync(IAmazonAppConfigData appConfigClient)
    {
        StartConfigurationSessionRequest request = new()
        {
            ApplicationIdentifier = _source.ApplicationId,
            EnvironmentIdentifier = _source.EnvironmentId,
            ConfigurationProfileIdentifier = _source.ConfigProfileId,
            RequiredMinimumPollIntervalInSeconds = _source.PollIntervalInSeconds
        };

        return (await appConfigClient.StartConfigurationSessionAsync(request).ConfigureAwait(continueOnCapturedContext: false)).InitialConfigurationToken;
    }

    private static IDictionary<string, string> ParseConfig(string contentType, Stream configuration)
    {
        if (contentType?.Contains("application/json") == true)
        {
            return JsonConfigurationParser.Parse(configuration);
        }

        throw new NotImplementedException("Not implemented AppConfig type: " + contentType);
    }
}
