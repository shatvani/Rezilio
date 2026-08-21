# PR Checks Workflow (`pr.yml`)

## Áttekintés

A `pr.yml` egy GitHub Actions workflow, amely automatikusan elindul,
amikor Pull Request-et nyitsz a `main` branch-re. Feladata a "kapuőr"
szerepe: megakadályozza, hogy hibás, teszteletlen vagy dokumentálatlan
kód kerüljön a fő ágba.

---

## Trigger (`on`)

```yaml
on:
  pull_request:
    branches: [ main ]
```

Kizárólag `main` branch-re nyitott PR-nál indul el. `development`-re
nyitott PR-oknál nem fut — ott nincs ilyen védelmi réteg szándékosan,
a fejlesztési ág szabadabb.

---

## Jogosultságok (`permissions`)

```yaml
permissions:
  pull-requests: write
  contents: read
```

| Jogosultság | Miért kell |
|---|---|
| `pull-requests: write` | A coverage kommentet a PR-ra kell írni |
| `contents: read` | A repository kódjának olvasásához |

GitHub Actions önmaga kap egy ideiglenes `GITHUB_TOKEN`-t minden
futtatáshoz — ezek a sorok korlátozzák, mit tehet ezzel a tokennel.

---

## Job-ok

A workflow öt párhuzamosan (vagy feltételesen) futó job-ból áll.

### 1. `build` – Fordítás és kódformázás

**Fut:** minden PR-ra  
**Célja:** ellenőrzi, hogy a kód lefordul-e és megfelel-e a
kódformázási szabályoknak.

Lépések:
- `dotnet restore` – NuGet csomagok visszaállítása
- `dotnet format --verify-no-changes` – kódformázás ellenőrzése
  az `.editorconfig` szabályai alapján (hibánál a pipeline piros)
- `dotnet build --configuration Release` – Release módú fordítás

Ha a `build` job elbukik, a többi job sem indul el (ők `needs: build`-del
vannak deklarálva).

---

### 2. `unit-tests` – Unit tesztek

**Fut:** `build` sikere után  
**Célja:** az üzleti logika izolált tesztelése, külső függőség nélkül.

```bash
dotnet test --filter "Category=Unit"
```

A tesztek `[Trait("Category", "Unit")]` attribútummal vannak megjelölve.
Ezek gyorsak, nincs adatbázis- vagy hálózati függőségük.

Eredmény: `.trx` fájl formátumban artifact-ként feltölti GitHubra,
ahol a PR oldalán megtekinthető.

---

### 3. `integration-tests` – Integrációs tesztek

**Fut:** `build` sikere után, a `unit-tests`-szel párhuzamosan  
**Célja:** valós adatbázis-műveleteket tesztel.

```bash
dotnet test --filter "Category=Integration"
```

A tesztek `[Trait("Category", "Integration")]` attribútummal vannak
megjelölve. A `Testcontainers.PostgreSql` csomag automatikusan indít
egy ideiglenes PostgreSQL Docker konténert a tesztek idejére — nincs
szükség külön szerverre vagy konfigurációra.

---

### 4. `coverage` – Lefedettségi kapu

**Fut:** mindkét teszt job sikere után  
**Célja:** biztosítja, hogy a kódnak legalább 70%-os lefedettség legyen.

Lépések:
1. Újrafuttatja az összes tesztet, coverage gyűjtéssel
2. `ReportGenerator` eszközzel összesíti a coverage adatokat
3. Ha a lefedettség < 70%: a pipeline piros, a PR nem mergelődhet
4. A coverage összesítőt kommentként teszi közzé a PR-ra
   (`marocchino/sticky-pull-request-comment`) — minden push után
   automatikusan frissül

---

### 5. `frontend-check` – Frontend ellenőrzés

**Állapot:** jelenleg letiltva (`if: false`)  
**Aktiválás:** Story 0.8 (Next.js frontend) elkészülte után

Majd végzi: TypeScript típusellenőrzés, ESLint, Next.js build.

---

### 6. `changelog-check` – CHANGELOG.md ellenőrzés

**Fut:** minden PR-ra, a többi job-tól függetlenül  
**Célja:** kikényszeríti, hogy minden PR frissítse a CHANGELOG.md-t.

```bash
git diff origin/main...HEAD -- CHANGELOG.md | grep -q "^\+"
```

Ha a CHANGELOG.md nem változott a PR-ban: a pipeline piros és a PR
nem mergelődhet.

---

## Job-ok kapcsolata
```
PR megnyitás
│
├──► build ──────────────┬──► unit-tests ───────┐
│                        │                      ├──► coverage
│                        └──► integration-tests ┘
│
└──► changelog-check (build-tól független, rögtön indul)
```


---

## A CHANGELOG.md és a PR workflow kapcsolata

### Miért kötelező frissíteni?

A `changelog-check` job megakadályozza a merge-t, ha a PR nem
tartalmaz CHANGELOG.md módosítást. Ez kikényszeríti, hogy minden
változás dokumentálva legyen — nem utólag, hanem a fejlesztéssel
egyidőben.

### A CHANGELOG.md struktúrája

```markdown
## [Unreleased]

### Added
- Új funkciók

### Changed
- Meglévő funkciók módosítása

### Fixed
- Hibajavítások

### Removed
- Törölt funkciók

## [0.1.0] - 2026-08-21

### Added
- ...
```

### Mikor kell frissíteni?

**Minden PR-ban**, mielőtt merge-eled — kivétel nélkül. Tipikusan a
commit előtt vagy az utolsó commitban.

### Mit kell írni?

| Változás típusa | Szekció | Példa |
|---|---|---|
| Új endpoint, új modul, új feature | `Added` | `- Risk aggregát létrehozása (Story 1.1)` |
| Meglévő viselkedés módosítása | `Changed` | `- TenantSettings: alapértelmezett pénznem módosítható` |
| Hibajavítás | `Fixed` | `- ModuleAccess: lejárt trial helyesen kezelt` |
| Törölt kód, eltávolított endpoint | `Removed` | `- Deprecated GetActiveModules endpoint eltávolítva` |
| Refaktor, dokumentáció, CI | `Changed` | `- PR workflow: coverage gate 70%-ra emelve` |

### Hogyan kell frissíteni?

Mindig az `[Unreleased]` szekció alá kerülnek az új bejegyzések:

```markdown
## [Unreleased]

### Added
- Story 1.1: Risk aggregát, RisksDbContext, első EF Core migráció
```

Amikor jön a release (pl. `v0.2.0`), az `[Unreleased]` tartalma
átkerül egy új verzió szekció alá, és az `[Unreleased]` újra üres lesz:

```markdown
## [Unreleased]

## [0.2.0] - 2026-09-15

### Added
- Story 1.1: Risk aggregát...
```

### Mit NE írj?

- ❌ Technikai implementációs részleteket (`EF Core migration hash: abc123`)
- ❌ Belső refaktorokat, amelyek a felhasználót nem érintik
- ❌ Commit hash-t vagy branch nevet
- ✅ Azt, amit egy fejlesztő (vagy jövőbeli te) meg akar érteni 6 hónap múlva