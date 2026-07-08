using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaHospitalar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceAppointmentScheduleNoOverlap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE EXTENSION IF NOT EXISTS btree_gist;

                UPDATE appointments a
                SET "Status" = 5,
                    "UpdatedAt" = NOW()
                WHERE a."Id" IN (
                    SELECT "Id"
                    FROM (
                        SELECT "Id",
                            ROW_NUMBER() OVER (
                                PARTITION BY "ProfessionalId", "ScheduledAt"
                                ORDER BY "CreatedAt", "Id"
                            ) AS rn
                        FROM appointments
                        WHERE "IsActive" = true
                          AND "Status" NOT IN (5, 6)
                    ) ranked
                    WHERE rn > 1
                );

                DO $$
                DECLARE
                    pass integer;
                BEGIN
                    FOR pass IN 1..5 LOOP
                        UPDATE appointments a
                        SET "Status" = 5,
                            "UpdatedAt" = NOW()
                        WHERE a."Id" IN (
                            SELECT newer."Id"
                            FROM appointments newer
                            INNER JOIN appointments older
                                ON newer."ProfessionalId" = older."ProfessionalId"
                               AND newer."Id" <> older."Id"
                               AND newer."IsActive" = true
                               AND older."IsActive" = true
                               AND newer."Status" NOT IN (5, 6)
                               AND older."Status" NOT IN (5, 6)
                               AND newer."ScheduledAt" < older."ScheduledAt"
                                    + make_interval(mins => GREATEST(older."DurationMinutes", 1))
                               AND newer."ScheduledAt"
                                    + make_interval(mins => GREATEST(newer."DurationMinutes", 1)) > older."ScheduledAt"
                               AND (newer."CreatedAt", newer."Id") > (older."CreatedAt", older."Id")
                        );
                    END LOOP;
                END $$;

                -- timestamptz + interval é STABLE no PostgreSQL; wrapper IMMUTABLE permite EXCLUDE GiST.
                CREATE OR REPLACE FUNCTION appointment_blocking_slot_range(
                    start_at timestamptz,
                    duration_mins integer)
                RETURNS tstzrange
                LANGUAGE sql
                IMMUTABLE
                PARALLEL SAFE
                STRICT
                AS $func$
                    SELECT tstzrange(
                        start_at,
                        start_at + (GREATEST(duration_mins, 1) * INTERVAL '1 minute'),
                        '[)'
                    );
                $func$;

                ALTER TABLE appointments
                ADD CONSTRAINT appointments_professional_no_overlap
                EXCLUDE USING gist (
                    "ProfessionalId" WITH =,
                    appointment_blocking_slot_range("ScheduledAt", "DurationMinutes") WITH &&
                )
                WHERE ("IsActive" = true AND "Status" NOT IN (5, 6));

                CREATE UNIQUE INDEX IF NOT EXISTS ix_appointments_professional_exact_slot
                ON appointments ("ProfessionalId", "ScheduledAt")
                WHERE ("IsActive" = true AND "Status" NOT IN (5, 6));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE appointments DROP CONSTRAINT IF EXISTS appointments_professional_no_overlap;
                DROP INDEX IF EXISTS ix_appointments_professional_exact_slot;
                DROP FUNCTION IF EXISTS appointment_blocking_slot_range(timestamptz, integer);
                """);
        }
    }
}
