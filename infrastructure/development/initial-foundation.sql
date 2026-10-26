DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'foundation') THEN
        CREATE SCHEMA foundation;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS foundation."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'organizations') THEN
        CREATE SCHEMA organizations;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'identity') THEN
        CREATE SCHEMA identity;
    END IF;
END $EF$;

CREATE TABLE organizations.capabilities (
    capability_code text NOT NULL,
    CONSTRAINT "PK_capabilities" PRIMARY KEY (capability_code),
    CONSTRAINT ck_capability_code CHECK (capability_code IN ('workspace.read','delegation.scope.evaluate'))
);

CREATE TABLE organizations.organizations (
    organization_id uuid NOT NULL,
    display_name character varying(160) NOT NULL,
    status text NOT NULL,
    CONSTRAINT "PK_organizations" PRIMARY KEY (organization_id),
    CONSTRAINT ck_org_status CHECK (status IN ('active','suspended','closed')),
    CONSTRAINT ck_organizations_organization_id_nonempty CHECK (organization_id <> '00000000-0000-0000-0000-000000000000'::uuid)
);

CREATE TABLE identity.staff_users (
    user_id uuid NOT NULL,
    is_enabled boolean NOT NULL,
    CONSTRAINT "PK_staff_users" PRIMARY KEY (user_id),
    CONSTRAINT ck_staff_user_nonempty CHECK (user_id <> '00000000-0000-0000-0000-000000000000'::uuid)
);

CREATE TABLE organizations.branches (
    organization_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    display_name character varying(160) NOT NULL,
    is_enabled boolean NOT NULL,
    CONSTRAINT "PK_branches" PRIMARY KEY (organization_id, branch_id),
    CONSTRAINT ck_branches_branch_id_nonempty CHECK (branch_id <> '00000000-0000-0000-0000-000000000000'::uuid),
    CONSTRAINT ck_branches_organization_id_nonempty CHECK (organization_id <> '00000000-0000-0000-0000-000000000000'::uuid),
    CONSTRAINT "FK_branches_organizations_organization_id" FOREIGN KEY (organization_id) REFERENCES organizations.organizations (organization_id) ON DELETE RESTRICT
);

CREATE TABLE organizations.memberships (
    organization_id uuid NOT NULL,
    user_id uuid NOT NULL,
    authorization_revision bigint NOT NULL,
    status text NOT NULL,
    CONSTRAINT "PK_memberships" PRIMARY KEY (organization_id, user_id),
    CONSTRAINT ck_membership_revision CHECK (authorization_revision > 0),
    CONSTRAINT ck_membership_status CHECK (status IN ('active','suspended','removed')),
    CONSTRAINT ck_memberships_organization_id_nonempty CHECK (organization_id <> '00000000-0000-0000-0000-000000000000'::uuid),
    CONSTRAINT ck_memberships_user_id_nonempty CHECK (user_id <> '00000000-0000-0000-0000-000000000000'::uuid),
    CONSTRAINT "FK_memberships_organizations_organization_id" FOREIGN KEY (organization_id) REFERENCES organizations.organizations (organization_id) ON DELETE RESTRICT
);

