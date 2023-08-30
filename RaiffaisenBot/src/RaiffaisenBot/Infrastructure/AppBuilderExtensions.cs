using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;
using RaiffaisenBot.Configuration;
using RaiffaisenBot.Logic.Handlers.Abstractions;
using RaiffaisenBot.Logic.Handlers;
using Serilog;
using Serilog.Formatting.Compact;
using Telegram.Bot;
using RaiffaisenBot.Logic;
using RaiffaisenBot.Logic.Handlers.Messages.Files;
using RaiffaisenBot.Logic.Handlers.Messages.Text;

namespace RaiffaisenBot.Infrastructure;

public static class AppBuilderExtensions
{
    public static void ConfigureHandlers(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<UpdateService>();

        // message handler fabric method
        builder.Services.AddTransient<Func<HandlerMessageType, IOrderedEnumerable<IMessageHandler>>>(serviceProvider => messageType =>
        {
            return serviceProvider.GetServices<IMessageHandler>().Where(handler => handler.MessageType.HasFlag(messageType)).OrderByDescending(x => x.Priority);
        });
        builder.Services.AddSingleton<UpdateHandlerFactory>();
        builder.Services.AddScoped<IMessageHandler, AccountStatementFilesHandler>();
        builder.Services.AddScoped<IMessageHandler, EchoReplyHandler>();
    }

    public static void ConfigureCors(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
    }

    public static void ConfigureServiceOptions(this WebApplicationBuilder builder)
    {
        builder.ConfigureOptionsFromAppConfig<BotConfiguration>();
    }


    public static OptionsBuilder<TOptions> ConfigureOptionsFromAppConfig<TOptions>(this WebApplicationBuilder builder)
        where TOptions : class
    {
        return builder.Services.AddOptions<TOptions>().BindConfiguration(typeof(TOptions).Name);
    }

    public static void ConfigureTelegramBot(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddHttpClient("tgwebhook")
            .AddTypedClient<ITelegramBotClient>((client, sp) =>
            {
                var configuration = sp.GetRequiredService<IOptionsMonitor<BotConfiguration>>();
                return new TelegramBotClient(configuration.CurrentValue.BotToken, client);
            });
    }
    public static void ConfigureLogging(this WebApplicationBuilder builder)
    {
        // log requests and responses bodies
        builder.Services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = HttpLoggingFields.Request;
        });

        Log.Logger = new LoggerConfiguration()
       .ReadFrom.Configuration(builder.Configuration)
       // writing to console is enough, aws would redirect it to cloudwatch
       .WriteTo.Console(
           formatter: new RenderedCompactJsonFormatter(),
           restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
          .CreateLogger();
        builder.Host.UseSerilog(Log.Logger);
    }
    public static void ConfigureAwsAppConfig(this WebApplicationBuilder builder)
    {
        AwsAppConfigOptions appConfigOptions = builder.Configuration.GetSection(nameof(AwsAppConfigOptions)).Get<AwsAppConfigOptions>()!;
        AWSOptions awsOptions = GetAwsOptions();
        // Add AwsAppConfig Configuration Provider for ApiConfig
        builder.Configuration.AddAwsAppConfig(
            appConfigOptions.Application,
            appConfigOptions.Environment,
            appConfigOptions.ConfigurationProfile,
            appConfigOptions.RequiredMinimumPollIntervalInSeconds,
            awsOptions);
    }


    public static void AddAwsAppConfig(this IConfigurationBuilder builder, string applicationId, string environmentId, string configProfileId, int pollIntervalInSeconds, AWSOptions options)
    {
        builder.Add(new AwsAppConfigSource(
            applicationId,
            environmentId,
            configProfileId,
            pollIntervalInSeconds,
            options));
    }

    public static void ConfigureAwsOptions(this WebApplicationBuilder builder)
    {
        AWSOptions awsOptions = GetAwsOptions();
        ImmutableCredentials credentialKeys = awsOptions.Credentials.GetCredentials();
        var defaultAwsParams = new
        {
            Region = awsOptions.Region.SystemName,
            CredentialsType = awsOptions.Credentials.GetType().Name,
            AccessKey = GetLastFourDigits(credentialKeys.AccessKey),
            SecretKey = GetLastFourDigits(credentialKeys.SecretKey),
            ServiceUrl = awsOptions.DefaultClientConfig.ServiceURL
        };
        Log.Logger.Information("AWS Defaults: {@defaults}", defaultAwsParams);

        builder.Services.AddDefaultAWSOptions(awsOptions);

        //returns last four digits just like in the 'aws configure' cli command.
        string GetLastFourDigits(string value)
        {
            return $"****{value.Substring(value.Length - 4)}";
        }
    }

    public static AWSOptions GetAwsOptions()
    {
        var awsOptions = new AWSOptions
        {
            //For more details regarding regions retrieval check https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-region-selection.html
            Region = FallbackRegionFactory.GetRegionEndpoint(),
            //For more details regarding credentials retrieval check https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/creds-assign.html
            Credentials = FallbackCredentialsFactory.GetCredentials(false)
        };

        return awsOptions;
    }
}
