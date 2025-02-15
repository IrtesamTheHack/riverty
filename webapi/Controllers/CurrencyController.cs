using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;

[ApiController]
[Route("api/[controller]")]
public class CurrencyController : ControllerBase
{
    private static readonly string AccessKey;
    private const string BaseUrl = "http://data.fixer.io/api";

    static CurrencyController()
    {
        Env.Load();
        AccessKey = Environment.GetEnvironmentVariable("FIXER_API_KEY") ?? throw new Exception("FIXER_API_KEY environment variable is not set");
    }

    public class CurrencyConversionRequest
    {
        public required string FromCurrency { get; set; }
        public required string ToCurrency { get; set; }
        public decimal Amount { get; set; }
        public string? Date { get; set; }
    }

    public class CurrencyConversionResponse
    {
        public bool Success { get; set; }
        public decimal? Result { get; set; }
        public string? Error { get; set; }
    }

    public class HistoricalRatesRequest
    {
        public required string CurrencyCode { get; set; }
        public required DateTime StartDate { get; set; }
        public required DateTime EndDate { get; set; }
    }

    public class HistoricalRatesResponse
    {
        public bool Success { get; set; }
        public List<RateData>? Rates { get; set; }
        public string? Error { get; set; }
    }

    public class RateData
    {
        public DateTime Timestamp { get; set; }
        public decimal Rate { get; set; }
    }

    [HttpPost("convert")]
    public async Task<ActionResult<CurrencyConversionResponse>> ConvertCurrency([FromBody] CurrencyConversionRequest request)
    {
        try
        {
            using var client = new HttpClient();
            string url = $"{BaseUrl}/latest?access_key={AccessKey}";

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            using JsonDocument document = JsonDocument.Parse(jsonResponse);
            JsonElement root = document.RootElement;

            if (root.GetProperty("success").GetBoolean())
            {
                var rates = root.GetProperty("rates");
                decimal fromRate = rates.GetProperty(request.FromCurrency).GetDecimal();
                decimal toRate = rates.GetProperty(request.ToCurrency).GetDecimal();

                decimal inEur = request.Amount / fromRate;
                decimal result = Math.Round(inEur * toRate, 2);

                return Ok(new CurrencyConversionResponse
                {
                    Result = result,
                });
            }
            else
            {
                string error = root.GetProperty("error").GetProperty("info").GetString() ?? "Unknown error";
                return BadRequest(new CurrencyConversionResponse
                {
                    Success = false,
                    Error = error
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new CurrencyConversionResponse
            {
                Success = false,
                Error = $"Error fetching currency data: {ex.Message}"
            });
        }
    }

    [HttpPost("convert/historical")]
    public async Task<ActionResult<CurrencyConversionResponse>> ConvertHistoricalCurrency([FromBody] CurrencyConversionRequest request)
    {
        if (string.IsNullOrEmpty(request.Date))
        {
            return BadRequest(new CurrencyConversionResponse
            {
                Success = false,
                Error = "Date is required for historical conversion"
            });
        }

        if (!DateTime.TryParse(request.Date, out DateTime date))
        {
            return BadRequest(new CurrencyConversionResponse
            {
                Success = false,
                Error = "Invalid date format. Please use YYYY-MM-DD"
            });
        }

        try
        {
            using var client = new HttpClient();
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
                decimal fromRate = rates.GetProperty(request.FromCurrency).GetDecimal();
                decimal toRate = rates.GetProperty(request.ToCurrency).GetDecimal();

                decimal inEur = request.Amount / fromRate;
                decimal result = Math.Round(inEur * toRate, 2);

                return Ok(new CurrencyConversionResponse
                {
                    Success = true,
                    Result = result,
                });
            }
            else
            {
                string error = root.GetProperty("error").GetProperty("info").GetString() ?? "Unknown error";
                return BadRequest(new CurrencyConversionResponse
                {
                    Success = false,
                    Error = error
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new CurrencyConversionResponse
            {
                Success = false,
                Error = $"Error fetching historical currency data: {ex.Message}"
            });
        }
    }

    [HttpPost("convert/database")]
public async Task<ActionResult<HistoricalRatesResponse>> GetHistoricalRates(
    [FromBody] HistoricalRatesRequest request,
    [FromServices] ExchangeRateContext dbContext)
{
    try
    {
        if (request.StartDate > request.EndDate)
        {
            return BadRequest(new HistoricalRatesResponse
            {
                Success = false,
                Error = "Start date must be before or equal to end date"
            });
        }

        var rates = await dbContext.ExchangeRates
            .Where(r => r.CurrencyCode == request.CurrencyCode
                    && r.Date >= request.StartDate
                    && r.Date <= request.EndDate)
            .OrderBy(r => r.Date)
            .Select(r => new RateData
            {
                Timestamp = r.Date,
                Rate = r.Rate
            })
            .ToListAsync();

        if (!rates.Any())
        {
            return NotFound(new HistoricalRatesResponse
            {
                Success = false,
                Error = $"No rates found for {request.CurrencyCode} between {request.StartDate:yyyy-MM-dd} and {request.EndDate:yyyy-MM-dd}"
            });
        }

        return Ok(new HistoricalRatesResponse
        {
            Rates = rates
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new HistoricalRatesResponse
        {
            Success = false,
            Error = $"Error retrieving historical rates: {ex.Message}"
        });
    }
}
}