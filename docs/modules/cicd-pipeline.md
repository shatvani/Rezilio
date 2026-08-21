# CI/CD Pipeline (Story 0.10)

## Áttekintés

A REZILIO projekt három GitHub Actions workflow-t használ a kód
minőségének biztosítására és az automatikus élesítéshez. Mindhárom
fájl a `.github/workflows/` mappában található.

| Fájl | Trigger | Feladata |
|---|---|---|
| `pr.yml` | PR → `main` | Ellenőrzés, tesztek, coverage |
| `deploy.yml` | Push → `main` | Docker build, push, élesítés |
| `release.yml` | Tag `v*.*.*` push | Verzió kiadás, GitHub Release |

Részletes leírások:
- [`docs/workflows/pr-workflow.md`](../workflows/pr-workflow.md)
- [`docs/workflows/deploy-workflow.md`](../workflows/deploy-workflow.md)
- [`docs/workflows/release-workflow.md`](../workflows/release-workflow.md)

---

## Self-hosted runner

A pipeline lokális Docker-alapú GitHub Actions runneren fut.
Részletes leírás: [`docs/modules/local-runner.md`](local-runner.md)

Runner label: `[self-hosted, linux, local]`

Hetzner VPS meglétekor a label `[self-hosted, linux, hetzner]`-re vált,
a workflow YAML fájlok változatlanok maradnak.

---

## Technológiai stack

| Eszköz | Szerepe |
|---|---|
| GitHub Actions | CI/CD futtatókörnyezet |
| `myoung34/github-runner` | Self-hosted runner Docker image |
| `ghcr.io` | GitHub Container Registry – Docker image-ek tárolása |
| `docker/build-push-action` | Docker image build és push |
| `docker/metadata-action` | Automatikus image tag generálás |
| `dotnet-reportgenerator-globaltool` | Coverage riport generálás |
| `marocchino/sticky-pull-request-comment` | Coverage komment PR-on |
| `Testcontainers.PostgreSql` | PostgreSQL konténer integrációs tesztekhez |
| `coverlet.collector` | .NET code coverage gyűjtés |

---

## Test projekt

Helye: `Rezilio.Tests/`  
Hozzáadva a solution-höz: `Rezilio.slnx`

### xUnit trait konvenciók

Minden tesztet kötelező kategorizálni:

```csharp
[Trait("Category", "Unit")]        // gyors, külső függőség nélkül
[Trait("Category", "Integration")] // Testcontainers PostgreSQL-lel
```

### Coverage gate

A pipeline megköveteli a ≥70% line coverage-t. Ha az érték ez alá esik,
a PR nem mergelődhet.

---

## Docker image-ek

Az API image a `Rezilio.Api/Dockerfile` alapján épül.

| Tag | Mikor keletkezik | Mit jelent |
|---|---|---|
| `sha-a1b2c3d` | Minden main push | Az adott commit image-e |
| `main` | Minden main push | Az utolsó main build |
| `v0.1.0` | Tag push | Adott verzió, örökre stabil |
| `latest` | Tag push | Az utolsó stabil release |

Elérhetők: **GitHub → repo → Packages**

---

## GitHub beállítások

### Branch protection ruleset

**Hol:** GitHub → repo → Settings → Branches → Add ruleset

| Beállítás | Érték |
|---|---|
| Ruleset name | `main-protection` |
| Enforcement status | Active |
| Target branches | `main` |
| Require a pull request | ✅ |
| Required status checks | lásd lent |
| Block force pushes | ✅ |

**Required status checks** – ezek a `pr.yml` job `name:` mezőiből jönnek:

| Status check neve | Job |
|---|---|
| `Build & Format` | `build` |
| `Unit Tests` | `unit-tests` |
| `Integration Tests` | `integration-tests` |
| `Coverage Gate (≥70%)` | `coverage` |
| `CHANGELOG.md Updated` | `changelog-check` |

> ⚠️ A status check nevek csak akkor jelennek meg a GitHub UI-ban,
> ha a `pr.yml` pipeline **legalább egyszer már lefutott**. Először
> commitolj és nyiss egy PR-t, utána add hozzá a check-eket.

> ⚠️ Privát repónál a branch protection csak GitHub Team vagy
> Enterprise csomaggal érvényes. Publikus repónál ingyenesen működik.

### GitHub Secrets (jelenleg szükséges)

Nincs kötelező secret – a `GITHUB_TOKEN` automatikus, a `ghcr.io`
push ehhez is elegendő.

### GitHub Secrets (Hetzner VPS aktiválásakor)

**Hol:** GitHub → repo → Settings → Secrets and variables → Actions

| Secret neve | Tartalma |
|---|---|
| `HETZNER_HOST` | VPS IP-cím vagy domain |
| `HETZNER_USER` | SSH felhasználónév |
| `HETZNER_SSH_KEY` | Privát SSH kulcs (PEM formátum) |
| `PROD_ENV_FILE` | Base64-kódolt production `.env` fájl |

---

## Aktiválandó funkciók (jövőben)

| Funkció | Feltétel | Hol kell módosítani |
|---|---|---|
| Frontend CI (tsc, eslint, build) | Story 0.8 elkészülte | `pr.yml` → `frontend-check` job `if: false` törlése |
| Frontend Docker image | Story 0.8 elkészülte | `deploy.yml` → frontend build lépés komment feloldása |
| Frontend re-tag | Story 0.8 elkészülte | `release.yml` → frontend re-tag lépés komment feloldása |
| SSH deploy | Hetzner VPS elérhető | `deploy.yml` → `deploy` job `if: false` törlése |

---

## A teljes pipeline folyamata

```
fejlesztés → story/ branch
    │
    └──► PR nyitás → main
              │
              └──► pr.yml fut:
                   build → unit + integration tesztek → coverage → changelog
                        │
                        ▼ (ha minden zöld)
                   PR mergelődhet
                        │
                        └──► deploy.yml fut:
                             tesztek → docker build → ghcr.io push
                                  │
                                  └──► (Hetzner után) SSH deploy + health check
                                            │
                                            └──► release folyamat:
                                                 CHANGELOG lezárás →
                                                 verzió frissítés →
                                                 git tag push →
                                                 release.yml fut →
                                                 GitHub Release létrejön
```
