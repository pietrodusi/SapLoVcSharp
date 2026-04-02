# SAP LO-VC Configuration API

REST API for the SAP LO-VC (Logistics - Variant Configuration) engine.

## Quick Start

### Run the API

```bash
cd SapLoVcSharp.Api
dotnet run
```

The API will start on:
- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:5001`
- **Swagger UI**: `https://localhost:5001/swagger`

## API Endpoints

### Health Check

```http
GET /health
```

**Response**:
```json
{
  "status": "healthy",
  "timestamp": "2025-01-15T10:30:00Z",
  "version": "1.0.0"
}
```

---

### Materials

#### List All Materials

```http
GET /api/materials
```

**Response**:
```json
[
  {
    "materialNumber": "SAMPLE_MAT",
    "description": "Sample Material",
    "status": "Active",
    "classes": ["CL_SAMPLE"]
  }
]
```

#### Get Material Details

```http
GET /api/materials/{materialNumber}
```

**Response**:
```json
{
  "materialNumber": "SAMPLE_MAT",
  "description": "Sample Material",
  "status": "Active",
  "hasConfigurationProfile": true,
  "characteristics": [
    {
      "name": "EST_IPVTYPE",
      "description": "EST_IPVTYPE",
      "dataType": "CHAR",
      "isRequired": true,
      "isRestrictable": true,
      "allowedValues": [
        {
          "value": "N",
          "description": "N value",
          "isDefault": false
        },
        {
          "value": "S",
          "description": "S value",
          "isDefault": false
        }
      ]
    }
  ]
}
```

---

### Configuration Execution

#### Execute Configuration

```http
POST /api/configurations/{materialNumber}/execute
```

**Example**:
```bash
curl -X POST https://localhost:5001/api/configurations/SAMPLE_MAT/execute
```

**Response**:
```json
{
  "materialNumber": "SAMPLE_MAT",
  "success": true,
  "isComplete": true,
  "isStable": true,
  "cyclesExecuted": 2,
  "durationMs": 120,
  "finalValues": {
    "EST_IPVTYPE": "D",
    "EST_CIRCUIT": "LS",
    "EST_DISENG": "H",
    "EST_IPV": "N",
    "EST_LSSIGNAL": "D"
  },
  "errors": []
}
```

---

### Variant Tables

#### List All Variant Tables

```http
GET /api/variant-tables
```

**Response**:
```json
[
  {
    "name": "T_VALID_COMBINATIONS",
    "description": "Valid characteristic combinations",
    "databaseTableName": "VT_T_VALID_COMBINATIONS",
    "rowCount": 12,
    "isTableCreated": true,
    "columns": [
      {
        "characteristicName": "EST_IPVTYPE",
        "sqlDataType": "VARCHAR(255)",
        "isKeyColumn": true
      },
      {
        "characteristicName": "EST_CIRCUIT",
        "sqlDataType": "VARCHAR(255)",
        "isKeyColumn": true
      }
    ]
  }
]
```

#### Create Variant Table

```http
POST /api/variant-tables
```

**Request Body**:
```json
{
  "name": "T_MY_TABLE",
  "description": "My custom table",
  "columns": [
    {
      "characteristicName": "COLOR",
      "sqlDataType": "VARCHAR(50)",
      "isKeyColumn": true
    },
    {
      "characteristicName": "SIZE",
      "sqlDataType": "VARCHAR(50)",
      "isKeyColumn": true
    },
    {
      "characteristicName": "PRICE",
      "sqlDataType": "VARCHAR(50)",
      "isKeyColumn": false
    }
  ],
  "rows": [
    ["Red", "Small", "100"],
    ["Red", "Large", "120"],
    ["Blue", "Small", "105"]
  ]
}
```

**Response**:
```http
201 Created
Location: /api/variant-tables/T_MY_TABLE
```

```json
{
  "message": "Variant table 'T_MY_TABLE' created successfully"
}
```

---

## Database

The API uses SQLite for storage. The database file is specified in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=saplo-vc.db"
  }
}
```

The database is automatically created on startup if it doesn't exist.

---

## Architecture

- **Minimal API**: Modern ASP.NET Core minimal APIs
- **Entity Framework Core**: ORM with SQLite
- **Swagger/OpenAPI**: Auto-generated API documentation
- **Dependency Injection**: Built-in DI container
- **CORS**: Enabled for development

---

## Testing with cURL

### Execute Configuration

```bash
curl -X POST http://localhost:5000/api/configurations/SAMPLE_MAT/execute | json_pp
```

### Get Material

```bash
curl http://localhost:5000/api/materials/SAMPLE_MAT | json_pp
```

### List Variant Tables

```bash
curl http://localhost:5000/api/variant-tables | json_pp
```

---

## Next Steps

1. **Add Authentication**: Implement JWT or API key authentication
2. **Add Validation**: FluentValidation for request DTOs
3. **Add Pagination**: For list endpoints
4. **Add Caching**: Redis or in-memory caching
5. **Add Rate Limiting**: Protect against abuse
6. **Add Logging**: Structured logging with Serilog
7. **Add Tests**: Integration tests for API endpoints
8. **Add Docker**: Containerization support

---

## Technologies

- **.NET 10.0**
- **ASP.NET Core Minimal APIs**
- **Entity Framework Core 10.0**
- **SQLite**
- **Swashbuckle (Swagger)**
