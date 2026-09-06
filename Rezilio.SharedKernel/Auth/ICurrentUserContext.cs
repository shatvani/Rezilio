namespace Rezilio.SharedKernel.Auth;

/// <summary>
/// Az aktuálisan bejelentkezett Keycloak-felhasználó identitás-kontextusa.
///
/// A UserId/Email a JWT claim-jeiből származik (ld. AppClaims). A GetKeyPersonIdAsync()
/// köti össze a Keycloak-identitást a hozzá tartozó Organization.KeyPerson rekorddal:
///   1. ha a userhez már korábban kötve lett egy KeyPerson (KeyPerson.UserId), azt adja vissza;
///   2. ha nincs ilyen, egyszeri "JIT-linking" történik: ha pontosan egy aktív KeyPerson
///      email címe egyezik a user email címével, ahhoz köti (és eltárolja a UserId-t);
///   3. minden más esetben (nincs egyezés, vagy több egyezés van) null-t ad vissza —
///      ilyenkor a hívó oldalnak csak a szerepkör-alapú [Authorize(Roles=...)] ellenőrzésre
///      szabad támaszkodnia, rekord-tulajdonos-ellenőrzést nem tud végezni.
///
/// Nem minden bejelentkezett felhasználóhoz tartozik KeyPerson (pl. Viewer/Executive) —
/// a null visszatérési érték ezért legitim állapot, nem hiba.
///
/// Ha a JIT-linking nem egyértelmű (nincs vagy több email-egyezés van), az Admin a
/// LinkKeyPersonToUser Command-dal kézzel köti össze a KeyPerson-t a UserId-vel.
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>A Keycloak user UUID (AppClaims.UserId / JWT "sub"), vagy null, ha nincs bejelentkezett felhasználó.</summary>
    string? UserId { get; }

    /// <summary>A felhasználó e-mail címe (AppClaims.Email), vagy null.</summary>
    string? Email { get; }

    /// <summary>
    /// A bejelentkezett felhasználóhoz tartozó KeyPerson.Id, vagy null, ha nincs
    /// (egyértelműen azonosítható) hozzá tartozó KeyPerson.
    /// </summary>
    Task<Guid?> GetKeyPersonIdAsync(CancellationToken ct = default);

    /// <summary>
    /// Igaz, ha a bejelentkezett felhasználónak van a megadott (Keycloak realm) szerepköre.
    /// Rekord-szintű tulajdonos-ellenőrzéssel kombinálva használandó (ld. §3.5): a
    /// szerepkör dönti el, MILYEN TÍPUSÚ műveletet végezhet valaki, a rekord-tulajdonos-
    /// ellenőrzés pedig, hogy KONKRÉTAN AZON a rekordon.
    /// </summary>
    bool IsInRole(string role);
}
