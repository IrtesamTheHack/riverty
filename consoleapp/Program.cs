// See https://aka.ms/new-console-template for more information
using System.Text.Json;
using DotNetEnv;

public class CurrencyConverter
{
    private static readonly string AccessKey;
    private const string BaseUrl = "http://data.fixer.io/api";

    static CurrencyConverter()
    {
        Env.Load();
        AccessKey = Environment.GetEnvironmentVariable("FIXER_API_KEY") ?? throw new Exception("FIXER_API_KEY environment variable is not set");
    }

    public static async Task<decimal> FetchHistoricalCurrencyData(string fromCurrency, string toCurrency, decimal amount, DateTime date)
    {
        using var client = new HttpClient();
        try
        {
            string formattedDate = date.ToString("yyyy-MM-dd");
            string url = $"{BaseUrl}/{formattedDate}?access_key={AccessKey}";

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            using JsonDocument document = JsonDocument.Parse(jsonResponse);
            JsonElement root = document.RootElement;

            if (root.GetProperty("success").GetBoolean())
            {
                var rates = root.GetProperty("rates");
                decimal fromRate = rates.GetProperty(fromCurrency).GetDecimal();
                decimal toRate = rates.GetProperty(toCurrency).GetDecimal();

                decimal inEur = amount / fromRate;
                decimal result = inEur * toRate;
                
                return Math.Round(result, 2);
            }
            else
            {
                string error = root.GetProperty("error").GetProperty("info").GetString() ?? "Unknown error";
                throw new Exception($"Historical currency conversion failed: {error}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error fetching historical currency data: {ex.Message}");
        }
    }
    public static async Task<decimal> FetchCurrencyData(string fromCurrency, string toCurrency, decimal amount)
    {
        using var client = new HttpClient();
        try
        {
            string url = $"{BaseUrl}/latest?access_key={AccessKey}";

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            using JsonDocument document = JsonDocument.Parse(jsonResponse);
            JsonElement root = document.RootElement;

            if (root.GetProperty("success").GetBoolean())
            {
                var rates = root.GetProperty("rates");
                decimal fromRate = rates.GetProperty(fromCurrency).GetDecimal();
                decimal toRate = rates.GetProperty(toCurrency).GetDecimal();

                decimal inEur = amount / fromRate;
                decimal result = inEur * toRate;
                
                return Math.Round(result, 2);
            }
            else
            {
                string error = root.GetProperty("error").GetProperty("info").GetString() ?? "Unknown error";
                throw new Exception($"Currency conversion failed: {error}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error fetching currency data: {ex.Message}");
        }
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Enter input currency: ");
        var fromCurrency = Console.ReadLine()?.ToUpper();
        Console.WriteLine("Enter output currency: ");
        var toCurrency = Console.ReadLine()?.ToUpper();
        Console.WriteLine("Enter amount in first currency: ");
        var amount = Console.ReadLine();
        Console.WriteLine("Enter date (YYYY-MM-DD) or press Enter for current rates: ");
        var dateStr = Console.ReadLine();

        decimal output;
        if (string.IsNullOrWhiteSpace(dateStr))
        {
            output = await FetchCurrencyData(fromCurrency, toCurrency, decimal.Parse(amount));
            Console.WriteLine($"Current converted amount: {output} {toCurrency}");
        }
        else if (DateTime.TryParse(dateStr, out DateTime date))
        {
            output = await FetchHistoricalCurrencyData(fromCurrency, toCurrency, decimal.Parse(amount), date);
            Console.WriteLine($"Historical converted amount for {date:yyyy-MM-dd}: {output} {toCurrency}");
        }
        else
        {
            Console.WriteLine("Invalid date format. Please use YYYY-MM-DD format.");
        }
    }
}
