# AzureEventGridApp

## Project Purpose

A .NET 8 sample that publishes order lifecycle events to **Azure Event Grid** and delivers them to HTTP webhook handlers. The API persists orders in **Azure SQL**, optionally uploads invoices to **Azure Blob Storage**, and lets Event Grid fan those domain and system events out to subscribers.

## Solution structure

| Project | Role |
|---|---|
| `App.API` | Order and invoice HTTP API. Writes to SQL, publishes custom events, uploads invoice blobs. |
| `App.Handlers` | Event Grid webhook subscribers for inventory, notifications, high-value orders, invoices, retries, and CloudEvents. |
| `App.Shared` | Shared event type names and payload contracts used by both publisher and subscribers. |

Solution file: `EventGridApp.sln`.

`App.API` is wired to `SqlOrderRepository`. An unused `InMemoryOrderRepository` exists in the project but is not registered.

## Architecture

Custom order events go to an Event Grid **custom topic**. Invoice blob events come from a Storage **system topic**. Both can subscribe to `App.Handlers`. The API never calls handlers directly.

```text
                                    Client / Swagger
                                           |
                                           v
                                        App.API
                      ---------------------+---------------------
                      |                                         |
                      v                    |                    v
                 Azure SQL                 |             Azure Blob Storage
                                           |             invoices/{orderId}/{fileName}
                                           |                        |
                                           |                        |  Microsoft.Storage.BlobCreated
                                           |                        v
                                           |              Event Grid system topic (Storage)
                                           |
                                           |  Order.Created / Order.Paid / Order.Cancelled
                                           v
                               Event Grid custom topic
                                           |
                                           |  subscriptions + optional filters
                                           v
 +-------------------------------------------------------------------------------------------------+
 |                                         App.Handlers                                            |
 |  POST /api/events/inventory     POST /api/events/notifications     POST /api/events/high-value  |
 |  POST /api/events/invoices      POST /api/events/fail              POST /api/events/cloudevents |
 +-------------------------------------------------------------------------------------------------+
```

## Event flow

### 1. Create, pay, or cancel an order

```text
Client            App.API             Azure SQL        Event Grid         App.Handlers
  |                  |                    |                 |                   |
  | POST /api/orders |                    |                 |                   |
  |----------------->|                    |                 |                   |
  |                  | insert Created     |                 |                   |
  |                  |------------------->|                 |                   |
  |                  | PublishAsync Order.Created           |                   |
  |                  |------------------------------------->|                   |
  | 201 Created      |                    |                 | fan-out           |
  |<-----------------|                    |                 |------------------>|
  |                  |                    |                 |                   |
  | POST /api/orders/{id}/pay             |                 |                   |
  |----------------->|                    |                 |                   |
  |                  | update Paid        |                 |                   |
  |                  |------------------->|                 |                   |
  |                  | PublishAsync Order.Paid              |                   |
  |                  |------------------------------------->|                   |
  | 200 OK           |                    |                 |------------------>|
  |<-----------------|                    |                 |                   |
  |                  |                    |                 |                   |
  | POST /api/orders/{id}/cancel          |                 |                   |
  |----------------->|                    |                 |                   |
  |                  | update Cancelled   |                 |                   |
  |                  |------------------->|                 |                   |
  |                  | PublishAsync Order.Cancelled         |                   |
  |                  |------------------------------------->|                   |
  | 200 OK           |                    |                 |------------------>|
  |<-----------------|                    |                 |                   |
```

`OrderEventPublisher` sends either an Event Grid schema event or a CloudEvent, controlled by `EventGridOptions.UseCloudEvents`:

- **Event Grid schema** (`UseCloudEvents: false`) — `EventGridEvent` with subject, event type, data version `1.0`, and the payload.
- **CloudEvents** (`UseCloudEvents: true`) — `CloudEvent` with source `/orderpulse/order-api`, the same event type, subject, and payload.

Subjects are `/orders/{orderId}`.

`PublishAsync` returns `false` on a non-200/201 response or exception, but `OrdersController` does not check that result. The HTTP response can still be success after a failed publish.

