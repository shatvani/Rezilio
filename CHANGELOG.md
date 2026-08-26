# Changelog

All notable changes to this project will be documented in this file.  
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)  
Versioning: [Semantic Versioning](https://semver.org/spec/v2.0.0.html)

## [Unreleased]

### Added
- Story ORG.1: Organization modul bővítése – `ImportJob` aggregate, `EntityType` és `ImportJobStatus` enum-ok, `ImportRowResult` value object, domain event-ek (`ImportJobCreated`, `ImportJobCompleted`, `ImportJobFailed`), EF Core mapping, `AddImportJob` migráció
- Story ORG.2: Excel import infrastruktúra – `IExcelTemplateGenerator`, `IExcelImportParser`, `IImportColumnDefinitionProvider` interfészek, ClosedXML alapú implementáció, sor-szintű validációs pipeline, Wolverine HTTP endpointok (template letöltés, feltöltés+validálás, megerősítés, státusz, eredmények)
- Story ORG.3: `OrganizationalUnit` aggregate (`Name`, `Code`, `ParentId?`, `Description?`), CRUD endpointok, Excel template + import pipeline, `ImportJob.FileContent byte[]` tárolás a confirm fázishoz, `AddOrganizationalUnit` és `AddImportJobFileContent` migrációk
- Story ORG.5: `Customer` aggregate (`Name`, `Code`, `Industry?`, `Country?`, `ContactEmail?`, `ContactPhone?`, `Description?`), CRUD endpointok, Excel template + import pipeline, `AddCustomer` migráció
- Story ORG.6: `Supplier` aggregate (`Name`, `Code`, `Industry?`, `Country?`, `ContactEmail?`, `ContactPhone?`, `Description?`), CRUD endpointok, Excel template + import pipeline, `AddSupplier` migráció
- Chore: `Microsoft.EntityFrameworkCore.Relational` explicit pinelve 10.0.11-re a `Rezilio.Tests`-ben az EF verziókonfliktus megszüntetéséhez
- Story ORG.7: KeyPerson aggregate – Name, Title?, Department?, OrgUnitId?, Email?, Phone?, BackupPersonName?, Description? mezők; CRUD endpointok; Excel template + import pipeline (OrgUnitCode → OrgUnitId feloldással); `AddKeyPerson` migráció

## [0.1.0] - 2026-08-21

### Added
- Story 0.6: Licensing modul – domain, application, infrastructure rétegek, Wolverine HTTP endpoints
- Story 0.7: Organization modul – TenantSettings aggregát, Money/CurrencyCode/LanguageCode value object-ek, lokalizáció
- Story 0.9: Lokális self-hosted GitHub Actions runner Docker Desktop alapon
- Story 0.10: CI/CD pipeline – PR checks, Docker build/push, release workflow-ok
- ADR-001: Lamar IoC container (Wolverine + EF Core kompatibilitás)

[Unreleased]: https://github.com/shatvani/Rezilio/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/shatvani/Rezilio/releases/tag/v0.1.0
