# MysticRiver Copilot Instructions

## Build, test, and lint commands

Use these commands from the repository root.

```powershell
# Restore (matches CI focus on backend API)
dotnet restore src\MysticRiver.HttpApi\MysticRiver.HttpApi.csproj

# Build (analyzers and code-style checks run during build)
dotnet build src\MysticRiver.HttpApi\MysticRiver.HttpApi.csproj --configuration Release

# Run all test projects (mirrors reusable-dotnet.yaml behavior)
Get-ChildItem test -Recurse -Filter *.csproj | ForEach-Object { dotnet test $_.FullName }

# Run a specific test project
dotnet test test\MysticRiver.UnitTests\MysticRiver.UnitTests.csproj
dotnet test test\MysticRiver.IntegrationTests\MysticRiver.IntegrationTests.csproj

# Run a single test method
dotnet test test\MysticRiver.UnitTests\MysticRiver.UnitTests.csproj --filter "FullyQualifiedName~MysticRiver.UnitTests.BattleTests.ExecuteAction_AfterBattleIsOver_ThrowsInvalidOperationException"
```

There is no separate lint command in this repo. Linting is enforced via .NET analyzers and code-style rules during `dotnet build` (`Directory.Build.props` + `.editorconfig`, warnings treated as errors).

## High-level architecture

- **Solution shape:** `MysticRiver.slnx` separates backend (`Domain`, `Contracts`, `HttpApi`), frontend (`Client`), and tests.
- **Domain core (`src/MysticRiver.Domain`):** deterministic battle logic is kept in plain C# classes (`Creature`, `Battle`, `BattleResult`) with guard-clause validation and explicit state transitions.
- **HTTP API (`src/MysticRiver.HttpApi`):** ASP.NET Core entrypoint in `Program.cs` with controllers and OpenAPI/Scalar enabled in development. Containerized by `Dockerfile`.
- **Desktop client (`src/MysticRiver.Client`):** WPF app uses Generic Host + DI in `App.xaml.cs`; `UpdateService` checks GitHub Releases and drives in-app updates through AutoUpdater.NET.
- **Tests:** 
  - Unit tests target domain behavior (`test/MysticRiver.UnitTests`).
  - Integration tests (`test/MysticRiver.IntegrationTests`) use Testcontainers + PostgreSQL with shared lifecycle in `IntegrationTestBase`.
- **Delivery/deployment pipeline:** GitHub Actions build/test via `reusable-dotnet.yaml`, build and push API image in `deploy.yaml`, then apply Kubernetes manifests in `k8s\base` + environment-specific ingress (`k8s\staging`, `k8s\prod`). PostgreSQL deployment manifests live at repo root (`postgres-*.yaml`) and are deployed by `deploy-postgres.yaml`.
- **Branch/release flow:** merges into `main` are restricted to PRs from `staging` (`check-main-pr.yaml`), and version tags are derived from PR labels (`major`/`minor`/`patch`) in `deploy.yaml`.

## Key repository-specific conventions

- **Code-style enforcement is strict and build-blocking:**
  - `TreatWarningsAsErrors=true`, `EnableNETAnalyzers=true`, `EnforceCodeStyleInBuild=true`.
  - File-scoped namespaces required.
  - `var` is required in all contexts.
  - `this.` qualification is disallowed.
  - Private `readonly` fields use `_camelCase`; private mutable fields use `camelCase`.
  - Prefer sealed/internal where analyzers suggest it.
- **Dependency versions are centralized** in `Directory.Packages.props`; prefer adding/updating package versions there rather than per-project version pinning.
- **Test infrastructure pattern:** integration tests should inherit `IntegrationTestBase` and register containers through the `AddPostgreSqlContainer` extension so container lifecycle is tracked and disposed consistently.
- **Architecture intent from README:** client/backend communication is intended as a hybrid model (HTTP for commands, real-time updates via WebSockets/SignalR). Keep this direction consistent when adding features.

## Product vision anchor

When implementing features, keep this target experience in focus:

- Multiplayer, turn-based **pixel** combat with a **medieval-fantasy** identity.
- Battle UI should include a clear **future turn-order list** (top-left initiative style).
- Combat depth should support varied abilities: **damage, healing, buffs, debuffs, and status-like effects**.
- Backend remains **authoritative** and validates all critical multiplayer actions and outcomes.
- Client/server communication should continue the hybrid model: **REST controllers + SignalR**.
