START TRANSACTION;
CREATE TABLE identity.authentication_issuers (
    issuer_id uuid NOT NULL DEFAULT (gen_random_uuid()),
    created_at timestamp with time zone NOT NULL DEFAULT (statement_timestamp()),
    is_enabled boolean NOT NULL DEFAULT FALSE,
    issuer text COLLATE "C" NOT NULL,
    revision bigint NOT NULL DEFAULT 1,
    updated_at timestamp with time zone NOT NULL DEFAULT (statement_timestamp()),
    CONSTRAINT "PK_authentication_issuers" PRIMARY KEY (issuer_id),
    CONSTRAINT ck_issuer_id CHECK (issuer_id <> '00000000-0000-0000-0000-000000000000'::uuid),
    CONSTRAINT ck_issuer_revision CHECK (revision > 0),
    CONSTRAINT ck_issuer_value CHECK (octet_length(issuer) BETWEEN 1 AND 1024 AND issuer ~* '^https://[^/?#@[:space:]]+(/[^?#[:space:]]*)?$' AND issuer !~ U&'[\0001-\0020\007F-\00A0\1680\2000-\200A\2028\2029\202F\205F\3000]')
);

CREATE TABLE identity.external_staff_identities (
    mapping_id uuid NOT NULL DEFAULT (gen_random_uuid()),
    created_at timestamp with time zone NOT NULL DEFAULT (statement_timestamp()),
    issuer_id uuid NOT NULL,
    revision bigint NOT NULL DEFAULT 1,
    state text NOT NULL DEFAULT 'disabled',
    subject text COLLATE "C" NOT NULL,
    updated_at timestamp with time zone NOT NULL DEFAULT (statement_timestamp()),
    user_id uuid NOT NULL,
    CONSTRAINT "PK_external_staff_identities" PRIMARY KEY (mapping_id),
    CONSTRAINT ck_mapping_ids CHECK (mapping_id <> '00000000-0000-0000-0000-000000000000'::uuid AND issuer_id <> '00000000-0000-0000-0000-000000000000'::uuid AND user_id <> '00000000-0000-0000-0000-000000000000'::uuid),
    CONSTRAINT ck_mapping_revision CHECK (revision > 0),
    CONSTRAINT ck_mapping_state CHECK (state IN ('active','disabled','unlinked')),
    CONSTRAINT ck_mapping_subject CHECK (octet_length(subject) BETWEEN 1 AND 1024 AND subject !~ U&'[\0001-\001F\007F-\009F]' AND btrim(subject, U&'\0020\00A0\1680\2000\2001\2002\2003\2004\2005\2006\2007\2008\2009\200A\2028\2029\202F\205F\3000') <> ''),
    CONSTRAINT "FK_external_staff_identities_authentication_issuers_issuer_id" FOREIGN KEY (issuer_id) REFERENCES identity.authentication_issuers (issuer_id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT "FK_external_staff_identities_staff_users_user_id" FOREIGN KEY (user_id) REFERENCES identity.staff_users (user_id) ON UPDATE RESTRICT ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_authentication_issuers_issuer" ON identity.authentication_issuers (issuer);

CREATE UNIQUE INDEX "IX_external_staff_identities_issuer_id_subject" ON identity.external_staff_identities (issuer_id, subject);

CREATE INDEX "IX_external_staff_identities_user_id" ON identity.external_staff_identities (user_id);

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

INSERT INTO foundation."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261111233123_IdentityMappings', '10.0.13');

COMMIT;
