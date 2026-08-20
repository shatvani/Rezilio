# ADR-003 – Egy Docker image, feature flag alapú modul-aktiváció

**Dátum:** 2026-08-16  
**Státusz:** Elfogadva  
**Döntéshozó:** Projekt architekt

---

## Kontextus

A platform több modul kombinációját kínálja különböző áron (Basic, Professional, Enterprise). Kérdés: a nem megvásárolt modulokat fizikailag ne telepítsük, vagy minden modul benne legyen a szoftverben és licensz alapján aktiválódjanak?

## Döntés

**Egyetlen Docker image** tartalmaz minden modult. A modulok **per-tenant licensz és feature flag** alapján aktiválódnak, nem telepítéssel.

## Indoklás

| Szempont | Egy image + feature flag | Külön telepítés |
|---|---|---|
| **Deployment** | ✅ Egyszerű, egy image | ❌ Sok variáció, nehéz karbantartani |
| **Frissítés** | ✅ Egy helyen frissítesz minden tenantot | ❌ Modulonként, tenantonként |
| **Upsell** | ✅ "Próbáld ki" gomb azonnal elérhető | ❌ Új telepítés kell |
| **QA** | ✅ Egy konfigurációt tesztelsz | ❌ Kombinációk robbanása |
| **Iparági precedens** | ✅ Microsoft 365, Jira, SAP mind így működik | ❌ Ritka megközelítés |
| **Biztonság** | ⚠️ Gondosan kell tiltani (két réteg) | ✅ Fizikailag nem érhető el |

A biztonsági kockázat két rétegen kezelve:
1. **API réteg:** `RequireModule()` extension minden endpoint-on
2. **Wolverine middleware:** `ModuleAccessBehavior` minden Handler-en

## Modul lifecycle

```
Trial indítás → 14 nap aktív → Lejárat → ReadOnly mód → Upgrade → Teljes hozzáférés
                                                        ↓
                                              Adatok megmaradnak
```

## Következmények

- A Licensing modul mindig aktív, és minden kérésnél ellenőriz
- Deaktivált modulra érkező API kérés: `403 Forbidden`
- UI-ban a deaktivált modulok "Upgrade szükséges" badge-et kapnak
- Trial mechanizmus beépített: egy gombnyomással kipróbálható bármely prémium modul
- Könnyű audit: minden modul-aktiváció/deaktiváció logolva van
