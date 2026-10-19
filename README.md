# GymCrest

**GymCrest** is a multi-tenant SaaS platform for gym owners, trainers, and members - one system to run a gym's day-to-day operations (memberships, billing, staff payroll, attendance) and give members a real fitness experience (workout & diet planning, progress tracking, nutrition logging).

It's built as a monorepo: an Angular web app (which also serves the public website) and a .NET Web API.

---

## 🚀 Live Demo

**[gymcrest.vercel.app](https://gymcrest.vercel.app/)**

Log in with either of these accounts to explore the product without signing up:

| Role | Email | Password | What you'll see |
|---|---|---|---|
| **Gym Owner** | `demo@gymcrest.com` | `GymOwner@19` | Full back-office: members, staff, billing, workout/diet libraries, attendance, gym plans |
| **Member (User)** | `demouser@gymcrest.com` | `DemoUser@19` | Self-service portal: workout & diet planner, progress dashboard, diet tracker, health tracker |

> These are shared demo accounts on a live environment - please don't rely on data you enter persisting, and don't use them for anything sensitive.

---

## ✨ Key Features

### For Gym Owners & Staff
- **Member management** - onboarding, subscriptions, plan assignment, attendance, and a self-service member portal with automatic email-based account linking (a member can be added by staff before they ever sign up).
- **Billing** - two independent billing flows: GymCrest's own SaaS subscription for the gym owner (Razorpay checkout, trial periods, automatic access cutoff on expiry) and the gym's own member billing (plans with promotional pricing, Razorpay self-checkout, offline/cash entry).
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
- Angular 21 (standalone components, signals), built as a static single-page app and hosted on Vercel; installable as an app (web manifest + launch splash)
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
- **Brevo** - transactional email via Brevo templates (wording and design are edited in Brevo; template IDs are set with `Brevo:Templates:*`)
- **Cloudinary** - image/file storage
- **USDA FoodData Central** (primary) + **CalorieNinjas** (fallback) - food/nutrition search for diet logging

**Website** - the public landing page is part of the web app (`features/landing`, route `/`); signed-in visitors go straight to their dashboard, and gym pricing is loaded live from the API.

---

## 📁 Repository Structure

```
GymForge/
├── apps/
│   ├── web/       # Angular frontend (public website + member, trainer, gym-owner, super-admin portals)
│   └── api/        # .NET Web API
│       ├── GymForge.Api/              # Controllers, Program.cs, startup config
│       ├── GymForge.Application/      # Business logic, services, DI wiring
│       ├── GymForge.Domain/           # Entities, enums, repository interfaces
│       ├── GymForge.Infrastructure/   # EF Core, external API clients, repositories
│       ├── GymForge.Contracts/        # Request/response DTOs shared across layers
│       └── GymForge.Shared/           # Cross-cutting enums/constants
└── docs/
    └── business-logic/    # In-depth write-ups of core domains (roles, billing, workout/diet planning, onboarding)
```

If you want to understand *why* something works the way it does (not just what the code does), start with `docs/business-logic/` - it covers the roles/permissions model, the subscription & billing rules, workout/diet plan semantics, and onboarding/invitation flow in plain English.

---

## 🏁 Getting Started (Local Setup)

### Prerequisites
- [Node.js](https://nodejs.org/) **20.19+ or 22.12+** (required by Angular 21) and npm
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- EF Core CLI: `dotnet tool install --global dotnet-ef`
- A PostgreSQL database (local or hosted - e.g. Supabase, Docker)

### 1. Clone the repo
```bash
git clone https://github.com/Neel-Parghi/GymForge.git
cd GymForge
```
> **Windows:** some file paths are long (up to ~160 characters). If the clone fails with *Filename too long*, run `git config --global core.longpaths true` (or clone into a short folder such as `C:\code`) and clone again.

### 2. Backend setup (`apps/api`)
Create `apps/api/GymForge.Api/appsettings.Development.json` (gitignored, safe for real secrets):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=gymcrest;Username=postgres;Password=yourpassword"
  },
  "Jwt": {
    "Key": "replace-with-a-random-secret-of-at-least-32-characters"
  },
  "GymCrest": {
    "BaseUrl": "http://localhost:4200"
  }
}
```
- Use the key=value connection-string format shown above (the EF tools don't accept `postgresql://` URLs).
- `Jwt:Key` **must** be at least 32 characters, otherwise login fails (the placeholder in `appsettings.json` is too short on purpose).
- `GymCrest:BaseUrl` is the frontend address used in emailed links and allowed by CORS.
- Other sections in `appsettings.json` (`Brevo`, `Cloudinary`, `Razorpay`, `Gemini`, `CalorieNinjas`, `UsdaFoodData`) hold placeholders: the app runs without them, but email, uploads, payments and food search stay disabled until you add real keys the same way. `apps/api/.env.example` lists every setting as environment variables (the format used by Docker and Render).

Trust the local HTTPS certificate (once per machine), apply migrations and run the **https** profile:
```bash
cd apps/api/GymForge.Api
dotnet dev-certs https --trust
dotnet ef database update --project ../GymForge.Infrastructure --startup-project .
dotnet run --launch-profile https
```
The API runs at `https://localhost:7184` (also `http://localhost:5250`); the web app's dev config calls the HTTPS address. The API also applies any pending migrations itself on startup.

### 3. Frontend setup (`apps/web`)
```bash
cd apps/web
npm ci
npm start
```
App runs at `http://localhost:4200` (the public website at `/`, the app at `/login`).

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

1. Branch from `main` (e.g. `feature/<short-name>`), commit, push and open a pull request into `main`.
2. **CI must pass** (`.github/workflows/ci.yml`): it builds the Angular app, builds the API, and fails if an EF entity changed without a migration (`dotnet ef migrations add <Name> --project GymForge.Infrastructure --startup-project GymForge.Api` from `apps/api`).
3. Never commit secrets: `.env`, `appsettings.Development.json` and `*.sql` dumps are gitignored for a reason.

If you're exploring the code and have questions, the `docs/business-logic/` folder is the best starting point before diving into the source.
