# Screenshots

These screenshots are captured from a real local Docker Compose run of Mando API. Do not add mock, generated, or placeholder images to this folder.

## Included Screenshots

| File | Demonstrates |
| --- | --- |
| `swagger.png` | Swagger UI loading the Mando API endpoint catalog in Development. |
| `health-live.png` | The `/health/live` endpoint returning a healthy application liveness response. |
| `health-ready.png` | The `/health/ready` endpoint returning a healthy database readiness response. |

## Safety Notes

- Do not include JWT tokens, refresh tokens, passwords, connection strings, local `.env` values, or private machine paths.
- Redact or crop any screenshot that shows Authorization headers, request passwords, bearer tokens, or local-only secrets.
- Keep screenshots focused on the API behavior, status codes, endpoint names, response shape, and safe demo data.

## Replacing Screenshots

1. Start the local stack with `docker compose up --build -d`.
2. Verify `http://localhost:8080/health/live`, `http://localhost:8080/health/ready`, and `http://localhost:8080/swagger/index.html`.
3. Replace images with real captures from the running project.
4. Review each image for secrets before committing.
5. Update [../../README.md](../../README.md) only for screenshot files that actually exist.
