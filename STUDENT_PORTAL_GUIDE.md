# Student Portal — Frontend Integration Guide

This is a focused guide for building the **student-facing** frontend against the CompSci API. It
only covers what a `Student` account can actually do: authenticate, view courses (with the staff/
lecturer allocated to each), view/download Notes, Assignments, and Past Question papers, and
upload/track their own internship report and dissertation/project write-up.

For the full API (Admin/Lecturer endpoints, dissertations, activity log, etc.) see the main
[`README.md`](README.md). Everything here is a subset of that.

## 1. Base setup

- All endpoints are under `/api`. Swagger UI is available at `/swagger` in Development.
- Every endpoint below (except registration/login) requires an `Authorization: Bearer <token>`
  header — get the token from `POST /api/auth/login`.
- Every response is wrapped the same way:
  ```json
  { "success": true, "message": "...", "data": { }, "errors": null }
  ```
  On failure, `success` is `false`, `data` is `null`, and `errors` is a list of validation messages
  (may be `null` for non-validation errors, e.g. 404s).
- Paginated endpoints (`.../paged`) return `data` shaped as:
  ```json
  { "data": [ ... ], "pageNumber": 1, "pageSize": 10, "totalRecords": 42, "totalPages": 5 }
  ```

## 2. Getting a student into the app

There are two ways a student account comes to exist — the frontend should support at least the
first, and handle the pending-approval state either way:

### 2a. Self-registration (student-initiated)
```
POST /api/auth/register-student   (no auth required)
```
Body:
```json
{
  "username": "jdoe2026",
  "email": "jdoe2026@example.com",
  "password": "Passw0rd!",
  "firstName": "John",
  "lastName": "Doe",
  "studentId": "CS/2026/001",
  "programName": "Computer Science",
  "year": 1,
  "enrollmentYear": 2026,
  "expectedGraduation": 2030
}
```
Response (`201`) — **no token is issued**, the account is not yet usable:
```json
{
  "success": true,
  "message": "Registration received and pending approval.",
  "data": {
    "userId": "efb58eff-...",
    "studentProfileId": "75b206bb-...",
    "email": "jdoe2026@example.com",
    "message": "Registration received. An administrator or lecturer must approve your account before you can log in."
  }
}
```
Show a "registration received, check your email once approved" confirmation screen — don't
redirect to login yet. An email is sent automatically at this point and again once approved.

### 2b. Admin/Lecturer creates the student directly
The student receives a **welcome email with a temporary password**. They should log in with it and
immediately be prompted to change it via `POST /api/auth/change-password` (see §4).

### Login
```
POST /api/auth/login   (no auth required)
```
Body: `{ "email": "...", "password": "..." }`

Success (`200`):
```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "id": "efb58eff-...",
    "username": "jdoe2026",
    "email": "jdoe2026@example.com",
    "role": "Student",
    "lecturerId": null,
    "token": "eyJhbGciOi...",
    "tokenExpiration": "2026-08-28T12:19:50Z"
  }
}
```
Store `token` (e.g. in memory + a secure/httpOnly-cookie-backed refresh strategy — this API doesn't
issue refresh tokens, so plan for re-login once `tokenExpiration` passes) and attach it as
`Authorization: Bearer <token>` on every request below.

If the account is a self-registered student **not yet approved**, login instead returns:
```
401 Unauthorized
{ "success": false, "message": "Your registration is pending approval by an administrator or lecturer.", "statusCode": 401 }
```
Detect this exact message (or just the 401 status right after a self-registration flow) and show a
"still pending approval" state rather than a generic login-failed error.

## 3. What a Student account can and can't do

| Area | Student can |
|---|---|
| Courses | View list/detail (read-only) |
| Course allocations | View list, **download** the compiled PDF for their program |
| Assignments | View list/detail, **download** the attached file |
| Notes | View list/detail, **download**, and also **upload** their own note |
| Past questions | View list/detail, **download** |
| Internship report | **Upload/re-upload** their own, view its status, read comments left on it |
| Dissertation write-up | **Upload/re-upload** their own, view its status, read comments left on it |
| Dissertations (the official Admin/Lecturer-authored record), Activity log, other Students' records, user management | ❌ No access (403) |

Students **cannot** create/edit/delete Courses, Assignments, or Past Questions, and cannot edit or
delete Notes (including ones they uploaded themselves) — only Admin/Lecturer can. Students also
cannot comment on their own internship report/dissertation submission — only Admin/their assigned
Lecturer can leave comments; the student can only read them. The write actions available to a
Student are: uploading a new Note, and uploading/re-uploading their internship report and
dissertation write-up.

## 4. Endpoint reference

All of these require `Authorize` (any authenticated role); none require Admin/Lecturer unless noted.

