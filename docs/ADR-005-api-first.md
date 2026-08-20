# ADR-005 – API-first fejlesztés, UI 1-2 sprinttel lemarad

**Dátum:** 2026-08-16  
**Státusz:** Elfogadva  
**Döntéshozó:** Projekt architekt

---

## Kontextus

A fejlesztés során dönteni kell, hogy az API és a Next.js frontend párhuzamosan, vagy egymás után készüljön. Három lehetséges megközelítés:
1. Teljesen szekvenciális: előbb az egész API, aztán az egész UI
2. Teljesen párhuzamos: minden sprint végén API + UI egyszerre
3. API-first: API 1-2 sprinttel előrébb jár, UI követi

## Döntés

**API-first fejlesztés:** az API mindig 1-2 sprinttel megelőzi a UI-t, de minden fázis végén van integrált, demózható állapot.

## Indoklás

| Szempont | Szekvenciális | Párhuzamos | API-first (választott) |
|---|---|---|---|
| **Korai visszajelzés** | ❌ Sokáig nincs UI | ⚠️ Lehet blokkoló | ✅ Sprint végén demó |
| **UI fejlesztő blokkolása** | – | ❌ Várhat az API-ra | ✅ Mindig van API |
| **Integráció kockázata** | ❌ Magas, late discovery | ✅ Folyamatos | ✅ Folyamatos |
| **Swagger tesztelhetőség** | ✅ | ✅ | ✅ API önállóan tesztelhető |
| **Fókusz** | ✅ Egyértelmű | ❌ Megosztott figyelem | ✅ Strukturált |

## Sprint struktúra

```
Sprint N:   [API: Feature X]  [API: Feature Y]
Sprint N+1: [UI: Feature X]   [API: Feature Z]
Sprint N+2: [UI: Feature Y]   [API: Feature W]
```

Minden **fázis végén** (2-3 sprint után) van egy teljes end-to-end integrált állapot, ami demózható és pilot ügyfeleknek adható.

## Következmények

- Az API-t Swagger UI-on keresztül önállóan lehet tesztelni és demonstrálni
- A frontend fejlesztő sosem vár teljesen – mindig van kész API endpoint
- A mock adatok minimalizálhatók, mert az API 1 sprinttel korábban kész
- Kontrakt (OpenAPI spec) az API sprintben keletkezik, UI sprint ezt követi
- Fázis végén kötelező integrációs teszt (E2E) az addig elkészült funkciókra
