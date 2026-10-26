START TRANSACTION;
CREATE ROLE qr_platform_owner NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
CREATE ROLE qr_migrator LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
CREATE ROLE qr_setup LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION BYPASSRLS;
CREATE ROLE qr_reader LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
GRANT qr_platform_owner TO qr_migrator WITH INHERIT FALSE, SET TRUE;
ALTER ROLE qr_reader SET default_transaction_read_only = on;
REVOKE CONNECT, TEMPORARY ON DATABASE qr_medication_dev FROM PUBLIC;
GRANT CONNECT ON DATABASE qr_medication_dev TO qr_platform_owner,qr_migrator,qr_setup,qr_reader;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
REVOKE ALL ON SCHEMA foundation,identity,organizations FROM PUBLIC;
GRANT USAGE ON SCHEMA foundation,identity,organizations TO qr_reader;
GRANT USAGE ON SCHEMA identity,organizations TO qr_setup;
GRANT SELECT ON ALL TABLES IN SCHEMA identity,organizations TO qr_reader,qr_setup;
GRANT INSERT,UPDATE,DELETE ON identity.staff_users,organizations.organizations,organizations.branches,
 organizations.membership_branch_grants,organizations.membership_capabilities TO qr_setup;
GRANT INSERT,UPDATE(status) ON organizations.memberships TO qr_setup;
-- qr_setup is a local synthetic fixture provisioner, never an application role.

CREATE FUNCTION foundation.context_uuid(setting text) RETURNS uuid
LANGUAGE plpgsql STABLE SECURITY INVOKER SET search_path=pg_catalog AS $fn$
DECLARE parsed uuid;
BEGIN
 IF setting NOT IN ('app.actor_id','app.organization_id','app.branch_id') THEN RETURN NULL; END IF;
 parsed := NULLIF(current_setting(setting,true),'')::uuid;
 IF parsed = '00000000-0000-0000-0000-000000000000'::uuid THEN RETURN NULL; END IF;
 RETURN parsed;
EXCEPTION WHEN invalid_text_representation THEN RETURN NULL;
END $fn$;
CREATE FUNCTION foundation.context_ready() RETURNS boolean
LANGUAGE sql STABLE SECURITY INVOKER SET search_path=pg_catalog AS $fn$
 SELECT foundation.context_uuid('app.actor_id') IS NOT NULL
 AND foundation.context_uuid('app.organization_id') IS NOT NULL
 AND foundation.context_uuid('app.branch_id') IS NOT NULL
$fn$;
REVOKE ALL ON FUNCTION foundation.context_uuid(text),foundation.context_ready() FROM PUBLIC;
GRANT EXECUTE ON FUNCTION foundation.context_uuid(text),foundation.context_ready() TO qr_reader;

ALTER TABLE identity.staff_users ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity.staff_users FORCE ROW LEVEL SECURITY;
ALTER TABLE organizations.organizations ENABLE ROW LEVEL SECURITY;
ALTER TABLE organizations.organizations FORCE ROW LEVEL SECURITY;
ALTER TABLE organizations.branches ENABLE ROW LEVEL SECURITY;
ALTER TABLE organizations.branches FORCE ROW LEVEL SECURITY;
ALTER TABLE organizations.memberships ENABLE ROW LEVEL SECURITY;
ALTER TABLE organizations.memberships FORCE ROW LEVEL SECURITY;
ALTER TABLE organizations.membership_branch_grants ENABLE ROW LEVEL SECURITY;
ALTER TABLE organizations.membership_branch_grants FORCE ROW LEVEL SECURITY;
ALTER TABLE organizations.membership_capabilities ENABLE ROW LEVEL SECURITY;
ALTER TABLE organizations.membership_capabilities FORCE ROW LEVEL SECURITY;

CREATE POLICY staff_self ON identity.staff_users FOR SELECT TO qr_reader
 USING (foundation.context_ready() AND user_id=foundation.context_uuid('app.actor_id'));
-- Bootstrap: deliberately independent of organization policy and membership activity.
CREATE POLICY membership_self ON organizations.memberships FOR SELECT TO qr_reader
 USING (foundation.context_ready() AND organization_id=foundation.context_uuid('app.organization_id')
 AND user_id=foundation.context_uuid('app.actor_id'));
