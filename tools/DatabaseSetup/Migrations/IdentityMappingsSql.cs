namespace DatabaseSetup.Migrations;

internal static class IdentityMappingsSql
{
    internal const string Up = """
CREATE FUNCTION identity.guard_external_identity() RETURNS trigger
LANGUAGE plpgsql SECURITY INVOKER SET search_path=pg_catalog AS $fn$
DECLARE stamp timestamptz := statement_timestamp();
BEGIN
 IF TG_OP='DELETE' THEN
  RAISE EXCEPTION 'Identity lifecycle tombstones are required' USING ERRCODE='23514';
 ELSIF TG_OP='INSERT' THEN
  IF NEW.revision <> 1 THEN RAISE EXCEPTION 'Initial identity revision must be one' USING ERRCODE='23514'; END IF;
  NEW.created_at := stamp; NEW.updated_at := stamp;
  RETURN NEW;
 END IF;
 IF NEW.issuer_id IS DISTINCT FROM OLD.issuer_id THEN
  RAISE EXCEPTION 'Identity ownership is immutable' USING ERRCODE='23514';
 END IF;
 IF TG_TABLE_NAME='authentication_issuers' THEN
  IF NEW.issuer COLLATE "C" IS DISTINCT FROM OLD.issuer COLLATE "C" THEN
   RAISE EXCEPTION 'Issuer key is immutable' USING ERRCODE='23514';
  END IF;
 ELSE
  IF NEW.mapping_id IS DISTINCT FROM OLD.mapping_id OR NEW.user_id IS DISTINCT FROM OLD.user_id
    OR NEW.subject COLLATE "C" IS DISTINCT FROM OLD.subject COLLATE "C" THEN
   RAISE EXCEPTION 'External identity binding is immutable' USING ERRCODE='23514';
  END IF;
  IF OLD.state='unlinked' AND NEW.state IS DISTINCT FROM OLD.state THEN
   RAISE EXCEPTION 'Unlinked identity is terminal' USING ERRCODE='23514';
  END IF;
 END IF;
 IF NEW.revision IS DISTINCT FROM OLD.revision OR NEW.created_at IS DISTINCT FROM OLD.created_at
    OR NEW.updated_at IS DISTINCT FROM OLD.updated_at THEN
  RAISE EXCEPTION 'Identity revision and timestamps are database maintained' USING ERRCODE='23514';
 END IF;
 IF OLD.revision=9223372036854775807 THEN RAISE EXCEPTION 'Identity revision exhausted' USING ERRCODE='22003'; END IF;
 NEW.revision := OLD.revision+1; NEW.updated_at := stamp;
 RETURN NEW;
END $fn$;
CREATE FUNCTION identity.reject_identity_truncate() RETURNS trigger
LANGUAGE plpgsql SECURITY INVOKER SET search_path=pg_catalog AS $fn$
BEGIN RAISE EXCEPTION 'Truncate would discard identity lifecycle history' USING ERRCODE='23514'; END $fn$;
REVOKE ALL ON FUNCTION identity.guard_external_identity(),identity.reject_identity_truncate() FROM PUBLIC;
CREATE TRIGGER issuer_integrity BEFORE INSERT OR UPDATE OR DELETE ON identity.authentication_issuers
 FOR EACH ROW EXECUTE FUNCTION identity.guard_external_identity();
CREATE TRIGGER mapping_integrity BEFORE INSERT OR UPDATE OR DELETE ON identity.external_staff_identities
 FOR EACH ROW EXECUTE FUNCTION identity.guard_external_identity();
CREATE TRIGGER issuer_no_truncate BEFORE TRUNCATE ON identity.authentication_issuers EXECUTE FUNCTION identity.reject_identity_truncate();
CREATE TRIGGER mapping_no_truncate BEFORE TRUNCATE ON identity.external_staff_identities EXECUTE FUNCTION identity.reject_identity_truncate();
ALTER TABLE identity.authentication_issuers ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity.authentication_issuers FORCE ROW LEVEL SECURITY;
ALTER TABLE identity.external_staff_identities ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity.external_staff_identities FORCE ROW LEVEL SECURITY;
-- No policy and no runtime grant: mapping bootstrap remains privileged synthetic testing only.
REVOKE ALL ON identity.authentication_issuers,identity.external_staff_identities FROM PUBLIC,qr_reader,qr_setup;
ALTER TABLE identity.authentication_issuers OWNER TO qr_platform_owner;
ALTER TABLE identity.external_staff_identities OWNER TO qr_platform_owner;
ALTER FUNCTION identity.guard_external_identity() OWNER TO qr_platform_owner;
ALTER FUNCTION identity.reject_identity_truncate() OWNER TO qr_platform_owner;
""";
}
