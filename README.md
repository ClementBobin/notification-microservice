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

Each service is its own solution made of four layered projects; the root `Genkan.slnx` contains everything.

```
src/
  Genkan.Shared/                       contracts, gateway header auth, DB bootstrap
  Genkan.Gateway/                      YARP reverse proxy (single project)
  Genkan.Auth/              Genkan.Auth.slnx
    Genkan.Auth.Api/                   web api: Controllers, Configuration, Extensions, Interceptors, Middleware
    Genkan.Auth.Application/           Interfaces, Services (login, password hashing)
    Genkan.Auth.Domain/                Dto, Entities
    Genkan.Auth.Infrastructure/        Data (DbContext, Redis sessions), Interfaces, Repositories
  Genkan.Notification/      Genkan.Notification.slnx
    Genkan.Notification.Api/           Controllers, Extensions (push wiring), ...
    Genkan.Notification.Application/   Interfaces, Services (processor, templates)
    Genkan.Notification.Domain/        Dto, Entities
    Genkan.Notification.Infrastructure/ Data, Interfaces, Repositories, Messaging (RabbitMQ), Push (FCM)
```

References: `Api -> Application, Domain, Infrastructure`, `Application -> Domain, Infrastructure`, `Infrastructure -> Domain`.
Everything may also use `Genkan.Shared`. Build one service on its own: `dotnet build src/Genkan.Auth/Genkan.Auth.slnx`.

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
| POST | `/auth/login` `{email,password}` → `{token, expiresAt, user}` | public, 10/min/IP |
| GET | `/auth/session` | bearer token |
| POST | `/auth/logout` | bearer token |
| POST | `/auth/users` `{email,displayName,password,role}` | Admin |
| GET | `/notifications/me` | any user (own + broadcast alerts) |
| GET | `/notifications/users/{userId}` | Admin |
| GET | `/notifications/logs?userId=&type=&status=&page=&pageSize=` | Admin |
| POST | `/notifications/alerts` `{title,message}` → publishes `alert.broadcast` | Admin |
| POST | `/notifications/devices` `{token,platform}` (`android`\|`ios`\|`web`) → registers the device's FCM token | any user |
| POST | `/notifications/devices/unregister` `{token}` (call on logout) | any user |

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

## Push notifications (Firebase)

The notification service consumes `booking.#`, `access.#`, `visitor.#` and `alert.#` from RabbitMQ, stores one notification per recipient
and pushes it to the recipient's registered devices through Firebase Cloud Messaging (an alert with no recipients goes to every device).
The app registers its FCM token with `POST /notifications/devices`.

Status of a stored notification: `Sent` (at least one device reached), `Skipped` (`no_registered_device`: still visible in `/notifications/me`), `Failed` (FCM error, see `error`).
Tokens FCM reports as unregistered are deleted automatically.

By default `Firebase:Enabled` is `false` and pushes are only logged. To send for real, add a service-account key and use the override file:

```bash
mkdir secrets   # put firebase-service-account.json in it (git- and docker-ignored)
docker compose -f docker-compose.yml -f docker-compose.firebase.yml up -d --build
```

With `Firebase:Enabled=true` the service refuses to start if the credentials cannot be loaded.

## Notes

- Tables are created by `EnsureSchemaAsync` at startup **only when the probe table is missing**; a new table or column added later is not applied to an existing database. Switch to EF migrations (history table per schema) once the model settles.
