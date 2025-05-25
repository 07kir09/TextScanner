# AntiPlagiarism — микросервисная платформа для обнаружения сходства текстов

![CI](https://img.shields.io/badge/build-passing-brightgreen) ![Docker](https://img.shields.io/badge/Docker-ready-blue)

Лёгкая горизонтально масштабируемая backend‑система для хранения документов и поиска заимствований. Написана на **.NET 9** и поставляется в виде набора Docker‑контейнеров.

---

## Оглавление

1. [Ключевые возможности](#ключевые-возможности)
2. [Архитектура](#архитектура)
3. [Необходимое ПО](#необходимое-по)
4. [Быстрый старт](#быстрый-старт)
5. [Структура проекта](#структура-проекта)
6. [Конфигурация](#конфигурация)
7. [API](#api)
8. [Процесс разработки](#процесс-разработки)
9. [Тестирование](#тестирование)
10. [CI и CD](#ci-и-cd)
11. [FAQ и устранение неполадок](#faq-и-устранение-неполадок)
12. [Как внести вклад](#как-внести-вклад)
13. [Лицензия](#лицензия)

---

## Ключевые возможности

* **Хранение документов** — быстрая загрузка и получение файлов на базе PostgreSQL Large Objects.
* **Анализ сходства** — токенизация текста, MinHash‑отпечатки и подробный отчёт о плагиате.
* **API‑шлюз** — единая точка входа с Swagger‑доками и трассировкой запросов.
* **Бессостояние сервисов** — каждый компонент масштабируется независимо.
* **Zero‑setup локальный запуск** — одна команда `docker compose up -d` и сервис готов.

---

## Архитектура

```text
┌────────────┐  HTTP  ┌────────────────────┐     gRPC      ┌────────────────────────┐
│  Клиент /  │ ─────► │  API‑шлюз (80)     │ ────────────► │  Сервис хранения файлов │
│  Frontend  │        │  ASP.NET           │              │  • Загрузка             │
└────────────┘        │  • Маршрутизация   │              │  • Выдача               │
                      │  • Swagger UI      │              └────────────────────────┘
                      │     ▲              │ gRPC         ┌────────────────────────┐
                      └─────┼──────────────┘─────────────►│  Сервис анализа файлов  │
                            │                               │  • Токенизация          │
                            │   /health, /metrics           │  • MinHash              │
                            ▼                               │  • Отчёт                │
                     ┌────────────────────┐                └────────────────────────┘
                     │    PostgreSQL      │  Две схемы
                     │   (5432, один)     │  томы Docker
                     └────────────────────┘
```

---

## Необходимое ПО

| Инструмент             | Версия    | Зачем                                                       |
| ---------------------- | --------- | ----------------------------------------------------------- |
| **Docker Engine**      | ≥ 24.0    | Запуск контейнеров                                          |
| **Docker Compose V2**  | ≥ 2.24    | Плагин `docker compose`                                     |
| **Git**                | последняя | Клонирование репо (опц.)                                    |
| **.NET SDK 9 Preview** | опц.      | Нужен только для локальной отладки и юнит‑тестов вне Docker |

> **Apple Silicon (M‑серия).** Добавьте `platform: linux/arm64` в каждый сервис `docker-compose.yml` или экспортируйте `DOCKER_DEFAULT_PLATFORM=linux/arm64`.

---

## Быстрый старт

```bash
# 1. Клонируем репозиторий
$ git clone https://github.com/<user>/AntiPlagiarism.git
$ cd AntiPlagiarism

# 2. Поднимаем стек (при необходимости строим образы)
$ docker compose up -d --build

# 3. Проверяем
$ open http://localhost:8080/swagger/index.html   # macOS
$ curl -i http://localhost:8080/health            # 200 OK ✓
```

Остановить и удалить тома:

```bash
docker compose down -v
```

---

## Структура проекта

```
AntiPlagiarism/
├── AntiPlagiarism.ApiGateway/         # Минимальный Web API
├── AntiPlagiarism.FileStoringService/ # Хранение файлов + метаданных
├── AntiPlagiarism.FileAnalysisService/# Движок поиска плагиата
├── AntiPlagiarism.Common/             # Общие модели и gRPC‑контракты
├── init-db/                           # SQL для инициализации
├── docker-compose.yml                 # Локальный стек
└── README.md
```

---

## Конфигурация

Все параметры передаются **переменными среды** (см. `docker-compose.yml`).

| Переменная                      | Сервис       | Значение по умолчанию                    | Описание                     |
| ------------------------------- | ------------ | ---------------------------------------- | ---------------------------- |
| `POSTGRES_USER`                 | postgres     | `postgres`                               | Суперпользователь БД         |
| `POSTGRES_PASSWORD`             | postgres     | `postgres`                               | Пароль БД                    |
| `ConnectionStrings__StorageDb`  | FileStoring  | `Host=postgres;Database=file_storage;…`  | Схема хранения               |
| `ConnectionStrings__AnalysisDb` | FileAnalysis | `Host=postgres;Database=file_analysis;…` | Схема анализа                |
| `Gateway__PublicOrigin`         | ApiGateway   | `http://localhost:8080`                  | Базовый URL для Swagger/CORS |

Для локальной отладки вне Docker используйте `appsettings.Development.json` или **User Secrets**.

---

## API

* **Swagger UI:** [`/swagger`](http://localhost:8080/swagger)
* **Health‑check:** `GET /health` → `200 OK` / `503 Service Unavailable`
* **Загрузка файла:** `POST /storage/files` — `multipart/form-data` (`file`) ⇒ `201 Created` + `fileId`
* **Запуск анализа:** `POST /analysis/jobs` — JSON `{ sourceFileId, referenceIds[] }` ⇒ `jobId`
* **Статус задачи:** `GET /analysis/jobs/{id}` — `200 OK` + отчёт или `202 Accepted`

Полные модели запросов/ответов смотрите в Swagger или в сгенерированных C# клиентах (`/src/Clients`).

---

## Процесс разработки

1. **IDE** — JetBrains Rider / VS Code с C#‑расширением.
2. Горячая перезагрузка:

   ```bash
   cd AntiPlagiarism.ApiGateway
   dotnet watch run
   ```
3. **Отладка в контейнере** — `docker compose -f docker-compose.debug.yml up` (порт 5678 для Rider).
4. **Миграции БД** — EF Core Code‑First:

   ```bash
   dotnet ef migrations add Init --project AntiPlagiarism.FileStoringService
   dotnet ef database update
   ```

---

## Тестирование

* **Юнит‑тесты** — `dotnet test AntiPlagiarism.sln` (xUnit + FluentAssertions)
* **Интеграционные** — поднимают TestContainers (PostgreSQL) и вызывают gRPC/HTTP пайплайны.
* **Статический анализ** — Roslyn Analyzers, сборка падает при уровне ≥ `Warning`.

---

## CI и CD

* **GitHub Actions** (см. `.github/workflows/ci.yml`):

  * Restore → Build → Test → Docker Build → Push в GH CR.
* **Образы** — в Docker Hub / GHCR c тегами `latest` и `vX.Y.Z`.
* **Деплой** — Helm‑чарт в `/deploy/helm` (Kubernetes ≥ 1.27).

---

## FAQ и устранение неполадок

| Симптом                        | Диагностика                 | Решение                                                          |
| ------------------------------ | --------------------------- | ---------------------------------------------------------------- |
| `port 8080 already in use`     | Порт занят другим процессом | Измените левую часть `ports:` и перезапустите стек.              |
| `pg_isready: no response`      | БД ещё поднимается          | Подождите 10 с или смотрите логи `docker compose logs postgres`. |
| Ошибка сборки на Apple Silicon | образы amd64 по умолчанию   | Добавьте `platform: linux/arm64`.                                |
| Долгая первая сборка           | .NET 9 \~400 МБ             | После кэширования быстрее; можно pre‑build в CI.                 |

---

## Как внести вклад

1. Сделайте форк репозитория.
2. Создайте ветку: `git checkout -b feat/my-feature`.
3. Закоммитьте изменения: `git commit -m 'feat: добавил…'`.
4. Запушьте ветку: `git push origin feat/my-feature`.
5. Откройте Pull Request.

Все коммиты должны проходить `dotnet test` и `dotnet format`.

---

## Лицензия

Распространяется под лицензией **MIT**. См. файл `LICENSE`.
