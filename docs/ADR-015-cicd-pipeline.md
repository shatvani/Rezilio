# ADR-015 – CI/CD Pipeline: GitHub Actions + Self-hosted Runner (Hetzner)

**Dátum:** 2026-08-19  
**Státusz:** Elfogadva  
**Döntéshozó:** Projekt architekt

---

## Kontextus

A platform fejlesztéséhez szükség van egy automatizált CI/CD pipeline-ra, amely:
- Minden PR-on lefuttatja az összes tesztet (unit + integrációs)
- Kódminőséget ellenőriz (Roslyn analyzer, code coverage ≥ 70%)
- Sikeres merge után automatikusan deployal a Hetzner production VPS-re
- A self-hosted runner a Hetzner VPS-en fut Docker-ben (nincs GitHub-hosted runner cost)

---

## Döntés

**GitHub Actions + self-hosted runner (Hetzner VPS) + Testcontainers (integrációs tesztek) + GitHub Container Registry (ghcr.io)**

---

## Pipeline struktúra

```
PR (pull_request → main)
  └── pr.yml
        ├── build          dotnet build, dotnet format --verify-no-changes
        ├── unit-tests     dotnet test --filter Category=Unit
        ├── integ-tests    dotnet test --filter Category=Integration  (Testcontainers)
        ├── coverage       Coverlet, 70% küszöb – blokkol ha nem teljesül
        └── frontend       npm ci, tsc --noEmit, eslint

Push (push → main)
  └── deploy.yml
        ├── test           (ugyanaz mint pr.yml, self-hosted runner-en)
        ├── docker-build   docker buildx build, tag: ghcr.io/<owner>/rezilio-api:<sha>
        ├── docker-push    ghcr.io push (GITHUB_TOKEN elegendő)
        └── deploy         SSH → Hetzner: docker compose pull + up -d

Tag (push → v*)
  └── release.yml
        ├── (inherits deploy)
        ├── docker-tag     rezilio-api:<sha> → rezilio-api:<version> + :latest
        └── gh-release     GitHub Release létrehozás CHANGELOG.md szekció alapján
```

---

## Test kategóriák (.NET xUnit)

```csharp
// Unit teszt – gyors, nincs külső függőség
[Trait("Category", "Unit")]
public class MoneyValueObjectTests { ... }

// Integrációs teszt – Testcontainers PostgreSQL konténer
[Trait("Category", "Integration")]
public class ImportJobRepositoryTests : IAsyncLifetime { ... }
```

**Testcontainers** (`Testcontainers.PostgreSql` NuGet, MIT licenc):
- Minden integrációs teszt class saját PostgreSQL konténert indít és állít le
- A self-hosted runneren a Docker socket mount-olva van – Testcontainers natívan működik
- Nincs szükség GitHub Actions service container konfigurációra

---

## Self-hosted runner (Hetzner VPS)

A runner Docker konténerként fut a production VPS mellé: `runner/docker-compose.runner.yml`

```
Hetzner VPS
├── docker-compose.prod.yml  (rezilio-api, frontend, postgres, keycloak, grafana, ...)
└── docker-compose.runner.yml  (github-actions-runner)
       └── /var/run/docker.sock mount  ← Testcontainers + image build
```

**Runner label:** `self-hosted, hetzner, linux`

---

## GitHub Secrets (repo Settings → Secrets → Actions)

| Secret | Leírás |
|---|---|
| `HETZNER_HOST` | VPS IP cím (pl. `95.217.x.x`) |
| `HETZNER_USER` | SSH felhasználó (pl. `deploy`) |
| `HETZNER_SSH_KEY` | Privát SSH kulcs (PEM formátum) |
| `RUNNER_REGISTRATION_TOKEN` | GitHub runner regisztrációs token (újra kell generálni ha lejár) |
| `PROD_ENV_FILE` | A teljes `.env` fájl tartalma (base64 kódolva) |

> ℹ️ `GITHUB_TOKEN` a ghcr.io push-hoz automatikusan elérhető – nem kell kézzel beállítani.

---

## Indoklás

| Szempont | GitHub-hosted runner | Self-hosted Hetzner runner |
|---|---|---|
| **Havi cost (500 build/hó)** | ~20-40 EUR | ✅ ~0 EUR (VPS már megvan) |
| **Docker socket elérés** | Korlátozott | ✅ Teljes (Testcontainers natív) |
| **Hálózat a VPS-hez** | Internet cross | ✅ Lokális hálózat (Hetzner Private Network) |
| **Build cache** | Elvész minden builden | ✅ Megmarad (volume) |
| **Párhuzamos job** | ✅ Több runner | Csak 1 runner (MVP fázisban elegendő) |

---

## Következmények

- PR merge nem lehetséges ha a `pr.yml` pipeline piros (branch protection rule)
- Coverage < 70% blokkol – a tesztek megírása kötelező, nem opcionális
- Self-hosted runner leállása esetén a CI/CD nem működik – monitorozni kell (Grafana alert)
- `PROD_ENV_FILE` secret rotálása: ha a `.env` változik, a secret-et frissíteni kell
- Docker image méret optimalizálás fontos: multi-stage build, slim base image
