using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using DotNetEnv;

public class ExchangeRateService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExchangeRateService> _logger;
    private readonly string _accessKey;
    private readonly string _baseUrl;

    public ExchangeRateService(
        IServiceProvider serviceProvider,
        ILogger<ExchangeRateService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        DotNetEnv.Env.Load();
        _accessKey = Environment.GetEnvironmentVariable("FIXER_API_KEY") 
            ?? throw new Exception("FIXER_API_KEY environment variable is not set");
        _baseUrl = "http://data.fixer.io/api";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FetchAndStoreRates();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching exchange rates");
            }

            // Run this at midnight
            var tomorrow = DateTime.UtcNow.Date.AddDays(1);
            var delay = tomorrow - DateTime.UtcNow;
            await Task.Delay(delay, stoppingToken);

            // Run every 2 minutes
            // await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }

    private async Task FetchAndStoreRates()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ExchangeRateContext>();
        
        using var client = new HttpClient();
        string url = $"{_baseUrl}/latest?access_key={_accessKey}";

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string jsonResponse = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(jsonResponse);
        JsonElement root = document.RootElement;

        if (!root.GetProperty("success").GetBoolean())
        {
            throw new Exception("API request failed");
        }

        var rates = root.GetProperty("rates");
        var date = DateTime.UtcNow.Date;

        var exchangeRates = new List<ExchangeRate>();
        foreach (var rate in rates.EnumerateObject())
        {
            exchangeRates.Add(new ExchangeRate
            {
                Date = date,
                CurrencyCode = rate.Name,
                Rate = rate.Value.GetDecimal()
            });
        }

        // Remove rates previously stored today in case the script has been rerun
        var existingRates = await context.ExchangeRates
            .Where(r => r.Date == date)
            .ToListAsync();
        context.ExchangeRates.RemoveRange(existingRates);

        await context.ExchangeRates.AddRangeAsync(exchangeRates);
        await context.SaveChangesAsync();
        
        _logger.LogInformation($"Successfully stored {exchangeRates.Count} exchange rates for {date:yyyy-MM-dd}");
    }
}
