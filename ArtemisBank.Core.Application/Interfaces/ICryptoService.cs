namespace ArtemisBank.Core.Application.Interfaces
{
    public interface ICryptoService
    {
        string GenerateSalt();

        string Hash(string value, string salt);

        bool Verify(string value, string salt, string expectedHash);

        int NextDigit();

        string RandomDigits(int length);
    }
}
