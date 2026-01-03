# Metrics API Service

A .NET 8 Web API service for receiving and storing metrics with PostgreSQL database.

## Features

- RESTful API endpoint for receiving metrics (MetricValue, WriteTimeMs, CreatedAt)
- API Key authentication
- PostgreSQL database storage
- Docker Compose setup with persistent database storage
- **Audit logging system** - Comprehensive event tracking and monitoring
- **Health check endpoint** - Monitor application and database status

## Prerequisites

- Docker and Docker Compose
- .NET 8 SDK (for local development)

## Setup

### 1. Configure API Key and Pulse Multiplier

Update the `ApiKey` and `PulseMultiplier` values in `docker-compose.yml` or `appsettings.json`:

```yaml
environment:
  - ApiKey=your-secret-api-key-change-this-in-production
  - PulseMultiplier=1.0
```

The `PulseMultiplier` is used to convert pulse counts to actual meter readings. For example, if 1 pulse = 0.1 units, set `PulseMultiplier=0.1`.

### 2. Run with Docker Compose

```bash
docker-compose up -d
```

The service will be available at:
- HTTP: http://localhost:8080

## API Usage

**Note:** All API endpoints (except `/api/health` and Swagger) require API key authentication via the `X-API-Key` header.

### Endpoint: POST /api/metrics

Send one or more metrics with API key authentication. The endpoint accepts an array of metrics:

```bash
# Send a single metric
curl -X POST http://localhost:8080/api/metrics \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-secret-api-key-change-this-in-production" \
  -d '[{
    "metricValue": 25,
    "writeTimeMs": 100
  }]'

# Send multiple metrics at once
curl -X POST http://localhost:8080/api/metrics \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-secret-api-key-change-this-in-production" \
  -d '[
    {
      "metricValue": 25,
      "writeTimeMs": 100
    },
    {
      "metricValue": 60,
      "writeTimeMs": 95
    }
  ]'
```

### Get Metrics: GET /api/metrics

```bash
curl -X GET http://localhost:8080/api/metrics \
  -H "X-API-Key: your-secret-api-key-change-this-in-production"
```

### Get Single Metric: GET /api/metrics/{id}

```bash
curl -X GET http://localhost:8080/api/metrics/1 \
  -H "X-API-Key: your-secret-api-key-change-this-in-production"
```

### Get Meter Data: GET /api/metrics/meter-data

Get the total meter reading by summing all pulse counts and multiplying by the configured PulseMultiplier:

```bash
curl -X GET http://localhost:8080/api/metrics/meter-data \
  -H "X-API-Key: your-secret-api-key-change-this-in-production"
```

Response:
```json
{
  "totalPulseCount": 1500,
  "pulseMultiplier": 1.0,
  "meterData": 1500.0
}
```

## Audit & Monitoring

### Health Check: GET /api/health

Check application and database health:

```bash
curl -X GET http://localhost:8080/api/health
```

### Get Audit Events: GET /api/audit

Query audit logs with filtering options:

```bash
# Get all audit events
curl -X GET http://localhost:8080/api/audit

# Filter by event type
curl -X GET "http://localhost:8080/api/audit?eventType=HealthCheck"

# Filter by date range
curl -X GET "http://localhost:8080/api/audit?startDate=2024-01-01T00:00:00Z&endDate=2024-01-31T23:59:59Z"

# Filter by source IP
curl -X GET "http://localhost:8080/api/audit?source=192.168.1.100"

# Pagination
curl -X GET "http://localhost:8080/api/audit?page=1&pageSize=50"
```

### Get Audit Event Types: GET /api/audit/types

List all available event types:

```bash
curl -X GET http://localhost:8080/api/audit/types
```

### Get Audit Statistics: GET /api/audit/stats

Get statistics about audit events:

```bash
curl -X GET http://localhost:8080/api/audit/stats
```

### Tracked Events

The audit system automatically tracks the following events:

- **Connected** - Application startup and database connection established
- **HealthCheck** - Health check endpoint calls (successful/failed)
- **ApiRequest** - All API requests with method, path, status code, and duration
- **MetricCreated** - Successful metric creation events
- **AuthenticationSuccess** - Successful API key authentication
- **AuthenticationFailure** - Failed authentication attempts (missing or invalid API key)
- **Error** - Application errors and exceptions

#### Recommended Additional Events (for future implementation):

- **DatabaseConnectionLost** - Database connectivity issues
- **DatabaseConnectionRestored** - Database reconnection events
- **RateLimitExceeded** - API rate limiting events
- **ConfigurationChanged** - Application configuration updates
- **Shutdown** - Application shutdown events

## Database

PostgreSQL data is persisted in `./postgres-data` directory on the host machine (not just in a Docker volume).

## Development

To run locally without Docker:

1. Install PostgreSQL and create a database
2. Update connection string in `appsettings.json`
3. Run: `dotnet run`

## Security Notes

- Change the default API key in production
- Consider using environment variables for sensitive configuration
- For production, consider enabling HTTPS with proper certificates

