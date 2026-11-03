-- migrations/006_documents.sql
-- One-shot migration for databases built before the document vault existed.
-- A full rebuild (drop, schema, procedures, triggers, seed) already produces
-- the target shape, so every step below is guarded and re-running this file
-- is a no-op.
--
-- Steps:
--   1. create document_types and documents
--   2. add the indexes
--   3. create the audit and updated_at triggers
--   4. verify
--
-- Every statement is terminated by a lone "/" so runner tools and SQL*Plus
-- both execute the script as written, semicolon by semicolon.

-- =========================================================
-- 1. TABLES
-- =========================================================

create table if not exists document_types (
   id         number generated always as identity,
   title      varchar2(60) not null,
   created_at timestamp default systimestamp not null,
   updated_at timestamp,
   constraint pk_document_type_id primary key ( id ),
   constraint uq_document_types_title unique ( title )
)
/

create table if not exists documents (
   id           number generated always as identity,
   employee_id  number not null,
   type_id      number not null,
   title        varchar2(120) not null,
   issued_on    date,
   expires_at   date,
   note         varchar2(500),
   created_at   timestamp default systimestamp not null,
   updated_at   timestamp,
   constraint pk_document_id primary key ( id ),
   constraint fk_doc_employee foreign key ( employee_id )
      references employees ( id ),
   constraint fk_doc_type foreign key ( type_id )
      references document_types ( id ),
   constraint chk_doc_dates check ( expires_at is null
       or issued_on is null
       or expires_at >= issued_on )
)
/

-- =========================================================
-- 2. INDEXES
-- =========================================================

create index if not exists ix_documents_employee on
   documents ( employee_id )
/

create index if not exists ix_documents_expires on
   documents ( expires_at )
/

-- =========================================================
-- 3. TRIGGERS
-- =========================================================

create or replace trigger trg_set_documents_updated_at before
   update on documents
   for each row
   when ( new.updated_at is null
       or old.updated_at is null
       or new.updated_at = old.updated_at )
begin
   :new.updated_at := systimestamp;
end;
/

create or replace trigger trg_audit_document_types
after insert or update or delete on document_types
for each row
begin
   if inserting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DOCUMENT_TYPES', :new.id, 'INSERT', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif updating then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DOCUMENT_TYPES', :new.id, 'UPDATE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif deleting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DOCUMENT_TYPES', :old.id, 'DELETE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   end if;
end;
/

create or replace trigger trg_audit_documents
after insert or update or delete on documents
for each row
begin
   if inserting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DOCUMENTS', :new.id, 'INSERT', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif updating then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DOCUMENTS', :new.id, 'UPDATE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   elsif deleting then
      insert into audit_log (table_name, pk_value, action, changed_by)
      values ('DOCUMENTS', :old.id, 'DELETE', nvl(sys_context('USERENV','CLIENT_IDENTIFIER'), 'anonymous'));
   end if;
end;
/

-- =========================================================
-- 4. VERIFICATION
-- =========================================================

select table_name
  from user_tables
 where table_name in ( 'DOCUMENT_TYPES', 'DOCUMENTS' )
/

select constraint_name, table_name, constraint_type, status
  from user_constraints
 where table_name in ( 'DOCUMENT_TYPES', 'DOCUMENTS' )
 order by table_name, constraint_name
/

select index_name, table_name
  from user_indexes
 where index_name in ( 'IX_DOCUMENTS_EMPLOYEE', 'IX_DOCUMENTS_EXPIRES' )
/

select count(*) as document_rows from documents
/
