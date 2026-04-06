## Corporate Application Checklist (v1)

### 1) Core Product Scope

#### Identity and Access

- [x] Register / Login
- [x] JWT authentication
- [ ] Refresh token with rotation and revocation
- [ ] Role-based access control (RBAC)
  - [ ] Super Admin
  - [ ] Admin
  - [ ] Manager
  - [ ] User
  - [ ] Viewer
- [ ] Permission matrix per endpoint/action
- [x] Password hashing (BCrypt)
- [ ] Password policy and account lockout
- [ ] SSO (OIDC/SAML) integration
- [ ] MFA for privileged roles

#### Task and Project Operations

- [x] Create task
- [x] Assign user
- [x] Update task
- [x] Delete task (soft delete)
- [x] Status workflow
  - [x] Todo
  - [x] In Progress
  - [x] Done
- [ ] Priority policy
  - [ ] Low / Medium / High
- [x] Filtering
  - [x] By status
  - [x] By assigned user
- [ ] Pagination and sorting
- [ ] Due dates and SLA breach rules
- [ ] Bulk actions (bulk assign, bulk status update)

#### Analytics and Reporting

- [x] Count tasks per user
- [x] Active tasks
- [x] Completed tasks
- [x] Overdue tasks (optional but impressive)
- [ ] Workload by user/team
- [ ] Pending vs completed trends
- [ ] Overdue task analytics
- [ ] Executive dashboard endpoint

#### AI Capability

- [x] Tasks per user
- [x] Pending vs completed
- [x] Workload distribution
- [x] Total tasks
- [x] Total users

### 🤖 AI Feature — Smart Assignment

- [ ] Suggest best user
- [ ] Suggest priority
- [ ] Deterministic input/output contract
- [ ] Explainable output reason
- [ ] Fallback logic when AI fails

---

## 2) Corporate API Baseline

### Auth

- [x] POST `/auth/register`
- [x] POST `/auth/login`
- [ ] POST `/auth/refresh`
- [ ] POST `/auth/logout`
- [ ] GET `/auth/me`

### Users

- [ ] GET `/users`
- [ ] GET `/users/:id`
- [ ] GET `/users/:id/workload`

### Projects

- [x] GET `/project`
- [ ] GET `/project/:id`
- [x] POST `/project`
- [ ] PUT `/project/:id`
- [ ] DELETE `/project/:id`

### Tasks

- [x] POST `/tasks`
- [x] GET `/tasks`
- [x] GET `/tasks/:id`
- [x] PUT `/tasks/:id`
- [x] DELETE `/tasks/:id`
- [x] PATCH `/tasks/:id/status`
- [x] PATCH `/tasks/:id/assign`

### Dashboard and Admin

- [x] GET `/dashboard`

### AI

- [ ] POST `/ai/suggest-assignment`

### Dashboard

- [ ] GET `/dashboard`
- [ ] GET `/audit-logs`
- [ ] POST `/ai/suggest-assignment`

---

## 3) Backend Production Checklist

### Security and Compliance (Must)

- [x] Authentication (JWT)
- [ ] Authorization (roles + ownership)
  - [ ] Role-based access
  - [ ] Resource ownership
- [x] Validation (FluentValidation)
- [ ] Logging (Serilog or basic)
- [X] CORS
- [X] Exception middleware
- [x] Global exception middleware
- [x] Standard API response format
- [x] CORS policy
- [ ] Secrets management strategy
- [ ] Data encryption at rest and in transit
- [ ] Audit logs for auth and data changes
- [ ] Data retention and deletion policy
- [ ] Security scanning (SAST/dependency)

### Reliability and Performance

- [ ] Pagination + filtering (`?status=todo&assignedTo=5&page=1&pageSize=10`)
- [ ] Seeding (test data)
- [ ] API versioning (`/api/v1`)
- [x] Rate limiting
- [x] Soft delete

### 🔥 BONUS

- [ ] Caching (Redis)

---

## ⚔️ Team Split Strategy

### 🧠 You (Lead / Core / Hard)

