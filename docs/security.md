# Security and Authorization Notes

## Authentication Model

Mando uses JWT bearer authentication with ASP.NET Core Identity. Tokens are validated against the current user record, active status, lockout state, security stamp, and current role set. A fallback authorization policy requires authentication for endpoints that do not explicitly opt out with `AllowAnonymous`.

## Rate Limiting

Login and selected high-impact mutation endpoints are protected with fixed-window rate limits. Rejected requests return HTTP 429 with the API error envelope code `rate_limit_exceeded` and a `Retry-After` header when available.

## Authorization Matrix

| Area | Method and route | Access | Data scope |
| --- | --- | --- | --- |
| Health | `GET /`, `GET /health/live`, `GET /health/ready` | Anonymous | No business data |
| Auth | `POST /api/auth/login` | Anonymous | Login only; rate limited by remote IP |
| Auth | `GET /api/auth/me` | Authenticated | Current user only |
| Audit logs | `GET /api/auditlogs` | Admin, Manager | All audit logs |
| Customers | `POST /api/customers` | Admin, Manager | Assigns to an active SalesRep |
| Customers | `GET /api/customers`, `GET /api/customers/{id}`, `GET /api/customers/{id}/history` | Authenticated | SalesRep sees assigned customers only; Admin/Manager see all |
| Customers | `PUT /api/customers/{id}`, `PATCH /api/customers/{id}/status` | Admin, Manager | All customers |
| Customers | `PATCH /api/customers/{id}/financial-settings` | Admin | All customers |
| Customers | `GET /api/customers/{id}/balance`, `statement`, `financial-ledger`, `credit-profile` | Authenticated | SalesRep sees assigned customers only; Admin/Manager see all |
| Dashboard | `GET /api/dashboard/summary` | Admin, Manager | Global summary |
| Dashboard | `GET /api/dashboard/my-summary` | SalesRep | Current SalesRep only |
| Notifications | `GET /api/notifications`, unread summary, item read APIs | Authenticated | Current user's notifications only |
| Operations | `GET /api/operations/**`, `POST /api/operations/alerts/reviews` | Admin, Manager | Global operations data |
| Orders | `POST /api/orders` | SalesRep | Current SalesRep's in-progress visit only |
| Orders | `PATCH /api/orders/{id}/cancel` | Admin, Manager, SalesRep | SalesRep can cancel own order only while related visit is in progress; Admin/Manager can cancel any eligible order |
| Orders | `GET /api/orders`, `GET /api/orders/{id}`, `GET /api/orders/{id}/history` | Authenticated | SalesRep sees own orders only; Admin/Manager see all |
| Orders | `GET /api/orders/operations-report` | Admin, Manager | Global operations data |
| Payments | `POST /api/payments` | SalesRep | Current SalesRep's in-progress visit only |
| Payments | `PATCH /api/payments/{id}/approve`, `reject`, `reverse`, `void` | Admin, Manager | Any eligible payment; self-review is blocked; rate limited per user and route |
| Payments | `GET /api/payments`, `GET /api/payments/{id}`, `GET /api/payments/{id}/history` | Authenticated | SalesRep sees own payments only; Admin/Manager see all |
| Payments | `GET /api/payments/review-queue`, `operations-report` | Admin, Manager | Global payment review/operations data |
| Products | `POST /api/products`, `PUT /api/products/{id}`, `PATCH /api/products/{id}/status` | Admin, Manager | Product catalog |
| Products | `GET /api/products`, `GET /api/products/{id}`, `GET /api/products/{id}/history` | Authenticated | Product catalog |
| Reports | `GET /api/reports/**`, `GET /api/reports/performance/**` | Admin, Manager | Global reporting data |
| Users | `POST /api/users`, `PATCH /api/users/{id}/status`, `PATCH /api/users/{id}/role` | Admin | User administration |
| Users | `GET /api/users`, `GET /api/users/{id}`, `GET /api/users/{id}/history`, `GET /api/users/sales-reps` | Admin, Manager | User lookup/read model |
| Visits | `POST /api/visits/start`, `end`, `cancel`, `POST /api/visits/{id}/images` | SalesRep | Current SalesRep's assigned customer/visit only; start/end are rate limited per user and route |
| Visits | `GET /api/visits`, `GET /api/visits/{id}`, history, timeline, image list/content/delete | Authenticated | SalesRep sees own visits only; Admin/Manager see all. Image delete still requires an in-progress visit |
| Visits | `GET /api/visits/operations-report` | Admin, Manager | Global operations data |

## Security Controls

- JWT signing key is required and must be at least 32 characters.
- Placeholder JWT keys are rejected during startup.
- Disabled, deleted, locked-out, or role-changed users cannot continue using stale tokens.
- Login and sensitive workflow mutations have configurable fixed-window rate limits.
- SalesRep data access is scoped in services, not only in controllers.
- Payment self-review is blocked for privileged users who submitted the payment.
- Visit images are stored under private `App_Data` storage and served only through authorized API endpoints.
- Request logging records route metadata and user identifiers, not request bodies or passwords.

## Remaining Risks

- Audit logs are append-only at application level, but not cryptographically tamper-evident.
- Local Docker Compose is provided for portfolio review, but should be verified in the target machine before claiming production readiness.
- SQLite integration tests exercise workflow behavior, but SQL Server remains the production database provider.
