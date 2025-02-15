public class ExchangeRate
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public required string CurrencyCode { get; set; }
    public decimal Rate { get; set; }
}
