namespace SupplyChain.Infrastructure.Storage;

public sealed class StorageOptions
{
    /// <summary>Absolute path to the web project content root.</summary>
    public string RootPath { get; init; } = Directory.GetCurrentDirectory();

    public long MaxImageBytes { get; init; } = 5 * 1024 * 1024;

    public IReadOnlySet<string> AllowedExtensions { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg"
        };
}

public sealed class LocalFileStorage : Domain.Abstractions.IFileStorage
{
    private readonly StorageOptions _options;

    public LocalFileStorage(StorageOptions options)
    {
        _options = options;
    }

    public async Task<Domain.Common.ServiceResult<string>> SaveImageAsync(
        Stream stream,
        string fileName,
        string folder,
        CancellationToken ct = default)
    {
        if (stream is null || !stream.CanRead)
        {
            return Domain.Common.ServiceResult<string>.Fail("No file was uploaded.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !_options.AllowedExtensions.Contains(extension))
        {
            return Domain.Common.ServiceResult<string>.Fail(
                "Please upload a JPG, PNG, WEBP, GIF or SVG image.");
        }

        if (stream.CanSeek && stream.Length > _options.MaxImageBytes)
        {
            return Domain.Common.ServiceResult<string>.Fail("Images must be smaller than 5 MB.");
        }

        var safeFolder = string.Concat(folder.Where(char.IsLetterOrDigit)).ToLowerInvariant();
        if (safeFolder.Length == 0)
        {
            safeFolder = "misc";
        }

        var directory = Path.Combine(_options.RootPath, "wwwroot", "uploads", safeFolder);
        Directory.CreateDirectory(directory);

        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var absolutePath = Path.Combine(directory, storedName);

        await using (var target = File.Create(absolutePath))
        {
            await stream.CopyToAsync(target, ct);

            if (target.Length > _options.MaxImageBytes)
            {
                target.Close();
                File.Delete(absolutePath);
                return Domain.Common.ServiceResult<string>.Fail("Images must be smaller than 5 MB.");
            }
        }

        return Domain.Common.ServiceResult<string>.Ok($"/uploads/{safeFolder}/{storedName}");
    }

    public bool Delete(string relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl) ||
            !relativeUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var relative = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var absolute = Path.GetFullPath(Path.Combine(_options.RootPath, "wwwroot", relative));
        var uploadsRoot = Path.GetFullPath(Path.Combine(_options.RootPath, "wwwroot", "uploads"));

        if (!absolute.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(absolute))
        {
            return false;
        }

        File.Delete(absolute);
        return true;
    }
}
