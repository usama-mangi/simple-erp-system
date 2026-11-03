-- migrations/001_positions.sql
-- One-shot migration for databases built by scripts 01..04 BEFORE the
-- positions table existed. A full rebuild (drop, schema, procedures,
-- triggers, seed) already produces the target shape, so every step
-- below is guarded and re-running this file is a no-op.
--
-- Steps:
--   1. create positions (same shape as schema.sql)
--   2. seed positions from the distinct employment_details.job_title values
--   3. move employment_details to position_id and drop job_title
--   4. verify
--
-- Every statement is terminated by a lone "/" so runner tools and SQL*Plus
-- both execute the script as written, semicolon by semicolon.

-- =========================================================
-- 1. POSITIONS TABLE
-- =========================================================

create table if not exists positions (
   id         number generated always as identity,
   title      varchar2(50) not null,
   created_at timestamp default systimestamp not null,
   updated_at timestamp,
   constraint pk_position_id primary key ( id ),
   constraint uq_positions_title unique ( title )
)
/
commit
/

-- =========================================================
-- 2. BACKFILL POSITION TITLES
--    Seeding from job_title only runs while that column still
--    exists; after the drop the merge is skipped and the table
--    keeps whatever titles the app maintains.
-- =========================================================

declare
   job_title_columns number := 0;
begin
   select count(*) into job_title_columns
      from user_tab_columns
     where table_name = 'EMPLOYMENT_DETAILS'
       and column_name = 'JOB_TITLE';

   if job_title_columns > 0 then
      -- Dynamic SQL, not static: with a static merge the PL/SQL compiler
      -- resolves ed.job_title at script load time, so a re-run against an
      -- already-migrated schema would fail on the missing column even
      -- though the guard would have skipped execution.
      execute immediate '
         merge into positions p
         using (
            select distinct ed.job_title as title
              from employment_details ed
             where ed.job_title is not null
         ) s
            on ( p.title = s.title )
         when not matched then
         insert ( title )
         values ( s.title )';
   end if;
end;
/
commit
/

-- =========================================================
-- 3. MOVE EMPLOYMENT_DETAILS ONTO POSITIONS
--    The whole repoint runs only when both signal columns are
--    in their pre-migration state: job_title present,
--    position_id absent. The backfill in step 2 seeded one
--    position per distinct title, so no row can land NULL.
-- =========================================================

declare
   job_title_columns   number := 0;
   position_id_columns number := 0;
begin
   select count(*) into job_title_columns
      from user_tab_columns
     where table_name = 'EMPLOYMENT_DETAILS'
       and column_name = 'JOB_TITLE';

   select count(*) into position_id_columns
      from user_tab_columns
     where table_name = 'EMPLOYMENT_DETAILS'
       and column_name = 'POSITION_ID';

   if job_title_columns > 0 and position_id_columns = 0 then
      execute immediate 'alter table employment_details add position_id number';

      -- Backfill keyed on the exact titles.
      execute immediate '
         update employment_details ed
            set ed.position_id = (
               select p.id
                 from positions p
                where p.title = ed.job_title
            )';

      execute immediate
         'alter table employment_details modify position_id not null';

      execute immediate
         'alter table employment_details add constraint fk_det_position
             foreign key (position_id) references positions (id)';

      -- The free-text column dies only after everything is consumed.
      execute immediate 'alter table employment_details drop column job_title';

      commit;
   end if;
end;
/

-- =========================================================
-- 4. VERIFICATION
-- =========================================================

-- Every assignment must reference an existing position (no matches = healthy).
select count(*) orphaned_assignments
  from employment_details ed
  left join positions p on p.id = ed.position_id
 where p.id is null
/

-- The repoint must be complete: job_title gone, position_id NOT NULL
-- and tracked by the FK (no rows = healthy).
select utc.column_name, utc.nullable
  from user_tab_columns utc
 where utc.table_name = 'EMPLOYMENT_DETAILS'
   and utc.column_name in ('JOB_TITLE', 'POSITION_ID')
/

select ucc.constraint_name
  from user_constraints uc
  join user_cons_columns ucc on ucc.constraint_name = uc.constraint_name
 where uc.table_name = 'EMPLOYMENT_DETAILS'
   and uc.constraint_type = 'R'
   and uc.constraint_name = 'FK_DET_POSITION'
/

-- Row count must match the source: nothing merged or dropped away.
select 'employment_details' table_name,
       count(*) total_rows,
       count(distinct position_id) positions_in_use
  from employment_details
union all
select 'positions', count(*), null
  from positions
/
