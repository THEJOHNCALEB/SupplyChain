using SupplyChain.Infrastructure.Security;

namespace SupplyChain.Tests;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesAUniqueSaltedValueThatVerifies()
    {
        var first = _hasher.Hash("Demo@123");
        var second = _hasher.Hash("Demo@123");

        Assert.NotEqual(first, second);
        Assert.StartsWith("pbkdf2-sha256$", first);
        Assert.True(_hasher.Verify("Demo@123", first));
        Assert.True(_hasher.Verify("Demo@123", second));
    }

    [Fact]
    public void Verify_RejectsWrongOrMalformedValues()
    {
        var hash = _hasher.Hash("Demo@123");

        Assert.False(_hasher.Verify("demo@123", hash));
        Assert.False(_hasher.Verify("Demo@123", "not-a-hash"));
        Assert.False(_hasher.Verify("", hash));
        Assert.False(_hasher.Verify("Demo@123", ""));
    }
}
