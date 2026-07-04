using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace SimpleAuthNet;

/// <summary>
/// Password hashing/verification for SimpleAuth (finding H1 remediation).
///
/// New passwords are hashed with <b>Argon2id</b> and stored in a self-describing PHC-style
/// string (algorithm + parameters + salt + hash) so <see cref="Verify"/> knows how to check them.
/// Legacy passwords were stored as a single-round HMAC-SHA512 over the plaintext with a random
/// per-user key acting as the salt; those rows have no PHC marker and are treated as legacy.
///
/// Migration is <b>rehash-on-login</b>: a legacy password that verifies is re-hashed with Argon2id
/// and overwritten transparently (see <see cref="VerifyResult.NeedsRehash"/>). No forced reset.
///
/// Stored format (UTF-8 bytes in the PasswordHash / HashedPassword columns):
///   $argon2id$v=19$m=&lt;memKiB&gt;,t=&lt;iterations&gt;,p=&lt;parallelism&gt;$&lt;saltBase64&gt;$&lt;hashBase64&gt;
/// (standard padded Base64 — this codec both writes and reads it, so padding is intentional.)
/// </summary>
public static class SimpleAuthPasswordHasher
{
    // OWASP-recommended Argon2id floor (second recommended option): 19 MiB memory, 2 iterations,
    // parallelism 1. Defensible starting point; raise as hardware allows. Changing any of these
    // makes existing Argon2id hashes report NeedsRehash so they upgrade on next successful login.
    private const int MemoryKiB = 19456;   // 19 MiB
    private const int Iterations = 2;
    private const int Parallelism = 1;
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;

    private const string Prefix = "$argon2id$";

    /// <summary>Hashes a new password with Argon2id.</summary>
    /// <returns>
    /// hash = UTF-8 bytes of the PHC-encoded string (self-describing).
    /// salt = the raw CSPRNG salt bytes. It is also embedded in the encoded hash; it is returned
    /// separately only so callers can populate the (NOT NULL) salt columns uniformly with legacy rows.
    /// </returns>
    public static (byte[] hash, byte[] salt) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var rawHash = ComputeArgon2(password, salt, MemoryKiB, Iterations, Parallelism, HashSizeBytes);
        var encoded = $"{Prefix}v=19$m={MemoryKiB},t={Iterations},p={Parallelism}$" +
                      $"{Convert.ToBase64String(salt)}${Convert.ToBase64String(rawHash)}";
        return (Encoding.UTF8.GetBytes(encoded), salt);
    }

    /// <summary>
    /// Verifies <paramref name="password"/> against a stored hash.
    /// Contract: <paramref name="storedSalt"/> is consumed <b>only</b> by the legacy HMAC branch.
    /// Argon2id records verify against the salt embedded in the PHC string, so the same call works
    /// uniformly for current credentials and mixed-scheme password-history entries.
    /// </summary>
    public static VerifyResult Verify(string password, byte[]? storedHash, byte[]? storedSalt)
    {
        if (storedHash == null || storedHash.Length == 0)
            return new VerifyResult(false, false);

        if (IsArgon2Encoded(storedHash))
            return VerifyArgon2(password, storedHash);

        // Legacy HMAC-SHA512 verification — RETAINED FOR MIGRATION ONLY. Do not remove until all
        // users have logged in at least once (rehash-on-login upgrades each row to Argon2id).
        if (storedSalt == null)
            return new VerifyResult(false, false);

        using var hmac = new HMACSHA512(storedSalt);
        var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        var ok = CryptographicOperations.FixedTimeEquals(computed, storedHash);
        // A verifying legacy password should be upgraded to Argon2id.
        return new VerifyResult(ok, ok);
    }

    /// <summary>True when the stored hash carries the Argon2id PHC marker (i.e. not a legacy row).</summary>
    public static bool IsArgon2Encoded(byte[]? storedHash)
    {
        if (storedHash == null || storedHash.Length < Prefix.Length) return false;
        // Raw HMAC-SHA512 output is 64 binary bytes and cannot begin with the ASCII "$argon2id$".
        var head = Encoding.UTF8.GetString(storedHash, 0, Prefix.Length);
        return head == Prefix;
    }

    private static VerifyResult VerifyArgon2(string password, byte[] storedHash)
    {
        var encoded = Encoding.UTF8.GetString(storedHash);
        // $argon2id$v=19$m=..,t=..,p=..$<salt>$<hash>
        var parts = encoded.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5) return new VerifyResult(false, false);

        var paramMap = parts[2].Split(',');
        int mem = ParseParam(paramMap, "m");
        int iter = ParseParam(paramMap, "t");
        int par = ParseParam(paramMap, "p");
        if (mem <= 0 || iter <= 0 || par <= 0) return new VerifyResult(false, false);

        var salt = Convert.FromBase64String(parts[3]);
        var expected = Convert.FromBase64String(parts[4]);

        var computed = ComputeArgon2(password, salt, mem, iter, par, expected.Length);
        var ok = CryptographicOperations.FixedTimeEquals(computed, expected);

        // Upgrade if the stored parameters no longer match the current cost settings.
        var needsRehash = ok && (mem != MemoryKiB || iter != Iterations || par != Parallelism);
        return new VerifyResult(ok, needsRehash);
    }

    private static byte[] ComputeArgon2(string password, byte[] salt, int memKiB, int iterations, int parallelism, int outputBytes)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memKiB,
            Iterations = iterations,
            DegreeOfParallelism = parallelism
        };
        return argon2.GetBytes(outputBytes);
    }

    private static int ParseParam(string[] pairs, string key)
    {
        foreach (var pair in pairs)
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2 && kv[0] == key && int.TryParse(kv[1], out var value))
                return value;
        }
        return -1;
    }
}

/// <summary>Outcome of a password verification: whether it matched and whether the stored hash
/// should be re-hashed with the current Argon2id scheme (legacy row or outdated parameters).</summary>
public readonly record struct VerifyResult(bool Verified, bool NeedsRehash);
