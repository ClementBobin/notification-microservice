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

## Notes

- Tables are created by `EnsureSchemaAsync` at startup **only when the probe table is missing**; a new table or column added later is not applied to an existing database. Switch to EF migrations (history table per schema) once the model settles.
