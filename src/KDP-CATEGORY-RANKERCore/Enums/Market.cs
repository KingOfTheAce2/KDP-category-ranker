namespace KDP_CATEGORY_RANKERCore.Enums;

public enum Market
{
    AmazonCom,
    AmazonCoUk,
    AmazonDe,
    AmazonFr,
    AmazonEs,
    AmazonIt,
    AmazonCa,
    AmazonComAu
}

public static class MarketExtensions
{
    public static string ToCode(this Market market) => market switch
    {
        Market.AmazonCom => "com",
        Market.AmazonCoUk => "co.uk",
        Market.AmazonDe => "de",
        Market.AmazonFr => "fr",
        Market.AmazonEs => "es",
        Market.AmazonIt => "it",
        Market.AmazonCa => "ca",
        Market.AmazonComAu => "com.au",
        _ => throw new ArgumentOutOfRangeException(nameof(market))
    };

    public static string ToDisplayName(this Market market) => market switch
    {
        Market.AmazonCom => "Amazon.com",
        Market.AmazonCoUk => "Amazon.co.uk",
        Market.AmazonDe => "Amazon.de",
        Market.AmazonFr => "Amazon.fr",
        Market.AmazonEs => "Amazon.es",
        Market.AmazonIt => "Amazon.it",
        Market.AmazonCa => "Amazon.ca",
        Market.AmazonComAu => "Amazon.com.au",
        _ => throw new ArgumentOutOfRangeException(nameof(market))
    };
}