CREATE TABLE organizations.membership_branch_grants (
    organization_id uuid NOT NULL,
    user_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    CONSTRAINT "PK_membership_branch_grants" PRIMARY KEY (organization_id, user_id, branch_id),
    CONSTRAINT ck_membership_branch_grants_branch_id_nonempty CHECK (branch_id <> '00000000-0000-0000-0000-000000000000'::uuid),
    CONSTRAINT ck_membership_branch_grants_organization_id_nonempty CHECK (organization_id <> '00000000-0000-0000-0000-000000000000'::uuid),
    CONSTRAINT ck_membership_branch_grants_user_id_nonempty CHECK (user_id <> '00000000-0000-0000-0000-000000000000'::uuid),
    CONSTRAINT "FK_membership_branch_grants_branches_organization_id_branch_id" FOREIGN KEY (organization_id, branch_id) REFERENCES organizations.branches (organization_id, branch_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_membership_branch_grants_memberships_organization_id_user_id" FOREIGN KEY (organization_id, user_id) REFERENCES organizations.memberships (organization_id, user_id) ON DELETE RESTRICT
);

CREATE TABLE organizations.membership_capabilities (
    organization_id uuid NOT NULL,
    user_id uuid NOT NULL,
    capability_code text NOT NULL,
    is_delegable boolean NOT NULL,
    CONSTRAINT "PK_membership_capabilities" PRIMARY KEY (organization_id, user_id, capability_code),
    CONSTRAINT ck_delegable CHECK (NOT is_delegable OR capability_code = 'workspace.read'),
    CONSTRAINT ck_membership_capabilities_organization_id_nonempty CHECK (organization_id <> '00000000-0000-0000-0000-000000000000'::uuid),
    CONSTRAINT ck_membership_capabilities_user_id_nonempty CHECK (user_id <> '00000000-0000-0000-0000-000000000000'::uuid),
    CONSTRAINT "FK_membership_capabilities_capabilities_capability_code" FOREIGN KEY (capability_code) REFERENCES organizations.capabilities (capability_code) ON DELETE RESTRICT,
    CONSTRAINT "FK_membership_capabilities_memberships_organization_id_user_id" FOREIGN KEY (organization_id, user_id) REFERENCES organizations.memberships (organization_id, user_id) ON DELETE RESTRICT
);

INSERT INTO organizations.capabilities (capability_code)
VALUES ('delegation.scope.evaluate');
INSERT INTO organizations.capabilities (capability_code)
VALUES ('workspace.read');

CREATE INDEX "IX_membership_branch_grants_organization_id_branch_id_user_id" ON organizations.membership_branch_grants (organization_id, branch_id, user_id);

CREATE INDEX "IX_membership_capabilities_capability_code" ON organizations.membership_capabilities (capability_code);

CREATE INDEX "IX_memberships_user_id_organization_id" ON organizations.memberships (user_id, organization_id);

-- Approved persistence-only cross-module integrity exception (ADR-005).
ALTER TABLE organizations.memberships ADD CONSTRAINT fk_memberships_staff_users
 FOREIGN KEY (user_id) REFERENCES identity.staff_users(user_id) ON DELETE RESTRICT ON UPDATE RESTRICT;
-- Refuse pre-existing cluster roles rather than adopting unknown authority.
CREATE ROLE qr_revision_maintainer NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
REVOKE ALL ON SCHEMA identity, organizations FROM PUBLIC;
REVOKE ALL ON ALL TABLES IN SCHEMA identity, organizations FROM PUBLIC;
GRANT USAGE ON SCHEMA organizations TO qr_revision_maintainer;
GRANT SELECT (organization_id,user_id,authorization_revision), UPDATE (authorization_revision)
 ON organizations.memberships TO qr_revision_maintainer;

CREATE FUNCTION organizations.guard_membership_revision() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog AS $fn$
BEGIN
 IF TG_OP = 'DELETE' THEN
  RAISE EXCEPTION 'Memberships must retain a lifecycle tombstone' USING ERRCODE='23514';
 ELSIF TG_OP = 'INSERT' THEN
  IF NEW.authorization_revision <> 1 THEN
   RAISE EXCEPTION 'Initial revision must be one' USING ERRCODE='23514';
  END IF;
  RETURN NEW;
 END IF;
 IF NEW.organization_id IS DISTINCT FROM OLD.organization_id OR NEW.user_id IS DISTINCT FROM OLD.user_id THEN
  RAISE EXCEPTION 'Membership ownership is immutable' USING ERRCODE='23514';
 END IF;
 IF NEW.authorization_revision IS DISTINCT FROM OLD.authorization_revision THEN
  RAISE EXCEPTION 'Revision is database maintained' USING ERRCODE='23514';
 END IF;
 IF OLD.authorization_revision = 9223372036854775807 THEN
  RAISE EXCEPTION 'Revision exhausted' USING ERRCODE='22003';
 END IF;
 NEW.authorization_revision := OLD.authorization_revision + 1;
 RETURN NEW;
END $fn$;
REVOKE ALL ON FUNCTION organizations.guard_membership_revision() FROM PUBLIC;
CREATE TRIGGER membership_revision BEFORE INSERT OR UPDATE OR DELETE ON organizations.memberships
 FOR EACH ROW EXECUTE FUNCTION organizations.guard_membership_revision();

CREATE FUNCTION organizations.bump_grant_revision() RETURNS trigger
LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog AS $fn$
DECLARE tenant uuid; actor uuid;
BEGIN
 IF TG_OP = 'UPDATE' THEN
  IF NEW.organization_id IS DISTINCT FROM OLD.organization_id OR NEW.user_id IS DISTINCT FROM OLD.user_id
   OR (TG_TABLE_NAME = 'membership_branch_grants' AND to_jsonb(NEW)->>'branch_id' IS DISTINCT FROM to_jsonb(OLD)->>'branch_id')
   OR (TG_TABLE_NAME = 'membership_capabilities' AND to_jsonb(NEW)->>'capability_code' IS DISTINCT FROM to_jsonb(OLD)->>'capability_code') THEN
   RAISE EXCEPTION 'Grant ownership and reference are immutable' USING ERRCODE='23514';
  END IF;
 END IF;
 IF TG_OP = 'DELETE' THEN tenant := OLD.organization_id; actor := OLD.user_id;
 ELSE tenant := NEW.organization_id; actor := NEW.user_id; END IF;
 -- UPDATE locks the exact parent; the parent trigger calculates the increment.
 UPDATE organizations.memberships SET authorization_revision = authorization_revision
 WHERE organization_id = tenant AND user_id = actor;
 IF NOT FOUND THEN RAISE EXCEPTION 'Membership required' USING ERRCODE='23503'; END IF;
 RETURN NULL;
END $fn$;
GRANT CREATE ON SCHEMA organizations TO qr_revision_maintainer;
ALTER FUNCTION organizations.bump_grant_revision() OWNER TO qr_revision_maintainer;
REVOKE CREATE ON SCHEMA organizations FROM qr_revision_maintainer;
REVOKE ALL ON FUNCTION organizations.bump_grant_revision() FROM PUBLIC;
CREATE TRIGGER branch_grant_revision AFTER INSERT OR UPDATE OR DELETE ON organizations.membership_branch_grants
 FOR EACH ROW EXECUTE FUNCTION organizations.bump_grant_revision();
CREATE TRIGGER capability_revision AFTER INSERT OR UPDATE OR DELETE ON organizations.membership_capabilities
 FOR EACH ROW EXECUTE FUNCTION organizations.bump_grant_revision();

CREATE FUNCTION organizations.reject_truncate() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog AS $fn$
BEGIN RAISE EXCEPTION 'Truncate would bypass revision history' USING ERRCODE='23514'; END $fn$;
REVOKE ALL ON FUNCTION organizations.reject_truncate() FROM PUBLIC;
CREATE TRIGGER membership_no_truncate BEFORE TRUNCATE ON organizations.memberships
 EXECUTE FUNCTION organizations.reject_truncate();
CREATE TRIGGER branch_grant_no_truncate BEFORE TRUNCATE ON organizations.membership_branch_grants
 EXECUTE FUNCTION organizations.reject_truncate();
CREATE TRIGGER capability_no_truncate BEFORE TRUNCATE ON organizations.membership_capabilities
 EXECUTE FUNCTION organizations.reject_truncate();

CREATE FUNCTION organizations.guard_branch_ownership() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog AS $fn$
BEGIN
 IF NEW.organization_id IS DISTINCT FROM OLD.organization_id OR NEW.branch_id IS DISTINCT FROM OLD.branch_id THEN
  RAISE EXCEPTION 'Branch ownership is immutable' USING ERRCODE='23514';
 END IF;
 RETURN NEW;
END $fn$;
REVOKE ALL ON FUNCTION organizations.guard_branch_ownership() FROM PUBLIC;
CREATE TRIGGER branch_ownership BEFORE UPDATE ON organizations.branches
 FOR EACH ROW EXECUTE FUNCTION organizations.guard_branch_ownership();

INSERT INTO foundation."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261026033754_InitialFoundation', '10.0.13');

COMMIT;