- [ ] Authorization (roles + ownership)
  - [ ] Role-based access
  - [ ] Resource ownership
- [ ] Refresh token system
- [x] Exception middleware (global)
  - [x] Standard API response format
- [ ] Transactions
- [ ] Concurrency (RowVersion)
- [ ] Indexing (DB performance)
  - [ ] Task.Status
  - [ ] Task.AssignedUserId
  - [ ] Task.ProjectId
- [x] Soft delete
- [ ] AI assignment (full logic)
  - [ ] Fallback logic
  - [ ] Deterministic API shape
- [ ] SignalR integration
- [ ] Activity log system
- [ ] Logging (Serilog)
- [ ] Testing (xUnit)
- [x] Rate limiting
- [x] Soft delete
- [ ] Pagination + filtering + sorting (`?status=todo&assignedTo=5&page=1&pageSize=10&sortBy=createdAt&sortDir=desc`)
- [ ] Caching (Redis)
- [ ] Backup and restore drill

### Observability and Operations

- [x] Update task
- [x] Delete task
- [ ] GET `/tasks/:id`
- [ ] GET `/users`
- [ ] GET `/users/:id`
- [x] Tasks per user
- [x] Pending vs completed
- [x] Total counts
- [x] Workload distribution
- [x] GET `/dashboard`
- [x] Count tasks per user
- [x] Active tasks
- [x] Completed tasks
- [x] Overdue tasks
- [ ] Pagination
- [ ] Combine filtering + pagination
- [ ] Backend → Render
- [ ] DB → PostgreSQL
- [ ] Basic env setup
- [ ] Seeding (5 users / 20 tasks / 2 projects)
- [ ] Basic notifications
- [ ] Priority field
- [ ] Structured logging (Serilog)
- [ ] Correlation ID middleware
- [ ] Centralized log sink
- [ ] Health checks (liveness/readiness)
- [ ] Metrics and dashboard (latency/error rate)
- [ ] Alerting and on-call rules

### Delivery and Governance

- [ ] API versioning (`/api/v1`)
- [ ] Seeding strategy for non-production
- [ ] Unit tests (xUnit)
- [ ] Integration tests
- [ ] CI/CD with quality gates
- [ ] Environment promotion (dev/staging/prod)
- [ ] Rollback strategy

---

## 4) Team Delivery Plan (Corporate)

### Lead Ownership (Platform/Security)

- [ ] Authorization and RBAC
- [ ] Token lifecycle (refresh/revoke)
- [x] Exception middleware + response standard
- [ ] Transactions + concurrency
- [ ] Logging + observability stack
- [ ] Security/compliance controls

### Teammate Ownership (Feature/API)

- [ ] Users endpoints
- [ ] Task details endpoint
- [ ] Dashboard endpoint
- [ ] Pagination/filter/sort wiring
- [ ] Workload analytics endpoints
- [ ] Seed data for dev/test

### Working Agreement

- [ ] API contracts first (DTO and swagger)
- [ ] Consistent response shape: `{ success, data, message, errors }`
- [ ] Shared code review checklist
- [ ] Definition of done includes tests and docs

---

## 5) Architecture Snapshot

- [ ] Frontend (React)
- [x] Backend (.NET API)
- [x] PostgreSQL
- [ ] Redis cache
- [ ] AI provider integration

---

## 📊 Class Diagram

```mermaid
classDiagram
    class User {
        +int Id
        +string Username
        +string Email
        +string PasswordHash
        +string Role
        +Register()
        +Login()
    }
    
    class Project {
        +int Id
        +string Name
        +string Description
        +int OwnerId
        +CreateProject()
    }
    
    class Task {
        +int Id
        +string Title
        +string Description
        +string Status
        +string Priority
        +int ProjectId
        +int AssignedUserId
        +UpdateStatus()
        +AssignUser()
    }
    
    User "1" -- "*" Project : Owns
    User "1" -- "*" Task : Assigned To
    Project "1" -- "*" Task : Contains
  ```
