# Navigation Platform

This project is a navigation module built with .NET 8 and Clean Architecture.  
It allows users to create, manage, and share their journeys, while administrators have additional tools to oversee the system.

## Features

### User functionality
- Register and login with JWT authentication, logout included
- Create, delete, and retrieve journeys (with start/end locations, times, transport type, and distance)
- Daily goal achievement: a badge is awarded when the user travels more than 20 km in a single day (covered by unit tests at 19.99, 20.00, and 20.01 km)
- Share journeys with friends or by generating a public link, with the option to revoke it
- Audit log for share and unshare actions

### Administrator functionality
- Filter journeys by user, transport type, and dates, with pagination and sorting
- Display total route distance per user on a monthly basis
- Change user status (Active, Suspended, Deactivated) with auditing and event publishing

## Architecture and technologies

- Clean Architecture: Domain → Application → Infrastructure → Presentation
- .NET 8 with Entity Framework Core 8
- MediatR for in-process messaging
- RabbitMQ for event publishing and background worker
- Serilog for structured logs with correlation IDs
- FluentValidation for validation
- Health endpoints at /healthz and /readyz
- Unit tests with xUnit

## Current status

What has been done:
- Journey CRUD operations
- Daily goal worker with unit tests
- Sharing with audit logs
- Admin filtering, reporting, and user management
- Validation, logging, and health checks
- RabbitMQ integration (running as Windows service, not Docker)

What still needs to be improved:
- Add Dockerfiles and docker-compose for APIs, database, and RabbitMQ
- Add CI/CD pipeline with GitHub Actions
- Increase unit test coverage to 70% or higher
- Add caching
- Improve security by moving secrets to environment variables and adding rate limiting
- Add observability (metrics and tracing)
- Split the monolith into separate services (Journey Service, Reward Worker, User Service, API Gateway)

## RabbitMQ setup

RabbitMQ is currently running as a native Windows service with the management plugin enabled.  
Default connection settings in appsettings.json:

```json
"RabbitMQ": {
  "HostName": "localhost",
  "Port": 5672,
  "UserName": "guest",
  "Password": "guest",
  "VirtualHost": "/"
}
