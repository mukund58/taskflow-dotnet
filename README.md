# GDG Hackathon Backend Status

Last synced with backend code: 2026-04-09

This README reflects what is currently implemented in the backend code under Backend.

## Core Progress

### Auth and Security

- [x] Register and login
- [x] JWT authentication
- [x] Role-based access control (Admin, Manager, User, Viewer)
- [x] Access checks using role + ownership/membership rules
- [x] Password hashing with BCrypt
- [x] Soft-deleted users blocked from auth/profile/settings flows
- [ ] Refresh token flow

### Task and Project Core

- [x] Task CRUD
- [x] Task assignment endpoint
- [x] Task status updates (Todo, In Progress, Done)
- [x] Task priority (Low, Medium, High)
- [x] Task due date
- [x] Project due date
- [x] Task soft delete
- [x] Pagination on task list
- [x] Filtering by status and assigned user
- [x] Sorting (createdAt, dueDate, priority, title, status)
- [x] Project CRUD
- [x] Project members and invitations
- [ ] Search tasks by title and description
- [ ] Custom per-project workflow statuses

### Jira-Style Collaboration

- [x] Task comments (add/list/update/delete)
- [x] @mentions in comments
- [x] In-app mention notifications
- [x] Email mention notifications (SMTP)
- [x] Slack/Teams webhook notifications (best effort)
- [x] Task activity history (TaskCreated, Assigned, StatusChanged)
- [x] Task checklist CRUD
- [x] Checklist completion toggle
- [x] Checklist reorder
- [x] Checklist completion summary
- [x] Labels CRUD by project
- [x] Assign/remove labels on tasks
- [x] Task attachments metadata + upload + download
- [x] Task watchers
- [x] User notifications API (list/unread/read/delete)

### Dashboard and Workload

- [x] Dashboard endpoint
- [x] Total tasks and total users
- [x] Active/completed/overdue metrics
- [x] Tasks by status and priority
- [x] Tasks per user and workload distribution
- [x] Redis-backed dashboard caching

### Platform and Quality

- [x] API versioning (v1 URL segment)
- [x] FluentValidation
- [x] Global exception middleware with consistent response format
- [x] Optimistic concurrency (Task RowVersion + HTTP 409 on conflict)
- [x] DB indexes for task status/assignee/project
- [x] Transaction-wrapped task writes
- [x] Rate limiting
- [x] Serilog request and file logging
- [x] CORS enabled
- [x] Startup migration + schema drift repair
- [x] Seed data support
- [x] xUnit test project
- [ ] AI assignment endpoint
- [ ] SignalR realtime updates

## API Endpoints (Current)

Most controllers expose both versioned routes (/api/v1/...) and compatibility routes (/api/...).

### Auth

- POST /api/v1/auth/register
- POST /api/v1/auth/login

### Users, Profile, Settings

- GET /api/v1/users (Admin, Manager)
- GET /api/v1/users/{id}
- GET /api/v1/profile
- PUT /api/v1/profile
- PUT /api/v1/profile/change-password
- DELETE /api/v1/profile
- GET /api/v1/settings
- PUT /api/v1/settings

### Projects

- GET /api/v1/projects
- POST /api/v1/projects
- GET /api/v1/projects/{id}
- PUT /api/v1/projects/{id}
- DELETE /api/v1/projects/{id}
- GET /api/v1/projects/{id}/members
- POST /api/v1/projects/{id}/members
- GET /api/v1/projects/{id}/invitations
- POST /api/v1/projects/{id}/invitations

### Tasks

- POST /api/v1/tasks
- GET /api/v1/tasks?page=1&pageSize=10&status=&assignedTo=&sortBy=&sortDescending=
- GET /api/v1/tasks/{id}
- PUT /api/v1/tasks/{id}
- DELETE /api/v1/tasks/{id}
- PATCH /api/v1/tasks/{id}/status
- PATCH /api/v1/tasks/{id}/assign
- GET /api/v1/tasks/{id}/activity
- PATCH /api/v1/tasks/{id}/checklist/{checklistItemId}

### Checklist

