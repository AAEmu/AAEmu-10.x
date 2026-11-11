using System.Text;

namespace AAEmu.Game.Models.Game.Char;

public static class UiData
{
    public const int MaximumBytes = 8191;

    private static readonly UTF8Encoding Encoding = new(false, true);

    public static bool IsSupported(ushort type) => type is >= 1 and <= 7 or 20;

    /// <summary>
    /// Sections the 10.0.2.13 client asks for itself with CSRequestUIData while it shows the character list
    /// (Live World log, 2026-11-02: every login requests exactly these).
    /// </summary>
    public static readonly ushort[] RequestedByClient = [1, 2, 3, 4, 20];

    /// <summary>
    /// Sections pushed with SCResponseUIData at character select. The client saves sections 1-7 and 20 when it
    /// leaves the world but never requests 5, 6 or 7, so anything not pushed here only lives in the client's
    /// memory and is gone after a client restart or a server reboot (the quest tracker reset report).
    /// </summary>
    public static readonly ushort[] SentAtCharacterSelect = [1, 2, 5, 6, 7];

    public static bool TryDecode(byte[] bytes, out string value)
    {
        value = null;
        if (bytes.Length > MaximumBytes || bytes.Contains((byte)0))
            return false;

        try
        {
            value = Encoding.GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    public static bool TryEncode(string value, out byte[] bytes)
    {
        bytes = [];
        if (value == null || value.Length > MaximumBytes || value.Contains('\0'))
            return false;

        try
        {
            if (Encoding.GetByteCount(value) > MaximumBytes)
                return false;
            bytes = Encoding.GetBytes(value);
            return true;
        }
        catch (EncoderFallbackException)
        {
            return false;
        }
    }
}
