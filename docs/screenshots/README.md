# Screenshots

These screenshots are captured from a real local Docker Compose run of Mando API. Do not add mock, generated, or placeholder images to this folder.

The `postman-*.png` files are sanitized API-client captures generated from live Mando API requests. They are intended to prove the same request/response workflows a reviewer would run in Postman. If you capture the real Postman desktop UI later, replace these files with those screenshots after redacting sensitive values.

## Included Screenshots

| File | Demonstrates |
| --- | --- |
| `swagger.png` | Swagger UI loading the Mando API endpoint catalog in Development. |
| `health-live.png` | The `/health/live` endpoint returning a healthy application liveness response. |
| `health-ready.png` | The `/health/ready` endpoint returning a healthy database readiness response. |
| `docker.png` | Docker Compose runtime with the API and SQL Server services running. |
| `postman-login.png` | Successful `POST /api/auth/login` response shape with token and password redacted. |
| `postman-me.png` | Successful authenticated `GET /api/auth/me` response with Authorization redacted. |
| `postman-customers.png` | Paged `GET /api/customers` response from seeded demo data with contact/location fields redacted. |
| `postman-visits.png` | Paged `GET /api/visits` workflow response with GPS coordinates redacted. |
| `postman-payments.png` | Paged `GET /api/payments` workflow response showing payment review state. |
| `postman-dashboard.png` | `GET /api/dashboard/summary` operational metrics response. |

## Safety Rules

- Do not include JWT tokens, refresh tokens, passwords, connection strings, local `.env` values, or private machine paths.
- Redact Authorization headers, request passwords, bearer tokens, phone numbers, street addresses, and GPS coordinates.
- Keep screenshots focused on API behavior, status codes, endpoint names, response shape, and safe demo data.
- Review every image visually before committing.

## Manual Postman Replacement Checklist

Take these manually only after the local stack is running and seeded:

- `postman-login.png`: `POST /api/auth/login`, status `200`, password hidden, token redacted.
- `postman-me.png`: `GET /api/auth/me`, status `200`, Authorization header/JWT redacted.
- `postman-customers.png`: `GET /api/customers?pageNumber=1&pageSize=3`, contact/location values redacted.
- `postman-visits.png`: `GET /api/visits?pageNumber=1&pageSize=3`, GPS values redacted.
- `postman-payments.png`: `GET /api/payments?pageNumber=1&pageSize=3`, no token/header visible.
- `postman-dashboard.png`: `GET /api/dashboard/summary`, no token/header visible.

## Replacing Screenshots

1. Start the local stack with `docker compose up --build -d`.
2. Verify `http://localhost:8080/health/live`, `http://localhost:8080/health/ready`, and `http://localhost:8080/swagger/index.html`.
3. Replace images with real captures from the running project.
4. Review each image for secrets before committing.
5. Update [../../README.md](../../README.md) only for screenshot files that actually exist.
