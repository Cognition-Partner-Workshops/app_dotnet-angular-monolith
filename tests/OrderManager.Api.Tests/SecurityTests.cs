using OrderManager.Api.Data;

namespace OrderManager.Api.Tests;

public class SecurityTests
{
    [Fact]
    public void PasswordHash_DifferentSaltsProduceDifferentHashes()
    {
        var password = "TestPassword@123";
        var hash1 = SeedData.HashPassword(password);
        var hash2 = SeedData.HashPassword(password);

        Assert.NotEqual(hash1, hash2);
        Assert.True(SeedData.VerifyPassword(password, hash1));
        Assert.True(SeedData.VerifyPassword(password, hash2));
    }

    [Fact]
    public void PasswordHash_EmptyPasswordStillWorks()
    {
        var password = "";
        var hash = SeedData.HashPassword(password);
        Assert.True(SeedData.VerifyPassword(password, hash));
        Assert.False(SeedData.VerifyPassword("notempty", hash));
    }

    [Fact]
    public void PasswordHash_LongPasswordWorks()
    {
        var password = new string('A', 500) + "@1a";
        var hash = SeedData.HashPassword(password);
        Assert.True(SeedData.VerifyPassword(password, hash));
    }

    [Fact]
    public void PasswordHash_SpecialCharactersWork()
    {
        var password = "P@$$w0rd!#%&*()_+-=[]{}|;':\",./<>?";
        var hash = SeedData.HashPassword(password);
        Assert.True(SeedData.VerifyPassword(password, hash));
    }

    [Fact]
    public void PasswordVerify_WrongPasswordReturnsFalse()
    {
        var hash = SeedData.HashPassword("CorrectPassword@1");
        Assert.False(SeedData.VerifyPassword("WrongPassword@1", hash));
    }

    [Fact]
    public void PasswordHash_OutputIsBase64()
    {
        var hash = SeedData.HashPassword("test");
        var bytes = Convert.FromBase64String(hash);
        Assert.Equal(48, bytes.Length); // 16 salt + 32 hash
    }
}
