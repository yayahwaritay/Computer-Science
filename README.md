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
- Twilio SendGrid (primary) with Mailgun as automatic fallback for outbound transactional email

## Getting started

1. Set your PostgreSQL connection string in `appsettings.Development.json` (`ConnectionStrings:DefaultConnection`).
2. Set real email provider API keys locally (never commit them — see [Email configuration](#email-configuration) below):
   ```
   dotnet user-secrets set "EmailSettings:SendGrid:ApiKey" "SG...."
   dotnet user-secrets set "EmailSettings:Mailgun:ApiKey" "key-..."
   dotnet user-secrets set "EmailSettings:Mailgun:Domain" "mg.yourdomain.com"
   ```
3. Run the API — migrations are applied automatically on startup:
   ```
   dotnet run
   ```
4. Browse Swagger UI at `/swagger` (Development environment only).

## Domain overview

- **User** — login account (`Username`, `Email`, `PasswordHash`, `Role`: `Admin` / `Lecturer` / `Student` / `Organization`, `IsApproved`, `CredentialsExpireAt`).
- **Student** — academic profile (name, program, year, enrollment/graduation years), linked 1:1 to a `User` via `UserId`.
- **Course**, **Assignment**, **Note**, **PastQuestion** — course-management content, generally readable by any authenticated user and writable by `Admin`/`Lecturer`.
- **Dissertation** — final year project/dissertation record, managed exclusively by `Admin`/`Lecturer`. Every record tracks `CreatedByUserId` — see [Dissertation ownership scoping](#recent-changes-dissertation-ownership-scoping-lecturer-vs-admin-visibility) below.
- **ActivityLog** — site-wide audit trail of who did what — see [Lecturer ID + activity log](#recent-changes-lecturer-id--site-wide-activity-log) below.
- **CourseAllocation** — which staff member teaches which course, per program/year of study/academic year/semester, managed exclusively by `Admin` — see [Course allocation](#recent-changes-course-allocation--pdf-export) below.
- **Organization**, **InternshipAllocation**, **InternshipEvaluation** — host-organization accounts, lecturer↔student internship grading assignments, and the digital internship evaluation form — see [Internship evaluation module](#recent-changes-internship-evaluation-module) below.
- **DissertationAllocation**, **StudentSubmission**, **SubmissionComment** — lecturer↔student dissertation supervision assignments, plus the student's own self-uploaded internship report / dissertation write-up and the comment thread on it — see [Student internship report & dissertation write-up submissions](#recent-changes-student-internship-report--dissertation-write-up-submissions) below.

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
| Course allocations | `POST /api/courseallocations`, `POST /api/courseallocations/bulk` (Admin only), `GET/PUT/DELETE /api/courseallocations`, `GET /api/courseallocations/paged`, `GET /api/courseallocations/{id}`, `GET /api/courseallocations/mine` (Lecturer only), `GET /api/courseallocations/export/pdf`, `GET /api/courseallocations/mine/export/pdf` (Lecturer only) |
| Organizations | `POST /api/organizations`, `GET /api/organizations`, `GET /api/organizations/{id}`, `POST /api/organizations/{id}/reissue-credentials` (all Admin/Lecturer), `DELETE /api/organizations/{id}` (Admin only) |
| Internship allocations | `POST/PUT/DELETE /api/internshipallocations` (Admin only), `GET /api/internshipallocations`, `GET /api/internshipallocations/{id}` (Admin/Lecturer only), `GET /api/internshipallocations/mine` (Lecturer **or** Organization, each scoped to their own side of the placement — see below) |
| Internship evaluations | `POST /api/internshipevaluations` (Organization only), `GET /api/internshipevaluations`, `GET /api/internshipevaluations/{id}`, `GET /api/internshipevaluations/by-student?studentId=...` (Admin/Lecturer/Organization, each scoped — see below), `PUT /api/internshipevaluations/{id}/report-grade` (Admin/Lecturer), `GET /api/internshipevaluations/compiled`, `GET /api/internshipevaluations/compiled/export/csv`, `GET /api/internshipevaluations/compiled/export/pdf` (Admin/Lecturer only) |
| Dissertation allocations | `POST/PUT/DELETE /api/dissertationallocations` (Admin only), `GET /api/dissertationallocations` (Admin/Lecturer), `GET /api/dissertationallocations/{id}` (Admin/Lecturer), `GET /api/dissertationallocations/mine` (Lecturer only) |
| Internship report submissions | `POST /api/internshipreports` (Student), `GET /api/internshipreports/mine` (Student), `GET /api/internshipreports`, `GET /api/internshipreports/{id}` (Admin/Lecturer, each scoped — see below), `GET /api/internshipreports/{id}/download`, `GET/POST /api/internshipreports/{id}/comments` (Admin/Lecturer/owning Student, each scoped — see below; `POST` is Admin/Lecturer only) |
| Dissertation write-up submissions | Same shape as above at `/api/dissertationsubmissions` — see [Student internship report & dissertation write-up submissions](#recent-changes-student-internship-report--dissertation-write-up-submissions) below |

Most write operations require `Admin` or `Lecturer`; reads on Courses/Assignments/Notes/Past questions/Course allocations/Students require any of `Admin`/`Lecturer`/`Student` (**not** `Organization` — see below) unless marked `AllowAnonymous`. The **Dissertations** area is stricter still — every endpoint, including reads, requires `Admin` or `Lecturer`, and within it a Lecturer only ever sees/manages the records they personally created; only Admin sees everything (including the `search`/`export` endpoints, which are Admin-only outright). **`Organization` accounts can reach only Organizations/Internship allocations/Internship evaluations** — everything else in this table returns `403` for them — and within those, an Organization is scoped to only the students explicitly placed with it; see [Internship evaluation module](#recent-changes-internship-evaluation-module) and [Organization access lock-down + student↔organization placement](#recent-changes-organization-access-lock-down--studentorganization-placement) below for the exact rules.

**Note on route naming**: routes follow `api/[controller]` with no manual kebab-casing, so a multi-word controller name becomes one lowercase word in the URL (case-insensitive) — e.g. `ActivityLogsController` → `/api/activitylogs` (**not** `/api/activity-logs`), `PastQuestionsController` → `/api/pastquestions`. Double-check this when wiring up the frontend's API client.

## Email configuration

Outbound email (registration, approval, welcome messages) goes through `IEmailSender`, implemented by `FallbackEmailSender` (`Infrastructure/Email/FallbackEmailSender.cs`). It tries **Twilio SendGrid** first (`SendGridEmailSender`); if SendGrid isn't configured or its send fails, it automatically retries via **Mailgun** (`MailgunEmailSender`). Configuration lives under `EmailSettings` in `appsettings*.json`:

```json
"EmailSettings": {
  "SenderEmail": "no-reply@compsci-portal.com",
  "SenderName": "CompSci Portal",
  "SendGrid": {
    "ApiKey": "",
    "BaseApiUrl": "https://api.sendgrid.com/v3/mail/send"
  },
  "Mailgun": {
    "ApiKey": "",
    "Domain": "",
    "BaseApiUrl": "https://api.mailgun.net/v3"
  }
}
```

- `SendGrid.ApiKey`, `Mailgun.ApiKey`, and `Mailgun.Domain` are intentionally left blank in source control — set them locally via `dotnet user-secrets` (or `EmailSettings__SendGrid__ApiKey` / `EmailSettings__Mailgun__ApiKey` / `EmailSettings__Mailgun__Domain` environment variables in production).
- `SenderEmail` must be a verified sender/domain in both your SendGrid and Mailgun accounts.
- `Mailgun.Domain` is the sending domain configured in your Mailgun account (e.g. `mg.compsci-portal.com`), not your app's domain — it's part of the API URL Mailgun requires. Use `https://api.eu.mailgun.net/v3` for `BaseApiUrl` if the domain is registered in Mailgun's EU region.
- If SendGrid isn't configured or returns an error, `FallbackEmailSender` logs it and tries Mailgun next. If both fail (or neither is configured), the failure is logged and swallowed — it never blocks registration or approval.

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
- **New email infrastructure**: `IEmailSender` / `FallbackEmailSender` (SendGrid primary, Mailgun fallback), `EmailSettings`, and `EmailTemplates` (welcome, pending, approved, rejected messages).
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

---

## Recent changes: Course allocation + PDF export

Added a course allocation feature so Admin can assign courses to lecturers (and, by extension, to the students in that program/year), and download the result as a PDF in the university's standard historical layout — e.g. `2021_2022 Second Semester Allocation.pdf`.

### Domain

- **New `CourseAllocation` entity** — one row of an allocation table: `AcademicYear` (e.g. `"2021/2022"`), `Semester` (`First`/`Second`), `ProgramName` (e.g. `"B.Sc. (Hons) Computer Science"`), `YearOfStudy` (1–6), `CourseCode`, `CourseDescription`, `CreditHours` (kept as text, e.g. `"3"` or `"3(P)"`, to preserve the "(P)" = practical marker used in the historical documents), `StaffName` (display text — a lecturer's name, or a department like `"Engl. Dept"` for staff without a login account).
- **`LecturerUserId`** (optional) links a row to an actual `User` (`Role: Lecturer`) account, so that lecturer can pull up their own allocation. Left `null` for rows staffed by something other than a lecturer account (e.g. `"Engl. Dept"`, `"Instructor"`).
- A "document" is simply every row sharing one `AcademicYear` + `Semester` — there's no separate document/header entity; the PDF export groups matching rows by `ProgramName` then `YearOfStudy` at render time, exactly like the historical spreadsheets (one table per program, split into FIRST/SECOND/THIRD YEAR sections, each ending in a SUB-TOTAL credit-hour row).

### Endpoints (`/api/courseallocations`)

All require authentication; **writes are Admin-only** (this is intentionally simpler than Dissertations/Courses, which also allow Lecturer writes — allocation is an Admin-only responsibility here):

| Endpoint | Access | Notes |
|---|---|---|
| `POST /api/courseallocations` | Admin | Create one row |
| `POST /api/courseallocations/bulk` | Admin | Create many rows in one call — body is `{ "allocations": [ {...}, {...} ] }`. This is the "simple for Admin" path: build one program's whole year-by-year table (or a whole semester across every program) and submit it in a single request instead of one call per course row. All rows are validated up front; if any row fails, nothing is saved and every row's errors are returned, prefixed `Row N: ...`. |
| `GET /api/courseallocations`, `/paged` | Any authenticated | Optional `?academicYear=&semester=&programName=` filters (all optional, combine with AND; `programName` is a case-insensitive contains match) |
| `GET /api/courseallocations/{id}` | Any authenticated | |
| `GET /api/courseallocations/mine` | Lecturer | Only the caller's own allocated courses, optional `?academicYear=&semester=` |
| `PUT /api/courseallocations/{id}`, `DELETE /api/courseallocations/{id}` | Admin | |
| `GET /api/courseallocations/export/pdf?academicYear=&semester=&programName=` | Any authenticated | Compiled PDF, standard layout. `academicYear` and `semester` are **required** — they drive both the title and the filename. `programName` optionally narrows to one program's table. |
| `GET /api/courseallocations/mine/export/pdf?academicYear=&semester=` | Lecturer | Same layout/filename as above, pre-filtered to the caller's own rows — this is what a Lecturer's "download my allocation" button should call. |

### PDF format

`GET /api/courseallocations/export/pdf?academicYear=2021/2022&semester=Second` downloads as **`2021_2022 Second Semester Allocation.pdf`** (`academicYear` with `/` → `_`, followed by ` <Semester> Semester Allocation.pdf`), containing:

- Title: `SECOND SEMESTER COURSE ALLOCATION -2021/22` (the year is shortened the same way the historical documents do it).
- One bordered table per matching program, headed by the program name, columns `Course Code | Course Description | Credit Hrs | Staff`.
- Each table is split into year-of-study sections (`FIRST YEAR`, `SECOND YEAR`, ...), each ending in a `SUB-TOTAL` row summing that section's credit hours (parsed from the leading digits of `CreditHours`, so `"3(P)"` contributes `3`).

Frontend integration note: like the Dissertation exports, this is a raw file stream, not `ApiResponse<T>` — trigger it with a `fetch()` + `Authorization` header + blob download, or a signed-link pattern, not a plain `<a href>`.

### What changed under the hood

- `Core/Entities/CourseAllocation.cs`, `Core/Enums/Semester.cs`, `Core/DTOs/CourseAllocationDtos.cs`.
- `CourseAllocationValidator` (`Core/Validators/Validators.cs`) — validates the `"YYYY/YYYY"` academic year shape among other fields.
- `ICourseAllocationRepository`/`CourseAllocationRepository`, wired into `IUnitOfWork`/`UnitOfWork`.
- `CourseAllocationService` — filtering is in-memory over `GetAllAsync()` (fine at this data volume, same tradeoff already made for Dissertation search — would need a DB-level query if the table grows very large); validates `LecturerUserId` references an existing `Role: Lecturer` account when supplied.
- PDF rendered by `Infrastructure/Reports/CourseAllocationPdfBuilder.cs` using the existing **PdfSharpCore** dependency (same one used for Dissertation PDF export).
- New EF Core migration `AddCourseAllocations` (new, purely additive table — no existing data affected).
- `[LogActivity("CourseAllocation", ...)]` on the single-row Create/Update/Delete endpoints, consistent with the site-wide activity log (bulk create isn't individually logged, since it doesn't map to a single entity ID).

Verified: project builds clean (`dotnet build`) and the migration was generated and reviewed (`dotnet ef migrations add AddCourseAllocations`) — purely additive `CourseAllocations` table, no changes to existing tables.

---

## Recent changes: Internship evaluation module

Digitizes the paper "Student Internship Evaluation Form" (School of Technology). A host
organization scores a student's internship performance; the student's allocated lecturer
separately grades the internship report; the system compiles both into a final grade per program.

> **`studentId` in every request/query below is the human-readable Student ID** — the `Students.StudentId` column (e.g. `"24807"`), the same value shown on a student's profile. It is **not** the internal database `Guid` (`Students.Id`). The server resolves it server-side via `IStudentRepository.GetByStudentIdAsync`; an unknown value returns a `400` (on create/allocate) or an empty result (on filters/lookups), not a type-conversion error. Response bodies do return an internal `studentId` `Guid` field alongside a `studentIdNumber` string field — those two are **not interchangeable**: `studentIdNumber` is what you send back in a future request, `studentId` in a response is just the record's internal FK.

### New role: `Organization`

- **`UserRole.Organization`** (`3`) — a fourth login role alongside `Admin`/`Lecturer`/`Student`. An Organization account can **only** reach the Organizations/InternshipAllocations/InternshipEvaluations endpoints described in this section — it has **no access at all** to Courses, Assignments, Notes, Past questions, Dissertations, Course allocations, or the general `GET /api/students*` endpoints (locked down further down this section — see [Organization access lock-down + student↔organization placement](#recent-changes-organization-access-lock-down--studentorganization-placement)).
- **`Organization` entity** (`Core/Entities/Organization.cs`) — mirrors `Student`: `Name`, linked 1:1 to a `User` via `UserId`.
- **Registered by Admin/Lecturer**, not self-service — from the email the organization sent in:
  - `POST /api/organizations` — body `{ "email", "name", "defaultPassword" }`. Creates the `User` (auto-derived unique `Username` slugified from `name`, e.g. `"Acme Ltd"` → `acmeltd`) + `Organization` profile, and emails the credentials.
  - `GET /api/organizations`, `GET /api/organizations/{id}` — list/inspect registered organizations, including `credentialsExpireAt` and a computed `credentialsExpired` flag.
  - `POST /api/organizations/{id}/reissue-credentials` — generates a new random password + fresh 2-week window, emails it. Use this once `credentialsExpired` is `true`.
  - `DELETE /api/organizations/{id}` — Admin only.
- **Credentials expire 2 weeks after issue/reissue** (`User.CredentialsExpireAt`). Once that passes, `POST /api/auth/login` rejects the organization with `401` and the message *"Your credentials have expired. Contact an administrator or lecturer to reissue them."* — reissue is required, there's no self-service recovery. If the organization changes its own password via `POST /api/auth/change-password` before expiry, the expiry clock is cleared (a password the org chose itself never expires).
- Login/response shape is unchanged otherwise — `AuthResponse.role` is `"Organization"` like any other role.

### Internship placement (student ↔ organization ↔ lecturer)

> **Updated by the change described in [Organization access lock-down + student↔organization placement](#recent-changes-organization-access-lock-down--studentorganization-placement) below** — `InternshipAllocation` now also records the host Organization, not just the Lecturer, and it's now a **prerequisite** (not an optional lookup) before an Organization can submit an evaluation. Read that section for the full behavior change; this subsection reflects the current shape.

Separate from `CourseAllocation` — this is the internship placement record: which Organization a
student is doing their internship with, and which Lecturer grades their internship *report* (not
a course). It's the single record that scopes both the Organization's and the Lecturer's access.

- **`InternshipAllocation` entity** — `StudentId` (internal FK), `OrganizationUserId` (internal FK to the host org's `User.Id`), `LecturerUserId`, `AcademicYear` (`"YYYY/YYYY"`), `Semester`. One row per student per academic year + semester (enforced by a unique index) — a student can only be placed with one organization at a time; Admin gets a friendly `400` on a duplicate rather than a DB error.
- **`/api/internshipallocations`** — `POST`/`PUT`/`DELETE` are Admin-only; `GET`/`GET /{id}` (unfiltered, every placement) are Admin/Lecturer only; `GET /mine` (optional `?academicYear=&semester=`) is role-aware — a **Lecturer** gets the students allocated to them for report grading, an **Organization** gets the students placed with them for evaluation. This is what both a Lecturer's "my allocated students" screen *and* an Organization's "students I can evaluate" screen should call — Organization has no other way to discover which students it can act on.
- `POST`/`PUT` body takes the human-readable Student ID string (see callout above) plus the host Organization's own `Organization.Id` (from `GET /api/organizations`, **not** its `User.Id`) and the Lecturer's `User.Id`, e.g.:
  ```json
  {
    "studentId": "24807",
    "organizationId": "b6e2b6b0-...",
    "lecturerUserId": "ae3bb1a3-1eb4-4fd8-b976-bebe20409773",
    "academicYear": "2019/2020",
    "semester": 1
  }
  ```
- **Admin must create this placement before the organization can do anything for that student.** `POST /api/internshipevaluations` (below) now checks that a placement exists matching the submitting organization + student + academic year + semester, and rejects with `400` otherwise — see the endpoint table.

### The evaluation form

- **`InternshipEvaluation` entity** reproduces the paper form's rating rows, each scored **1 (Poor) – 4 (Excellent)**: rapport with supervisor, rapport with staff/client, communicates well, seeks new knowledge, shows initiative, manages time well, produces accurate reports, demonstrates adequate knowledge, dresses professionally, is punctual, is dependable, accepts constructive criticism, demonstrates enthusiasm — **13 fixed criteria** (the paper form's "Personal Qualities" row was dropped by request; it is no longer a field anywhere in the API). Plus the form's open "Other ratings, please specify" row (`otherRatingLabel`/`otherRatingScore`) — recorded but **excluded** from the scored total. 13 × 4 = 52, so the total is scaled `÷52` (not `÷56`).  Also carries company supervisor name/phone, academic year, semester, internship start date/months, comments, supervisor signature name, and certification date.
- **Scoring** (`Core/Services/GradeCalculator.cs`, single source of truth for both endpoints below):
  - `rawRatingTotal` = sum of the 13 fixed ratings (0–52).
  - `evaluationScore` = `rawRatingTotal / 52 × 70`, rounded to 2 decimals (0–70).
  - `reportScore` — entered separately by the lecturer, 0–30.
  - `totalScore` = `evaluationScore + reportScore` (0–100), computed only once `reportScore` is set.
  - `grade`: `75–100 → A`, `60–74 → B`, `50–59 → C`, `40–49 → D`, `30–39 → E`, `0–29 → F`.

### `/api/internshipevaluations` endpoints

| Endpoint | Access | Notes |
|---|---|---|
| `POST /api/internshipevaluations` | Organization | Submits the 13-rating form for one `studentId`. **Requires an existing `InternshipAllocation` placing that student with the calling organization for the given `academicYear`+`semester`** — no placement (or one belonging to a different organization) returns `400` with a message telling the org to ask Admin to set up the placement. On success, server snapshots the student's name/ID/program/year at submission time (so the record is stable even if the profile changes later), computes `rawRatingTotal`/`evaluationScore`, and fills `allocatedLecturerUserId` from the placement. |
| `GET /api/internshipevaluations`, `/by-student?studentId=` | Admin, Lecturer, Organization | **Scoped**: Admin sees everything; Organization sees only what it submitted; Lecturer sees only evaluations allocated to it. |
| `GET /api/internshipevaluations/{id}` | Admin, Lecturer, Organization | Same scoping — an out-of-scope ID returns `404`, not `403` (consistent with the Dissertation ownership pattern). |
| `PUT /api/internshipevaluations/{id}/report-grade` | Admin, Lecturer | Body `{ "reportScore": 27 }` (0–30). Only the lecturer named in `allocatedLecturerUserId` (or Admin) may call this — anyone else gets `401`. Recomputes `totalScore`/`grade`. |
| `GET /api/internshipevaluations/compiled?programName=&academicYear=&semester=` | Admin, Lecturer | All filters optional/combine with AND. Returns records grouped by program: `[{ "programName", "rows": [{ "studentFullName", "studentIdNumber", "evaluationScore", "reportScore", "grade" }] }]` — exactly the requested `(Name, ID, Evaluation Score, Report Score, Grade)` compiled format, one section per program (Business and Information Technology / Computer Science / Electronics and Telecommunication, or whatever `Student.ProgramName` values exist). |
| `GET /api/internshipevaluations/compiled/export/csv`, `/compiled/export/pdf` | Admin, Lecturer | Same filters as `/compiled`; raw file download (`internship_grades_yyyyMMdd_HHmmss.csv`/`.pdf`), same "not `ApiResponse<T>`, use a blob-download pattern" caveat as the Dissertation/CourseAllocation exports above. |

Example: a perfect scorecard (all 13 ratings = 4) gives `rawRatingTotal: 52` → `evaluationScore: 70.00`. After the lecturer grades the report `27/30`, `totalScore: 97.00`, `grade: "A"`.

### What changed under the hood

- `Core/Enums/UserRole.cs` (+`Organization`), `Core/Entities/User.cs` (+`CredentialsExpireAt`), `Core/Entities/{Organization,InternshipAllocation,InternshipEvaluation}.cs`.
- `Core/DTOs/{OrganizationDtos,InternshipAllocationDtos,InternshipEvaluationDtos}.cs`; validators appended to `Core/Validators/Validators.cs` (`OrganizationValidator`, `InternshipAllocationValidator`, `InternshipEvaluationValidator`).
- `IOrganizationRepository`/`IInternshipAllocationRepository`/`IInternshipEvaluationRepository` + implementations, wired into `IUnitOfWork`/`UnitOfWork`; corresponding `I*Service`/`*Service` classes; `GradeCalculator`.
- `AuthService.LoginAsync` (credentials-expired check) and `ChangePasswordAsync` (clears the expiry) updated.
- `EmailTemplates.OrganizationCredentialsIssued` — new template used by both org registration and credential reissue.
- CSV via `Core/Services/Export/InternshipGradeCsvBuilder.cs`; PDF via `Infrastructure/Reports/InternshipCompiledPdfBuilder.cs` (same **PdfSharpCore** dependency as the other two PDF exports — one bordered table per program).
- `Api/Controllers/{OrganizationsController,InternshipAllocationsController,InternshipEvaluationsController}.cs`; `[LogActivity(...)]` on all write endpoints, consistent with the site-wide activity log.
- New EF Core migration `AddInternshipEvaluationModule` (purely additive: `Users.CredentialsExpireAt` column + three new tables — no existing data affected).

### Not yet done / frontend should know

- **Not verified against a live database in this environment** (no reachable Postgres instance here) — `dotnet build` succeeds and every migration was generated cleanly, but run the full create-org → allocate-placement → login-as-org → submit-evaluation → grade-report → compiled-export flow via Swagger yourself before wiring up the UI.
- `EvaluationForm.pdf` (the paper sample this module was modeled on) is sitting at the repo root, currently untracked in git — move it into a `docs/` folder or similar before committing if you'd rather it not live at the root.
- The Organization-scoping gap flagged in an earlier version of this doc (an org could browse the full student list) is now closed — see the next section.

---

## Recent changes: Organization access lock-down + student↔organization placement

Two access-control gaps in the Organization role (above) are now closed:

1. An Organization account could reach `Courses`/`Assignments`/`Notes`/`PastQuestions`/`CourseAllocations`/`Students` — every one of those was `[Authorize]` with no role restriction, so *any* authenticated role (Organization included) could read them. **An Organization can now only ever reach `Organizations`/`InternshipAllocations`/`InternshipEvaluations`.**
2. An Organization could submit an evaluation for **any** student on the platform (via the open `GET /api/students*` endpoints to look one up) — there was no concept of "this student belongs to this organization". **A student must now be explicitly placed with an organization (via `InternshipAllocation`, extended below) before that organization can see or evaluate them.**

### 1. Organization locked out of course-management areas

No new code — a **narrower `[Authorize]`** on six existing controllers. Each was `[Authorize]` (any authenticated role); each is now `[Authorize(Roles = "Admin,Lecturer,Student")]`, explicitly excluding `Organization`:

| Controller | Base route |
|---|---|
| `CoursesController` | `/api/courses` |
| `AssignmentsController` | `/api/assignments` |
| `NotesController` | `/api/notes` |
| `PastQuestionsController` | `/api/pastquestions` |
| `CourseAllocationsController` | `/api/courseallocations` |
| `StudentsController` | `/api/students` |

An Organization account calling any endpoint under these six routes now gets **`403 Forbidden`**, at every verb (previously reads succeeded for any authenticated role). `Dissertations`/`ActivityLogs` needed no change — they were already `Admin,Lecturer`-only.

**Frontend action**: if the Organization-facing UI is a separate app/shell, it should never call these six route groups at all — don't just hide the nav links, since the API now hard-rejects them. If it shares a component/router with the Admin/Lecturer/Student UI, gate every Courses/Assignments/Notes/Past-Questions/Course-Allocations/Students screen and nav entry behind `role !== "Organization"`, and handle the `403` defensively in case a stale link is followed.

### 2. `InternshipAllocation` now also carries the host Organization

The entity/endpoint from the [Internship evaluation module](#recent-changes-internship-evaluation-module) section above was extended rather than replaced — re-read that section's "Internship placement" subsection for the full current shape. Summary of what actually changed:

- **New required field `OrganizationUserId`** on `InternshipAllocation` (internal FK to the host org's `User.Id`) alongside the existing `LecturerUserId`. One row now fully describes a placement: *this student, at this organization, graded by this lecturer, for this period.*
- **`POST`/`PUT /api/internshipallocations`** request body gained a required `organizationId` field — the **`Organization.Id`** from `GET /api/organizations` (not the org's `User.Id`, and not `LecturerUserId`'s shape — see the worked example in the section above). Omitting it now returns a `400` validation error.
- **`GET /api/internshipallocations/mine`** is no longer Lecturer-only — it's now `[Authorize(Roles = "Lecturer,Organization")]` and branches on the caller's role: a Lecturer gets their allocated students (unchanged), an **Organization gets the students placed with it**. This is the endpoint an Organization's dashboard should call to populate "students I can evaluate" — there is no other way for an Organization to discover this.
- **`GET`/`GET /{id}` on `/api/internshipallocations` (the unfiltered, cross-organization list/lookup) are now `Admin,Lecturer` only** — an Organization calling either gets `403`. Previously these were open to any authenticated role, which would have let an Organization browse every other organization's placements too.
- **`POST /api/internshipevaluations` now requires the placement to exist first.** Previously, submitting without a matching `InternshipAllocation` was allowed and just left `allocatedLecturerUserId` null. Now it's a hard `400`: *"Student '{studentId}' is not placed with your organization for {academicYear} {semester} semester. Ask an administrator to set up the internship placement first."* This is enforced server-side regardless of what the UI shows, so it also covers an org retrying with a stale/tampered `studentId`.

**Frontend action for the Organization experience**:
- Replace any "pick a student" UI backed by `/api/students*` with `GET /api/internshipallocations/mine` — render its `studentFullName`/`studentIdNumber`/`programName` fields (each row already carries them) instead of fetching student profiles separately.
- On the evaluation form's submit, handle the new `400` case above distinctly from field-validation errors (e.g. show "no placement set up — contact your university coordinator" rather than a generic form error) — it means the student truly isn't assigned to this org for that period, not that the form was filled in wrong.
- Admin's "internship allocation" screen needs an **Organization picker** (populated from `GET /api/organizations`, submitting back its `id` as `organizationId`) alongside the existing Student-ID field and Lecturer picker — a placement can't be created without all three.

### What changed under the hood

- `Core/Entities/InternshipAllocation.cs` (+`OrganizationUserId`); `Infrastructure/Data/AppDbContext.cs` (+index on it).
- `Core/DTOs/InternshipAllocationDtos.cs` (+`OrganizationId` on the request, +`OrganizationId`/`OrganizationUserId`/`OrganizationName` on the response, +`OrganizationUserId` on the filter); `InternshipAllocationValidator` (+required-field check).
- `InternshipAllocationService` — resolves `organizationId` → `Organization` → its `UserId` (mirrors the existing Lecturer-lookup pattern), maps organization name/id into every response.
- `InternshipAllocationsController` — class-level `[Authorize(Roles = "Admin,Lecturer,Organization")]` (was blanket `[Authorize]`) with explicit narrower `[Authorize(Roles = "Admin,Lecturer")]` on `GetAll`/`GetById`/`Create`/`Update`/`Delete`, and `GetMine` broadened + branched by role (Lecturer/Organization AND-combine with the class-level list — see the note in that file if you're touching authorization here again: stacked `[Authorize]` attributes combine with AND, not OR, so the class-level list has to be a *superset* of everything any method needs to allow).
- `InternshipEvaluationService.CreateAsync` — added the placement-ownership check before building the evaluation record.
- `CoursesController`/`AssignmentsController`/`NotesController`/`PastQuestionsController`/`CourseAllocationsController`/`StudentsController` — class-level `[Authorize]` narrowed to `[Authorize(Roles = "Admin,Lecturer,Student")]`.
- New EF Core migration `AddOrganizationToInternshipAllocation` (purely additive: one new column + index on the existing `InternshipAllocations` table — no existing data affected).

Verified: `dotnet build` succeeds (0 errors/warnings) and the migration was generated and reviewed — not yet exercised against a live database in this environment (same caveat as the base module above); run the full flow via Swagger before shipping the frontend changes.

---

## Recent changes: dropped "Personal Qualities" from the internship evaluation rating

The evaluation form's fixed rating set went from 14 criteria to **13** — "Personal Qualities" was removed entirely, by request.

**This is a breaking API change for anything already built against the module above** — every place that referenced 14 criteria / ÷56 has been corrected in this doc (see the [Internship evaluation module](#recent-changes-internship-evaluation-module) section) to 13 criteria / ÷52. Specifically:

- **`personalQualities` no longer exists** on `InternshipEvaluationRequest` or `InternshipEvaluationResponse` — remove it from any form field list, request payload, and results table/column the frontend has built.
- **The scoring divisor changed from 56 to 52** (13 fixed criteria × 4 max, not 14 × 4). `evaluationScore = rawRatingTotal / 52 × 70`. A full-marks scorecard (all 13 ratings = 4) still yields `evaluationScore: 70.00` — the scale's top end is unchanged, only the criteria count and the intermediate math changed. **If the frontend does any of its own score preview/estimate math client-side (e.g. a running total while the form is filled in), update that divisor too.**
- No other field, endpoint, or access rule changed — the 13 remaining criteria keep their existing names/order (rapport with supervisor, rapport with staff/client, communicates well, seeks new knowledge, shows initiative, manages time well, produces accurate reports, demonstrates adequate knowledge, dresses professionally, is punctual, is dependable, accepts constructive criticism, demonstrates enthusiasm), and the "Other ratings, please specify" row is unaffected (still excluded from the total).

### What changed under the hood

- `Core/Entities/InternshipEvaluation.cs`, `Core/DTOs/InternshipEvaluationDtos.cs` (both Request and Response), `InternshipEvaluationValidator` — `PersonalQualities`/`personalQualities` removed.
- `InternshipEvaluationService` — removed from the `rawRatingTotal` sum and both entity/response mappings.
- `GradeCalculator.EvaluationScoreFromRawTotal` — divisor `56m` → `52m`.
- New EF Core migration `RemovePersonalQualitiesRating` — drops the `PersonalQualities` column from `InternshipEvaluations`. **This is a data-loss migration** (EF's scaffolder flags it as such): any `PersonalQualities` scores already recorded on existing evaluation rows are discarded when this migration runs. If real data has been collected before applying it, export/back up that column first if it needs to be kept for any historical record.

Verified: `dotnet build` succeeds (0 errors/warnings) and the migration was generated and reviewed (a clean, single `DropColumn` — no other schema changes); not yet exercised against a live database in this environment.

---

## Recent changes: student internship report & dissertation write-up submissions

Students can now self-upload their own internship report and their own dissertation/final-year-project
write-up (as opposed to the pre-existing `Dissertation` area, which is an official record entered
*by* Admin/Lecturer, not uploaded *by* the student — the two are unrelated and live at different
routes). Only Admin and the Lecturer assigned to that student can see the file, comment on it, and
only the owning student can see those comments. Both sides get an email notification.

### Domain

- **`DissertationAllocation`** (`Core/Entities/DissertationAllocation.cs`) — new entity, the missing
  piece needed to know *which Lecturer supervises which student's dissertation*. It's the exact
  counterpart of the existing `InternshipAllocation` (which already assigns a Lecturer to grade a
  student's internship report) but for dissertation supervision: `StudentId`, `LecturerUserId`,
  `AcademicYear` (`"YYYY/YYYY"`). One row per student per academic year (unique index). Managed at
  `/api/dissertationallocations`, mirroring `/api/internshipallocations`'s access rules: `POST`/`PUT`/`DELETE`
  Admin-only, `GET`/`GET /{id}` Admin/Lecturer, `GET /mine` Lecturer-only (their own assigned students).
  **This has to exist before a student's dissertation write-up is visible to any Lecturer** — until
  Admin creates it, only Admin can see that student's uploaded write-up. (No equivalent step is
  needed for internship reports — `InternshipAllocation` already existed for grading purposes.)
- **`StudentSubmission`** (`Core/Entities/StudentSubmission.cs`) — the uploaded file itself. One row
  per student per `SubmissionType` (`InternshipReport` / `Dissertation`), enforced by a unique index
  on `(StudentId, Type)`. **Re-uploading overwrites the existing row's file in place** (deletes the
  old stored file, replaces `FilePath`/`OriginalFileName`, bumps `SubmissionCount` and `UpdatedAt`)
  — it does **not** create a new record or keep prior versions. `SubmittedAt` is the original
  submission time; `UpdatedAt` reflects the most recent re-submission.
- **`SubmissionComment`** (`Core/Entities/SubmissionComment.cs`) — a comment thread on one
  `StudentSubmission`, `AuthorUserId` + `Text` + `CreatedAt`. Comments are never edited/deleted via
  the API (append-only), consistent with how ActivityLog is handled elsewhere in this app.
- **`SubmissionType`** (`Core/Enums/SubmissionType.cs`) — `InternshipReport = 0`, `Dissertation = 1`.

### Access rules (enforced in `StudentSubmissionService`, shared by both endpoint groups below)

- **A Student** can upload/re-upload only their own submission (resolved from their JWT, not a
  client-supplied student ID), and can view/download only their own submission + its comments.
- **Admin** can see/download/comment on every submission of a given type, no restrictions.
- **A Lecturer** can see/download/comment on a submission **only if they are the Lecturer currently
  assigned to that student** for that submission type — via `InternshipAllocation` for
  `InternshipReport`, via `DissertationAllocation` for `Dissertation`. This is **not** scoped to a
  single academic year — any allocation row, past or present, naming that Lecturer for that student
  grants access, treating the assignment as a durable relationship. A Lecturer with no matching
  allocation gets the same `404` a nonexistent ID would give (consistent with the Dissertation
  ownership-scoping pattern elsewhere in this app — existence of another Lecturer's/student's
  submission is never revealed).
- A student can upload before any allocation exists — the file is safely stored, but until Admin
  creates the allocation, **only Admin** can see it (no Lecturer is "assigned" yet, so the
  Lecturer-side check has nothing to match).

### `/api/internshipreports` and `/api/dissertationsubmissions` endpoints (identical shape, different `SubmissionType`)

| Endpoint | Access | Notes |
|---|---|---|
| `POST /api/internshipreports`, `POST /api/dissertationsubmissions` | Student | Multipart form, field name `file`. PDF/DOC/DOCX, up to 50MB (same limits as the existing Dissertation/Notes/Assignments uploads). Re-upload overwrites the previous file — there is no separate "update" endpoint. On success, emails every Lecturer currently assigned to the caller for this type (`EmailTemplates.SubmissionUploaded`); silently skips the email if no Lecturer is assigned yet. |
| `GET /api/internshipreports/mine`, `GET /api/dissertationsubmissions/mine` | Student | The caller's own submission. `404` if they haven't uploaded one yet. |
| `GET /api/internshipreports`, `GET /api/dissertationsubmissions` | Admin, Lecturer | Admin: every submission of this type. Lecturer: only submissions from students assigned to them. |
| `GET /api/internshipreports/{id}`, `GET /api/dissertationsubmissions/{id}` | Admin, Lecturer | Same scoping as above; out-of-scope ID → `404`. |
| `GET /api/internshipreports/{id}/download`, `GET /api/dissertationsubmissions/{id}/download` | Admin, Lecturer, Student | Admin/assigned Lecturer/**owning student only** — raw file stream, not `ApiResponse<T>` (same "use a blob-download pattern" caveat as every other file download in this API). |
| `GET /api/internshipreports/{id}/comments`, `GET /api/dissertationsubmissions/{id}/comments` | Admin, Lecturer, Student | Same three-way scoping as download — this is what lets the owning student read comments left on their own work, and nothing else. |
| `POST /api/internshipreports/{id}/comments`, `POST /api/dissertationsubmissions/{id}/comments` | Admin, Lecturer | Body `{ "text": "..." }` (max 2000 chars). Only Admin/the assigned Lecturer may post — the student themself cannot comment on their own submission via this endpoint. On success, emails the owning student (`EmailTemplates.SubmissionCommented`). |

Example upload response:
```json
{
  "success": true,
  "message": "Internship report submitted successfully.",
  "data": {
    "id": "5b1e...",
    "studentId": "b6e2...",
    "studentFullName": "Alice Test",
    "studentIdNumber": "CS/2023/001",
    "programName": "Computer Science",
    "type": 0,
    "typeText": "InternshipReport",
    "filePath": "uploads/internship-reports/....pdf",
    "originalFileName": "my-report.pdf",
    "submissionCount": 2,
    "submittedAt": "2026-08-15T10:02:00Z",
    "updatedAt": "2026-09-02T17:10:00Z",
    "commentCount": 1
  },
  "errors": null
}
```

Example comment response (`POST .../{id}/comments`):
```json
{
  "success": true,
  "message": "Comment added successfully.",
  "data": {
    "id": "f2a0...",
    "studentSubmissionId": "5b1e...",
    "authorUserId": "3e64...",
    "authorUsername": "lect3",
    "authorRole": "Lecturer",
    "text": "Please expand section 3 with more detail on your methodology.",
    "createdAt": "2026-09-02T17:12:00Z"
  },
  "errors": null
}
```

### Frontend integration notes

- **Route naming**: same convention as the rest of the API — `InternshipReportsController` →
  `/api/internshipreports`, `DissertationSubmissionsController` → `/api/dissertationsubmissions`,
  `DissertationAllocationsController` → `/api/dissertationallocations` (no hyphens, all lowercase).
- **Don't confuse this with `/api/dissertations`** — that's the pre-existing, unrelated
  Admin/Lecturer-authored official record (grade, topic, department, etc.). This new area is the
  student's own working-document upload + review/comment loop, entirely separate data.
- Admin's "assign dissertation supervisor" screen needs a simple Student-ID + Lecturer picker +
  academic-year form posting to `POST /api/dissertationallocations` — no Organization/Semester field
  needed here (unlike `InternshipAllocation`), since dissertation supervision isn't tied to a host
  organization or a specific semester.
- A student's "my internship report" / "my dissertation" screen should call `GET .../mine` first to
  render current status (handle its `404` as "nothing uploaded yet", not an error state), show
  `submissionCount`/`submittedAt`/`updatedAt` so the student knows a re-upload succeeded, and fetch
  `GET .../{id}/comments` underneath to render the lecturer's feedback thread.
- A Lecturer's dashboard should call `GET /api/internshipreports` and `GET /api/dissertationsubmissions`
  (both auto-scoped to their own assigned students) rather than trying to filter a general list
  client-side — there is no `/mine` alias here since the unfiltered `GET` is already scoped for a
  Lecturer caller (only Admin sees everything on that same endpoint).
- Notification emails are fire-and-forget from the frontend's perspective — like every other email in
  this app, delivery failures are logged server-side and never block the underlying request (upload
  still succeeds, comment still posts, even if both SendGrid and Mailgun are down).

### What changed under the hood

- `Core/Entities/{DissertationAllocation,StudentSubmission,SubmissionComment}.cs`, `Core/Enums/SubmissionType.cs`.
- `Core/DTOs/{DissertationAllocationDtos,StudentSubmissionDtos}.cs`; `DissertationAllocationValidator`/`SubmissionCommentValidator` appended to `Core/Validators/Validators.cs`.
- `I{DissertationAllocation,StudentSubmission,SubmissionComment}Repository` + implementations, wired into `IUnitOfWork`/`UnitOfWork`; `IDissertationAllocationService`/`DissertationAllocationService` (mirrors `InternshipAllocationService`); `IStudentSubmissionService`/`StudentSubmissionService` (one shared implementation parametrized by `SubmissionType`, backing both endpoint groups).
- `EmailTemplates.SubmissionUploaded` / `SubmissionCommented` — new templates sent via the existing `IEmailSender`/`FallbackEmailSender` pipeline, no new email infrastructure needed.
- `Api/Controllers/DissertationAllocationsController.cs` (mirrors `InternshipAllocationsController`); `Api/Controllers/StudentSubmissionsControllerBase.cs` (shared abstract base holding every upload/view/download/comment action) with two thin concrete subclasses, `InternshipReportsController` and `DissertationSubmissionsController`, that only override the `SubmissionType` and a couple of display strings.
- `[LogActivity(...)]` on the write endpoints (`StudentSubmission`/`SubmissionComment`/`DissertationAllocation` entity types), consistent with the site-wide activity log.
- New EF Core migration `AddStudentSubmissions` (purely additive: three new tables — `DissertationAllocations`, `StudentSubmissions`, `SubmissionComments` — no changes to any existing table).

Verified: `dotnet build` succeeds (0 errors/warnings) and the migration was generated and reviewed (a clean, additive 3-table migration); not yet exercised against a live database in this environment — run the full assign-supervisor → upload → re-upload (confirm overwrite, not duplicate) → comment → view-as-student flow via Swagger before wiring up the UI, for both `internshipreports` and `dissertationsubmissions`.