CREATE POLICY organization_self ON organizations.organizations FOR SELECT TO qr_reader
 USING (foundation.context_ready() AND organization_id=foundation.context_uuid('app.organization_id')
 AND EXISTS (SELECT 1 FROM identity.staff_users u WHERE u.user_id=foundation.context_uuid('app.actor_id') AND u.is_enabled)
 AND EXISTS (SELECT 1 FROM organizations.memberships m WHERE m.organization_id=organizations.organization_id
 AND m.user_id=foundation.context_uuid('app.actor_id') AND m.status='active'));
CREATE POLICY branch_grants_self ON organizations.membership_branch_grants FOR SELECT TO qr_reader
 USING (foundation.context_ready() AND organization_id=foundation.context_uuid('app.organization_id')
 AND user_id=foundation.context_uuid('app.actor_id')
 AND EXISTS (SELECT 1 FROM organizations.organizations o WHERE o.organization_id=membership_branch_grants.organization_id AND o.status='active'));
CREATE POLICY capabilities_self ON organizations.membership_capabilities FOR SELECT TO qr_reader
 USING (foundation.context_ready() AND organization_id=foundation.context_uuid('app.organization_id')
 AND user_id=foundation.context_uuid('app.actor_id')
 AND EXISTS (SELECT 1 FROM organizations.organizations o WHERE o.organization_id=membership_capabilities.organization_id AND o.status='active'));
CREATE POLICY branch_scope ON organizations.branches FOR SELECT TO qr_reader
 USING (foundation.context_ready() AND organization_id=foundation.context_uuid('app.organization_id')
 AND branch_id=foundation.context_uuid('app.branch_id')
 AND EXISTS (SELECT 1 FROM organizations.membership_branch_grants g WHERE g.organization_id=branches.organization_id
 AND g.branch_id=branches.branch_id AND g.user_id=foundation.context_uuid('app.actor_id')));
-- Maintenance exception: role-targeted, independent of caller's descriptive context.
-- The nonlogin role can be entered only by the fixed, non-public trigger function.
CREATE POLICY revision_read ON organizations.memberships FOR SELECT TO qr_revision_maintainer USING (true);
CREATE POLICY revision_update ON organizations.memberships FOR UPDATE TO qr_revision_maintainer USING (true) WITH CHECK (true);

ALTER TABLE identity.staff_users OWNER TO qr_platform_owner;
ALTER TABLE organizations.organizations OWNER TO qr_platform_owner;
ALTER TABLE organizations.branches OWNER TO qr_platform_owner;
ALTER TABLE organizations.memberships OWNER TO qr_platform_owner;
ALTER TABLE organizations.membership_branch_grants OWNER TO qr_platform_owner;
ALTER TABLE organizations.capabilities OWNER TO qr_platform_owner;
ALTER TABLE organizations.membership_capabilities OWNER TO qr_platform_owner;
ALTER TABLE foundation."__EFMigrationsHistory" OWNER TO qr_platform_owner;
ALTER FUNCTION organizations.guard_membership_revision() OWNER TO qr_platform_owner;
ALTER FUNCTION organizations.reject_truncate() OWNER TO qr_platform_owner;
ALTER FUNCTION organizations.guard_branch_ownership() OWNER TO qr_platform_owner;
ALTER FUNCTION foundation.context_uuid(text) OWNER TO qr_platform_owner;
ALTER FUNCTION foundation.context_ready() OWNER TO qr_platform_owner;
ALTER SCHEMA foundation OWNER TO qr_platform_owner;
ALTER SCHEMA identity OWNER TO qr_platform_owner;
ALTER SCHEMA organizations OWNER TO qr_platform_owner;
ALTER DEFAULT PRIVILEGES FOR ROLE qr_platform_owner IN SCHEMA foundation,identity,organizations REVOKE ALL ON TABLES FROM PUBLIC;
ALTER DEFAULT PRIVILEGES FOR ROLE qr_platform_owner IN SCHEMA foundation,identity,organizations REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC;

INSERT INTO foundation."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261026040035_TenantRowSecurity', '10.0.13');

COMMIT;
