using Xunit;

namespace KampusKayipEsya.Api.Tests;

/// <summary>
/// F0-INF-02 bitti tanımı: kasıtlı bozuk test CI'yı kırmızıya düşürür.
/// Bu dosya kırmızı koşu kanıtından sonra geri alınır; PR'da kalmaz.
/// </summary>
public sealed class CiFailProbe
{
    [Fact]
    public void IntentionalFailure_ForCiRedProof()
    {
        Assert.Fail("F0-INF-02 kasıtlı kırmızı kanıt — bu test PR'da kalmamalı.");
    }
}
