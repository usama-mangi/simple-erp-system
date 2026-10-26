# SmartERP

An ERP learning project focused on the HR domain: employees, departments, and employment records. The database design is fully built out in Oracle; the ASP.NET Core web app is still at the scaffold stage.

![Entity relationship diagram](ERD.png)

## Structure

```
SmartERP.slnx
├── src/
│   ├── SmartERP.Web      # ASP.NET Core MVC front end (scaffolded, not built out yet)
│   └── SmartERP.Data     # Data layer
└── db/                   # Oracle SQL scripts, run them in numeric order
    ├── 00_drop.sql       # Drop all objects for a clean rebuild
    ├── 01_schema.sql     # Tables, constraints, unique indexes
    ├── 02_seed.sql       # Reference and sample data
    └── 04_triggers.sql   # Triggers (no 03 script exists)
```

## Data model

Seven tables in three groups.

Lookup tables:

| Table | Columns |
|---|---|
| `genders` | `title` unique |
| `employment_types` | `title` unique |
| `employment_status` | `title` unique |

Main tables:

| Table | What it stores |
|---|---|
| `employees` | Personal info: names, unique email and phone, date of birth, gender, address |
| `departments` | Name, unique 3-letter code, location, established date |
| `employment_details` | One job assignment: job title, salary, start and end dates, plus FKs to employee, department, type, and status |
| `department_managers` | Management assignment: department, manager, start and end dates |

Design rules worth noting:

- All primary keys use `number generated always as identity`.
- Every table carrying real data has `created_at` (defaults to `systimestamp`) and `updated_at`.
- `salary >= 0` is enforced, and `end_date`, where present, must be later than `start_date`.
- Two function-based unique indexes prevent duplicate open assignments:
  - `uq_det_current_job`: an employee can have only one `employment_details` row with a null `end_date`, so only one current job.
  - `uq_dept_current_manager`: a manager can head only one department at a time.
- Closed rows (with an `end_date`) are the history; there is no separate history table.

## Relationships

```
employees 1───* employment_details *───1 departments
employees 1───* department_managers *───1 departments
employment_details *───1 employment_types
employment_details *───1 employment_status
employees *───1 genders
```

The diagram above (generated with dbdiagram.io) shows the same model with column types.

## Running the database

Execute the scripts in `db/` in filename order against an Oracle instance: `00_drop`, `01_schema`, `02_seed`, `04_triggers`.

## Running the web app

```bash
dotnet run --project src/SmartERP.Web
```

Requires the .NET SDK. The home page is still the default template; the data layer and UI work are where this project goes next.