Missing orders on pay or cancel return `404`.

### 2. Upload an invoice

```text
Client              App.API           Azure SQL      Blob Storage      Event Grid      /api/events/invoices
  |                    |                  |                |                |                   |
  | POST /api/orders/{id}/invoice         |                |                |                   |
  |------------------->|                  |                |                |                   |
  |                    | order exists?    |                |                |                   |
  |                    |----------------->|                |                |                   |
  |                    | upload {orderId}/{fileName}       |                |                   |
  |                    |---------------------------------->|                |                   |
  | 202 Accepted       |                  |                | BlobCreated    |                   |
  |<-------------------|                  |                |--------------->|                   |
  |                    |                  |                |                | webhook           |
  |                    |                  |                |                |------------------>|
```

Invoice upload is a separate path. The API does not publish a custom invoice event. Blob Storage can raise `Microsoft.Storage.BlobCreated` on a Storage system topic, which a subscription can route to the invoices handler.

Upload rules in code:

- Missing or empty file returns `400 File is required.`
- Unknown order returns `404`
- Request body is limited to 5 MB
- Blob path is `{orderId}/{fileName}` in the configured container (Development uses `invoices`)
- The container is created automatically if it does not exist

### 3. Subscription handshake and delivery

Event Grid must validate a webhook before it starts delivering events. Schema must match the endpoint: Event Grid schema endpoints parse `EventGridEvent`; the CloudEvents endpoint parses `CloudEvent`.

```text
Event Grid schema  (POST /api/events/{inventory|notifications|high-value|invoices|fail})
  Event Grid  --POST SubscriptionValidationEvent-->  EventGridController
  Event Grid  <--200 { validationResponse }--------- EventGridController
  Event Grid  --POST order or blob events----------> EventGridController
  Event Grid  <--200 OK----------------------------- EventGridController

CloudEvents schema  (OPTIONS + POST /api/events/cloudevents)
  Event Grid  --OPTIONS WebHook-Request-Origin-----> CloudEventsController
  Event Grid  <--WebHook-Allowed-Origin + Rate 120-- CloudEventsController
  Event Grid  --POST CloudEvents-------------------> CloudEventsController
  Event Grid  <--200 OK----------------------------- CloudEventsController
```

`POST /api/events/fail` answers the validation event, then returns HTTP 500 for later deliveries so Event Grid retries (and can dead-letter).

## Custom events

Defined in `App.Shared`:

| Event type | When it is published | Payload |
|---|---|---|
| `Order.Created` | `POST /api/orders` | `OrderId`, `CustomerId`, `Total`, `CreatedAtUtc` |
| `Order.Paid` | `POST /api/orders/{id}/pay` | `OrderId`, `PaymentRef`, `PaidAtUtc` |
| `Order.Cancelled` | `POST /api/orders/{id}/cancel` | `OrderId`, `Reason`, `CancelledAtUtc` |

System event handled by Event Grid schema subscribers:

| Event type | Source | Meaning |
|---|---|---|
| `Microsoft.Storage.BlobCreated` | Invoice container | A file was written under `{orderId}/{fileName}` |

## Handler endpoints

All live on `App.Handlers`.

The Event Grid schema endpoints share one `HandleAsync` method. They do not filter by event type in code. Whatever Event Grid delivers is logged as follows:

| Incoming event type | Log |
|---|---|
| `Order.Created` | `Reserve stock {OrderId} {Total}` |
| `Order.Paid` | `Paid {OrderId} {Ref}` |
| `Order.Cancelled` | `Cancelled {OrderId} {Reason}` |
| `Microsoft.Storage.BlobCreated` | `Invoice blob {Subject}` |

| Endpoint | Purpose |
|---|---|
| `POST /api/events/inventory` | Shared handler (`failAfterValidation: false`) |
| `POST /api/events/notifications` | Shared handler (`failAfterValidation: false`) |
| `POST /api/events/high-value` | Shared handler (`failAfterValidation: false`) |
| `POST /api/events/invoices` | Shared handler (`failAfterValidation: false`) |
| `POST /api/events/fail` | Shared handler; after validation returns `500` |
| `OPTIONS` / `POST /api/events/cloudevents` | CloudEvents handshake; receive only logs `type`, `subject`, and `id` |

