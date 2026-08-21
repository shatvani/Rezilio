# Deploy Workflow (`deploy.yml`)

## Áttekintés

A `deploy.yml` egy GitHub Actions workflow, amely automatikusan elindul,
amikor kód kerül a `main` branch-re — tehát minden sikeres PR merge után.
Feladata a lefordított alkalmazás Docker image-be csomagolása, feltöltése
a GitHub Container Registry-be, és (ha elérhető a VPS) az éles szerverre
való telepítése.

---

## Trigger (`on`)

```yaml
on:
  push:
    branches: [ main ]
```

Csak a `main` branch-re történő push indítja el — közvetlenül nem lehet
pusholni ide (branch protection rule tiltja), tehát a gyakorlatban mindig
egy PR merge az, ami elindítja.

---

## Környezeti változók (`env`)

```yaml
env:
  REGISTRY: ghcr.io
  API_IMAGE: ghcr.io/${{ github.repository_owner }}/rezilio-api
  FRONTEND_IMAGE: ghcr.io/${{ github.repository_owner }}/rezilio-frontend
```

Globálisan elérhető változók az összes job-ban. A `github.repository_owner`
automatikusan a GitHub felhasználónevet jelenti (`shatvani`), így az image
neve: `ghcr.io/shatvani/rezilio-api`.

---

## Job-ok

### 1. `test` – Tesztek újrafuttatása

**Célja:** deploy nem indulhat el tört teszteken. Még ha a PR pipeline
zöld volt is, egy közbülső merge ronthatja el a helyzetet.

Lépések:
- `dotnet restore` + `dotnet build --configuration Release`
- Unit tesztek futtatása (`Category=Unit`)
- Integrációs tesztek futtatása (`Category=Integration`)
- Coverage gate: ≥70% line coverage kötelező

Ha a `test` job elbukik, a `docker-build-push` és `deploy` job nem indul el.

---

### 2. `docker-build-push` – Docker image build és push

**Fut:** `test` sikere után  
**Jogosultságok szükségesek:**

```yaml
permissions:
  contents: read
  packages: write   # ghcr.io-ra való push miatt
```

Lépések:

**Bejelentkezés a GitHub Container Registry-be:**
```yaml
uses: docker/login-action@v3
with:
  registry: ghcr.io
  username: ${{ github.actor }}
  password: ${{ secrets.GITHUB_TOKEN }}
```
A `GITHUB_TOKEN` automatikusan elérhető minden workflow futtatásban —
nem kell külön beállítani.

**Metadata kinyerése:**
```yaml
uses: docker/metadata-action@v5
```
Automatikusan generálja a Docker tag-eket:
- `sha-a1b2c3d` – a commit SHA első 7 karaktere
- `main` – az aktuális branch neve

**Docker Buildx beállítása:**
```yaml
uses: docker/setup-buildx-action@v3
```
A Buildx a Docker build kiterjesztett verziója — támogatja a build
cache-t (`cache-from`, `cache-to`), ami gyorsítja a pipeline-t.

**API image build és push:**
```yaml
uses: docker/build-push-action@v5
with:
  context: .
  file: ./Rezilio.Api/Dockerfile
  push: true
  tags: ${{ steps.meta.outputs.tags }}
  cache-from: type=gha
  cache-to: type=gha,mode=max
```

- `context: .` – a build kontextus a repo gyökere (minden fájl elérhető)
- `file: ./Rezilio.Api/Dockerfile` – a meglévő VS-generált Dockerfile
- `cache-from/to: type=gha` – GitHub Actions cache, az újraépítés
  csak a változott rétegeket fordítja le újra

**Frontend image:** jelenleg kommentezve, Story 0.8 után aktiválandó.

---

### 3. `deploy` – Élesítés Hetzner VPS-re

**Állapot:** jelenleg letiltva (`if: false`)  
**Aktiválás:** amikor Hetzner VPS elérhető

Ez a job SSH-n kapcsolódik a production szerverhez és elvégzi a frissítést.

Amit majd csinál:
1. Beírja a production `.env` fájlt (titkosítva tárolt GitHub Secret-ből)
2. Lehúzza az új Docker image-et (`docker compose pull`)
3. Újraindítja a konténereket (`docker compose up -d`)
4. Health check: `curl --fail https://app.rezilio.hu/health`
5. Ha a health check sikertelen: rollback (`docker compose restart`)

**Szükséges GitHub Secrets** (majd beállítandó):

| Secret | Tartalma |
|---|---|
| `HETZNER_HOST` | A VPS IP-címe vagy domainneve |
| `HETZNER_USER` | SSH felhasználónév (pl. `ubuntu`) |
| `HETZNER_SSH_KEY` | Privát SSH kulcs (PEM formátum) |
| `PROD_ENV_FILE` | Base64-kódolt production `.env` fájl |

---

## Job-ok kapcsolata
```
push → main
│
└──► test
│
└──► docker-build-push
│
└──► deploy (jelenleg: if: false)
```

Szigorúan szekvenciális: ha bármelyik elbukik, a következő nem indul el.

---

## Amit a deploy pipeline most már tud (VPS nélkül)

Minden `main` merge-re:
- ✅ Tesztek lefutnak
- ✅ Docker image buildelődik
- ✅ Image feltöltődik `ghcr.io/shatvani/rezilio-api:main` és
  `ghcr.io/shatvani/rezilio-api:sha-<commit>` tag-ekkel
- ⏸ SSH deploy: letiltva

A feltöltött image-ek megtekinthetők:  
**GitHub → repo → Packages** (jobb oldal)

---

## Hetzner aktiválás lépései (majd)

1. VPS provisionálás, Docker telepítés
2. `docker-compose.prod.yml` feltöltése a szerverre
3. GitHub Secrets beállítása (fent felsorolt 4 secret)
4. `deploy.yml`-ben az `if: false` sor törlése
5. Branch protection rule frissítése: `deploy` job is legyen required
