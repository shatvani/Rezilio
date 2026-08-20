# ADR-001 – Modular Monolith, nem Microservice

**Dátum:** 2026-08-16  
**Státusz:** Elfogadva  
**Döntéshozó:** Projekt architekt

---

## Kontextus

A rendszernek több önálló üzleti területet (modulokat) kell kezelnie: kockázat-nyilvántartás, értékelés, kezelés, monitoring, incidensek, compliance, riportolás. Kérdés: ezeket önálló microservice-ekként, vagy egyetlen alkalmazásként valósítsuk meg?

## Döntés

**Modular Monolith** architektúrát választunk Vertical Slice Architecture-rel kombinálva.

## Indoklás

| Szempont | Modular Monolith | Microservice |
|---|---|---|
| **Fejlesztési sebesség** | ✅ Gyors, nincs hálózati overhead | ❌ Lassú, sok boilerplate |
| **Deployment komplexitás** | ✅ Egyetlen image | ❌ Orchestration, service mesh |
| **Modulok közötti tranzakciók** | ✅ Egy adatbázis, ACID | ❌ Distributed transaction (saga) |
| **Csapatméret** | ✅ Kis csapatnak ideális | ❌ Nagyobb csapat kell |
| **Skálázhatóság** | ⚠️ Vertikális (Azure Container Apps) | ✅ Horizontális per-service |
| **Jövőbeli microservice migráció** | ✅ Tiszta modulhatárok lehetővé teszik | – |

A modulok közötti kommunikáció Wolverine event-eken keresztül zajlik, ami biztosítja, hogy a határok tiszták maradnak – ez megkönnyíti egy esetleges jövőbeli microservice kiemelést.

## Következmények

- Modulok között nincs közvetlen projekt referencia, csak shared kernel és event-ek
- Egyszerűbb deployment, monitoring, debugging
- Egy adatbázis (per-modul séma szeparációval)
- Ha egy modul kiemelése szükségessé válik (pl. AIInsights nagy terhelés esetén), a tiszta határok miatt ez elvégezhető
