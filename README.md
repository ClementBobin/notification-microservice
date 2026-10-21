# Genkan – API Gateway, Auth, Notification

```
clients ──HTTP──▶ gateway ──▶ auth          (Redis sessions)
                     └──────▶ notification
        both use the PostgreSQL schema "genkan", created and migrated by the one-shot `migrator` container
                                  ▲
        RabbitMQ topic exchange "workplace.events" ─ queue "notification-service"
        bindings: booking.#  access.#  visitor.#  alert.#   (dead-letter: workplace.events.dlx)
```

One PostgreSQL database (`genkan`) and one shared schema (`genkan`). Only `Genkan.Migrator` creates or alters it; the services just read and write their tables (see *Database*).
Only the gateway is published on the host. Services trust the `X-User-*` headers set by the gateway
(it strips client-supplied ones), so never expose `auth` / `notification` directly.

## Project layout

Each service is its own solution made of four layered projects; the root `Genkan.slnx` contains everything.

```
src/
  Genkan.Shared/                       contracts, gateway header auth, DB bootstrap, service defaults (logging, API setup, middleware order), health endpoints
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
| GET | `/notifications/firebase-config` → `{apiKey, applicationId, messagingSenderId, projectId}` (public client identifiers, never the service-account key) | any user |
| POST | `/notifications/push/individual` `{userId \| token, message}` | Admin |
| POST | `/notifications/push/multicast` `{userIds, tokens, message}` | Admin |
| GET | `/notifications/push/groups` | Admin |
| PUT / DELETE | `/notifications/push/groups/{name}` (`{userIds}` replaces the members, an empty list deletes the group) | Admin |
| POST | `/notifications/push/groups/{name}/send` `{title, body, data}` | Admin |
| POST | `/notifications/push/topics/{topic}/send` `{title, body, data}` | Admin |
| POST | `/notifications/push/topics/{topic}/subscribe` and `/unsubscribe` `{userIds}` | Admin |

`message` is `{ "title": "...", "body": "...", "data": { "doorId": "102" } }` (data values are strings).

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

### Four ways to send on demand (Admin)

| Kind | Target | Implementation |
|---|---|---|
| Individual | one token, or one user's most recently used device | `IndividualPushService` -> `FcmIndividualPushSender` |
| Multicast | an ad-hoc list of tokens and/or users | `MulticastPushService` -> `FcmMulticastPushSender` (500 per FCM request) |
| Device group | a named group of users kept by this service | `DeviceGroupPushService` -> multicast |
| Topic | an FCM topic | `TopicPushService` -> `FcmTopicPushSender` |

Device groups are a mapping kept in our own database (`device_group_members`) and resolved to tokens at send time:
FCM's native device groups are deprecated (decommissioned on 2027-09-29), and Firebase's migration guide recommends exactly this approach.
Topic subscriptions are stored per user (`topic_subscriptions`) and managed by an admin, so a device that registers a new token is
re-subscribed to its user's topics. `FirebaseAdmin` must be >= 3.7.0 for topic management.

### Firebase client config for the app

`GET /notifications/firebase-config` returns the public identifiers the app needs to initialise Firebase at runtime
(`apiKey`, `applicationId`, `messagingSenderId`, `projectId`, the values found in `google-services.json`). Configure them with
`Firebase__Client__ApiKey`, `Firebase__Client__ApplicationId`, `Firebase__Client__MessagingSenderId` and `Firebase__Client__ProjectId`.
The service-account key (`firebase-service-account.json`) contains a private key, stays on the server and is never returned by any route.

### Enabling real pushes

By default `Firebase:Enabled` is `false` and pushes are only logged. To send for real, add a service-account key and use the override file:

```bash
mkdir secrets   # put firebase-service-account.json in it (git- and docker-ignored)
docker compose -f docker-compose.yml -f docker-compose.firebase.yml up -d --build
```

With `Firebase:Enabled=true` the service refuses to start if the credentials cannot be loaded.

## Rate limiting (gateway)

A token bucket in front of the proxy, applied after the token check: one bucket per user (`X-User-Id`), and per client IP for requests
without a user (login). With the default policy a bucket holds 30 tokens, every request takes one, and tokens come back continuously at
30 per 60 s (one every 2 s) instead of all at once when a fixed minute ends. An empty bucket answers `429` with `Retry-After`
(seconds until one token is back); every response carries `X-RateLimit-Limit` and `X-RateLimit-Remaining`. `/health` is never limited.

```json
"RateLimiting": {
  "Enabled": true,
  "Store": "Memory",
  "Policies": {
    "user": { "Key": "User", "Capacity": 30, "RefillTokens": 30, "RefillPeriodSeconds": 60 }
  }
}
```

Environment overrides: `RateLimiting__Policies__user__Capacity=60`, `RateLimiting__Enabled=false`, `RATE_LIMIT_STORE=Redis` (compose).
`Capacity` is the largest burst after being idle; `RefillTokens` / `RefillPeriodSeconds` is the sustained rate.

- `Store: Memory` keeps the buckets in the gateway process. With several gateway instances each instance counts on its own (N instances allow about N times the limit).
- `Store: Redis` shares the buckets between all gateway instances (atomic Lua script, Redis clock). If Redis is unreachable requests are allowed and a warning is logged.
- Another limit, for example per IP before authentication, is another entry in `Policies` plus `app.UseGatewayRateLimit("ip")` before `TokenValidationMiddleware`.
- Auth keeps its own, stricter limit on `POST /auth/login` (10 / min / IP, per Auth instance).

## Notes

- Schema changes go through `Genkan.Migrator` (see *Database*); services never create or alter tables.

## Database

One schema (`genkan`), one owner: **`Genkan.Migrator`** is a console project run as its own container (`restart: "no"`). It applies the EF Core
migrations and exits; `auth` and `notification` have `depends_on: migrator: service_completed_successfully`, so they start on an up-to-date schema.

| Project | Role |
|---|---|
| `Genkan.Shared` (`Entities/`, `Configurations/`) | schema name + entities + mappings (`IEntityTypeConfiguration<T>`), the single definition of every table |
| `Genkan.Migrator` | `GenkanDbContext` (full model **with the cross-service foreign keys**), migrations, one `__EFMigrationsHistory` in schema `genkan` |
| `AuthDbContext`, `NotificationDbContext` | runtime access to their own tables, using the mappings from `Genkan.Shared`; they never migrate |

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate -p src/Genkan.Migrator -o Migrations     # again for every model change
docker compose up -d --build        # migrator runs, exits 0, then auth / notification start
# outside docker: dotnet run --project src/Genkan.Migrator   (ConnectionStrings__Postgres must be set)
```

To change the model: edit the entity / its configuration in `Genkan.Shared`, add a migration, commit it, deploy. Foreign keys are declared only in `GenkanDbContext`.
Table names are global to the schema, so two services must not use the same table name. Without any migration the migrator creates the tables directly (development only).
Databases created by the previous layout (schemas `auth` / `notification`) are not migrated automatically: `docker compose down -v` once.
