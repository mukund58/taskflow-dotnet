# GDG Taskboard

> A production-grade, Jira-style task management workspace built for fast-moving teams and hackathon execution.

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](./LICENSE)
[![Next.js](https://img.shields.io/badge/Next.js-16-black?logo=next.js)](https://nextjs.org)
[![.NET](https://img.shields.io/badge/.NET-9-purple?logo=dotnet)](https://dotnet.microsoft.com)
[![Docker](https://img.shields.io/badge/Docker-Compose-blue?logo=docker)](https://www.docker.com)

---

## 📌 Project Overview

**GDG Taskboard** is a full-stack task management platform that combines project ownership, task lifecycle management, team collaboration, and real-time dashboard analytics in a single clean UI.

It was built as part of the **GDG Hackathon 2026** to demonstrate a production-quality, end-to-end web application using modern tools and best practices.

---

## 🧩 Problem It Solves

Hackathon teams and fast-moving engineering squads often lack lightweight tools that are:

- **Easy to spin up** — no complex enterprise setup
- **Feature-rich** — real task tracking, not just a to-do list
- **Collaborative** — comments, mentions, watchers, invitations
- **Insightful** — workload distribution, progress metrics, overdue tracking

GDG Taskboard fills this gap: it's a mini Jira that you can self-host in minutes.

---

## ✨ Features

### 🔐 Auth & Access
- Register & login with JWT authentication
- Role-based access control (Admin, Manager, User, Viewer)
- Ownership and project-level access rules
- Secure password hashing with BCrypt

### ✅ Task Management
- Full CRUD with status, priority, due dates, and soft delete
- Pagination, filtering (status, assignee), and sorting
- Task assignment, status transitions (Todo → In Progress → Done)
- Optimistic concurrency with HTTP 409 conflict detection

### 🗂️ Project & Team Collaboration
- Project CRUD with member management and invitations
- Task comments with `@mention` support
- Email & Slack/Teams webhook notifications on mentions
- Task activity history and audit log
- Checklists with drag-to-reorder, completion toggle, and progress summary
- Labels, attachments (upload/download), and task watchers

### 📊 Dashboard & Analytics
- Real-time metrics: total, active, completed, overdue tasks
- Tasks by status and priority breakdown
- Per-user workload distribution charts
- Redis-backed caching for performance

<!-- ### 🤖 AI Suggestions
- AI-powered task assignment suggestions (assignee + priority + explanation) -->

### 🖥️ Frontend UX
- Modern landing page with full responsive design (mobile + desktop)
- Kanban board view + table view toggle
- Skeleton loaders, empty states, and error states
- Debounced search with URL-synced query state
- Optimistic updates for fast interactions

---

## 🗂️ Repository Structure

```
Task-flow/
├── Backend/           # .NET 9 Web API (ASP.NET Core)
│   ├── Controllers/   # API controllers
│   ├── Services/      # Business logic
│   ├── Models/        # EF Core models
│   ├── Data/          # DbContext & migrations
│   ├── Middleware/    # Global error handling, logging
│   └── docker-compose.yml
├── Backend.Tests/     # xUnit test project
├── frontend/          # Next.js 16 (App Router)
│   ├── app/           # Pages & routes
│   ├── components/    # Reusable UI components
│   ├── services/      # API client modules
│   ├── store/         # Zustand state management
│   └── types/         # TypeScript types
└── README.md
```

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| **Frontend** | Next.js 16, TypeScript, Tailwind CSS, Zustand, TanStack Query, Recharts |
| **Backend** | .NET 9, ASP.NET Core Web API, Entity Framework Core |
| **Database** | PostgreSQL |
| **Cache** | Redis |
| **Auth** | JWT Bearer Tokens |
| **Notifications** | SMTP Email, Slack Webhook, Teams Webhook |
| **Testing** | xUnit |
| **Containerization** | Docker, Docker Compose |
| **CI/CD** | GitHub Actions → Azure App Service |

---

## 🚀 Getting Started

### Prerequisites

- [Docker & Docker Compose](https://docs.docker.com/get-docker/)
- [Node.js 20+](https://nodejs.org/) (for local frontend dev)
- [.NET 9 SDK](https://dotnet.microsoft.com/download) (for local backend dev)

---

### 1. Clone the repository

```bash
git clone https://github.com/mukund58/GDG-Hackathon.git
cd GDG-Hackathon
```

---

### 2. Backend Setup (Docker — recommended)

```bash
cd Backend
```

Create a `.env` file in the `Backend/` directory:

```env
# Required
POSTGRES_DB=gdgtaskboard
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_password
CONNECTION_STRING=Host=db;Port=5432;Database=gdgtaskboard;Username=postgres;Password=your_password

# Optional
REDIS_CONNECTION_STRING=redis:6379
FRONTEND_BASE_URL=http://localhost:3000
INVITATION_TOKEN_SECRET=your_secret_here

# SMTP (for email notifications)
SMTP_HOST=smtp.example.com
SMTP_PORT=587
SMTP_USERNAME=your@email.com
SMTP_PASSWORD=your_smtp_password
SMTP_FROM_ADDRESS=no-reply@taskboard.dev
SMTP_FROM_NAME=GDG Taskboard
SMTP_USE_SSL=true

# Webhooks (optional)
SLACK_WEBHOOK_URL=https://hooks.slack.com/...
TEAMS_WEBHOOK_URL=https://outlook.office.com/webhook/...
```

Start all services (API + PostgreSQL + Redis):

```bash
docker compose up --build
```

| Service | URL |
|---|---|
| API | http://localhost:5000 |
| Swagger UI | http://localhost:5000/swagger |

Stop services:

```bash
docker compose down

# To also remove the database volume:
docker compose down -v
```

---

### 3. Frontend Setup

```bash
cd frontend
```

Create a `.env.local` file:

```env
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
NEXT_PUBLIC_API_PATH_PREFIX=/api/v1
```

Install dependencies and start the dev server:

```bash
npm install
npm run dev
```

Open **http://localhost:3000** in your browser.

---

### 4. Local Development (without Docker)

**Backend:**

```bash
# Ensure PostgreSQL is running and CONNECTION_STRING is set in your environment
dotnet run --project Backend/Backend.csproj
```

**Run tests:**

```bash
dotnet test GDG-Hackathon.sln
```

---

## 📚 Documentation

| Resource | Link |
|---|---|
| **Backend API Reference** | [Backend/README.md](./Backend/README.md) |
| **Frontend Dev Checklist** | [frontend/README.md](./frontend/README.md) |
| **Swagger UI** | http://localhost:5000/swagger (when running) |
| **Contributing Guide** | [CONTRIBUTING.md](./CONTRIBUTING.md) |
| **Code of Conduct** | [CODE_OF_CONDUCT.md](./CODE_OF_CONDUCT.md) |

### Key API Endpoints

```
POST   /api/v1/auth/register
POST   /api/v1/auth/login
GET    /api/v1/dashboard
GET    /api/v1/tasks?page=1&status=&assignedTo=&sortBy=
POST   /api/v1/tasks
GET    /api/v1/projects
POST   /api/v1/projects
GET    /api/v1/notifications
```

> Full endpoint list available in [Backend/README.md](./Backend/README.md)

---

## ☁️ Deployment (Azure)

This repo includes GitHub Actions workflows for continuous deployment to **Azure App Service**.

See [Backend/README.md → Azure App Service CD](./Backend/README.md#azure-app-service-cd-backend--frontend--postgresql) for required secrets and deployment behavior.

---

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](./CONTRIBUTING.md) for guidelines on submitting pull requests.

---

## 📄 License

This project is licensed under the **GNU General Public License v3.0** — see [LICENSE](./LICENSE) for details.

---
