# Release Workflow (`release.yml`)

## Áttekintés

A `release.yml` egy GitHub Actions workflow, amely kizárólag git tag
push-ra indul el. Feladata egy verzió "hivatalos kiadása": ellenőrzi,
hogy minden konzisztens (verziószám, CHANGELOG), átcímkézi a Docker
image-t, és automatikusan létrehoz egy GitHub Release-t a CHANGELOG.md
tartalmával.

---

## Trigger (`on`)

```yaml
on:
  push:
    tags:
      - 'v*.*.*'
```

Csak `v` prefixű, szemantikus verziójú tag-ekre indul el:
- ✅ `v0.1.0`, `v1.0.0`, `v1.2.3`
- ✅ `v1.0.0-beta.1` (pre-release: a név tartalmaz `-t`)
- ❌ `0.1.0` (hiányzik a `v` prefix)
- ❌ `release-1.0` (nem SemVer formátum)

**Tag pusholása:**
```bash
git tag v0.1.0
git push origin v0.1.0
```

---

## Jogosultságok (`permissions`)

```yaml
permissions:
  contents: write   # GitHub Release létrehozáshoz
  packages: write   # Docker image re-taggeléshez (ghcr.io)
```

---

## Job-ok

### 1. `validate-tag` – Konzisztencia ellenőrzés

**Célja:** megakadályozza, hogy téves verziójú release kerüljön ki.
Két dolgot ellenőriz egymás után.

**Verziószám egyezés (`Directory.Build.props` vs. tag):**
```bash
PROPS_VERSION=$(grep -oP '(?<=<Version>)[^<]+' Directory.Build.props)
TAG_VERSION="0.1.0"   # a tag-ből kinyerve (v prefix levágva)

# Ha nem egyeznek → pipeline piros, release nem jön létre
```

Például: ha a tag `v0.2.0` de a `Directory.Build.props`-ban még `0.1.0`
van → a pipeline hibával leáll. Ez kikényszeríti, hogy release előtt
mindig frissítsd a verziószámot.

**CHANGELOG.md bejegyzés ellenőrzése:**
```bash
grep -q "\[0.1.0\]" CHANGELOG.md
```

Ha a CHANGELOG.md nem tartalmaz bejegyzést az adott verzióhoz
→ a pipeline piros. Ez kikényszeríti, hogy release előtt mindig
lezárd az `[Unreleased]` szekciót.

---

### 2. `docker-tag-release` – Docker image átcímkézése

**Fut:** `validate-tag` sikere után  
**Célja:** a `deploy.yml` által már feltöltött `main` tag-ű image-t
ellátja a verzió tag-gel is.

```bash
# Lehúzza a már meglévő main image-t
docker pull ghcr.io/shatvani/rezilio-api:main

# Átcímkézi verzióra és latest-re
docker tag ghcr.io/shatvani/rezilio-api:main \
           ghcr.io/shatvani/rezilio-api:v0.1.0
docker tag ghcr.io/shatvani/rezilio-api:main \
           ghcr.io/shatvani/rezilio-api:latest

# Feltölti az új tag-ekkel
docker push ghcr.io/shatvani/rezilio-api:v0.1.0
docker push ghcr.io/shatvani/rezilio-api:latest
```

Ezután a `ghcr.io/shatvani/rezilio-api` image háromféle tag-en elérhető:
- `main` – az utolsó main branch build
- `v0.1.0` – ez a konkrét verzió örökre
- `latest` – mindig az utolsó stabil release

**Frontend image re-tag:** jelenleg kommentezve, Story 0.8 után aktiválandó.

---

### 3. `github-release` – GitHub Release létrehozása

**Fut:** `docker-tag-release` sikere után  
**Célja:** automatikusan létrehoz egy GitHub Release-t a CHANGELOG.md
megfelelő szekciójának tartalmával.

**CHANGELOG.md szekció kinyerése:**
```bash
# Kikeresi a [0.1.0] és a következő verzió szekció közötti szöveget
awk "/^## \[0.1.0\]/{found=1; next} found && /^## \[/{exit} found{print}" CHANGELOG.md
```

Ez a szöveg lesz a GitHub Release leírása — nem kell kézzel másolni,
automatikusan a CHANGELOG-ból jön.

**Release létrehozása:**
```yaml
uses: softprops/action-gh-release@v1
with:
  name: "Release v0.1.0"
  body: <a CHANGELOG szekció tartalma>
  draft: false
  prerelease: ${{ contains(github.ref_name, '-') }}
```

A `prerelease` flag automatikus: ha a tag tartalmaz kötőjelet
(`v1.0.0-beta.1`), a release pre-release-ként jelenik meg GitHubon.

---

## Job-ok kapcsolata
```
git tag v0.1.0 && git push origin v0.1.0
│
└──► validate-tag
(verziószám + CHANGELOG ellenőrzés)
│
└──► docker-tag-release
(main → v0.1.0 + latest)
│
└──► github-release
(Release létrehozás CHANGELOG tartalommal)
```

---

## A release folyamata lépésről lépésre

Ez az a folyamat, amit minden release előtt végig kell csinálni:

### 1. Release branch létrehozása
```bash
git checkout development
git pull
git checkout -b release/0.1.0
```

### 2. Verziószám frissítése
`Directory.Build.props`:
```xml
<Version>0.1.0</Version>
```

### 3. CHANGELOG.md lezárása
Az `[Unreleased]` szekció tartalmát áthelyezed egy új verzió szekcióba,
és az `[Unreleased]` újra üres lesz:

```markdown
## [Unreleased]

## [0.1.0] - 2026-08-21

### Added
- Story 0.6: Licensing modul
- Story 0.7: Organization modul
- ...

[Unreleased]: https://github.com/shatvani/Rezilio/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/shatvani/Rezilio/releases/tag/v0.1.0
```

### 4. PR → main
```bash
git add Directory.Build.props CHANGELOG.md
git commit -m "chore: release v0.1.0"
git push -u origin release/0.1.0
# PR: release/0.1.0 → main, merge
```

### 5. Tag pusholása
```bash
git checkout main
git pull
git tag v0.1.0
git push origin v0.1.0
```

Ezután a `release.yml` automatikusan elindul és elvégzi a többit.

---

## Mit NE csinálj

- ❌ Ne pusholj tag-et, ha a `Directory.Build.props` verziója nem egyezik
  → a pipeline úgyis megállítja, de felesleges futtatás
- ❌ Ne hozz létre GitHub Release-t kézzel → a workflow csinálja
- ❌ Ne taggelj a `development` branch-en – mindig a `main`-en legyél
- ❌ Ne töröld a `main` tag-et az image-ről re-taggelés után
  → a `deploy.yml` is használja