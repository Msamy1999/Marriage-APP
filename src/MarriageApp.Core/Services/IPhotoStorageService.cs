namespace MarriageApp.Core.Services;

/// <summary>Decrypted photo bytes ready to stream to an authorized viewer.</summary>
public record PhotoData(byte[] Content, string ContentType, string? FileName);

/// <summary>
/// Encrypts photos at rest and decrypts them for authorized access. Files are stored
/// under random names; the actual access-control decision lives in the controller, not here.
/// </summary>
public interface IPhotoStorageService
{
    /// <summary>Encrypts and persists the uploaded bytes; returns the stored blob path.</summary>
    Task<string> SaveEncryptedAsync(byte[] content, string contentType, CancellationToken ct = default);

    /// <summary>Reads and decrypts a stored blob.</summary>
    Task<PhotoData> ReadDecryptedAsync(string blobPath, string contentType, string? fileName, CancellationToken ct = default);

    /// <summary>Deletes a stored blob (best-effort).</summary>
    Task DeleteAsync(string blobPath, CancellationToken ct = default);
}
