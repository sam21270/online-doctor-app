# Online Doctor Application — Modern Port

A working port of my bachelor's final-year project web service (ASP.NET / .NET Framework 4.0 / SQL Server, 2022) to **ASP.NET Core (.NET 10)** with a local **SQLite** database, so it runs natively on macOS/Linux/Windows with no IIS and no SQL Server.

The original source is preserved in [`legacy/WebAPI`](legacy/WebAPI) (database credentials redacted) for a before/after comparison — same endpoints and behavior, modernized stack.

## What's preserved from the original

- All 45 API methods from `IServiceAPI` (admin, patient, and doctor portals plus the symptom-checker expert system).
- The exact JSON response format the Android app expects:
  `{ "status" : "ok","Data" :[{ "data0" : "...", "data1" : "..." }] }`
- The original ID scheme (doctors from 1000, patients from 100, appointments from 100000, diseases from 10001).
- The database schema, reconstructed from the SQL in `ServiceAPI.cs` (see `schema.sql`).

## What's different

- SQL Server → SQLite file (`adoc.db`, auto-created and seeded from `schema.sql` on first run).
- All SQL is parameterized — the original concatenated user input into query strings (SQL injection).
- Endpoints are `GET /api/{MethodName}?args...` instead of the JsonServices `Handler1.ashx` RPC handler.

## Run it

```bash
cd OnlineDoctorApi
dotnet run --urls http://localhost:5210
```

Then open http://localhost:5210 for a quick endpoint reference.

## Seeded demo accounts

| Role    | Login                                   |
|---------|-----------------------------------------|
| Admin   | `admin` / `admin123`                    |
| Patient | `ravi@test.com` / `1234` (Pid 100)      |
| Doctor  | `asha@adoc.com` / `doc123` (Did 1000)   |

## Try the main flows

```bash
# logins
curl "http://localhost:5210/api/ALogin?usern=admin&pass=admin123"
curl "http://localhost:5210/api/PLogin?email=ravi@test.com&pass=1234"

# doctors & diseases
curl "http://localhost:5210/api/getDoctors"
curl "http://localhost:5210/api/getDiseaselist"

# appointment lifecycle
curl "http://localhost:5210/api/PaddAppointment?did=1001&pid=100&note=Chest+pain&adate=2026-07-20&atime=11:00+AM&price=800&date=2026-07-10&time=10:00+AM"
curl "http://localhost:5210/api/DgetAppointment?did=1001&src=pending&date=2026-07-10"
curl "http://localhost:5210/api/DChangeStatus?aid=100001&did=1001&pid=100&price=800&date=2026-07-10&time=10:05+AM&status=Confirmed"
curl "http://localhost:5210/api/getNotification?uid=100"

# symptom checker (answer follow-up symptoms, then get diagnosis + doctor suggestions)
curl "http://localhost:5210/api/sysone?sys=fever"
curl "http://localhost:5210/api/systwo?sys=headache"
curl "http://localhost:5210/api/final1?ID=100&Date=2026-07-10"
```

To reset the demo data, stop the server and delete `adoc.db`.
