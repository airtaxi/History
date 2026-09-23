using System.Text;

namespace History.ApiService.Helpers;

// Uploaded files cannot be trusted on their extension or declared content type, so the media kind
// is resolved from the header bytes instead: image signatures, video container signatures, and the
// ISO base media (ftyp) brands that separate HEIF/AVIF images from MP4-family videos.
public static class MediaTypeDetector
{
    private static readonly byte[] s_jpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] s_pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] s_tiffLittleEndianSignature = [0x49, 0x49, 0x2A, 0x00];
    private static readonly byte[] s_tiffBigEndianSignature = [0x4D, 0x4D, 0x00, 0x2A];
    private static readonly byte[] s_ebmlSignature = [0x1A, 0x45, 0xDF, 0xA3];
    private static readonly byte[] s_asfSignature = [0x30, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11];
    private static readonly byte[] s_mpegProgramStreamSignature = [0x00, 0x00, 0x01, 0xBA];
    private static readonly byte[] s_asciiGif87a = Encoding.ASCII.GetBytes("GIF87a");
    private static readonly byte[] s_asciiGif89a = Encoding.ASCII.GetBytes("GIF89a");
    private static readonly byte[] s_asciiBitmap = Encoding.ASCII.GetBytes("BM");
    private static readonly byte[] s_asciiRiff = Encoding.ASCII.GetBytes("RIFF");
    private static readonly byte[] s_asciiWebP = Encoding.ASCII.GetBytes("WEBP");
    private static readonly byte[] s_asciiAvi = Encoding.ASCII.GetBytes("AVI ");
    private static readonly byte[] s_asciiFlv = Encoding.ASCII.GetBytes("FLV");
    private static readonly byte[] s_asciiFtyp = Encoding.ASCII.GetBytes("ftyp");
    private static readonly string[] s_imageFtypBrands = ["avif", "avis", "heic", "heix", "heim", "heis", "hevc", "hevm", "hevs", "hevx", "mif1", "msf1"];

    public static bool IsImage(byte[] bytes) =>
        HasPrefix(bytes, s_jpegSignature) ||
        HasPrefix(bytes, s_pngSignature) ||
        HasPrefix(bytes, s_tiffLittleEndianSignature) ||
        HasPrefix(bytes, s_tiffBigEndianSignature) ||
        HasPrefix(bytes, s_asciiGif87a) ||
        HasPrefix(bytes, s_asciiGif89a) ||
        HasPrefix(bytes, s_asciiBitmap) ||
        (HasPrefix(bytes, s_asciiRiff) && HasPrefixAt(bytes, 8, s_asciiWebP)) ||
        IsImageFtyp(bytes);

    public static bool IsVideo(byte[] bytes) =>
        HasPrefix(bytes, s_ebmlSignature) ||
        HasPrefix(bytes, s_asfSignature) ||
        HasPrefix(bytes, s_mpegProgramStreamSignature) ||
        HasPrefix(bytes, s_asciiFlv) ||
        (HasPrefix(bytes, s_asciiRiff) && HasPrefixAt(bytes, 8, s_asciiAvi)) ||
        (bytes.Length >= 189 && bytes[0] == 0x47 && bytes[188] == 0x47) ||
        (HasPrefixAt(bytes, 4, s_asciiFtyp) && !IsImageFtyp(bytes));

    private static bool IsImageFtyp(byte[] bytes) =>
        bytes.Length >= 12 && HasPrefixAt(bytes, 4, s_asciiFtyp) && Array.IndexOf(s_imageFtypBrands, Encoding.ASCII.GetString(bytes, 8, 4)) >= 0;

    private static bool HasPrefix(byte[] bytes, byte[] signature) => HasPrefixAt(bytes, 0, signature);

    private static bool HasPrefixAt(byte[] bytes, int offset, byte[] signature) =>
        bytes.Length >= offset + signature.Length && bytes.AsSpan(offset, signature.Length).SequenceEqual(signature);
}
