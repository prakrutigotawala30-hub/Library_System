using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.ClassLibrary.Data
{
    /// <summary>
    /// Manual schema patches for Sqlite — works around the fact that
    /// `EnsureCreated` is a one-shot operation: if any tables already exist
    /// in the .db file, EnsureCreated does NOTHING, even for new tables
    /// added to the model. That means schema-changing model edits leave
    /// existing Mac/Linux dev databases in a broken state.
    ///
    /// This patcher runs after EnsureCreated and uses `CREATE TABLE IF NOT
    /// EXISTS` to add any tables the EF model expects but the .db is
    /// missing. Safe to run on every startup — does nothing when tables
    /// already exist.
    ///
    /// Caveat: this only handles MISSING tables. If a column was added to
    /// an existing table, you still need to either drop that table or
    /// `ALTER TABLE ... ADD COLUMN ...` it. Document any such cases here
    /// when the model changes.
    /// </summary>
    public static class SqliteSchemaPatcher
    {
        public static async Task PatchAsync(AppDbContext db)
        {
            if (!db.Database.IsSqlite())
                return;

            // Wishlists — gained EventId column in commit c1ec76f / model rewrite.
            // Older Sqlite DBs created before that migration have the table
            // without EventId, or someone manually DROP TABLE'd it. Either way,
            // this CREATE TABLE IF NOT EXISTS makes sure the right shape is
            // present on every startup.
            await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS ""Wishlists"" (
    ""Id""       INTEGER NOT NULL CONSTRAINT ""PK_Wishlists"" PRIMARY KEY AUTOINCREMENT,
    ""MemberId"" TEXT    NOT NULL,
    ""BookId""   INTEGER NULL,
    ""EventId""  INTEGER NULL,
    ""AddedOn""  TEXT    NOT NULL,
    CONSTRAINT ""FK_Wishlists_AspNetUsers_MemberId""
        FOREIGN KEY (""MemberId"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE,
    CONSTRAINT ""FK_Wishlists_Books_BookId""
        FOREIGN KEY (""BookId"")   REFERENCES ""Books""       (""Id""),
    CONSTRAINT ""FK_Wishlists_Events_EventId""
        FOREIGN KEY (""EventId"")  REFERENCES ""Events""      (""Id"")
);
CREATE INDEX IF NOT EXISTS ""IX_Wishlists_MemberId"" ON ""Wishlists"" (""MemberId"");
CREATE INDEX IF NOT EXISTS ""IX_Wishlists_BookId""   ON ""Wishlists"" (""BookId"");
CREATE INDEX IF NOT EXISTS ""IX_Wishlists_EventId""  ON ""Wishlists"" (""EventId"");
");

            // If the existing Wishlists table predates EventId (older schema),
            // patch it in-place. ALTER TABLE ADD COLUMN is a no-op if the
            // column already exists in newer Sqlite, but older versions throw
            // — so wrap in try/catch.
            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    @"ALTER TABLE ""Wishlists"" ADD COLUMN ""EventId"" INTEGER NULL;");
            }
            catch
            {
                // Column already exists, or another harmless schema mismatch.
                // Ignore — the CREATE TABLE IF NOT EXISTS above already
                // handles the missing-table case.
            }
        }
    }
}
