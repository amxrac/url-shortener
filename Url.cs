namespace UrlShortener
{
    public class Url
    {
        public int Id { get; set; }
        public required string LongUrl { get; set; }
        public required string ShortUrl { get; set; }
        public required string Code { get; set; }
        public required DateTime CreatedAt { get; set; }
    }
}
