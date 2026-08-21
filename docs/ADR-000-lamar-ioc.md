# ADR-001: Lamar IoC container használata Microsoft DI helyett

**Status:** Accepted  
**Date:** 2026-08-21  
**Deciders:** Hatvani Sándor

## Context

A Rezilio API Wolverine-t használ message bus és HTTP handler pipeline-ként.
A Wolverine futásidőben C# kódot generál a handlerekhez, és ehhez végig kell
követnie a teljes DI dependency fát. Az EF Core `AddDbContext<T>()` belső
lambda factory-val regisztrálja a `DbContextOptions<T>`-t — ezt Wolverine
nem tudja átlátni, ezért service location-t igényelne, amit Wolverine
(helyesen) tilt (`ServiceLocationPolicy.NotAllowed`).

Ez a probléma minden Wolverine handler esetén fennáll ahol EF Core DbContext-et
injektálunk közvetlenül. A Story 0.6 fejlesztésekor ütköztünk ebbe először.

## Decision

A Microsoft DI containert lecseréljük **Lamar**-ra
(`Lamar.Microsoft.DependencyInjection` NuGet csomag).

Lamar a JasperFx ökoszisztéma IoC containere — ugyanattól a csapattól mint
Wolverine. Lamar mélyebb introspection képességgel rendelkezik: átlát a lambda
factory-kon, ezért az EF Core DbContext regisztráció probléma nem áll fenn.

A csere minimálisan invazív: Lamar teljes mértékben kompatibilis az
`IServiceCollection` API-val, meglévő regisztrációk változatlanul működnek.

## Options Considered

### Option A: Maradni Microsoft DI-nál (workaround-dal)

| Dimension | Assessment |
|-----------|------------|
| Komplexitás | Közepes — minden DbContext-et injektáló handlernél explicit `AddSingleton(options) + AddScoped<DbContext>()` kell |
| Karbantarthatóság | Alacsony — nem standard EF Core regisztráció, zavaró jövőbeli fejlesztőknek |
| Wolverine kompatibilitás | Korlátozott — más opaque factory is problémát okozhat |
| Csapatismeret | Magas — Microsoft DI mindenki ismeri |

**Hátrány:** Minden modulban egyedi workaround kell DbContext-hez; nem standard.

### Option B: Lamar IoC container (választott)

| Dimension | Assessment |
|-----------|------------|
| Komplexitás | Alacsony — `builder.Host.UseLamar()` egy sor |
| Karbantarthatóság | Magas — standard `AddDbContext` használható minden modulban |
| Wolverine kompatibilitás | Natív — Wolverine Lamarhoz van optimalizálva |
| Csapatismeret | Alacsony — de az API kompatibilis Microsoft DI-val |

**Startup:** Lamar valamivel lassabb a startup-ban (mélyebb introspection),
futásidőben azonos vagy gyorsabb.

## Trade-off Analysis

A Lamar-ra való átállás egyszeri, minimális kockázatú lépés a projekt korai
szakaszában. Az alternatíva (Microsoft DI + workaround) technikai adósságot
halmoz fel: minden új EF Core DbContext-et használó modul ugyanezt a problémát
fogja örökölni, és minden esetben nem-standard regisztrációt igényel.

## Consequences

- `AddDbContext<T>()` standard formában használható minden modulban
- Wolverine handler code generation problémamentes EF Core-ral
- `LicensingModule.cs` visszaáll a clean `AddDbContext` regisztrációra
- Jövőbeli modulok (RiskRegister, Assessment stb.) problémamentesen
  injektálhatnak DbContext-et handlerekbe
- Lamar ismerős Microsoft DI `IServiceCollection` API-n keresztül —
  meglévő tudás érvényes marad

## Action Items

1. [ ] `Lamar.Microsoft.DependencyInjection` csomag telepítése `Rezilio.Api`-ba
2. [ ] `builder.Host.UseLamar()` hozzáadása `Program.cs`-be
3. [ ] `LicensingModule.cs` visszaállítása standard `AddDbContext`-re
4. [ ] Build + teljes teszt futtatása
5. [ ] Jövőbeli modulok `CLAUDE.md` szabályba: DbContext regisztrációhoz mindig `AddDbContext<T>()` használandó