## 🎯 EXACT FEATURES (BUILD THIS ONLY)

### 🟢 CORE (MUST HAVE — non-negotiable)
- [ ] 🔐 **Auth System**
  - [x] Register / Login
  - [ ] JWT based auth
  - [ ] Roles:
    - [ ] Admin
    - [ ] User
  - [ ] *No OAuth / Google login*

- [ ] 📋 **Task Management (CRUD)**
  - [x] Create task
  - [x] Assign user
  - [ ] Priority: Low / Medium / High
  - [x] Status: Todo / In Progress / Done

- [ ] 👥 **User Workload Tracking**
  - [ ] Count tasks per user
  - [ ] Show active tasks
  - [ ] Show completed tasks

- [ ] 🤖 **AI Feature — Smart Task Assignment**
  - [ ] Suggest best user to assign
  - [ ] Suggest priority
  - [ ] Example explanation: “Assign to Mukund — low workload + high completion rate”
  - [ ] Use Gemini API

- [ ] 📊 **Dashboard (simple but clean)**
  - [ ] Tasks per user
  - [ ] Pending vs completed
  - [ ] Workload distribution (basic chart)

- [ ] 🌐 **Deployment**
  - [ ] Backend → Render
  - [ ] Frontend → Vercel
  - [ ] DB → MongoDB Atlas

### 🟡 GOOD TO HAVE (if time permits)
- [ ] 🧠 **AI Task Prioritization**
  - [ ] Suggest urgency based on deadline + dependency
- [ ] 🔔 **Notifications (basic)**
  - [ ] Alert on task assignment

---

## ✅ Minimum APIs
- [x] POST   `/auth/register`
- [x] POST   `/auth/login`
- [ ] GET    `/users`
- [ ] GET    `/users/:id`
- [x] POST   `/tasks`
- [x] GET    `/tasks`
- [ ] PUT    `/tasks/:id`
- [ ] DELETE `/tasks/:id`
- [ ] GET    `/dashboard`
- [ ] POST   `/ai/suggest-assignment`

---

## 🤖 AI Logic (Simple but Effective)
**Send to Gemini:**
- [ ] Task title
- [ ] Description
- [ ] List of users with workload

**Receive from Gemini:**
- [ ] Best user
- [ ] Priority
- [ ] Short explanation

---

## 🧱 Simple Architecture
- [ ] Frontend (React)
- [x] Backend (Node/.NET API)
- [ ] MongoDB Atlas
- [ ] Gemini API