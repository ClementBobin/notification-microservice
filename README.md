# Genkan – API Gateway, Auth, Notification

```
clients ──HTTP──▶ gateway ──▶ auth          (PostgreSQL schema "auth",  Redis sessions)
                     └──────▶ notification  (PostgreSQL schema "notification")
                                  ▲
        RabbitMQ topic exchange "workplace.events" ─ queue "notification-service"
        bindings: booking.#  access.#  visitor.#  alert.#   (dead-letter: workplace.events.dlx)
```

One PostgreSQL database (`genkan`), one schema per service, tables created at startup.
Only the gateway is published on the host. Services trust the `X-User-*` headers set by the gateway
(it strips client-supplied ones), so never expose `auth` / `notification` directly.

## Project layout

Each service is its own solution made of layered projects; the root `Genkan.slnx` contains everything.

```
src/
  Genkan.Shared/                      contracts, gateway header auth, DB bootstrap
  Genkan.Gateway/                     YARP reverse proxy (single project)
  Genkan.Auth/            Genkan.Auth.slnx
    Genkan.Auth.Domain/               entities
    Genkan.Auth.Data/                 DbContext + repositories
    Genkan.Auth.Business/             AuthService, TOTP/password crypto, Redis session store
    Genkan.Auth.Api/                  Program.cs + endpoints (the only runnable project)
  Genkan.Notification/    Genkan.Notification.slnx
    Genkan.Notification.Domain/
    Genkan.Notification.Data/
    Genkan.Notification.Business/     services, templates, IPushSender, IEventPublisher (port)
    Genkan.Notification.Messaging/    RabbitMQ consumer + publisher (implements IEventPublisher)
    Genkan.Notification.Api/
```

References point one way: `Api → Business → Data → Domain` (Messaging → Business, everything may use Shared).
Build one service on its own: `dotnet build src/Genkan.Auth/Genkan.Auth.slnx`.

## Run

```bash
cp .env.example .env        # then edit the secrets
docker compose up -d --build postgres redis rabbitmq auth notification gateway   
```

Gateway: http://localhost:8080 · RabbitMQ UI: http://localhost:15672 · OpenAPI (inside the network): `/openapi/v1.json` on each service.

## Endpoints (all through the gateway)

| Method | Path | Access |
|---|---|---|
| GET | `/health`, `/auth/health`, `/notifications/health` | public |
| POST | `/auth/login` `{email,password}` → session or `2fa_required` + `challengeId` | public, 10/min/IP |
| POST | `/auth/2fa/verify` `{challengeId,code}` | public, 10/min/IP |
| POST | `/auth/2fa/setup` → `{secret, otpauthUri}`; POST `/auth/2fa/confirm` `{code}` | any user |
| GET | `/auth/session` | bearer token |
| POST | `/auth/logout` | bearer token |
| POST | `/auth/users` `{email,displayName,password,role}` | Admin |
| GET | `/notifications/me` | any user (own + broadcast alerts) |
| GET | `/notifications/users/{userId}` | Admin |
| GET | `/notifications/logs?userId=&type=&status=&page=&pageSize=` | Admin |
| POST | `/notifications/alerts` `{title,message}` → publishes `alert.broadcast` | Admin |

## Try it

```bash
TOKEN=$(curl -s localhost:8080/auth/login -H 'content-type: application/json' \
  -d '{"email":"admin@genkan.local","password":"SuperSecureAdmin123!"}' | jq -r .token)

curl -s localhost:8080/notifications/alerts -H "authorization: Bearer $TOKEN" -H 'content-type: application/json' \
  -d '{"title":"Fire drill","message":"Assembly point B at 10:00"}'

curl -s localhost:8080/notifications/logs -H "authorization: Bearer $TOKEN"
```

Publish a test event from the RabbitMQ UI (exchange `workplace.events`, routing key `booking.created`):

```json
{"eventId":"3f0c6d52-6a41-4c0a-9d0e-1f6a0f1f2b11","type":"booking.created","occurredAt":"2026-10-19T08:30:00Z",
 "recipientUserIds":["<a user id>"],"data":{"room":"Kaede","start":"09:00"}}
```
Sending it twice creates one notification (idempotent on eventId + user).

## Notes

- Push delivery is a stub (`LoggingPushSender`); implement `IPushSender` with FCM (+ device tokens) when needed.
- Tables are created by `EnsureSchemaAsync` at startup; switch to EF migrations (history table per schema) once the model settles.
- TOTP secrets are stored unencrypted for now (spec section 8: encryption at rest to be added).
