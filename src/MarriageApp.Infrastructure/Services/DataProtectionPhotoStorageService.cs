using MarriageApp.Core.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarriageApp.Infrastructure.Services;

/// <summary>Options for where encrypted photo blobs are stored on disk.</summary>
public class PhotoStorageOptions
{
    public const string SectionName = "PhotoStorage";
    /// <summary>Absolute or content-root-relative directory for encrypted blobs.</summary>
    public string StorageRoot { get; set; } = "App_Data/photos";
}

/// <summary>
/// Stores photos encrypted at rest using ASP.NET Core Data Protection. Each blob gets
/// a random GUID filename (not guessable / not enumerable), and the bytes are protected
/// with a dedicated purpose-scoped protector. Files are never publicly served — only
/// streamed back through the gated PhotoController after an authorization check.
/// </summary>
public class DataProtectionPhotoStorageService : IPhotoStorageService
{
    private const string ProtectorPurpose = "MarriageApp.ProfilePhotos.v1";

    private readonly IDataProtector _protector;
    private readonly string _root;
    private readonly ILogger<DataProtectionPhotoStorageService> _logger;

    public DataProtectionPhotoStorageService(
        IDataProtectionProvider provider,
        IOptions<PhotoStorageOptions> options,
        ILogger<DataProtectionPhotoStorageService> logger)
    {
        _protector = provider.CreateProtector(ProtectorPurpose);
        _root = options.Value.StorageRoot;
        _logger = logger;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveEncryptedAsync(byte[] content, string contentType, CancellationToken ct = default)
    {
        var fileName = $"{Guid.NewGuid():N}.bin";
        var fullPath = Path.Combine(_root, fileName);

        // Encrypt the raw bytes before they ever touch disk.
        var protectedBytes = _protector.Protect(content);
        await File.WriteAllBytesAsync(fullPath, protectedBytes, ct);

        // Persist a relative path so the storage root can move between environments.
        return fileName;
    }

    public async Task<PhotoData> ReadDecryptedAsync(string blobPath, string contentType, string? fileName, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_root, blobPath);
        var protectedBytes = await File.ReadAllBytesAsync(fullPath, ct);
        var content = _protector.Unprotect(protectedBytes);
        return new PhotoData(content, contentType, fileName);
    }

    public Task DeleteAsync(string blobPath, CancellationToken ct = default)
    {
        try
        {
            var fullPath = Path.Combine(_root, blobPath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete photo blob {Blob}", blobPath);
        }
        return Task.CompletedTask;
    }
}
