## Pipeline létrehozása

A `Rezilio\docs\modules` helyen lévő docker-compose.yaml fájlall indítjuk a konténert, ami a myoung34/github-runner image-t használja fel.

A .env fájlban a GITHUB_RUNNER_TOKEN paraméterben adjuk meg a tokent, amit a következő helyről tudjuk beszerezni:

GitHub → repo → Settings → Actions → Runners → **New self-hosted runner**

Itt válasszuk ki a Linux image-t és a lenti konzolos kiírásokból kimásolhatjuk a tokent.

Indítás:
```powershell
docker compose up -d
```
Ezután a GitHub Settings → Runners oldalon megjelenik Idle státusszal.

---

A GitHub Settings → Actions → Runners oldalon **Idle** (zöld) státusszal jelenik meg.

## Pipeline használat

A workflow YAML-ban így hivatkozz rá:

```yaml
runs-on: [self-hosted, linux, local]
```

## Hetzner-re migráció

Ha éles VPS-re kerül a runner, csak a label változik:

```yaml
runs-on: [self-hosted, linux, hetzner]
```

A pipeline YAML többi része változatlan marad.

## Fontos megjegyzések

- A `.env` fájl soha nem kerül verziókezelésbe
- A runner Docker socket mountolással fut, tehát képes Docker image-eket buildelni
- Ha a PC újraindul, a `restart: unless-stopped` policy automatikusan újraindítja
  a runnert (ha Docker Desktop is elindul)
- Token lejárta esetén: `docker compose down`, új token a `.env`-be, `docker compose up -d`