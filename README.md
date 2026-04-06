## 🎯 CORE (MUST COMPLETE)

## 🔐 Auth & Security

- [x] Register / Login
- [x] JWT authentication
- [ ] Role-based access (RBAC)
  - [ ] Admin / Manager / User
- [ ] Ownership check (user can access only their tasks)
- [x] Password hashing (BCrypt)
- [ ] Refresh token *(optional but strong)*

---

## 📋 Task + Project System (MAIN CORE)

- [x] Create task
- [x] Assign user
- [x] Update task
- [x] Soft delete task
- [x] Status workflow:
  - Todo / In Progress / Done / add Custom status

### 🔥 ADD THIS (VERY IMPORTANT)

- [ ] Priority (Low / Medium / High)
- [ ] Due date
- [ ] Pagination (`?page=1&pageSize=10`)
- [ ] Sorting (`?sortBy=createdAt`)

---

## 🧩 REAL JIRA FEATURES (YOU WERE MISSING)

### ✅ Comments System

- [ ] Add comment to task
- [ ] Get task comments

### ✅ Activity Log (VERY IMPORTANT)

- [ ] Track:
  - task created
  - assigned
  - status changed

### ✅ Checklist / Subtasks (🔥 HIGH VALUE)

- [ ] Task checklist items
- [ ] Mark complete/incomplete

---

## 👥 Workload & Dashboard

- [x] Tasks per user
- [x] Active tasks
- [x] Completed tasks

### Improve:

- [ ] Overdue tasks
- [ ] Total tasks / users
- [ ] Workload distribution

---

## 🤖 AI Feature (KEEP SIMPLE)

- [ ] Suggest best user
- [ ] Suggest priority
- [ ] Return explanation

### 🔥 Add fallback:

Assign user with least tasks

---

## 🌐 Deployment

- [x] Docker (API + DB)
- [ ] Deploy backend (Render)
- [ ] PostgreSQL (cloud)

---

# ⚡ PHASE 2 (IF TIME)

## 🔔 Notifications

- [ ] Notify on assignment
- [ ] Notify on comment

## 🔍 Search

- [ ] Search tasks by title

## 🏷️ Labels

- [ ] Tag tasks

---

# 🧠 Backend Must-Haves (KEEP CLEAN)

## ✅ Required

- [x] JWT auth
- [ ] Authorization (roles + ownership)
- [x] Validation
- [x] Exception middleware
- [x] CORS
- [ ] Logging (basic is enough)

## ⚡ Important

- [ ] Pagination + filtering
- [ ] API versioning `/api/v1`
- [ ] Seeding

## 🔥 Bonus

- [ ] Redis caching
- [ ] Rate limiting

---

# 🧠 API FIXES (YOU MISSED)

Add:

```text
GET    /tasks/:id
GET    /tasks?status=&assignedTo=&page=
POST   /tasks/:id/comments
GET    /tasks/:id/comments
GET    /activity
```

---

# ⚔️ Team Split (FIXED)

## 🧠 YOU (CORE / HARD)

- Auth + JWT
- RBAC + ownership
- Activity log
- AI logic
- Pagination + filtering
- Docker + infra

## 👤 TEAMMATE

- Project APIs
- Comments
- Dashboard
- User APIs
- Seeding
- Swagger / Postman

---

# 🧠 Final Reality Check

If you complete:

✅ Auth
✅ Task system
✅ Comments
✅ Activity log
✅ Dashboard
✅ Docker

---

👉 You already beat **80–90% students**

---

# 🔥 What Makes Your Project “Stand Out”

These 3:

1. Activity log
2. Checklist/subtasks
3. AI suggestion

---

# ✅ Recommended Next Features

## 1) Task Checklists / Subtasks

- [ ] Add `ChecklistItem` entity linked to `TaskItem`
- [ ] Add checklist CRUD endpoints
- [ ] Mark checklist items complete/incomplete
- [ ] Keep checklist order
- [ ] Show task `% complete` rollup

## 2) Comments / Activity Feed / Audit Log

- [ ] Add comments per task
- [ ] Get task comments
- [ ] Track activity history for task changes
- [ ] Store audit logs for admin/compliance

## 3) RBAC + Permissions per Endpoint

- [ ] Add roles: Admin / Manager / User / Viewer
- [ ] Add permission matrix per endpoint/action
- [ ] Add project-level access control
- [ ] Use `[Authorize(Roles=...)]` where needed

## 4) Pagination + Sorting + Search

- [ ] Add paging to `GET /tasks`
- [ ] Add sorting by due date, priority, and created date
- [ ] Add search by title and description
- [ ] Keep filtering + pagination together

## 5) More Workflow Depth

- [ ] Add custom workflows per project
- [ ] Add validation rules for task transitions
- [ ] Add extra statuses like Blocked / In Review / QA

## 6) Labels, Attachments, Watchers, Notifications

- [ ] Add labels/tags
- [ ] Add file attachments
- [ ] Add watchers / @mentions
- [ ] Add email / Slack / Teams notifications

## 7) Bulk Actions

- [ ] Bulk assign tasks
- [ ] Bulk change status
- [ ] Bulk close tasks

## 8) Ownership + Multi-Tenancy

- [ ] Add CreatedBy / UpdatedBy metadata
- [ ] Add organization / tenant boundaries if needed
- [ ] Enforce ownership and visibility rules

---

# 🐳 Run With Docker

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