# AntiPlagiarism — Micro‑Service Platform for Similarity Detection

![CI](https://img.shields.io/badge/build-passing-brightgreen) ![Docker](https://img.shields.io/badge/Docker-ready-blue)

A lightweight, horizontally‑scalable backend for storing documents and detecting near‑duplicate text fragments.  Written in **.NET 9** and shipped as a set of Docker containers.

---

## Table of Contents

1. [Key Features](#key-features)
2. [Architecture](#architecture)
3. [Prerequisites](#prerequisites)
4. [Quick Start](#quick-start)
5. [Project Structure](#project-structure)
6. [Configuration](#configuration)
7. [API Reference](#api-reference)
8. [Development Workflow](#development-workflow)
9. [Testing](#testing)
10. [CI & CD](#ci--cd)
11. [Troubleshooting & FAQ](#troubleshooting--faq)
12. [Contributing](#contributing)
13. [License](#license)

---

## Key Features

* **Document Storage** — fast upload & retrieval backed by PostgreSQL Large Objects.
* **Similarity Analysis** — tokenises text, generates min‑hash fingerprints and produces a detailed plagiarism report.
* **API Gateway** — single entry point with automatic Swagger docs & request tracing.
* **Stateless Services** — every component can be scaled out independently.
* **Zero‑setup local run** — one command (`docker compose up -d`) and you are ready to call the API.

---

## Architecture

```text
┌────────────┐  HTTP  ┌────────────────────┐     gRPC      ┌────────────────────────┐
│  Client /  │ ─────► │  API Gateway (80)  │ ────────────► │  File Storing Service  │
│  Frontend  │        │      ASP.NET       │              │  • Upload              │
└────────────┘        │  • Routing         │              │  • Download            │
                      │  • Auth placeholder│              └────────────────────────┘
                      │  • Swagger UI      │
                      │     ▲              │ gRPC         ┌────────────────────────┐
                      └─────┼──────────────┘─────────────►│  File Analysis Service │
                            │                               │  • Tokenisation        │
                            │   Health‑check /metrics       │  • Shingle + MinHash   │
                            ▼                               │  • Report              │
                     ┌────────────────────┐                └────────────────────────┘
                     │    PostgreSQL      │  DB schema
                     │  (5432, single)    │  mounted volume
                     └────────────────────┘
```

---

## Prerequisites

| Tool                   | Version  | Why                                                             |
| ---------------------- | -------- | --------------------------------------------------------------- |
| **Docker Engine**      | ≥ 24.0   | Container runtime                                               |
| **Docker Compose V2**  | ≥ 2.24   | Or `docker compose` plugin                                      |
| **Git**                | latest   | Clone repo (optional)                                           |
| **.NET SDK 9 Preview** | optional | Needed only for local debugging & unit tests outside containers |

> **Apple Silicon (M‑series)** — add `platform: linux/arm64` to every service in `docker‑compose.yml` or export `DOCKER_DEFAULT_PLATFORM=linux/arm64`.

---

## Quick Start

```bash
# 1. Clone & enter project
$ git clone https://github.com/<user>/AntiPlagiarism.git
$ cd AntiPlagiarism

# 2. Spin up stack (build if images absent)
$ docker compose up -d --build

# 3. Verify
$ open http://localhost:8080/swagger/index.html   # macOS shortcut
$ curl -i http://localhost:8080/health            # 200 OK ✓
```

To stop and wipe everything including volumes:

```bash
docker compose down -v
```

---

## Project Structure

```
AntiPlagiarism/
├── AntiPlagiarism.ApiGateway/         # Minimal Clean‑Architecture Web API
├── AntiPlagiarism.FileStoringService/ # Stores raw files & meta
├── AntiPlagiarism.FileAnalysisService/# Plagiarism detection engine
├── AntiPlagiarism.Common/             # Shared models & proto contracts
├── init-db/                           # SQL scripts for bootstrap
├── docker-compose.yml                 # Local multi‑container dev stack
└── README.md
```

---

## Configuration

All runtime configuration is handled through **environment variables** (see `docker-compose.yml`).

| Variable                        | Service      | Default                                  | Description                     |
| ------------------------------- | ------------ | ---------------------------------------- | ------------------------------- |
| `POSTGRES_USER`                 | postgres     | `postgres`                               | DB superuser                    |
| `POSTGRES_PASSWORD`             | postgres     | `postgres`                               | DB password                     |
| `ConnectionStrings__StorageDb`  | FileStoring  | `Host=postgres;Database=file_storage;…`  | Storage schema                  |
| `ConnectionStrings__AnalysisDb` | FileAnalysis | `Host=postgres;Database=file_analysis;…` | Analysis schema                 |
| `Gateway__PublicOrigin`         | ApiGateway   | `http://localhost:8080`                  | Absolute URL for Swagger & CORS |

For local debugging outside Docker create `appsettings.Development.json` files or use the **.NET User Secrets** feature.

---

## API Reference

Each micro‑service exposes its own Swagger/Proto, proxied to a single UI:

* **Swagger UI:** [`/swagger`](http://localhost:8080/swagger)
* **Health check:** `GET /health` → `200 OK` / `503 Service Unavailable`
* **Upload file:** `POST /storage/files` — multipart/form‑data (`file`) ⇒ `201 Created` + `fileId`
* **Run analysis:** `POST /analysis/jobs` — JSON `{ sourceFileId, referenceIds[] }` ⇒ jobId
* **Poll job result:** `GET /analysis/jobs/{id}` — `200 OK` + JSON report or `202 Accepted`

> Full request/response models are documented in Swagger and generated C# clients (`/src/Clients`).

---

## Development Workflow

1. **IDE** — JetBrains Rider / VS Code + C# extension.
2. `dotnet watch` — Hot reload each service:

   ```bash
   cd AntiPlagiarism.ApiGateway
   dotnet watch run
   ```
3. **Debug inside container** — use `docker compose -f docker-compose.debug.yml up` (exposes port 5678 for Rider).
4. **Database migrations** — EF Core Code‑First:

   ```bash
   dotnet ef migrations add Init --project AntiPlagiarism.FileStoringService
   dotnet ef database update
   ```

---

## Testing

* **Unit tests** — `dotnet test AntiPlagiarism.sln` (xUnit + FluentAssertions)
* **Integration tests** — spins up ephemeral TestContainers (PostgreSQL) and calls gRPC/HTTP pipelines.
* **Static analysis** — Rider Code Inspection + Roslyn Analyzers, fails CI on severity ≥ `Warning`.

---

## CI & CD

* **GitHub Actions** (see `.github/workflows/ci.yml`):

  * Restore → Build → Test → Docker Build → Push to GH CR.
* **Docker Hub / GHCR** images tagged `latest` and `vX.Y.Z`.
* **Deploy** — Helm chart in `/deploy/helm` (Kubernetes ≥ 1.27).

---

## Troubleshooting & FAQ

| Problem                    | Diagnosis                      | Fix                                                                     |
| -------------------------- | ------------------------------ | ----------------------------------------------------------------------- |
| `port 8080 already in use` | Another app occupies 8080      | Change left side of `ports:` mapping and re‑run `docker compose up -d`. |
| `pg_isready: no response`  | DB not ready, services waiting | Wait 10 s or inspect DB logs with `docker compose logs postgres`.       |
| Apple Silicon build fails  | amd64 images by default        | Add `platform: linux/arm64`.                                            |
| Very slow first build      | .NET 9 images \~400 MB         | They are cached afterwards; use CI to pre‑build.                        |

---

## Contributing

Pull requests are welcome!  Please open an issue first to discuss what you would like to change.

1. Fork the repository
2. Create your feature branch (`git checkout -b feat/my-awesome-feature`)
3. Commit your changes (`git commit -m 'feat: add amazing feature'`)
4. Push to the branch (`git push origin feat/my-awesome-feature`)
5. Open a Pull Request

All commits must pass `dotnet test` and `dotnet format`.

---

## License

Distributed under the **MIT License**.  See `LICENSE` for more information.
