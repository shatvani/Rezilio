# ADR-006 – Money value object, tenant szintű pénznem konfiguráció

**Dátum:** 2026-08-16  
**Státusz:** Elfogadva  
**Döntéshozó:** Projekt architekt

---

## Kontextus

A rendszer pénzügyi kockázatokat, veszteség-becsléseket, KRI értékeket kezel. Ezekhez pénznemeket kell tárolni és megjeleníteni. Kérdés: sima `decimal` mezőt használunk, vagy strukturált pénznem-kezelést?

## Döntés

**`Money` value object** használata minden pénzügyi értékhez, **tenant szintű alapértelmezett pénznem** konfigurációval.

```csharp
// SharedKernel
public record Money(decimal Amount, CurrencyCode Currency)
{
    public static Money Zero(CurrencyCode currency) => new(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new CurrencyMismatchException(Currency, other.Currency);
        return new Money(Amount + other.Amount, Currency);
    }

    public string Format(string locale) // pl. "1 234 567 Ft" vagy "$1,234,567"
        => Amount.ToString("C", CultureInfo.GetCultureInfo(locale));
}

public record CurrencyCode(string Value) // ISO 4217: "HUF", "EUR", "USD", "GBP"
{
    public static CurrencyCode HUF => new("HUF");
    public static CurrencyCode EUR => new("EUR");
    public static CurrencyCode USD => new("USD");
}
```

## Tenant konfiguráció

```json
{
  "DefaultCurrency": "HUF",
  "Locale": "hu-HU"
}
```

A `TenantSettings` aggregate tárolja és API-n keresztül módosítható.

## Indoklás

| Szempont | `decimal` | `Money` value object |
|---|---|---|
| **Pénznem info elvesztése** | ❌ Nincs pénznem adat | ✅ Mindig együtt utazik |
| **Összehasonlítás biztonsága** | ❌ Különböző pénznemek összeadhatók | ✅ Compile/runtime védelem |
| **Megjelenítés** | ❌ Külön logika kell | ✅ `Format()` metódus |
| **Multi-currency jövő** | ❌ Nehéz retrofittálni | ✅ Már készen áll |

## Multi-currency (Phase 2+)

Ha multinacionális vállalat esetén több pénznem egyidejű kezelése szükséges:
- Árfolyam-service bevezetése (`IExchangeRateService`)
- Konverzió csak explicit hívásra, automatikus soha
- Riportokban: választott riport pénznem + eredeti érték is megőrzve

## Következmények

- Minden `decimal` típusú pénzügyi mező `Money`-ra cserélendő
- EF Core: `Money` owned entity-ként tárolva (két oszlop: `Amount`, `CurrencyCode`)
- Pénznem hardcode-olása a kódban tilos – mindig `TenantSettings.DefaultCurrency`
