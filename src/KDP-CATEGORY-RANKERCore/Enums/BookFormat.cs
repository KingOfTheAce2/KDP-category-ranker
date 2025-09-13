namespace KDP_CATEGORY_RANKERCore.Enums;

public enum BookFormat
{
    Kindle,
    Paperback,
    Hardcover,
    AudioBook
}

public static class BookFormatExtensions
{
    public static string ToDisplayName(this BookFormat format) => format switch
    {
        BookFormat.Kindle => "Kindle",
        BookFormat.Paperback => "Paperback",
        BookFormat.Hardcover => "Hardcover", 
        BookFormat.AudioBook => "Audiobook",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    public static bool IsDigital(this BookFormat format) => format switch
    {
        BookFormat.Kindle => true,
        BookFormat.AudioBook => true,
        _ => false
    };

    public static bool IsPhysical(this BookFormat format) => format switch
    {
        BookFormat.Paperback => true,
        BookFormat.Hardcover => true,
        _ => false
    };
}