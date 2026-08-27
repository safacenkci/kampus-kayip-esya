namespace KampusKayipEsya.Api.Authorization;

/// <summary>
/// İletişim bilgisinin kimlere açılabileceğini tek yerde tutar.
/// F0: yalnız geçerli manage_token ve yalnız ilan detayı.
/// F3-BE-03 onaylı talep sahibini ve Admin'i ekler; listede asla açılmaz.
/// </summary>
public static class ContactVisibilityPolicy
{
    public static bool CanRevealContact(bool isItemDetail, bool hasValidManageToken) =>
        isItemDetail && hasValidManageToken;
}
