# GDG Hackathon Frontend

Frontend for the team task management platform.

This app is built with React + Vite and connects to the backend API for auth, task CRUD, workload views, dashboard metrics, and AI-based assignment suggestions.

## Stack

- React 19
- Vite 8
- ESLint 9

## Required Feature Scope

### Core

- Auth: register and login using JWT
- Role-aware UI: Admin and User
- Task management: create, list, edit, delete
- Task fields: assignee, priority (Low/Medium/High), status (Todo/In Progress/Done)
- Workload tracking: per-user totals, active tasks, completed tasks
- Dashboard: tasks per user, pending vs completed, workload distribution
- AI helper: suggest assignee + priority + short explanation

### Good to Have

- AI task prioritization using deadline/dependency hints
- Assignment notifications

## API Contract Used By Frontend

- POST /auth/register
- POST /auth/login
- GET /users
- GET /users/:id
- POST /tasks
- GET /tasks
- PUT /tasks/:id
- DELETE /tasks/:id
- GET /dashboard
- POST /ai/suggest-assignment

## Environment

Create a .env file in this folder.

Example:

```env
VITE_API_BASE_URL=http://localhost:5000
```

Use VITE_API_BASE_URL for all API requests from the frontend.

## Local Development

Install dependencies:

```bash
npm install
```

Run dev server:

```bash
npm run dev
```

Build for production:

```bash
npm run build
```

Preview production build:

```bash
npm run preview
```

## Suggested Frontend Pages

- Login/Register
- Task List + Filters
- Create/Edit Task Form
- Users/Workload View
- Dashboard
- AI Assignment Panel

## Deployment Target

- Frontend: Vercel

Set VITE_API_BASE_URL in Vercel environment variables to point to the deployed backend.
