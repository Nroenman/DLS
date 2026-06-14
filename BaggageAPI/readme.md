## Features

- Create baggage 
- Retrieve baggage information
- Update baggage details
- REST API endpoints
- API versioning support
- Swagger/OpenAPI documentation
- Entity Framework Core data access
- SQL Server database integration

## Technologies
- ASP.NET Core Web API
- Entity Framework Core
- Postgress DB
- Swagger / OpenAPI
- xUnit (Testing)
- Docker
- rabbitmq

## API Versioning

The API uses URL-based versioning.

Example:

```http
GET /api/v1/baggage
```
Controller example:

```csharp
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/baggage")]
```



## Architecture
The service follows a layered architecture:

```
Controller Layer
       │
Service Layer
       │
Repository Layer
       │
Database
    
## Testing

The service uses xUnit for automated testing.

### Run Tests

```bash
dotnet test
```

### Testing Strategy

- Unit tests focus on business logic in the service layer.
- White-box testing is used for service methods.
- Repository and database interactions are tested through integration tests.

---

## Example Endpoints

### Get All Baggage

```http
GET /api/v1/baggage
```

### Get Baggage by ID

```http
GET /api/v1/baggage/{id}
```

### Create Baggage

```http
POST /api/v1/baggage
```

