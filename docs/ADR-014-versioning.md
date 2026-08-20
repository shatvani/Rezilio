# ADR-014 – Verziókezelési stratégia: SemVer + Keep a Changelog

**Dátum:** 2026-08-19  
**Státusz:** Elfogadva  
**Döntéshozó:** Projekt architekt

---

## Kontextus

A SaaS platform fejlesztése során egyértelmű verziókezelési stratégiára van szükség, amely:
- Kommunikálja a változások jellegét a csapatnak és az ügyfeleknek
- Lehetővé teszi a deployment pipeline-ban a verzió nyomon követését
- Dokumentálja a változásokat fejlesztők és üzemeltetők számára
- Egységes alapot teremt a GitHub Releases és Docker image tagek számára

---

## Döntés

**Semantic Versioning 2.0 (SemVer)** a verzióformátumhoz, **Keep a Changelog** a változásnapló vezetéséhez.

---

## Verzió formátum

`MAJOR.MINOR.PATCH[-prerelease]`

| Szegmens | Mikor nő | Tipikus trigger |
|---|---|---|
| **MAJOR** | Visszafelé nem kompatibilis változás | API breaking change, inkompatibilis adatmodell migráció |
| **MINOR** | Új feature, visszafelé kompatibilis | Új modul aktiválva, új endpoint, bővített funkcionalitás |
| **PATCH** | Hibajavítás, biztonsági patch | Bug fix, dependency frissítés, kisebb javítás |

**Fejlesztési fázis:** `0.x.y` – nincs stabil API garancia, MAJOR szinte soha nem nő.  
**Első stabil release:** `1.0.0` – az EPIC 1 (MVP) lezárultával, első éles ügyfélnél.

---

## Konvenciók

### Git tagek

- Formátum: `v1.0.0` (kisbetűs `v` prefix + SemVer)
- Minden release → annotált tag: `git tag -a v1.0.0 -m "Release 1.0.0"`
- Tag kizárólag a `main` branch commit-jain kerül elhelyezésre
- Pre-release tag: `v1.0.0-beta.1`, `v1.0.0-rc.1`

### Docker image tagek

```
rezilio-api:1.0.0          # verziózott, immutable – soha nem felülírni
rezilio-api:latest         # mindig a legutóbbi stable release
rezilio-api:0.2.0-beta.1   # pre-release jelölés
```

### Verzió a kódban

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <Version>0.1.0</Version>
    <AssemblyVersion>0.1.0.0</AssemblyVersion>
    <FileVersion>0.1.0.0</FileVersion>
  </PropertyGroup>
</Project>
```

A verzió automatikusan bekerül:
- Az `/api/version` endpointba (GET, publikus, health check célra)
- Az OpenTelemetry `service.version` resource attribútumba
- A Docker image label-ekbe (GitHub Actions build során)

---

## Release folyamat

```
develop / main
    │
    ├── git checkout -b release/1.0.0
    │       ├── CHANGELOG.md: [Unreleased] → [1.0.0] – dátum
    │       ├── Directory.Build.props: <Version>1.0.0</Version>
    │       └── PR → main, review, squash merge
    │
    ├── git tag -a v1.0.0 -m "Release 1.0.0"
    ├── git push origin v1.0.0
    │
    └── GitHub Actions:
            ├── Docker build + push (rezilio-api:1.0.0 + :latest)
            └── GitHub Release létrehozás (CHANGELOG.md szekció tartalommal)
```

## Hotfix folyamat

```
main (v1.0.0)
    │
    ├── git checkout -b hotfix/1.0.1-keycloak-token-expiry
    │       ├── Javítás + teszt
    │       ├── CHANGELOG.md: [1.0.1] felvétele
    │       └── PR → main, merge
    │
    ├── git tag -a v1.0.1 -m "Hotfix 1.0.1"
    └── cherry-pick → develop (ha külön develop branch van)
```

---

## CHANGELOG.md formátum

```markdown
# Changelog

Minden változás ebben a fájlban kerül dokumentálásra.

Formátum: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
Verziókezelés: [SemVer](https://semver.org/spec/v2.0.0.html)

## [Unreleased]
### Added
### Changed
### Fixed
### Removed
### Security

## [0.1.0] – 2026-08-19
### Added
- Projekt inicializálás, alapinfrastruktúra (Story 0.1–0.4)
```

**Szekciók:**
- **Added** – új funkció
- **Changed** – meglévő funkció megváltozott
- **Deprecated** – hamarosan eltávolítandó
- **Removed** – eltávolított funkció
- **Fixed** – hibajavítás
- **Security** – biztonsági javítás

**Kötelező PR ellenőrzés:** minden PR-hoz kell `[Unreleased]` szekció bejegyzés a CHANGELOG.md-ben (CI ellenőrzi).

---

## Indoklás

| Szempont | CalVer (alternatíva) | SemVer (döntés) |
|---|---|---|
| Változás jellege kommunikálva | ❌ Csak dátum | ✅ MAJOR/MINOR/PATCH egyértelmű |
| Iparági standard | Részben | ✅ npm, NuGet, Docker mind ismeri |
| Automation (Renovate, Dependabot) | Részben | ✅ Natívan támogatott |
| Pre-release jelölés | Nehézkes | ✅ `-alpha.1`, `-beta.1`, `-rc.1` |

---

## Következmények

- Minden PR-hoz szükséges a CHANGELOG.md `[Unreleased]` szekció frissítése
- A `0.x.y` fázisban a MINOR és PATCH különbség kevésbé szigorú – a lényeg a changelog
- GitHub Releases automatikusan linkeli a git tag-et és a changelog szekciót
- A Docker image tag és git tag szinkronban marad a CI pipeline-ban (GitHub Actions)
- Jövőbeli Renovate/Dependabot konfiguráció SemVer range-ek alapján működik
