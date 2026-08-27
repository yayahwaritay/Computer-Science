# CompSci API

A RESTful API for Computer Science course management, built with **.NET 8** and **Clean Architecture** (Api / Core / Infrastructure), using **PostgreSQL** (EF Core) and **JWT** authentication.

> Building the student-facing frontend? See [`STUDENT_PORTAL_GUIDE.md`](STUDENT_PORTAL_GUIDE.md) for a focused guide to just the endpoints a Student account can use (courses, assignments, notes, past questions, auth).

## Project structure

```
Api/             Controllers, DI/Swagger/JWT configuration, middlewares
Core/            Entities, DTOs, service interfaces + implementations, validators
Infrastructure/  EF Core DbContext, repositories, file storage, email sending, JWT generation
Migrations/      EF Core migrations
```

## Tech stack

- ASP.NET Core 8 Web API
- Entity Framework Core 8 + Npgsql (PostgreSQL)
- JWT Bearer authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- BCrypt.Net for password hashing
- Swagger / Swashbuckle for API docs
- Brevo (transactional email API) for outbound email

## Getting started

1. Set your PostgreSQL connection string in `appsettings.Development.json` (`ConnectionStrings:DefaultConnection`).
2. Set a real Brevo API key locally (never commit it — see [Email configuration](#email-configuration) below):
   ```
   dotnet user-secrets set "EmailSettings:ApiKey" "xkeysib-..."
   ```
3. Run the API — migrations are applied automatically on startup:
   ```
   dotnet run
   ```
4. Browse Swagger UI at `/swagger` (Development environment only).

## Domain overview

- **User** — login account (`Username`, `Email`, `PasswordHash`, `Role`: `Admin` / `Lecturer` / `Student`, `IsApproved`).
- **Student** — academic profile (name, program, year, enrollment/graduation years), linked 1:1 to a `User` via `UserId`.
- **Course**, **Assignment**, **Note**, **PastQuestion** — course-management content, generally readable by any authenticated user and writable by `Admin`/`Lecturer`.
- **Dissertation** — final year project/dissertation record, managed exclusively by `Admin`/`Lecturer`. Every record tracks `CreatedByUserId` — see [Dissertation ownership scoping](#recent-changes-dissertation-ownership-scoping-lecturer-vs-admin-visibility) below.
- **ActivityLog** — site-wide audit trail of who did what — see [Lecturer ID + activity log](#recent-changes-lecturer-id--site-wide-activity-log) below.

## API summary

All responses are wrapped in `ApiResponse<T>` (`success`, `message`, `data`, `errors`).

| Area | Endpoints |
|---|---|
| Auth | `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/users`, `GET /api/auth/users/{id}` |
| Student registration | `POST /api/auth/register-student`, `GET /api/auth/pending-registrations`, `POST /api/auth/{userId}/approve`, `POST /api/auth/{userId}/reject`, `POST /api/auth/change-password` |
| Students | `POST/GET/PUT/DELETE /api/students`, `GET /api/students/paged`, `GET /api/students/{id}` |
| Courses | `POST/GET/PUT/DELETE /api/courses`, `GET /api/courses/paged`, `GET /api/courses/{id}` |
| Assignments | `POST/GET/PUT/DELETE /api/assignments`, `GET /api/assignments/paged`, `GET /api/assignments/{id}`, `GET /api/assignments/{id}/download` |
| Notes | `POST/GET/PUT/DELETE /api/notes`, `GET /api/notes/paged`, `GET /api/notes/{id}`, `GET /api/notes/{id}/download` |
| Past questions | `POST/GET/PUT/DELETE /api/pastquestions`, `GET /api/pastquestions/paged`, `GET /api/pastquestions/{id}`, `GET /api/pastquestions/{id}/download` |
| Dissertations | `POST/GET/PUT/DELETE /api/dissertations`, `GET /api/dissertations/paged`, `GET /api/dissertations/{id}`, `GET /api/dissertations/by-student?studentId=...`, `GET /api/dissertations/{id}/download`, `GET /api/dissertations/search`, `GET /api/dissertations/export/csv`, `GET /api/dissertations/export/pdf` |
| Activity log | `GET /api/activitylogs` (Admin only, all users), `GET /api/activitylogs/mine` (Admin or Lecturer, own activity only) |

Most write operations require `Admin` or `Lecturer`; read operations require any authenticated user unless marked `AllowAnonymous`. The **Dissertations** area is the one exception — every endpoint, including reads, requires `Admin` or `Lecturer`, and within it a Lecturer only ever sees/manages the records they personally created; only Admin sees everything (including the `search`/`export` endpoints, which are Admin-only outright).

**Note on route naming**: routes follow `api/[controller]` with no manual kebab-casing, so a multi-word controller name becomes one lowercase word in the URL (case-insensitive) — e.g. `ActivityLogsController` → `/api/activitylogs` (**not** `/api/activity-logs`), `PastQuestionsController` → `/api/pastquestions`. Double-check this when wiring up the frontend's API client.

## Email configuration

Outbound email (registration, approval, welcome messages) is sent via **Brevo**'s transactional email API through `IEmailSender` (`Infrastructure/Email/BrevoEmailSender.cs`). Configuration lives under `EmailSettings` in `appsettings*.json`:

```json
"EmailSettings": {
  "ApiKey": "",
  "SenderEmail": "no-reply@compsci-portal.com",
  "SenderName": "CompSci Portal",
  "BaseApiUrl": "https://api.brevo.com/v3/smtp/email"
}
```

- `ApiKey` is intentionally left blank in source control — set it locally via `dotnet user-secrets` (or an `EmailSettings__ApiKey` environment variable in production).
- `SenderEmail` must be a verified sender/domain in your Brevo account.
- If `ApiKey` is not configured, or Brevo returns an error, the failure is logged and swallowed — it never blocks registration or approval.

---

## Recent changes: student self-registration + email notifications

Two capabilities were added on top of the existing admin-driven student management:

1. **Students receive an email when registered**, whichever path is used.
2. **Students can self-register**, landing in a pending state until an Admin/Lecturer confirms them — an alternative to the admin-driven flow.

### What changed

- **`Student` is now linked to `User`.** Previously `Student` had no email or login of its own. `Student.UserId` (unique FK → `User.Id`) links every student profile to a login account; `User.Email` is now the single source of truth for a student's email.
- **`User.IsApproved` / `User.ApprovedAt`** were added. Admin/Lecturer-created accounts are auto-approved; self-registered student accounts start unapproved and can't log in until approved.
- **Admin/Lecturer direct-create flow** (`POST /api/students`): `StudentRequest` now requires an `Email`. Creation now also creates the linked `User` with a randomly generated temporary password, and emails the student their login details (`EmailTemplates.StudentWelcome`). `StudentResponse` now includes `Email`.
- **New self-registration flow**:
  - `POST /api/auth/register-student` (anonymous) — creates a `User` (`IsApproved = false`) + `Student` in one call and sends a "registration received / pending approval" email.
  - `POST /api/auth/login` now blocks login for unapproved student accounts with a clear pending-approval message.
  - `GET /api/auth/pending-registrations` (Admin/Lecturer) — lists registrations awaiting review.
  - `POST /api/auth/{userId}/approve` (Admin/Lecturer) — approves the account and emails the student.
  - `POST /api/auth/{userId}/reject` (Admin/Lecturer) — deletes the pending account and emails the student.
- **New `POST /api/auth/change-password`** (authenticated) — lets a user (e.g. a student issued a temp password) set their own password.
- **New email infrastructure**: `IEmailSender` / `BrevoEmailSender`, `EmailSettings`, and `EmailTemplates` (welcome, pending, approved, rejected messages).
- **New EF Core migration** `AddStudentUserLinkAndApproval` — adds `Users.IsApproved`, `Users.ApprovedAt`, `Students.UserId` (+ FK/unique index). Existing rows default `IsApproved = true` so previously-created accounts keep working unchanged.

### Not changed (flagged, out of scope)

- `POST /api/auth/register` still lets the caller pick any `Role` (including `Admin`) and remains anonymous — pre-existing behavior, unrelated to this change.
- The plaintext DB credentials already committed in `appsettings*.json` / `Infrastructure/AppDbContextFactory.cs` were left as-is — a pre-existing, separate concern.

---

## Recent changes: dissertation repository

Added a repository area for keeping an official record of each student's final year project/dissertation before they graduate, managed exclusively by `Admin`/`Lecturer` — students have no access to this area.

### What changed

- **New `Dissertation` entity** (`Core/Entities/Dissertation.cs`) with the requested fields: `StudentName`, `StudentId`, `Program`, `Department`, `School`, `Topic` (dissertation/project topic), `AcademicYear`, `Grade`, plus the uploaded documentation (`FilePath`/`OriginalFileName`, matching the existing Notes/Assignments/PastQuestions upload pattern) and `UploadDate`/`UpdatedAt`.
- **New `DissertationsController`** at `/api/dissertations`, entirely gated behind `[Authorize(Roles = "Admin,Lecturer")]` (unlike Notes/Assignments/etc., even reads are staff-only here):
  - `POST /api/dissertations` (multipart form) — record a submission with its full write-up (PDF/DOC/DOCX, up to 50MB).
  - `GET /api/dissertations`, `GET /api/dissertations/paged`, `GET /api/dissertations/{id}` — list/browse records.
  - `GET /api/dissertations/by-student?studentId=...` — look up a student's submission(s) by Student ID (query parameter, not a route segment, since Student IDs contain `/`, e.g. `CS/2026/998`).
  - `GET /api/dissertations/{id}/download` — download the stored documentation file.
  - `PUT /api/dissertations/{id}` — update the record (fields and/or replace the file).
  - `DELETE /api/dissertations/{id}` — remove the record and its stored file.
- Standard `DissertationValidator` (required-field/length checks) and a new EF Core migration `AddDissertations` (new, purely additive table — no existing data affected).
- Design note: like `Assignment`/`Note`/`PastQuestion` referencing a course by plain `CourseName`/`CourseCode` fields rather than a foreign key, `Dissertation` stores the student's details as plain fields rather than a foreign key to `Student` — consistent with the existing convention, and needed anyway since `Department`/`School` aren't fields on `Student`.

### Bugs found and fixed along the way

While wiring up file download/delete for this feature, two pre-existing bugs surfaced that affect file handling project-wide (not introduced by this change, but blocking the new feature so fixed here):

- **`LocalFileStorageService`** (`Infrastructure/FileStorage/LocalFileStorageService.cs`) built its file-lookup path with `wwwroot` duplicated in it, so `GetFileAsync`/`DeleteFileAsync`/`FileExists` could never find a file that `SaveFileAsync` had just saved — every download (`Notes`, `Assignments`, `PastQuestions`, and the new `Dissertations`) was silently broken. Fixed by resolving all paths from a single stored `wwwroot` path.
- **Downloaded files were never released** after being read (`DownloadAsync` never disposed the opened `FileStream`), which left the file locked and made a subsequent delete fail with "file is being used by another process." Fixed in the new `DissertationService.DownloadAsync`. The identical pattern exists in `NoteService`/`AssignmentService`/`PastQuestionService` and wasn't touched — let me know if you'd like the same one-line fix applied there.

Both fixes were verified live: create → download → delete now succeeds end-to-end for a dissertation record.

---

## Recent changes: dissertation ownership scoping (Lecturer vs Admin visibility)

Within the dissertation repository, **Lecturers can now only see/manage the records they personally created; only Admin can see and manage every record.**

### What changed

- **`Dissertation.CreatedByUserId`** — every record now tracks the `User` who created it, set automatically from the caller's JWT at creation time (not a client-supplied field).
- **`DissertationAccessContext`** (`Core/DTOs/DissertationDtos.cs`) — a small `(UserId, IsAdmin)` context built by the controller from the caller's claims and threaded through every `IDissertationService` method.
- **Scoping rules**, enforced in `DissertationService`:
  - `GET /api/dissertations`, `/paged`, `/by-student` — Admin gets every record; Lecturer gets only records where `CreatedByUserId` matches them (filtered at the database level via new repository methods `GetByCreatorAsync`/`GetPagedByCreatorAsync`, so pagination totals stay correct).
  - `GET /{id}`, `/{id}/download`, `PUT /{id}`, `DELETE /{id}` — a Lecturer accessing a record they didn't create gets the same `404 Not Found` as a nonexistent record (rather than a distinct "forbidden" response), so the existence of other lecturers' records isn't revealed.
  - Admin is unrestricted on all of the above.
- **`DissertationResponse`** now also includes `CreatedByUserId`, `CreatedByUsername`, `CreatedByEmail` so Admin's "see everything" view shows who recorded each entry.
- New EF Core migration `AddDissertationOwnership` adds the `CreatedByUserId` column (+ index). Existing rows (including real data already in the table) default to an unattributed zero GUID — still visible to Admin, correctly excluded from every Lecturer's filtered view.

Verified live with two Lecturer accounts each creating one record: each Lecturer's list/paged/by-student views showed only their own record, direct access to the other Lecturer's record (`GET`, `download`, `DELETE`) returned 404, and Admin's view showed both plus a pre-existing record.

---

## Recent changes: Lecturer ID + site-wide activity log

Two related capabilities were added:

1. **Every Lecturer gets a unique, human-readable identifier** (e.g. `LEC-0001`), assigned automatically at registration.
2. **A site-wide activity log** now records every create/update/delete a Lecturer or Admin performs, so "who did what" is auditable across the whole app — not just Dissertations.

### 1. Lecturer ID

- Format: `LEC-####`, sequential, assigned once at registration and never changes. Only `Role: Lecturer` accounts get one — Admin and Student accounts have `lecturerId: null`.
- Assigned in `POST /api/auth/register` when `role` is `Lecturer` (`1`) — see the `RegisterRequest`/`UserRole` enum below. There is no separate "Lecturer registration" endpoint; it's the same `/api/auth/register` used today, just now populating `lecturerId` when the role is Lecturer.
- **Existing Lecturer accounts were backfilled** (migration `AddLecturerIdAndActivityLog`), ordered by `createdAt`, so this applies retroactively, not just to new registrations.
- Surfaced in three places the frontend should read from:
  - `AuthResponse` (from `POST /api/auth/register` and `POST /api/auth/login`) — show it immediately after a Lecturer logs in.
  - `UserResponse` (from `GET /api/auth/users`, `GET /api/auth/users/{id}`) — Admin's user list/detail view.
  - `ActivityLogResponse.lecturerId` (see below) — a snapshot of the acting Lecturer's ID at the time of each logged action.

Example `POST /api/auth/login` response for a Lecturer:
```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "id": "3e64bfc9-4e12-4238-a2f8-f6fd5a98527e",
    "username": "lect3",
    "email": "lect3@example.com",
    "role": "Lecturer",
    "lecturerId": "LEC-0003",
    "token": "...",
    "tokenExpiration": "2026-08-28T15:59:40Z"
  },
  "errors": null
}
```
For an Admin or Student account, `lecturerId` is `null`.

### 2. Site-wide activity log

- **New `ActivityLog` entity** — records `userId`, `username`, `userRole`, `lecturerId` (snapshot, null if the actor wasn't a Lecturer), `entityType`, `action`, `entityId`, `timestamp`.
- **Automatic, not something the frontend calls directly** — a new `[LogActivity(entityType, action)]` filter (`Api/Filters/LogActivityAttribute.cs`) is attached to every Create/Update/Delete endpoint across **Students, Courses, Assignments, Notes, PastQuestions, and Dissertations**. It reads the entity ID from the route (`Update`/`Delete`) or from the `CreatedAtAction` response (`Create`), and the actor from the JWT — no request changes needed on the frontend's part. `action` is one of `"Create"` / `"Update"` / `"Delete"`; `entityType` is one of `"Student"` / `"Course"` / `"Assignment"` / `"Note"` / `"PastQuestion"` / `"Dissertation"`.
- Logging failures never block the underlying request — if logging fails for any reason, the Create/Update/Delete still succeeds.
- **New `ActivityLogsController`** at `/api/activitylogs` (⚠️ no hyphen — see routing note above):
  - `GET /api/activitylogs?pageNumber=&pageSize=&userId=&entityType=` — **Admin only**. Every log entry, newest first, optionally filtered by `userId` and/or `entityType`.
  - `GET /api/activitylogs/mine?pageNumber=&pageSize=&entityType=` — Admin **or** Lecturer. Only the caller's own entries — this is what a Lecturer's "my activity" screen should call.

Example response (paginated, same `PagedResponse<T>` envelope used elsewhere):
```json
{
  "success": true,
  "message": "Operation successful",
  "data": {
    "data": [
      {
        "id": "f95a0739-24fd-4043-b05d-30b6b9a0687a",
        "userId": "3e64bfc9-4e12-4238-a2f8-f6fd5a98527e",
        "username": "lect3",
        "userRole": "Lecturer",
        "lecturerId": "LEC-0003",
        "entityType": "Course",
        "action": "Update",
        "entityId": "9fa6a3fe-ba0f-44d0-89ac-0d3328893110",
        "timestamp": "2026-08-27T15:59:51.389089Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalRecords": 1,
    "totalPages": 1
  },
  "errors": null
}
```

Verified live: registering a new Lecturer assigned the next sequential ID, existing Lecturer accounts were correctly backfilled, a Lecturer's Course create+update both appeared in `/mine`, a Lecturer got `403` on the Admin-only `/api/activitylogs`, and Admin's view showed the same entries with full attribution.

---

## Recent changes: Admin dissertation search & compiled export (CSV/PDF)

Admin can now search across **every** Lecturer's dissertation records (bypassing the ownership scoping described above, since this is Admin-only) by academic year range / program / department / school, and download a compiled report containing only the requested fields.

### Endpoints (all three share the same query parameters; all Admin-only)

`GET /api/dissertations/search`, `GET /api/dissertations/export/csv`, `GET /api/dissertations/export/pdf`

| Query param | Type | Behavior |
|---|---|---|
| `fromYear` | `int?` | Optional. Matches the **leading year** in `academicYear` (e.g. `2025` matches `"2025/2026"`). Inclusive lower bound. |
| `toYear` | `int?` | Optional. Inclusive upper bound, same leading-year comparison. |
| `program` | `string?` | Optional. Case-insensitive **contains** match. |
| `department` | `string?` | Optional. Case-insensitive **contains** match. |
| `school` | `string?` | Optional. Case-insensitive **contains** match. |

All filters are optional and combine with AND. Omit all of them to match every record. This is the intended frontend flow: Admin picks any combination of academic year range / program / department / school in a filter form, calls `/search` to preview the matching records ("all the records will appear"), then calls `/export/csv` or `/export/pdf` with the **same query params** to download the compiled file.

- `GET /search` → `200 OK`, `ApiResponse<DissertationResponse[]>` — full record objects (same shape as the other Dissertation endpoints), for on-screen preview before download.
- `GET /export/csv` → `200 OK`, `Content-Type: text/csv`, file download (`dissertations_yyyyMMdd_HHmmss.csv`).
- `GET /export/pdf` → `200 OK`, `Content-Type: application/pdf`, file download (`dissertations_yyyyMMdd_HHmmss.pdf`).

**The compiled export (CSV and PDF) intentionally contains only 5 columns, regardless of what other fields exist on the record**, per the requested format:

`Student Name, Student ID, Program, Dissertation/Project Topic, Academic Year`

(Department, School, Grade, and the file itself are *filter* criteria / preview fields only — they are deliberately excluded from the compiled file.)

Example CSV output:
```csv
Student Name,Student ID,Program,Dissertation/Project Topic,Academic Year
Alice Test,CS/2023/001,Computer Science,Topic Alice,2023/2024
Bob Test,CS/2025/002,Computer Science,Topic Bob,2025/2026
```

Frontend integration note: since these are file downloads, trigger them with a plain link/`window.location` navigation (with the JWT either as a query param your auth setup supports, or via a fetch-then-blob-download pattern) rather than a JSON `fetch` — the response is a raw file stream, not `ApiResponse<T>`.

### What changed under the hood

- `DissertationFilter` / `DissertationExportRow` DTOs (`Core/DTOs/DissertationDtos.cs`).
- `IDissertationService.SearchAsync/ExportCsvAsync/ExportPdfAsync`, filtering in `DissertationService` (in-memory over `GetAllAsync()` — fine at this data volume; would need to move to a DB-level query if the table grows very large).
- CSV built by `Core/Services/Export/DissertationCsvBuilder.cs` (plain BCL, no dependency).
- PDF built by `Infrastructure/Reports/DissertationPdfBuilder.cs` using the new **PdfSharpCore** NuGet package (MIT-licensed, no revenue-based restrictions — chosen deliberately over alternatives like QuestPDF that have revenue-conditional licensing terms).

Verified live: created three dissertation records spanning 2023–2026 across two programs, confirmed `fromYear`/`toYear`/`program`/`school` filters (individually and combined) returned the correct subset, downloaded both CSV and PDF and confirmed the CSV contained exactly the 5 specified columns and the PDF was a valid file, and confirmed a Lecturer gets `403` on all three endpoints.
