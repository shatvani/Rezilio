# ADR-011 – Infrastruktúra és deployment stratégia: Hetzner + Docker + Traefik + Let's Encrypt

**Dátum:** 2026-08-19  
**Státusz:** Elfogadva  
**Döntéshozó:** Projekt architekt

---

## Kontextus

A REZILIO SaaS platform production-ba kell kerüljön. A korábbi tervben Azure Container Apps szerepelt, de a projekt open-source / cost-first elvek mentén halad. Az első célpiac magyar KKV és középvállalat – számukra elegendő egy megbízható, skálázható, de olcsón üzemeltethető infrastruktúra. A jövőben AWS vagy Azure-ra való migráció lehetőségét meg kell őrizni.

---

## Döntés

**Hetzner VPS + Docker Compose (prod) + Traefik reverse proxy + Let's Encrypt SSL**

Az alkalmazás egyetlen Docker image-ben van csomagolva (ADR-001 alapján). A production deployment Hetzner Cloud VPS-en fut, Docker Compose-zal orkesztrálva. A Traefik reverse proxy kezeli az SSL-t és a routing-ot.

---

## Indoklás

| Szempont | Azure Container Apps | Hetzner + Docker + Traefik |
|---|---|---|
| **Havi költség (MVP fázis)** | ~80–150 EUR | ~10–25 EUR |
| **SSL kezelés** | Manuális / Azure cert | ✅ Automatikus (Traefik + ACME) |
| **Vendor lock-in** | ❌ Erős | ✅ Nincs |
| **Cloud-agnosztikus** | ❌ | ✅ Docker image bárhol futtatható |
| **Komplexitás** | Magas | Közepes |
| **Open-source** | ❌ | ✅ (Traefik, Docker) |
| **Skálázhatóság** | Automatikus | Hetzner LB + több node (manuális) |

---

## Infrastruktúra komponensek

### Hetzner Cloud VPS

- **MVP fázis:** 1 db CX31 (2 vCPU, 8 GB RAM, 80 GB SSD) – ~14 EUR/hó
- **Skálázás:** Hetzner Load Balancer + több VPS node (horizontális), vagy CX51 (vertikális)
- **Backup:** Hetzner automatikus snapshot (heti, 20% felár)
- **Hálózat:** Hetzner Private Network a DB és app servicek között

### Traefik (Reverse Proxy + SSL)

- Automatikus Let's Encrypt tanúsítvány-kezelés (ACME protokoll, HTTP-01 challenge)
- Automatikus megújítás (90 napos LE cert, Traefik 30 nappal előtte megújít)
- Docker label alapú konfiguráció – nincs külön config fájl módosítás service hozzáadáskor
- HTTPS redirect automatikusan minden HTTP kérésre
- Dashboard (lokálisan, nem publikusan)

```yaml
# docker-compose.yml – Traefik konfiguráció példa
services:
  traefik:
    image: traefik:v3
    command:
      - --providers.docker=true
      - --providers.docker.exposedbydefault=false
      - --entrypoints.web.address=:80
      - --entrypoints.websecure.address=:443
      - --certificatesresolvers.le.acme.httpchallenge=true
      - --certificatesresolvers.le.acme.httpchallenge.entrypoint=web
      - --certificatesresolvers.le.acme.email=${ACME_EMAIL}
      - --certificatesresolvers.le.acme.storage=/letsencrypt/acme.json
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - letsencrypt:/letsencrypt

  api:
    image: rezilio-api:latest
    labels:
      - traefik.enable=true
      - traefik.http.routers.api.rule=Host(`app.rezilio.hu`)
      - traefik.http.routers.api.entrypoints=websecure
      - traefik.http.routers.api.tls.certresolver=le
```

### Docker Compose (prod)

Services:
- `traefik` – reverse proxy + SSL
- `rezilio-api` – ASP.NET Core Minimal API
- `rezilio-frontend` – Next.js (statikus export vagy SSR)
- `postgres` – PostgreSQL 16
- `keycloak` – Keycloak identity provider (ADR-012)
- `grafana` + `prometheus` + `loki` – observability (ADR-013)

### Hetzner Firewall

- Port 80 (HTTP, Traefik redirect HTTPS-re)
- Port 443 (HTTPS)
- Port 22 (SSH, IP whitelist)
- Minden más zárva

---

## Deployment pipeline

```
GitHub Actions (CI)
  → Docker image build
  → Image push (GitHub Container Registry, ghcr.io)
  → SSH a Hetzner VPS-re
  → docker compose pull + docker compose up -d
```

Zero-downtime deploy: Traefik health check alapú container csere.

---

## Cloud migrációs path (jövő)

A döntés cloud-agnosztikus:
- Az alkalmazás egyetlen Docker image – ugyanaz fut Hetzner-en, AWS ECS-en és Azure Container Apps-en
- `docker-compose.prod.yml` → `docker-compose.aws.yml` / `docker-compose.azure.yml` csere
- Terraform szkriptek a jövőbeli cloud erőforrásokhoz (opcionális, fázisosan)
- Let's Encrypt cert → cloud provider managed cert (AWS ACM / Azure App Service cert) – Traefik config csere

---

## Konvenciók

- Minden secret `.env` fájlban van, `.gitignore`-ban – **soha nem kerül repóba**
- `.env.example` tartalmazza az összes változó nevét (értékek nélkül)
- Production `.env` fájl Hetzner VPS-en, kézzel karbantartva (jövőben: HashiCorp Vault)
- `docker-compose.dev.yml` – lokális dev (PostgreSQL + Keycloak dev realm)
- `docker-compose.prod.yml` – production (Traefik + teljes stack)

---

## Következmények

- Nincs Azure dependency az MVP-ben → alacsonyabb üzemeltetési költség
- SSL kezelés teljesen automatikus, nincs manuális cert megújítás
- Horizontális skálázás Hetzner Load Balancer + több node-dal lehetséges, de manuálisabb mint Container Apps autoscaling
- Jövőbeli cloud migráció: csak az orchestration layer cserélődik, az alkalmazás kód érintetlen