### Courses — read-only
```
GET /api/courses                          → CourseResponse[]
GET /api/courses/paged?pageNumber=&pageSize=
GET /api/courses/{id}                     → CourseResponse
```
`CourseResponse`:
```json
{
  "id": "guid",
  "courseCode": "CSC401",
  "courseName": "Software Engineering",
  "creditHour": 3,
  "staff": "Dr. Jane Smith",
  "createdAt": "2026-01-01T00:00:00Z",
  "updatedAt": null
}
```
`staff` is the lecturer allocated to the course — it's a plain text field (not a link to a user
account), so just display it as-is; there's no "click through to lecturer profile" today.

### Course allocations — view + download the compiled PDF
```
GET /api/courseallocations?academicYear=&semester=&programName=       → CourseAllocationResponse[]
GET /api/courseallocations/paged?pageNumber=&pageSize=&academicYear=&semester=&programName=
GET /api/courseallocations/{id}                                       → CourseAllocationResponse
GET /api/courseallocations/export/pdf?academicYear=&semester=&programName=   → raw PDF (see §5)
```
`academicYear` (e.g. `"2021/2022"`) and `semester` (`First`/`Second`) are required on `export/pdf`;
pass `programName` to narrow either endpoint to just the student's own program (e.g.
`"B.Sc. (Hons) Computer Science"`). The PDF downloads as e.g. `2021_2022 Second Semester
Allocation.pdf` and lists, per year of study, each course's code/description/credit hours/staff —
the same document Admin compiles for the whole department.

### Assignments — view + download
```
GET /api/assignments                          → AssignmentResponse[]
GET /api/assignments/paged?pageNumber=&pageSize=
GET /api/assignments/{id}                     → AssignmentResponse
GET /api/assignments/{id}/download            → raw file (see §5)
```
`AssignmentResponse`:
```json
{
  "id": "guid",
  "courseName": "Software Engineering",
  "courseCode": "CSC401",
  "assignmentTitle": "Design a REST API",
  "importance": 2,
  "importanceText": "High",
  "dateCreated": "2026-08-01T00:00:00Z",
  "dueDate": "2026-09-01T00:00:00Z",
  "filePath": "uploads/assignments/....pdf",
  "originalFileName": "assignment-brief.pdf"
}
```
Use `importanceText` (`"Low"`/`"Medium"`/`"High"`) for display; `importance` is the raw numeric enum
if you need it for sorting/badges. `filePath`/`originalFileName` may be `null` if no file was
attached — hide the download button in that case.

### Notes — view, download, and upload
```
GET  /api/notes                          → NoteResponse[]
GET  /api/notes/paged?pageNumber=&pageSize=
GET  /api/notes/{id}                     → NoteResponse
GET  /api/notes/{id}/download            → raw file (see §5)
POST /api/notes   (multipart/form-data)  → NoteResponse   (Student allowed)
```
`NoteResponse`:
```json
{
  "id": "guid",
  "courseName": "Software Engineering",
  "courseCode": "CSC401",
  "filePath": "uploads/notes/....pdf",
  "originalFileName": "week3-notes.pdf",
  "uploadDate": "2026-08-15T00:00:00Z"
}
```
Upload form fields (multipart, **not** JSON): `courseName` (text), `courseCode` (text), `file`
(binary — PDF or DOCX only, 20MB max; the API rejects anything else with a `400` validation error).

### Past questions — view + download
```
GET /api/pastquestions                          → PastQuestionResponse[]
GET /api/pastquestions/paged?pageNumber=&pageSize=
GET /api/pastquestions/{id}                     → PastQuestionResponse
GET /api/pastquestions/{id}/download            → raw file (see §5)
```
`PastQuestionResponse` has the same shape as `NoteResponse` (`courseName`, `courseCode`, `filePath`,
`originalFileName`, `uploadDate`).

### Internship report & dissertation write-up — upload, status, and comments

Two independent endpoint groups with the identical shape — `/api/internshipreports` for the
internship report, `/api/dissertationsubmissions` for the dissertation/final-year-project write-up.
Neither is related to `GET /api/dissertations` (that's a separate, staff-only official record — a
Student gets `403` on it).

```
POST /api/internshipreports                    (multipart/form-data, field "file") → StudentSubmissionResponse
GET  /api/internshipreports/mine                                                   → StudentSubmissionResponse | 404
GET  /api/internshipreports/{id}/download                                          → raw file (see §5)
GET  /api/internshipreports/{id}/comments                                          → SubmissionCommentResponse[]
```
Same four routes under `/api/dissertationsubmissions` for the dissertation write-up.

`StudentSubmissionResponse`:
```json
{
  "id": "guid",
  "studentId": "guid",
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
}
```
- **Uploading again overwrites the previous file** — there's no separate "update" endpoint and no
  version history; `submissionCount` tells you how many times the student has (re-)submitted, and
  `updatedAt` (once present) is the last re-submission time. There's no confirmation step needed
  before re-upload replaces the old file — build any "are you sure you want to replace your
  previous submission?" confirmation client-side if you want one.
