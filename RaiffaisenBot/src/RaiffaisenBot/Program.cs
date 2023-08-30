using RaiffaisenBot.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    // having newtonsoft json is essential, because telegram.bot types are heavily
    // relying on converters and using converters from json.net package
    .AddNewtonsoftJson();

builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);
builder.ConfigureAwsOptions();
builder.ConfigureAwsAppConfig();
builder.ConfigureServiceOptions();
builder.ConfigureLogging();
builder.ConfigureTelegramBot();
builder.ConfigureCors();
builder.ConfigureHandlers();

var app = builder.Build();


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "добро пожаловать отсюда");

app.Run();