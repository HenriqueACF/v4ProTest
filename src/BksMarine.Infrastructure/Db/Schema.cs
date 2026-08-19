namespace BksMarine.Infrastructure.Db;

public static class Schema
{
    public const string Sql = """
        CREATE TABLE IF NOT EXISTS profiles (
            id UUID PRIMARY KEY,
            name TEXT NOT NULL UNIQUE
        );

        CREATE TABLE IF NOT EXISTS users (
            id UUID PRIMARY KEY,
            email TEXT NOT NULL UNIQUE,
            password_hash TEXT NOT NULL,
            profile_id UUID NOT NULL REFERENCES profiles(id),
            is_active BOOLEAN NOT NULL DEFAULT TRUE
        );

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
        """;
}
