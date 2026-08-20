# Rezilio

Vállalati kockázatelemző és -kezelő SaaS platform.

## Lokális fejlesztői környezet

### Előfeltételek

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Indítás

1. Másold le a környezeti változók sablonját:
```bash
   cp .env.example .env
```

2. Töltsd ki a `.env` fájlt (dev értékek elegendők).

3. Indítsd el a stack-et:
```bash
   docker compose up --build
```

4. Az API elérhető: `http://localhost:8081`
5. Health check: `http://localhost:8081/healthz`

### Hasznos parancsok

```bash
# Háttérben futtatás
docker compose up -d --build

# Logok követése
docker compose logs -f api

# Leállítás (adatok megmaradnak)
docker compose down

# Leállítás + adatok törlése
docker compose down -v
```

### Projekt struktúra

```
Rezilio.Api/          # ASP.NET Core Minimal API host
Rezilio.SharedKernel/ # DDD alap osztályok, közös value object-ek
docs/                 # Architektúrális döntések (ADR-ok), specifikáció
```
