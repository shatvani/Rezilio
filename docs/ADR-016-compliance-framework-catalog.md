# ADR-016 – Compliance Framework Catalog: lazán csatolt, verziózott megfelelőségi keretrendszerek

**Dátum:** 2026-09-02
**Státusz:** Elfogadva
**Döntéshozó:** Projekt architekt

---

## Kontextus

A RiskRegister modul tervezése (Story 1.1) közben felmerült, hogy az első
konkrét, kiemelt kockázati terület a NIS2 (2024. évi LXIX. törvény
Magyarország kiberbiztonságáról) megfelelőség lesz, amit várhatóan a DORA
és később más keretrendszerek (ISO 27001, egyedi ügyfél-specifikus
taxonómiák) is követnek majd.

Két, egymással versengő igény van:

1. A `Risk` aggregate és a köré épülő motor (Assessment, Treatment,
   Monitoring) **maradjon framework-agnosztikus** — ne kelljen tudnia,
   hogy éppen NIS2-es vagy DORA-s kockázatról van szó.
2. Ugyanakkor a platformnak **konkrét, jogszabályhoz/szabványhoz köthető
   kategorizálást és riportozást** kell tudnia nyújtani (pl. "mutasd meg a
   NIS2 10 minimumkövetelmény-kategóriájának lefedettségét"), és ezt
   auditkor **visszamenőlegesen, az akkor hatályos szöveg szerint** kell
   tudni bemutatni.

Emellett üzleti döntés is született: a keretrendszerek aktiválása
**licenszkérdés** — a NIS2 vagy DORA modul-szerű, önállóan értékesíthető
tétel, nem egy ingyenes beállítás.

## Döntés

Három, egymástól elválasztott réteget vezetünk be.

### 1. Framework-agnosztikus motor (változatlan)

A `Risk`, `Assessment`, `Treatment`, `Monitoring` domain modell semmit nem
tud a konkrét keretrendszerekről. Egy `Risk` szabadon állhat, semmilyen
framework-hez nem kötve (0..N kapcsolat, sosem kötelező 1).

### 2. Verziózott framework-katalógus (új, a Compliance modulban)

```
ComplianceFramework  (pl. "NIS2", "DORA")
      └── FrameworkVersion  (pl. "2024. évi LXIX. törvény", EffectiveFrom, EffectiveTo?)
              └── FrameworkControl  (pl. NIS2 10 minimumkövetelmény-kategóriája)
```

A `FrameworkVersion` és az alá tartozó `FrameworkControl`-ok **append-only**
jellegűek — soha nem módosulnak utólag. Ha a jogszabály/szabvány változik,
egy új `FrameworkVersion` jön létre (`EffectiveFrom` a hatálybalépés napja),
a régi lezárásra kerül (`EffectiveTo` kitöltve), de megmarad.

Egy `Risk` és egy `FrameworkControl` közötti kapcsolat (`RiskFrameworkMapping`,
many-to-many join tábla) mindig egy **konkrét `FrameworkControl` példányra**
mutat, ami egy konkrét, lezárt `FrameworkVersion`-höz tartozik. Így egy
auditnál pontosan visszanézhető, hogy a hozzárendelés idején mi volt a
hatályos szöveg — a katalógus utólagos bővülése/változása a régi
hozzárendeléseket nem írja felül.

### 3. Licensing-szintű framework-aktiválás (a TenantLicense bővítése)

A `TenantLicense` aggregate a meglévő `ModuleAccesses` mintáját megismétli
egy második dimenzióként:

```csharp
public sealed record FrameworkAccess(
    ComplianceFrameworkType Framework,
    bool IsActive,
    DateTimeOffset? TrialEndsAt)
{
    public bool IsAccessible => IsActive && (TrialEndsAt is null || TrialEndsAt > DateTimeOffset.UtcNow);
}

public enum ComplianceFrameworkType { Nis2, Dora, Iso27001 /* bővíthető */ }
```

A `TenantLicense`-en `ActivateFramework`/`DeactivateFramework`/
`StartFrameworkTrial` metódusok jönnek létre, tükrözve a meglévő
`ActivateModule`/`DeactivateModule`/`StartTrial` mintát. Egy tenant tehát
tetszőleges kombinációban aktiválhatja/deaktiválhatja a NIS2-t és a DORA-t,
egymástól függetlenül, licenszszinten.

## Indoklás

| Szempont | Ez a megoldás (3 réteg, lazán csatolva) | Alternatíva: `RiskDomain` = hardcoded NIS2 enum |
|---|---|---|
| **Több framework egyszerre** | ✅ Egy `Risk` egyszerre NIS2- és DORA-releváns lehet, duplikáció nélkül | ❌ Egy kockázat egy kategóriába tartozna, nem fejezné ki a valódi átfedést |
| **Szabadon álló kockázatok** | ✅ 0..N kapcsolat, nem kötelező | ❌ Minden kockázatnak muszáj lenne kategóriát választania |
| **Auditálhatóság** | ✅ Verzió-pontos visszanézhetőség, jogszabályváltozás esetén is | ❌ Ha a taxonómia változik, a régi hozzárendelések értelmüket vesztik |
| **Licenszelhetőség** | ✅ Framework-önként külön aktiválható/értékesíthető | ⚠️ Csak modul-szinten (Compliance be/ki), nem framework-szinten |
| **Motor függetlensége** | ✅ RiskRegister/Assessment/Treatment nem tud a NIS2-ről | ❌ A `RiskDomain` enum a motorba égetné a framework-fogalmat |
| **Komplexitás** | ⚠️ 3 szintű katalógus-hierarchia + join tábla | ✅ Egyetlen enum, egyszerűbb kezdetben |

A komplexitás-többletet vállaljuk, mert a verziózás és a több-framework
támogatás nem utólag hozzáadható tulajdonság — ha most egy lapos enumot
építünk be a `Risk` közelébe, egy jövőbeli audit-igény vagy egy második
framework bevezetése komoly migrációt igényelne.

## Következmények

- A `Licensing` modul `TenantLicense` aggregate-je egy második owned
  collection-t kap (`FrameworkAccesses`), a `ModuleAccesses` mellett.
- Egy új, önálló **Compliance modul** felelős a `ComplianceFramework` /
  `FrameworkVersion` / `FrameworkControl` katalógusért — ez a `ModuleType`
  enumban már szereplő `Compliance` modul tartalmát pontosítja
  (`story/3.11-compliance-api`).
- A `RiskRegister` modul kap egy `RiskFrameworkMapping` join táblát, ami a
  Compliance modul `FrameworkControl`-jaira hivatkozik (modulok közötti
  hivatkozás azonosítóval, nem közvetlen entitás-referenciával — ugyanaz a
  minta, mint a `Risk.OwnerId` → `KeyPerson.Id` hivatkozásnál).
- A NIS2 lesz az első ténylegesen feltöltött `FrameworkVersion` tartalom
  (2024. évi LXIX. törvény 10 minimumkövetelmény-kategóriája), de a
  katalógus-struktúra bármely más keretrendszerre (DORA, ISO 27001,
  egyedi ügyfél-taxonómia) ugyanúgy alkalmazható, kódmódosítás nélkül —
  csak új `ComplianceFramework`/`FrameworkVersion`/`FrameworkControl`
  adatsorok szükségesek.
- A `Risk` aggregate-en (Story 1.1) nem kell semmilyen framework-specifikus
  mezőt felvenni — a kapcsolat kizárólag a `RiskFrameworkMapping` táblán
  keresztül létezik, ezért a Story 1.1 scope-ja karcsú maradhat.
- Ez a döntés **nem** tartalmazza a katalógus tényleges implementációját —
  az a `story/3.11-compliance-api` (vagy egy korábbra hozott, dedikált
  story) feladata lesz, ez az ADR csak az architektúrát rögzíti előre.