- **`GET .../mine` returns `404`** ("You have not submitted your ... yet.") until the student has
  uploaded at least once — treat that as an empty/not-yet-started state on the dashboard, not an
  error toast.
- File constraints match Notes/Dissertations: PDF/DOC/DOCX only, 50MB max.
- **Visibility of the file/comments to staff depends on an assignment step Admin performs**
  (`InternshipAllocation` for the internship report, `DissertationAllocation` for the dissertation)
  — the student can always upload regardless, but no Lecturer can see or comment on it until Admin
  has assigned one. There's nothing for the frontend to do differently here; just don't be surprised
  if `commentCount` stays `0` for a while after upload.
- `GET .../{id}/comments` returns every comment left by Admin/the assigned Lecturer, oldest first:
  ```json
  [
    {
      "id": "guid",
      "studentSubmissionId": "guid",
      "authorUserId": "guid",
      "authorUsername": "lect3",
      "authorRole": "Lecturer",
      "text": "Please expand section 3 with more detail on your methodology.",
      "createdAt": "2026-09-02T17:12:00Z"
    }
  ]
  ```
  A Student can only read this list for their own `{id}` — there is no comment-posting endpoint
  available to a Student (posting is Admin/Lecturer only, and emails the student automatically when
  it happens, so a "you have a new comment" notification doesn't need to be built client-side, just
  surface it in-app when the student next opens the app).
- `{id}` in the `/download` and `/comments` routes is the `StudentSubmissionResponse.id` from either
  the upload response or `GET .../mine` — fetch `mine` first to get it, there's no shortcut route
  that resolves "my current submission's comments" directly by type.

## 5. Handling file downloads

The `/{id}/download` endpoints (Assignments, Notes, Past questions, plus the new internship
report/dissertation submissions) return the **raw file bytes** with the correct `Content-Type`
and `Content-Disposition` — not a JSON envelope. Don't `fetch().then(res => res.json())` these;
either:
- Open them in a new tab/`window.location` with the JWT passed however your auth layer supports
  (e.g. a short-lived signed link if you add one), or
- `fetch()` with the `Authorization` header, read the response as a `Blob`, and trigger a
  client-side download (`URL.createObjectURL` + an anchor click) — this is the more common pattern
  since these endpoints require the bearer token, which a plain `<a href>` can't attach.

## 6. Known gaps to design around

These are real limitations in the current API — plan the UI accordingly rather than assuming
them away:

- **No "my profile" endpoint.** There's no `GET /api/students/me` — only `GET /api/students/{id}`
  by the student's `Student.Id` (a different GUID from their login `User.Id`, returned nowhere on
  login). Until this is added, the student portal can't easily show "my program / year / etc."
  self-service. Flag this to the backend team if a student profile/dashboard page is in scope.
- **No "materials for this course" filter.** Assignments/Notes/PastQuestions link to a course only
  by matching `courseCode` text, not a foreign key, and there's no `?courseCode=` query filter on
  any of the three list endpoints today. To build a course-detail page showing "assignments/notes/
  past questions for this course," fetch the full list (`GET /api/assignments`, etc.) and filter
  client-side by `courseCode === course.courseCode`. Fine at small data volumes; ask the backend
  team to add a filtered endpoint if the dataset grows.
- **`GET /api/courses`, `/api/assignments`, `/api/notes`, `/api/pastquestions` (unpaged) return
  every record with no limit** — prefer the `/paged` variant for any list view to avoid pulling
  a large payload.

## 7. Suggested screen → endpoint mapping

| Screen | Endpoint(s) |
|---|---|
| Register | `POST /api/auth/register-student` |
| Login | `POST /api/auth/login` |
| Force password change (first login with temp password) | `POST /api/auth/change-password` |
| Course list | `GET /api/courses/paged` |
| Course detail (+ its materials, client-filtered — see §6) | `GET /api/courses/{id}`, `GET /api/assignments`, `GET /api/notes`, `GET /api/pastquestions` |
| Assignments list/detail + download | `GET /api/assignments/paged`, `GET /api/assignments/{id}/download` |
| Notes list/detail + download + upload | `GET /api/notes/paged`, `GET /api/notes/{id}/download`, `POST /api/notes` |
| Past questions list/detail + download | `GET /api/pastquestions/paged`, `GET /api/pastquestions/{id}/download` |
| Internship report status + upload + comments | `GET /api/internshipreports/mine`, `POST /api/internshipreports`, `GET /api/internshipreports/{id}/download`, `GET /api/internshipreports/{id}/comments` |
| Dissertation write-up status + upload + comments | `GET /api/dissertationsubmissions/mine`, `POST /api/dissertationsubmissions`, `GET /api/dissertationsubmissions/{id}/download`, `GET /api/dissertationsubmissions/{id}/comments` |
