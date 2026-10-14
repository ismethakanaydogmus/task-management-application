using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tasks_due_date",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "ix_tasks_is_completed",
                table: "tasks");

            // ---- ELLE DÜZENLENDİ (veri migration'ı) ----
            // EF'nin ürettiği hali: AddColumn(nullable: false, defaultValue: 0)
            // Sorun: Tablodaki MEVCUT görevlere user_id = 0 yazılır; 0 numaralı kullanıcı olmadığı için
            // aşağıdaki AddForeignKey adımı "violates foreign key constraint" hatasıyla migration'ı yarıda keserdi.
            //
            // Doğru sıra: (1) kolonu NULL olabilir ekle → (2) mevcut veriyi doldur → (3) NOT NULL yap → (4) FK ekle.

            // (1)
            migrationBuilder.AddColumn<int>(
                name: "user_id",
                table: "tasks",
                type: "integer",
                nullable: true);

            // (2) Kimlik doğrulama öncesinde oluşturulmuş görevlerin sahibi bilinmiyor.
            //     Bilinçli karar: hepsini EN ESKİ kullanıcıya ata (genelde sistemi ilk kuran / yönetici).
            //     Hiç kullanıcı yoksa sahiplenilemeyecekleri için silinir (sadece o durumda).
            migrationBuilder.Sql("""
                UPDATE tasks
                SET user_id = (SELECT id FROM users ORDER BY id LIMIT 1)
                WHERE user_id IS NULL;

                DELETE FROM tasks WHERE user_id IS NULL;
                """);

            // (3)
            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "tasks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_tasks_user_id_created_at",
                table: "tasks",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_user_id_due_date",
                table: "tasks",
                columns: new[] { "user_id", "due_date" });

            migrationBuilder.AddForeignKey(
                name: "fk_tasks_users_user_id",
                table: "tasks",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tasks_users_user_id",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "ix_tasks_user_id_created_at",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "ix_tasks_user_id_due_date",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "tasks");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_due_date",
                table: "tasks",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_is_completed",
                table: "tasks",
                column: "is_completed");
        }
    }
}
