## Svelte Frontend

## Navigate to Frontend

```bash
cd Frontend
```

## Install Dependencies

```bash
npm install
```

## Start Dev Server

```bash
npm run dev
```

## Start Dev Server (exposed on local network)

```bash
npm run dev -- --host
```

## Production Build

```bash
npm run build
```

## Preview Production Build

```bash
npm run preview
```

---

## Shadcn-Svelte Setup

### 1. Add Tailwind CSS

```bash
npx svelte-add@latest tailwindcss
```

### 2. Initialize Shadcn-Svelte

```bash
npx shadcn-svelte@latest init
```

### 3. Add Base Components

```bash
npx shadcn-svelte@latest add button input label card form
```

---

## Add Extra Shadcn Components

### Table (absence management / user list)

```bash
npx shadcn-svelte@latest add table
```

### Badge (status: pending / approved / denied)

```bash
npx shadcn-svelte@latest add badge
```

### Dialog / Modal

```bash
npx shadcn-svelte@latest add dialog
```

### Select Dropdown (filters)

```bash
npx shadcn-svelte@latest add select
```

### Toast Notifications

```bash
npx shadcn-svelte@latest add toast
```

---

## Git Workflow

### Check status

```bash
git status
```

### Stage all changes

```bash
git add .
```

### Commit

```bash
git commit -m "feat: add role-based dashboard"
```

### Push to branch

```bash
git push origin your-branch-name
```

---

## Docker

### Build frontend image

```bash
docker build -t schichtpilot-frontend .
```

### Run container on port 5173

```bash
docker run -p 5173:5173 schichtpilot-frontend
```

### Run full stack with docker-compose

```bash
docker-compose up --build
```