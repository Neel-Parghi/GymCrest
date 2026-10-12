# Elevatyon

**Elevatyon** is a multi-tenant SaaS platform for gym owners, trainers, and members - one system to run a gym's day-to-day operations (memberships, billing, staff payroll, attendance) and give members a real fitness experience (workout & diet planning, progress tracking, nutrition logging).

It's built as a monorepo: an Angular web app, a .NET Web API, and a static marketing landing page.

---

## 🚀 Live Demo

**[elevatyon.vercel.app](https://elevatyon.vercel.app/)**

Log in with either of these accounts to explore the product without signing up:

| Role | Email | Password | What you'll see |
|---|---|---|---|
| **Gym Owner** | `demo@elevatyon.com` | `GymOwner@19` | Full back-office: members, staff, billing, workout/diet libraries, attendance, gym plans |
| **Member (User)** | `demouser@elevatyon.com` | `DemoUser@19` | Self-service portal: workout & diet planner, progress dashboard, diet tracker, health tracker |

> These are shared demo accounts on a live environment - please don't rely on data you enter persisting, and don't use them for anything sensitive.

---

## ✨ Key Features

### For Gym Owners & Staff
- **Member management** - onboarding, subscriptions, plan assignment, attendance, and a self-service member portal with automatic email-based account linking (a member can be added by staff before they ever sign up).
- **Billing** - two independent billing flows: Elevatyon's own SaaS subscription for the gym owner (Razorpay checkout, trial periods, automatic access cutoff on expiry) and the gym's own member billing (plans with promotional pricing, Razorpay self-checkout, offline/cash entry).
- **Staff & payroll** - role-based staff accounts (Trainer / Staff) with invite-based onboarding, a configurable permission matrix for what Staff can see, PT/Rehab commission tracking, and trainer-linked custom invoices.
- **Workout & Diet libraries** - trainers build reusable Daily/Weekly/Split workout templates and diet plans with calorie/macro targets; gym owners can view, edit, and assign any trainer's plans to any member from a dedicated Resources section.
- **Multi-branch support**, gym announcements, and an inventory/equipment tracker.

### For Members
- **Personalized onboarding** - a starter workout + diet plan is auto-generated from BMR/TDEE at signup (goal, experience level, height, weight, age), so new members never start from a blank slate.
- **Workout planner & tracker** - assigned plans, daily performance logging, a muscle-activation heatmap, and personal records.
- **Diet planner & tracker** - assigned meal plans, real-time calorie/macro logging against daily targets, and a food-search lookup (USDA FoodData Central, with a CalorieNinjas fallback) so members can log food by name instead of typing macros by hand.
- **Health tracker** - weight/BMI/body-composition history with trend comparisons.
- **A real dashboard** - activity rings, streaks, monthly consistency, personal records, and recovery status, not just a table of numbers.

### Platform-wide
- **Five roles** (SuperAdmin, GymOwner, Trainer, Staff, User), each gym-scoped via JWT claims rather than client-supplied IDs - a Staff or Trainer literally cannot request another gym's data, because the boundary is enforced at the token level, not just in application logic.
- **Background automation** via Hangfire - daily jobs auto-expire overdue member and SaaS subscriptions and send reminder notifications.

---

## 🛠 Tech Stack

**Frontend** (`apps/web`)
- Angular 21 (standalone components, SSR via `@angular/ssr` + Express)
- Angular Material + CDK
- Chart.js (via `ng2-charts`) for data visualization
- RxJS-based services (no NgRx/Akita - state lives in services + signals)

**Backend** (`apps/api`)
- .NET 10 / ASP.NET Core Web API, Clean-Architecture-style layering (`Api` → `Application` → `Domain` / `Infrastructure` / `Contracts`)
- Entity Framework Core + PostgreSQL (Npgsql)
- JWT bearer authentication, BCrypt password hashing
- Hangfire (PostgreSQL storage) for scheduled/background jobs
- AutoMapper for entity ↔ DTO mapping

**Integrations**
- **Razorpay** - SaaS and member subscription payments
- **Brevo** - transactional email (SMTP + REST)
- **Cloudinary** - image/file storage
- **USDA FoodData Central** (primary) + **CalorieNinjas** (fallback) - food/nutrition search for diet logging

**Marketing site** (`apps/landing`) - static HTML/CSS/JS.

---

## 📁 Repository Structure

```
GymForge/
├── apps/
│   ├── web/       # Angular frontend (member, trainer, gym-owner, super-admin portals)
│   ├── api/        # .NET Web API
│   │   ├── GymForge.Api/              # Controllers, Program.cs, startup config
│   │   ├── GymForge.Application/      # Business logic, services, DI wiring
│   │   ├── GymForge.Domain/           # Entities, enums, repository interfaces
│   │   ├── GymForge.Infrastructure/   # EF Core, external API clients, repositories
│   │   ├── GymForge.Contracts/        # Request/response DTOs shared across layers
│   │   └── GymForge.Shared/           # Cross-cutting enums/constants
│   └── landing/    # Static marketing site
└── docs/
    └── business-logic/    # In-depth write-ups of core domains (roles, billing, workout/diet planning, onboarding)
```

If you want to understand *why* something works the way it does (not just what the code does), start with `docs/business-logic/` - it covers the roles/permissions model, the subscription & billing rules, workout/diet plan semantics, and onboarding/invitation flow in plain English.

---

## 🏁 Getting Started (Local Setup)

### Prerequisites
- [Node.js](https://nodejs.org/) 20+ and npm
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A PostgreSQL database (local or hosted - e.g. Supabase, Render, Docker)

### 1. Clone the repo
```bash
git clone https://github.com/Neel-Parghi/GymForge.git
cd GymForge
```

### 2. Backend setup (`apps/api`)
```bash
cd apps/api/GymForge.Api
```

Create `apps/api/GymForge.Api/appsettings.Development.json` (gitignored, safe for real secrets) with at least:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=elevatyon;Username=postgres;Password=yourpassword"
  }
}
```

Other config sections in `appsettings.json` (`Jwt`, `Brevo`, `Cloudinary`, `Razorpay` keys per gym, `CalorieNinjas`, `UsdaFoodData`) ship with placeholder values - the app runs without them, but the features they back (email, file uploads, payments, food search) will be degraded or disabled until you supply real keys the same way.

Apply migrations and run:
```bash
dotnet ef database update --project ../GymForge.Infrastructure --startup-project .
dotnet run
```
API runs at `http://localhost:5250` (HTTPS: `https://localhost:7184`).

### 3. Frontend setup (`apps/web`)
```bash
cd apps/web
npm install
npm start
```
App runs at `http://localhost:4200`.

### 4. Landing page (optional)
`apps/landing` is static - open `index.html` directly or serve it with any static file server.

---

## 🔑 Getting Your Own API Keys (for full functionality)

| Service | Used for | Where to get a free key |
|---|---|---|
| USDA FoodData Central | Food/nutrition search | [fdc.nal.usda.gov/api-key-signup.html](https://fdc.nal.usda.gov/api-key-signup.html) - instant, no approval |
| CalorieNinjas | Food search fallback | [calorieninjas.com](https://calorieninjas.com/) |
| Cloudinary | Image/file uploads | [cloudinary.com](https://cloudinary.com/) |
| Brevo | Transactional email | [brevo.com](https://www.brevo.com/) |
| Razorpay | Payments | [razorpay.com](https://razorpay.com/) |

---

## 🤝 Contributing

This is currently a solo/small-team project. If you're exploring the code and have questions, the `docs/business-logic/` folder is the best starting point before diving into the source.
