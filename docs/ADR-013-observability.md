# ADR-013 – Observability stack: OpenTelemetry + Grafana + Prometheus + Loki

**Dátum:** 2026-08-19  
**Státusz:** Elfogadva  
**Felváltja:** Azure Application Insights (korábbi terv)  
**Döntéshozó:** Projekt architekt

---

## Kontextus

A korábbi SPEC.md Azure Application Insights-ot jelölt meg observability megoldásként. Ez Azure-specifikus, fizetős és vendor lock-in-t jelent. A projekt open-source / cost-first iránya alapján self-hosted, nyílt forrású alternatívát kell választani.

---

## Döntés

**OpenTelemetry (instrumentation) + Prometheus (metrikák) + Loki (logok) + Grafana (dashboardok)**

Minden komponens open-source, self-hosted, Hetzner VPS-en fut Docker Compose-ban.

---

## Indoklás

| Szempont | Azure Application Insights | OTel + Grafana stack |
|---|---|---|
| **Havi költség** | ~20–80 EUR (forgalomtól függ) | ✅ ~0 EUR (self-hosted) |
| **Vendor lock-in** | ❌ Azure | ✅ Nincs |
| **Open-source** | ❌ | ✅ |
| **Instrumentáció** | Azure SDK | ✅ OpenTelemetry (CNCF standard) |
| **Metrikák** | ✅ | ✅ Prometheus |
| **Logok** | ✅ | ✅ Loki |
| **Alerting** | Azure Monitor | ✅ Grafana Alerting |
| **Cloud migráció** | Csak Azure | ✅ Bárhol fut |

---

## Stack komponensek

### OpenTelemetry (instrumentáció)

Az ASP.NET Core alkalmazás OpenTelemetry SDK-val van instrumentálva – ez a CNCF standard, ami egyszerre exportál metrikákat, trace-eket és logokat. Az export target konfigurálható: dev-ben konzol, prod-ban az OTel Collector.

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddWolverineInstrumentation()
        .AddOtlpExporter())          // → OTel Collector
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter())
    .WithLogging(logging => logging
        .AddOtlpExporter());
```

### OpenTelemetry Collector

Köztes réteg az alkalmazás és a backend-ek között. Fogadja az OTel protokollon érkező adatokat, és továbbítja:
- Trace-ek → Tempo (opcionális, fázisosan) vagy egyből Grafana Cloud-ba (ha kell)
- Metrikák → Prometheus
- Logok → Loki

### Prometheus (metrikák)

- Scrape-eli az OTel Collector Prometheus endpointját
- Tárolja a time-series metrikákat
- Adatforrásként Grafana-ba van kötve

Kulcs metrikák:
- HTTP kérések száma, latencia (p50, p95, p99)
- EF Core query idők
- Wolverine üzenetfeldolgozási idők
- JVM / .NET runtime metrikák (GC, thread pool)
- Keycloak token validációk

### Loki (logok)

- Strukturált log aggregáció
- Promtail agent gyűjti a Docker container logokat
- Grafana Explore-ban kereshető

```yaml
# docker-compose.prod.yml részlet
  loki:
    image: grafana/loki:3
    ports:
      - "3100:3100"

  promtail:
    image: grafana/promtail:3
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - /var/lib/docker/containers:/var/lib/docker/containers:ro
```

### Grafana (dashboardok + alerting)

- Adatforrások: Prometheus + Loki
- Előre konfigurált dashboardok:
  - API health dashboard (kérések, hibák, latencia)
  - DB performance (slow query-k, connection pool)
  - Business metrikák (aktív tenantek, kockázat bejegyzések)
  - Infrastruktúra (CPU, RAM, disk – Hetzner node exporter)
- Alerting: e-mail értesítés kritikus hibáknál (Grafana Alerting, SMTP-n keresztül)

---

## Docker Compose konfiguráció

```yaml
# docker-compose.prod.yml – observability stack
services:
  otel-collector:
    image: otel/opentelemetry-collector-contrib:latest
    volumes:
      - ./otel/otel-collector-config.yaml:/etc/otel/config.yaml
    command: ["--config=/etc/otel/config.yaml"]

  prometheus:
    image: prom/prometheus:latest
    volumes:
      - ./prometheus/prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus_data:/prometheus

  loki:
    image: grafana/loki:3
    volumes:
      - loki_data:/loki

  promtail:
    image: grafana/promtail:3
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - /var/lib/docker/containers:/var/lib/docker/containers:ro
      - ./promtail/config.yaml:/etc/promtail/config.yaml

  grafana:
    image: grafana/grafana:latest
    environment:
      GF_SECURITY_ADMIN_PASSWORD: ${GRAFANA_ADMIN_PASSWORD}
      GF_SERVER_ROOT_URL: https://monitoring.rezilio.hu
    volumes:
      - grafana_data:/var/lib/grafana
      - ./grafana/provisioning:/etc/grafana/provisioning
    labels:
      - traefik.enable=true
      - traefik.http.routers.grafana.rule=Host(`monitoring.rezilio.hu`)
      - traefik.http.routers.grafana.entrypoints=websecure
      - traefik.http.routers.grafana.tls.certresolver=le
```

---

## Dev vs. prod

**Dev:** Csak konzol logok, nincs teljes stack – az OTel exportáló console exporter-re vált:

```csharp
// Csak dev-ben:
.AddConsoleExporter()
```

**Prod:** Teljes OTel Collector + Prometheus + Loki + Grafana stack.

---

## Következmények

- Azure Application Insights NuGet csomag és SDK nem kerül a projektbe
- OpenTelemetry SDK vendor-agnosztikus – ha a jövőben cloud-ra kerül a prod, csak az exporter target változik (pl. AWS X-Ray, Azure Monitor OTel exporter)
- Az observability stack is Hetzner VPS-en fut – erőforrás-igény ~1 GB RAM a teljes stackhez
- Grafana dashboard konfigurációk a repóban (`/grafana/provisioning/`) – Infrastructure as Code
- Alerting: kezdetben csak e-mail (Grafana SMTP), később bővíthető (Slack webhook, PagerDuty stb.)
