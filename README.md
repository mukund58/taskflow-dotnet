## 🎯 EXACT FEATURES (FINAL VERSION)

### 🟢 CORE (MUST HAVE — non-negotiable)

### 🔐 Auth System

- [x] Register / Login
- [x] JWT based auth
- [ ] Refresh token (optional but 🔥 bonus)
- [ ] Roles:
  - [ ] Admin
  - [ ] User
- [x] Password hashing (BCrypt)

### 📋 Task Management (REAL CORE)

- [x] Create task
- [x] Assign user
- [x] Update task
- [x] Delete task
- [x] Status:
  - [x] Todo
  - [x] In Progress
  - [x] Done
- [ ] Priority:
  - [ ] Low / Medium / High
- [x] Filter:
  - [x] By status
  - [x] By assigned user
- [ ] Pagination (`?page=1&pageSize=10`)

### 👥 User Workload Tracking

- [ ] Count tasks per user
- [ ] Active tasks
- [ ] Completed tasks
- [ ] Overdue tasks (optional but impressive)

### 📊 Dashboard (KEEP SIMPLE BUT SMART)

- [ ] Tasks per user
- [ ] Pending vs completed
- [ ] Workload distribution
- [ ] Total tasks
- [ ] Total users

### 🤖 AI Feature — Smart Assignment

- [ ] Suggest best user
- [ ] Suggest priority
- [ ] Return explanation
- [ ] Deterministic input/output shape
- [ ] Use Gemini API
- [ ] Fallback logic (if AI fails -> basic logic)
  - Example: Assign user with least active tasks

### 🌐 Deployment

- [ ] Backend → Render
- [ ] Frontend → Vercel
- [ ] DB → PostgreSQL

### 🟡 GOOD TO HAVE

- [ ] Notifications (basic)
- [ ] AI prioritization

---

## ✅ Minimum APIs (UPDATED)

### Auth

- [x] POST `/auth/register`
- [x] POST `/auth/login`

### Users

- [ ] GET `/users`
- [ ] GET `/users/:id`

### Project

- [x] GET `/project`
- [ ] GET `/project/:id`
- [x] POST `/project`
- [ ] PUT `/project/:id`
- [ ] DELETE `/project/:id`

### Tasks

- [x] POST `/tasks`
- [x] GET `/tasks`
- [ ] GET `/tasks/:id`
- [x] PUT `/tasks/:id`
- [x] DELETE `/tasks/:id`
- [x] PATCH `/tasks/:id/status`
- [x] PATCH `/tasks/:id/assign`

### Dashboard

- [ ] GET `/dashboard`

### AI

- [ ] POST `/ai/suggest-assignment`

---

## 🧠 Backend Checklist (IMPORTANT FIXES)

### ✅ MUST

- [X] Authentication (JWT)
- [ ] Authorization (roles + ownership)
  - [ ] Role-based access
  - [ ] Resource ownership
- [X] Validation (FluentValidation)
- [ ] Logging (Serilog or basic)
- [X] CORS
- [X] Exception middleware

### ⚡ SHOULD HAVE

- [ ] Pagination + filtering (`?status=todo&assignedTo=5&page=1&pageSize=10`)
- [ ] Seeding (test data)
- [ ] API versioning (`/api/v1`)
- [X] Rate limiting
- [X] Soft delete

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
- [ ] Repository layer
- [ ] Indexing (DB performance)
  - [ ] Task.Status
  - [ ] Task.AssignedUserId
  - [ ] Task.ProjectId
- [X] Soft delete
- [ ] AI assignment (full logic)
  - [ ] Fallback logic
  - [ ] Deterministic API shape
- [ ] SignalR integration
- [ ] Activity log system
- [ ] Logging (Serilog)
- [ ] Testing (xUnit)

### 🧑‍💻 Teammate (Easy / Safe / Visible)

- [x] Update task
- [x] Delete task
- [ ] GET `/tasks/:id`
- [ ] GET `/users`
- [ ] GET `/users/:id`
- [ ] Tasks per user
- [ ] Pending vs completed
- [ ] Total counts
- [ ] Workload distribution
- [ ] GET `/dashboard`
- [ ] Count tasks per user
- [ ] Active tasks
- [ ] Completed tasks
- [ ] Overdue tasks
- [ ] Pagination
- [ ] Combine filtering + pagination
- [ ] Backend → Render
- [ ] DB → PostgreSQL
- [ ] Basic env setup
- [ ] Seeding (5 users / 20 tasks / 2 projects)
- [ ] Basic notifications
- [ ] Priority field

### 🔗 How To Work Together

- [ ] Define DTOs and APIs first
- [ ] Keep response format consistent: `{ success, data, message }`
- [ ] You own `Services/`, `Repositories/`, `Middleware/`
- [ ] Teammate owns `Controllers/`, `DTOs/`, dashboard wiring

### ⚡ Final Execution Plan

- [ ] Week 1: You handle auth, authorization, middleware; teammate handles CRUD and basic APIs
- [ ] Week 2: You handle transactions, repository, logging; teammate handles pagination and dashboard
- [ ] Week 3: You handle SignalR and AI; teammate handles deployment and seeding

---

## 🔥 What Makes Your Project Stand Out

### 1. Activity Log (IMPORTANT)

- [ ] Track actions:
  - [ ] task created
  - [ ] task assigned
  - [ ] status changed

### 2. Ownership Security

- [ ] User can only access their tasks

### 3. Clean API Responses

- [ ] Use DTOs
- [ ] No raw entities

---

## 🧱 Simple Architecture

- [ ] Frontend (React)
- [x] Backend (.NET API)
- [x] PostgreSQL
- [ ] Gemini API

---

## 🐳 Run With Docker

This project uses Docker Compose from the Backend folder. It starts the API and a PostgreSQL container together.

### 1. Go to the backend folder

```bash
cd Backend
```

### 2. Make sure `.env` exists

The backend expects these values in `Backend/.env`:

- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `CONNECTION_STRING`

### 3. Start the containers

```bash
docker compose up --build
```

### 4. Open the API

- API: `http://localhost:5000`
- Swagger: `http://localhost:5000/swagger`

### 5. Stop the containers

```bash
docker compose down
```

If you want to remove the database volume too:

```bash
docker compose down -v
```
