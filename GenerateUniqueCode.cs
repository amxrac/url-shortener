using Microsoft.EntityFrameworkCore;

namespace UrlShortener;

public class GenerateUniqueCode
{
    private readonly AppDbContext _context;
    public GenerateUniqueCode(AppDbContext context)
    {
        _context = context;
    }
    public async Task<string> GenerateCodeAsync()
    {
        while (true)
        {
            var guid = Guid.NewGuid().ToByteArray();
            var code = Convert.ToBase64String(guid)
                    .Replace("+", "")
                    .Replace("/", "")
                    .Replace("=", "")
                    .Substring(0, 8);

            if (!await _context.Urls.AnyAsync(url => url.Code == code))
            {
                return code;
            }
        }
        
    }    
}
