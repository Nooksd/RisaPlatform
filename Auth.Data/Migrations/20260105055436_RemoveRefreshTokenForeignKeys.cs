using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRefreshTokenForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_public_users_user_id",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_tenant_accounts_user_id",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_tenant_users_user_id",
                table: "refresh_tokens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_public_users_user_id",
                table: "refresh_tokens",
                column: "user_id",
                principalTable: "public_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_tenant_accounts_user_id",
                table: "refresh_tokens",
                column: "user_id",
                principalTable: "tenant_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_tenant_users_user_id",
                table: "refresh_tokens",
                column: "user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
