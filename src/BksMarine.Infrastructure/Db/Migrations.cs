namespace BksMarine.Infrastructure.Db;

public sealed record Migration(string Version, string Sql);

public static class Migrations
{
    public static readonly IReadOnlyList<Migration> All = new[]
    {
        new Migration("001_baseline", BaselineSql)
    };

    public const string BaselineSql = """
        CREATE TABLE IF NOT EXISTS profiles (
            id UUID PRIMARY KEY,
            name TEXT NOT NULL UNIQUE
        );

        CREATE TABLE IF NOT EXISTS users (
            id UUID PRIMARY KEY,
            name TEXT NOT NULL DEFAULT '',
            job_title TEXT,
            email TEXT NOT NULL UNIQUE,
            password_hash TEXT NOT NULL,
            profile_id UUID NOT NULL REFERENCES profiles(id),
            is_active BOOLEAN NOT NULL DEFAULT TRUE
        );

        ALTER TABLE users ADD COLUMN IF NOT EXISTS name TEXT NOT NULL DEFAULT '';
        ALTER TABLE users ADD COLUMN IF NOT EXISTS job_title TEXT;

        CREATE TABLE IF NOT EXISTS profile_modules (
            profile_id UUID NOT NULL REFERENCES profiles(id),
            module TEXT NOT NULL,
            PRIMARY KEY (profile_id, module)
        );

        INSERT INTO profiles (id, name) VALUES
            (gen_random_uuid(), 'Full'),
            (gen_random_uuid(), 'Operational'),
            (gen_random_uuid(), 'Common')
        ON CONFLICT (name) DO NOTHING;

        INSERT INTO profile_modules (profile_id, module)
        SELECT p.id, m.module
        FROM profiles p
        CROSS JOIN (VALUES ('Configuration'), ('Operations'), ('Reports')) AS m(module)
        WHERE p.name = 'Full'
        ON CONFLICT DO NOTHING;

        INSERT INTO profile_modules (profile_id, module)
        SELECT p.id, m.module
        FROM profiles p
        CROSS JOIN (VALUES ('Operations'), ('Reports')) AS m(module)
        WHERE p.name = 'Operational'
        ON CONFLICT DO NOTHING;

        INSERT INTO profile_modules (profile_id, module)
        SELECT p.id, m.module
        FROM profiles p
        CROSS JOIN (VALUES ('Reports')) AS m(module)
        WHERE p.name = 'Common'
        ON CONFLICT DO NOTHING;

        CREATE TABLE IF NOT EXISTS ports (
            id UUID PRIMARY KEY,
            name TEXT NOT NULL,
            code TEXT NOT NULL UNIQUE,
            address TEXT,
            contact TEXT,
            notes TEXT,
            is_active BOOLEAN NOT NULL DEFAULT TRUE
        );

        CREATE TABLE IF NOT EXISTS berths (
            id UUID PRIMARY KEY,
            name TEXT NOT NULL,
            port_id UUID NOT NULL REFERENCES ports(id),
            max_loa NUMERIC,
            max_dwt NUMERIC,
            type TEXT NOT NULL,
            notes TEXT,
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            UNIQUE (port_id, name)
        );

        CREATE TABLE IF NOT EXISTS ships (
            id UUID PRIMARY KEY,
            name TEXT NOT NULL,
            loa NUMERIC NOT NULL,
            dwt NUMERIC NOT NULL,
            is_active BOOLEAN NOT NULL DEFAULT TRUE
        );

        CREATE TABLE IF NOT EXISTS operations (
            id UUID PRIMARY KEY,
            type TEXT NOT NULL,
            ship_id UUID NOT NULL REFERENCES ships(id),
            port_id UUID NOT NULL REFERENCES ports(id),
            berth_id UUID NOT NULL REFERENCES berths(id),
            responsible_user_id UUID REFERENCES users(id),
            agency_name TEXT,
            pilot_name TEXT,
            pilot_boarding_time TIMESTAMPTZ,
            tug_bow_name TEXT,
            tug_bow_time TIMESTAMPTZ,
            tug_stern_name TEXT,
            tug_stern_time TIMESTAMPTZ,
            first_line_time TIMESTAMPTZ,
            last_line_time TIMESTAMPTZ,
            draft_bow NUMERIC,
            draft_midship NUMERIC,
            draft_stern NUMERIC,
            side TEXT,
            notes TEXT,
            occurred_at TIMESTAMPTZ NOT NULL,
            undocking_time TIMESTAMPTZ,
            photos TEXT[] NOT NULL DEFAULT '{}',
            transmission_status TEXT NOT NULL DEFAULT 'NotTransmitted',
            created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        );

        ALTER TABLE operations ADD COLUMN IF NOT EXISTS responsible_user_id UUID REFERENCES users(id);

        CREATE TABLE IF NOT EXISTS login_attempts (
            email TEXT NOT NULL,
            attempted_at TIMESTAMPTZ NOT NULL DEFAULT now(),
            success BOOLEAN NOT NULL
        );

        CREATE TABLE IF NOT EXISTS refresh_tokens (
            id UUID PRIMARY KEY,
            user_id UUID NOT NULL REFERENCES users(id),
            token_hash TEXT NOT NULL UNIQUE,
            expires_at TIMESTAMPTZ NOT NULL,
            revoked_at TIMESTAMPTZ,
            created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        );
        """;
}
