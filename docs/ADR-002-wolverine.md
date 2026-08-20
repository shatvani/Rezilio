# ADR-002 – Wolverine mint Message Bus és Middleware framework

**Dátum:** 2026-08-16  
**Státusz:** Elfogadva  
**Döntéshozó:** Projekt architekt

---

## Kontextus

A VSA architektúrában szükség van egy Command/Query dispatch mechanizmusra, domain event kezelésre, ütemezett feladatokra és egy middleware pipeline-ra (pl. licensz-ellenőrzés). Alternatívák: MediatR, MassTransit, saját implementáció.

## Döntés

**Wolverine** framework használata, amely egyszerre kezeli:
- HTTP endpoint routing (WolverineHttp)
- Command és Query dispatch
- Domain Event publikálás és kezelés
- Middleware pipeline (Behavior-ok)
- Scheduled messages (ütemezett feladatok)

## Indoklás

| Szempont | Wolverine | MediatR + MassTransit |
|---|---|---|
| **Integráció** | ✅ Egységes, egy csomag | ❌ Két külön framework |
| **HTTP routing** | ✅ WolverineHttp, nincs Controller | ❌ MediatR-nek nincs |
| **Scheduled messages** | ✅ Beépített | ❌ Külön megoldás kell |
| **Middleware (Behavior)** | ✅ Elegáns pipeline | ⚠️ MediatR pipeline behavior |
| **Teljesítmény** | ✅ Source generator alapú | ⚠️ Reflection alapú (MediatR) |
| **Tanulási görbe** | ⚠️ Meredekebb | ✅ MediatR ismertebb |

## Wolverine használati minták ebben a projektben

```csharp
// Command dispatch – szinkron
await bus.InvokeAsync(new CreateRiskCommand(...));

// Event publish – aszinkron, loose coupling
await bus.PublishAsync(new RiskCreated(riskId, tenantId));

// Scheduled message – napi KRI ellenőrzés
await bus.ScheduleAsync(new CheckKRIThresholds(), TimeSpan.FromHours(24));

// Middleware pipeline – licensz ellenőrzés minden handler előtt
public class ModuleAccessBehavior<T> : IMessageMiddleware { ... }
```

## Következmények

- Nincs szükség külön Controller osztályokra – Minimal API + WolverineHttp kezeli
- A `ModuleAccessBehavior` egységesen védi az összes Handler-t
- Modulok közötti laza csatolás event publish/subscribe mintával
- Scheduled message-ek Wolverine-ban kezelve (nem Hangfire vagy Quartz)
