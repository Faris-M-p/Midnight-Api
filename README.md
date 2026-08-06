# MidnightApi — Family Tree Management System

A production-ready **REST API backend** for managing multi-generational family trees. MidnightApi lets you create families, add members with parent/spouse relationships, and retrieve a fully nested tree structure from the server — so a React (or any) frontend can render the tree without building it client-side.

---

## Project Idea

Family history is naturally hierarchical: a root ancestor, their spouse, children, grandchildren, and so on. MidnightApi models that structure in PostgreSQL and exposes it through clean HTTP endpoints.

**Core goals:**

- Store rich member profiles (addresses, photos, life events, notes, social links)
- Represent relationships explicitly (parent, spouse, children)
- Return the family tree already nested from the API
- Support business operations a family-tree UI needs (search, generation level, add child, link spouse)
- Never hard-delete data — use soft delete with full audit trails

A **React frontend** is planned as a separate layer that will consume these APIs.

---

## Technology Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 10 Web API |
| Data access | Dapper + PostgreSQL stored procedures |
| Database | PostgreSQL (SQL-first `Database` project) |
| API docs | Swagger / OpenAPI |
| Patterns | Repository Pattern, Dependency Injection, View/Input/Output Models |

---

## Architecture

```
Controllers → Repositories → Dapper → Stored Procedures → PostgreSQL
```

- **3 controllers** keep the API surface simple:
  - `FamilyController` — family records
  - `MemberController` — members, tree logic, and all member sub-resources
  - `AccountController` — login accounts linked to families

- **Repositories** only call stored procedures via Dapper. Business validation stays in services.

- **Soft delete** — `DELETE` endpoints set `IsCancelled = true` instead of removing rows.

- **Database project** — `Database/` holds tables, functions, views, indexes, procedures, and seed scripts. Use `InstallDatabase.bat` / `PatchDatabase.bat`, or let startup apply schema when tables are missing.

---

## Database Design

### Tables

| Table | Description |
|-------|-------------|
| `Families` | Family groups (unique `FamilyCode`) |
| `Members` | People in a family; self-references for parent & spouse |
| `MemberAddresses` | Physical addresses per member |
| `MemberImages` | Photos / avatars |
| `MemberEvents` | Life events (birth, marriage, etc.) |
| `MemberSocialLinks` | Social media profiles |
| `MemberNotes` | Free-text notes |
| `UserAccounts` | One login account per family |

### Key relationships

```
Families  1 ──→  Many  Members

Members (self-reference)
  ├── FK_Members_Parent  →  one parent, many children
  └── FK_Members_Spouse  →  one spouse (bidirectional link)

Members  1 ──→  Many   Addresses | Images | Events | SocialLinks | Notes
Members  1 ──→  One    UserAccount
```

Every table includes audit fields: `CreatedBy`, `CreatedOn`, `UpdatedBy`, `UpdatedOn`, `IsCancelled`, `CancelledBy`, `CancelledOn`.

Primary keys use `BIGINT` with naming `ID_TableName`. Foreign keys follow `FK_TableName`.

---

## Project Structure

```
MidnightApi/
├── Controllers/
│   ├── FamilyController.cs      # /api/families
│   ├── MemberController.cs      # /api/members (+ sub-resources)
│   └── AccountController.cs     # /api/accounts
├── Data/
│   ├── IDbConnectionFactory.cs
│   ├── NpgsqlConnectionFactory.cs
│   ├── StoredProcedures.cs
│   ├── DapperExtensions.cs
│   └── DatabaseInitializer.cs   # Ensure DB + apply SQL schema
├── Database/                    # SQL-first schema (tables, views, SPs, patches)
├── Interfaces/                  # Repository contracts
├── Models/
│   └── Api/                     # Input, Output, and View models
├── Repositories/                # Dapper → stored procedure calls only
├── Services/
│   └── MemberValidationService.cs
├── Program.cs
└── appsettings.json
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/) running locally

### Configuration

Update the connection string in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=midnight_family_tree;Username=postgres;Password=YOUR_PASSWORD;"
}
```

### Run

```bash
dotnet run --project MidnightApi.csproj
```

The API starts at **http://localhost:5196**. Swagger UI is at:

**http://localhost:5196/swagger**

On first run the database is created if missing, and SQL schema/procedures from the `Database` project are applied.

---

## API Reference

### Families — `/api/families`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/families` | List all families |
| GET | `/api/families/{id}` | Get family by ID |
| POST | `/api/families` | Create family |
| PUT | `/api/families/{id}` | Update family |
| DELETE | `/api/families/{id}` | Soft delete family |

### Members — `/api/members`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/members` | List all members |
| GET | `/api/members/{id}` | Get member by ID |
| POST | `/api/members` | Create member |
| PUT | `/api/members/{id}` | Update member |
| DELETE | `/api/members/{id}` | Soft delete member |
| GET | `/api/members/tree/{familyId}` | **Full nested family tree** |
| GET | `/api/members/root/{familyId}` | Root member of a family |
| GET | `/api/members/profile/{memberId}` | Member + addresses, images, events, notes, links |
| GET | `/api/members/family/{familyId}` | All members in a family |
| GET | `/api/members/search?name=` | Search members by name |
| GET | `/api/members/generation/{familyId}/{level}` | Members at generation level (0 = root) |
| POST | `/api/members/{memberId}/spouse` | Link or create a spouse |
| POST | `/api/members/{parentId}/child` | Add a child under a parent |

### Member sub-resources — `/api/members/...`

Each supports GET (all), GET by ID, POST, PUT, DELETE:

| Route prefix | Resource |
|--------------|----------|
| `/api/members/addresses` | Member addresses |
| `/api/members/images` | Member photos |
| `/api/members/events` | Life events |
| `/api/members/notes` | Notes |
| `/api/members/social-links` | Social media links |

### Accounts — `/api/accounts`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/accounts` | List all accounts |
| GET | `/api/accounts/{id}` | Get account by ID |
| GET | `/api/accounts/member/{memberId}` | Get account for a member |
| POST | `/api/accounts` | Create account |
| PUT | `/api/accounts/{id}` | Update account |
| DELETE | `/api/accounts/{id}` | Soft delete account |

---

## Validation Rules

The API enforces these business rules:

- **Email** and **phone** format validation
- **Unique family code** per active family
- **Unique username** per active account
- **One root member** per family
- A member **cannot be their own parent or spouse**
- **Circular parent references** are blocked

---

## Tree Response Example

`GET /api/members/tree/1` returns a nested structure ready for UI rendering:

```json
{
  "fullName": "John Demo",
  "isRoot": true,
  "spouse": { "fullName": "Jane Demo", "children": [] },
  "children": [
    {
      "fullName": "Michael Demo",
      "children": [
        { "fullName": "Emily Demo", "children": [] }
      ]
    },
    {
      "fullName": "Sarah Demo",
      "children": [
        { "fullName": "David Demo", "children": [] }
      ]
    }
  ],
  "images": [],
  "events": [],
  "notes": [],
  "socialLinks": []
}
```

---

## Design Principles

- **Repository pattern** — data access is isolated; controllers stay thin
- **View/Input/Output models** — EF entities never leak to the client
- **Soft delete** — data is preserved with cancellation audit fields
- **Backend-built tree** — the React frontend receives ready-to-render JSON
- **Simple DB bootstrap** — no manual migrations required; tables are created on first run

---

## Roadmap

- [ ] React frontend for interactive tree visualization
- [ ] JWT authentication wired to `UserAccounts`
- [ ] Pagination on list endpoints
- [ ] Integration tests

---

## License

Private project — Midnight Family Tree Management System.
