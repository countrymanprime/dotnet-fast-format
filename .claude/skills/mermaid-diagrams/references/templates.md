# Mermaid templates

Copy a template, then replace **every** node, label, and description with what the code
actually contains. These templates stick to syntax that renders on GitHub, DocFX (modern
template), and, for everything except the `flowchart` keyword, Azure DevOps. For Azure
DevOps, change `flowchart LR` to `graph LR`.

Shape conventions used throughout:

| Shape | Syntax | Use for |
| --- | --- | --- |
| Stadium | `id([Name])` | Person or role |
| Rectangle | `id["Name (tech)"]` | Container you own: app, API, worker, library |
| Cylinder | `id[("Name (tech)")]` | Database or durable store |
| Hexagon | `id{{"Name"}}` | Queue, topic, or event bus |
| Dashed border | `class id external` | System you don't own |

## 1. System context

Covers who uses the system and which external systems it depends on. There is one box for "our system".

```mermaid
flowchart LR
  accTitle: Ordering - system context
  accDescr {
    Customers place orders through the Ordering system. Ordering takes payments
    through Stripe and sends confirmation emails through SendGrid.
  }

  customer([Customer])
  ordering["Ordering system"]
  stripe["Stripe (payments)"]
  sendgrid["SendGrid (email)"]

  customer -->|places orders| ordering
  ordering -->|charges cards via REST| stripe
  ordering -->|sends confirmations via REST| sendgrid

  classDef external stroke-dasharray: 5 5
  class stripe,sendgrid external
```

## 2. Container view

This view opens up "our system" and shows its runnable or deployable parts and how they communicate.

```mermaid
flowchart LR
  accTitle: Ordering - container view
  accDescr {
    The Blazor web app calls the Orders API over HTTPS. The API stores orders in
    SQL Server through EF Core and publishes OrderPlaced events to Azure Service Bus.
    The Fulfilment worker consumes those events and updates order status through the API.
  }

  customer([Customer])
  subgraph ordering [Ordering system]
    web["Web app (Blazor)"]
    api["Orders API (ASP.NET Core)"]
    worker["Fulfilment worker (.NET Worker Service)"]
    db[("Orders DB (SQL Server)")]
  end
  bus{{"Azure Service Bus"}}

  customer -->|uses| web
  web -->|HTTPS/JSON| api
  api -->|EF Core| db
  api -->|publishes OrderPlaced| bus
  bus -->|delivers OrderPlaced| worker
  worker -->|PATCH /orders/id/status| api

  classDef external stroke-dasharray: 5 5
  class bus external
```

## 3. Sequence: one use case

Shows the happy path plus the one or two alternative paths that matter. Keep it to 8 participants or fewer.

- `->>` is a call.
- `-->>` is a reply.
- `-)` is an asynchronous message.
- `+` and `-` activate and deactivate a participant.

```mermaid
sequenceDiagram
  accTitle: Place order - sequence
  accDescr {
    The web app posts an order to the Orders API. The API validates it, charges the card
    through Stripe, saves the order, publishes OrderPlaced, and returns 201. A declined card
    returns 402 and nothing is saved.
  }
  autonumber
  actor C as Customer
  participant W as Web app
  participant A as Orders API
  participant S as Stripe
  participant D as Orders DB
  participant B as Service Bus

  C->>W: Submit checkout
  W->>+A: POST /orders
  A->>A: Validate request
  A->>+S: Create payment intent
  alt Card declined
    S-->>A: 402 declined
    A-->>W: 402 Payment Required
  else Card charged
    S-->>-A: 200 succeeded
    A->>D: INSERT order (status Placed)
    A-)B: OrderPlaced
    A-->>-W: 201 Created + Location
  end
  W-->>C: Show confirmation or error
```

## 4. State lifecycle

States and transitions must match the code, for example the enum and the methods that change it.
Label each transition with the method or event that causes it.

```mermaid
stateDiagram-v2
  accTitle: Order status lifecycle
  accDescr {
    An order starts as Placed. Payment capture moves it to Paid; cancellation before
    shipping moves it to Cancelled. Shipping moves Paid to Shipped, and delivery
    confirmation moves Shipped to Delivered. Cancelled and Delivered are final.
  }
  [*] --> Placed
  Placed --> Paid: CapturePayment()
  Placed --> Cancelled: Cancel()
  Paid --> Cancelled: Cancel() [refund issued]
  Paid --> Shipped: MarkShipped(trackingNo)
  Shipped --> Delivered: ConfirmDelivery()
  Cancelled --> [*]
  Delivered --> [*]
```

## 5. Entity relationships

**Generate this from the EF Core model when you can** (see the compatibility and tooling reference).
Hand-write it only when there is no EF model, and then show only the entities and key columns that
matter to the reader.

Cardinality markers are mirrored on each side of the line:

| Meaning | Left side | Right side |
| --- | --- | --- |
| Exactly one | `\|\|` | `\|\|` |
| Zero or one | `\|o` | `o\|` |
| Zero or more | `}o` | `o{` |
| One or more | `}\|` | `\|{` |

Use `--` for an identifying relationship and `..` for a non-identifying one. For example,
`CUSTOMER ||--o{ ORDER : places` reads "one customer places zero or more orders".

```mermaid
erDiagram
  accTitle: Ordering data model
  accDescr {
    A customer has zero or more orders. Each order has one or more order lines,
    and each order line references exactly one product.
  }
  CUSTOMER ||--o{ ORDER : places
  ORDER ||--|{ ORDER_LINE : contains
  PRODUCT ||--o{ ORDER_LINE : "appears in"

  CUSTOMER {
    int Id PK
    string Email UK
  }
  ORDER {
    int Id PK
    int CustomerId FK
    string Status
    datetime PlacedAtUtc
  }
  ORDER_LINE {
    int OrderId PK, FK
    int ProductId PK, FK
    int Quantity
    decimal UnitPrice
  }
  PRODUCT {
    int Id PK
    string Sku UK
  }
```

## 6. Pipeline or process flow

```mermaid
flowchart LR
  accTitle: Release pipeline
  accDescr {
    A push to main builds and tests the solution. If tests pass, packages are packed
    and pushed to NuGet; the docs site is built and published in parallel.
  }
  push([Push to main]) --> build["dotnet build"]
  build --> test["dotnet test"]
  test -->|pass| pack["dotnet pack"]
  test -->|fail| stop(["Stop: fix the build"])
  pack --> nuget["Push to NuGet"]
  test -->|pass| docs["docfx build"]
  docs --> pages["Publish to GitHub Pages"]
```

## 7. Small domain core (class diagram)

Use this only for a handful of stable, central types. Show the relationships and the few
members that explain them, not every property.

```mermaid
classDiagram
  accTitle: Pricing domain core
  accDescr {
    An Order aggregates OrderLines. Each OrderLine has a Money unit price.
    A PricingPolicy computes the order total from its lines.
  }
  class Order {
    +IReadOnlyList~OrderLine~ Lines
    +Money Total()
  }
  class OrderLine {
    +Sku Sku
    +int Quantity
    +Money UnitPrice
  }
  class Money {
    <<value object>>
    +decimal Amount
    +string Currency
  }
  class PricingPolicy {
    <<interface>>
    +Money Price(Order order)
  }
  Order "1" *-- "1..*" OrderLine
  OrderLine --> Money : unit price
  PricingPolicy ..> Order : prices
```
