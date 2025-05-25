# AntiPlagiarism — Платформа поиска текстовых заимствований

Простая микросервисная система: загружаете документ — получаете статистику, отчёт о схожести и облако слов.

---

## Что умеет система

* Загружать файлы **.txt / .docx / .pdf** (до 20 MB)
* Считать **слова, символы, абзацы**
* Находить **дубликаты** по SHA‑256
* Сравнивать **похожесть документов** (MinHash + коэффициент Жаккара)
* Генерировать **облака слов**
* Предоставлять удобный **Swagger‑интерфейс** и REST API

---

## Быстрый старт

1. **Установите Docker Engine и Docker Compose V2**

   ```bash
   # macOS (Homebrew)
   brew install --cask docker
   # Ubuntu
   sudo apt install docker.io docker-compose-plugin
   ```
2. **Запустите систему**

   ```bash
   git clone https://github.com/07kir09/TextScanner
   docker compose up -d --build   # первый запуск c билдом
   ```
3. **Откройте браузер**
   [http://localhost:8080](http://localhost:8080)

---

## Как пользоваться

### Через веб‑интерфейс

1. Перейдите на `http://localhost:8080`
2. Перетащите файл в область загрузки
3. Изучите статистику и отчёты
4. Сравните документ с уже загруженными

### Через API

```bash
# Загрузить файл
curl -X POST -F "file=@test.txt" http://localhost:8080/api/files

# Получить список файлов
curl http://localhost:8080/api/files

# Статистика файла (замените {id})
curl http://localhost:8080/api/files/{id}/stats

# Сравнить два файла
curl -X POST -H "Content-Type: application/json" \
  -d '{"file_id":"id1","other_file_id":"id2"}' \
  http://localhost:8080/api/analysis/compare

# Облако слов
curl http://localhost:8080/api/analysis/wordcloud/{id}

# Health‑check
curl http://localhost:8080/health
```

---

## ⚠️ Важно: схема портов

| URL                                                                                   | Назначение                     | Доступ            |
| ------------------------------------------------------------------------------------- | ------------------------------ | ----------------- |
| **`http://localhost:8080`**                                                           | API Gateway + Swagger + веб‑UI | для пользователей |
| `http://localhost:8081`                                                               | File Storing Service           | внутренний        |
| `http://localhost:8082`                                                               | File Analysis Service          | внутренний        |
| Работайте только через порт **8080** — внутренние сервисы не проходят аутентификацию. |                                |                   |

---

## Структура проекта

```
AntiPlagiarism/
├── api_gateway/               # Веб‑UI + прокси (порт 8080)
├── file_storing_service/      # Хранение файлов (порт 8081)
├── file_analysis_service/     # Анализ текста (порт 8082)
├── docker-compose.yml         # Скрипт запуска сервисов
└── README.md
```

---

## Что внутри

### API Gateway (8080)

* Принимает файлы от пользователей
* Проксирует запросы к другим сервисам
* Swagger‑документация (`/swagger`)

### File Storing Service (8081)

* Сохраняет файлы в PostgreSQL Large Objects
* Определяет дубликаты по SHA‑256
* Хранит метаданные

### File Analysis Service (8082)

* Токенизация ➜ шинглы ➜ MinHash
* Сравнение пар документов (Жаккар)
* Генерация облака слов (WordCloud)

---

## Технологии

* **.NET 9** — основной фреймворк
* **ASP.NET Core + gRPC** — веб‑серверы
* **PostgreSQL 16** — база данных
* **Docker / Docker Compose** — контейнеризация
* **xUnit + TestContainers** — тестирование

---

## API‑методы

### API Gateway (`http://localhost:8080`)

| Метод  | URL                          | Описание               |
| ------ | ---------------------------- | ---------------------- |
| GET    | /                            | Веб‑интерфейс          |
| GET    | /health                      | Проверка всех сервисов |
| POST   | /api/files                   | Загрузить файл         |
| GET    | /api/files                   | Список файлов          |
| GET    | /api/files/{id}              | Информация о файле     |
| DELETE | /api/files/{id}              | Удалить файл           |
| GET    | /api/files/{id}/content      | Скачать файл           |
| GET    | /api/files/{id}/stats        | Статистика текста      |
| POST   | /api/analysis/compare        | Сравнить два файла     |
| GET    | /api/analysis/wordcloud/{id} | Облако слов            |
| GET    | /swagger                     | Swagger UI             |

### File Storing Service (`http://localhost:8081`)

| Метод  | URL                     | Описание           |
| ------ | ----------------------- | ------------------ |
| GET    | /health                 | Проверка сервиса   |
| POST   | /api/files              | Загрузить файл     |
| GET    | /api/files              | Список файлов      |
| GET    | /api/files/{id}         | Метаданные         |
| DELETE | /api/files/{id}         | Удалить            |
| GET    | /api/files/{id}/content | Скачать содержимое |

### File Analysis Service (`http://localhost:8082`)

| Метод | URL                          | Описание           |
| ----- | ---------------------------- | ------------------ |
| GET   | /health                      | Проверка сервиса   |
| GET   | /api/analysis/{id}/stats     | Статистика текста  |
| POST  | /api/analysis/compare        | Сравнить два файла |
| GET   | /api/analysis/{id}/wordcloud | Облако слов        |

---

## Системные требования

* Docker (или .NET SDK 9 для локального запуска)
* 1 ГБ RAM
* 300 MB места на диске
* Windows / macOS / Linux

---

## Запуск для разработки (без Docker)

```bash
# Терминал 1
cd file_storing_service && dotnet run
# Терминал 2
cd file_analysis_service && dotnet run
# Терминал 3
cd api_gateway && dotnet run
```

---

## Тестирование

```bash
# Запустить все тесты
dotnet test

# Тесты только для Gateway
dotnet test api_gateway.tests/
```

---

## Возможные проблемы

| Симптом                    | Причина               | Решение                                                |
| -------------------------- | --------------------- | ------------------------------------------------------ |
| Порт 8080 занят            | Запущен другой сервис | `lsof -i :8080` → изменить порт в `docker-compose.yml` |
| `pg_isready: no response`  | БД ещё стартует       | Подождать 10 сек или проверить логи Postgres           |
| `Cannot connect to daemon` | Docker не запущен     | Стартовать Docker Desktop / сервис docker              |

---

## Примеры использования

### Загрузка файла

```bash
echo "Привет мир" > test.txt
curl -X POST -F "file=@test.txt" http://localhost:8080/api/files
```

Ответ:

```json
{"fileId":"d8f…","filename":"test.txt","size":12,"duplicate":false}
```

### Получение статистики

```bash
curl http://localhost:8080/api/files/d8f…/stats
```

```json
{"wordCount":2,"charCount":12,"paragraphCount":1}
```

### Сравнение двух файлов

```bash
curl -X POST -H "Content-Type: application/json" \
  -d '{"file_id":"d8f…","other_file_id":"a1b…"}' \
  http://localhost:8080/api/analysis/compare
```

```json
{"jaccard":0.83}
```

---

## Алгоритмы внутри

* **Статистика**: токенизация по пробелам, символы = длина текста, абзацы = двойной перевод строки.
* **MinHash**: 128 хэш‑функций → сигнатуры → коэффициент Жаккара.
* **Дубликаты**: сравнение SHA‑256.

---

## Swagger‑документация

* API Gateway — [http://localhost:8080/swagger](http://localhost:8080/swagger)
* File Storing Service — [http://localhost:8081/swagger](http://localhost:8081/swagger) (внутренний)
* File Analysis Service — [http://localhost:8082/swagger](http://localhost:8082/swagger) (внутренний)

Для тестирования удобно использовать **curl** или **Postman**.

---

Система полностью готова к работе после запуска `docker compose up -d`. Приятного использования!