Suggested Event Grid subscription filters (configured in Azure, not in this repo):

- **inventory** — `Order.Created`
- **notifications** — `Order.Created`, `Order.Paid`, `Order.Cancelled`
- **high-value** — `Order.Created` plus a data filter on `Total`
- **invoices** — `Microsoft.Storage.BlobCreated` on the Storage system topic
- **fail** — any custom event you want to retry or dead-letter
- **cloudevents** — same custom topic with CloudEvents delivery schema

## Prerequisites

- .NET 8 SDK
- Azure Event Grid custom topic (and a Storage system topic if you want invoice events)
- Azure SQL Database (or a compatible SQL Server) for orders
- Azure Storage account and a blob container for invoice uploads
- A public or tunneled HTTPS URL for `App.Handlers` (Event Grid cannot call `localhost` directly)

## Configuration

`App.API` binds the whole configuration to `AppSettings`. The sections the code reads are `AppDbContextOptions`, `AzureStorageOptions`, and `EventGridOptions`. The leftover `ConnectionStrings` section in `appsettings.json` is not used.

```json
{
  "AppDbContextOptions": {
    "ConnectionString": "Server=tcp:YOUR-SQL.database.windows.net,1433;Initial Catalog=sqldb-orderpulse;Encrypt=True;..."
  },
  "AzureStorageOptions": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net",
    "ContainerName": "invoices"
  },
  "EventGridOptions": {
    "TopicEndpoint": "https://YOUR-TOPIC.REGION-1.eventgrid.azure.net/api/events",
    "TopicKey": "YOUR_TOPIC_KEY",
    "UseCloudEvents": false
  }
}
```

Set `UseCloudEvents` to `true` only when the topic and subscriptions use the CloudEvents schema. `appsettings.Development.json` currently sets `UseCloudEvents` to `true`.

Do not commit real connection strings or topic keys. Use [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), environment variables, or Azure Key Vault.

## Run locally

```bash
dotnet restore EventGridApp.sln
dotnet ef database update --project App.API
dotnet run --project App.API --launch-profile https
dotnet run --project App.Handlers --launch-profile https
```

`dotnet run` without `--launch-profile` uses the first `Project` profile (`http`):

- API: `http://localhost:5273`
- Handlers: `http://localhost:5230`

The `https` profile URLs:

- API: `https://localhost:7049` (HTTP also on `http://localhost:5273`) — Swagger at `/swagger` in Development
- Handlers: `https://localhost:7055` (HTTP also on `http://localhost:5230`) — Swagger at `/swagger` in Development

The `dotnet-ef` tool must be installed if it is not already (`dotnet tool install --global dotnet-ef`). Apply the migration before creating orders.

Expose `App.Handlers` with a tunnel (for example ngrok) and register that HTTPS base URL on each Event Grid subscription.

## Sample API calls

Create an order (publishes `Order.Created`, returns `201`):

```http
POST /api/orders
Content-Type: application/json

{
  "customerId": "cust-1001",
  "total": 250.00
}
```

Get an order (no event published). Missing orders return `404`:

```http
GET /api/orders/{id}
```

Pay an order (publishes `Order.Paid`, returns `200`):

```http
POST /api/orders/{id}/pay
Content-Type: application/json

{
  "paymentRef": "pay-abc-123"
}
```

Cancel an order (publishes `Order.Cancelled`, returns `200`):

```http
POST /api/orders/{id}/cancel
Content-Type: application/json

{
  "reason": "Customer requested cancellation"
}
```

Upload an invoice (blob write; Event Grid can raise `BlobCreated`; returns `202`). The multipart field name must be `file`:

```http
POST /api/orders/{id}/invoice
Content-Type: multipart/form-data

file=@invoice.pdf
```

JSON property names follow ASP.NET Core camelCase (`customerId`, `total`, `paymentRef`, `reason`).

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
