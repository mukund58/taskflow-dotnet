# Frontend Product Checklist

This checklist follows the product-oriented frontend master plan for the existing backend.

## 0. Stack Direction

- [x] Use Next.js with App Router
- [x] Use TailwindCSS as primary styling system
- [x] Add route-level SSR where useful (dashboard-heavy reads)

## 1. Folder Structure

Target structure:

```text
/app
	/auth
		/login
		/register
	/dashboard
	/projects
	/tasks
	/task/[id]

/components
	/ui
	/task
	/project
	/dashboard

/services
	api.ts
	auth.ts
	task.ts

/store
	authStore.ts
	taskStore.ts

/types
```

- [x] Baseline folders created: app, components, services, hooks, types
- [x] Add reusable ui folder and foundation components
- [x] Add feature folders: components/task, components/project, components/dashboard
- [x] Add store folder and initial stores
- [x] Add route folders: auth/login, auth/register, dashboard, projects, tasks, task/[id]

## 2. Auth Flow (Critical UX)

- [x] Create login page at /auth/login
- [x] Create register page at /auth/register
- [x] Add JWT storage utility (current implementation uses local storage)
- [x] Auto-attach JWT in API client
- [x] Add redirect if already authenticated
- [x] Add role-based UI rendering
- [x] Add invalid credential and loading state UX
- [x] Evaluate move to httpOnly cookie flow if backend supports it
	Result: added optional cookie mode (`NEXT_PUBLIC_USE_AUTH_COOKIES=true`) in API client while keeping bearer-token mode as default.

## 3. Dashboard (WOW Factor)

- [x] Create dashboard page and app layout with sidebar + topbar
- [x] Show total tasks
- [x] Show completed tasks
- [x] Show active tasks
- [x] Show overdue tasks
- [x] Show tasks per user chart
- [x] Show workload distribution chart
- [x] Integrate chart library (recharts or chart.js)

## 4. Task Management UI

### Task List (/tasks)

- [x] Build task table view
- [x] Add optional Kanban toggle (bonus)
- [x] Add filters: status + assigned user
- [x] Add pagination
- [x] Add sorting
- [x] Show card metadata: title, priority, status, due date

### Task Details (/task/[id])

- [x] Show task info block
- [x] Show assigned user
- [x] Add status dropdown update
- [x] Add priority dropdown update
- [x] Add due date update

### Checklist and Comments

- [x] Add checklist item create
- [x] Add checklist toggle complete
- [x] Add checklist progress bar
- [x] Add comment create
- [x] Add comment list
- [x] Add activity log section (if backend activity endpoint is available)

## 5. Project Management

- [x] Create projects list page at /projects
- [x] Create project detail page at /projects/[id]
- [x] Create project flow
- [x] List projects flow
- [x] Show project tasks flow

## 6. AI Suggestion UI

- [x] Add Suggest Assignment button
- [x] Show suggested user
- [x] Show suggested priority
- [x] Show explanation text
- [x] Present in modal or side panel

## 7. Search and Filter UX

- [x] Add debounced search input
- [x] Combine search + status + assigned user filters
- [x] Keep query-state URL synced
- [x] Support query shape like status, assignedTo, search, page

## 8. Notifications (Phase 2)

- [x] Add toast notifications for assignment/comment/task actions
- [x] Integrate react-hot-toast

## 9. UI Library Strategy

- [x] TailwindCSS integrated
- [x] Add shadcn/ui setup
- [x] Map existing reusable primitives to shadcn where useful

## 10. State Management

- [x] Add Zustand for auth state
- [x] Add TanStack Query for task data, caching, and refetch
- [x] Replace ad hoc fetching in pages with query hooks

## 11. API Integration Standards

- [x] Centralized API client exists
- [x] Add auth/logout handling on 401 responses
- [x] Add service modules split by domain (auth, task, project, dashboard)
- [x] Keep response typing consistent across services

## 12. UX Quality Bar

- [x] Skeleton loaders available
- [x] Empty states available
- [x] Error states available
- [x] Use optimistic updates for fast actions (status changes)
- [x] Confirm destructive actions before delete
- [x] Avoid spinner-only pages when data is loading

## 13. MVP Frontend Checklist (Must Build First)

- [x] Auth (login/register)
- [x] Dashboard
- [x] Task list with filter + pagination
- [x] Task detail page
- [x] Create/update task
- [x] Comments
- [x] Checklist

## 14. Add After MVP

- [x] Kanban board
- [x] Activity log UI
- [x] AI suggestion UI
- [x] Notifications

---

## Recommended Build Flow

1. Auth
2. Task CRUD
3. Dashboard
4. Polish UX

## Deployment Readiness

- [x] Configure production API URL
- [x] Add .env.example for frontend variables
- [x] Verify CORS and API integration in deployed environment
- [x] Final responsive QA pass (desktop + mobile)

Deployment verification command:

```bash
npm run verify:deployment
```