- POST /api/v1/tasks/{taskId}/checklist
- GET /api/v1/tasks/{taskId}/checklist
- GET /api/v1/tasks/{taskId}/checklist/summary
- PUT /api/v1/tasks/{taskId}/checklist/{checklistItemId}
- PATCH /api/v1/tasks/{taskId}/checklist/{checklistItemId}/toggle
- DELETE /api/v1/tasks/{taskId}/checklist/{checklistItemId}
- POST /api/v1/tasks/{taskId}/checklist/reorder

### Comments

- POST /api/v1/tasks/{taskId}/comments
- GET /api/v1/tasks/{taskId}/comments
- PUT /api/v1/tasks/{taskId}/comments/{commentId}
- DELETE /api/v1/tasks/{taskId}/comments/{commentId}

### Labels

- GET /api/v1/projects/{projectId}/labels
- POST /api/v1/projects/{projectId}/labels
- GET /api/v1/projects/{projectId}/labels/{labelId}
- PUT /api/v1/projects/{projectId}/labels/{labelId}
- DELETE /api/v1/projects/{projectId}/labels/{labelId}
- POST /api/v1/projects/{projectId}/labels/tasks/{taskId}/assign?labelId=
- DELETE /api/v1/projects/{projectId}/labels/tasks/{taskId}/remove?labelId=
- GET /api/v1/projects/{projectId}/labels/tasks/{taskId}

### Attachments

- GET /api/v1/tasks/{taskId}/attachments
- GET /api/v1/tasks/{taskId}/attachments/{attachmentId}
- GET /api/v1/tasks/{taskId}/attachments/{attachmentId}/download
- POST /api/v1/tasks/{taskId}/attachments
- POST /api/v1/tasks/{taskId}/attachments/upload
- DELETE /api/v1/tasks/{taskId}/attachments/{attachmentId}

### Watchers

- POST /api/v1/tasks/{taskId}/watchers
- POST /api/v1/tasks/{taskId}/watchers/add-user?userId=
- DELETE /api/v1/tasks/{taskId}/watchers
- DELETE /api/v1/tasks/{taskId}/watchers/remove-user?userId=
- GET /api/v1/tasks/{taskId}/watchers
- GET /api/v1/tasks/{taskId}/watchers/my-watched-tasks
- GET /api/v1/tasks/{taskId}/watchers/is-watching

### Notifications

- GET /api/v1/notifications?page=1&pageSize=20
- GET /api/v1/notifications/unread
- PUT /api/v1/notifications/{notificationId}/read
- PUT /api/v1/notifications/read-multiple
- DELETE /api/v1/notifications/{notificationId}
- DELETE /api/v1/notifications

### Dashboard

- GET /api/v1/dashboard (Admin, Manager)

## Gaps and Next Priorities

- Refresh token issue/rotate endpoint and persistence
- AI assignment endpoint with deterministic response
- Search filter on task title/description
- Custom workflow engine (Blocked/In Review/QA, per project)
- Bulk actions (assign/status/close)
- SignalR realtime notification channel
- Audit log endpoints beyond per-task activity stream

## Run with Docker

This project uses Docker Compose in the Backend folder and starts API + PostgreSQL + Redis.

1. Go to backend folder:

   ```bash
   cd Backend
   ```

2. Create Backend/.env with at least:

   - POSTGRES_DB
   - POSTGRES_USER
   - POSTGRES_PASSWORD
   - CONNECTION_STRING

   Optional:

   - REDIS_CONNECTION_STRING
   - SMTP_HOST
   - SMTP_PORT
   - SMTP_USERNAME
   - SMTP_PASSWORD
   - SMTP_FROM_ADDRESS
   - SMTP_FROM_NAME
   - SMTP_USE_SSL
   - SLACK_WEBHOOK_URL
   - TEAMS_WEBHOOK_URL

3. Start services:

   ```bash
   docker compose up --build
   ```

4. Open:

   - API: http://localhost:5000
   - Swagger: http://localhost:5000/swagger

5. Stop:

   ```bash
   docker compose down
   ```

   Remove DB volume too:

   ```bash
   docker compose down -v
   ```

## Local Dev (Without Docker)

- Ensure PostgreSQL is running and CONNECTION_STRING is set
- Optionally set REDIS_CONNECTION_STRING
- Run API:

  ```bash
  dotnet run --project Backend/Backend.csproj
  ```

## Tests

Run backend tests:

```bash
dotnet test GDG-Hackathon.sln
```
