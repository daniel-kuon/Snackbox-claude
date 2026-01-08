using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snackbox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievementImages2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 1,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFE5B4\" stroke=\"#FF8C00\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FF8C00\">🍪</text><text x=\"60\" y=\"85\" font-size=\"24\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FF8C00\">€2</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 2,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFD700\" stroke=\"#FF6347\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FF0000\">⚡</text><text x=\"60\" y=\"85\" font-size=\"24\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FF0000\">€3</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 3,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFB6C1\" stroke=\"#FF69B4\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#DC143C\">🦛</text><text x=\"60\" y=\"85\" font-size=\"24\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#DC143C\">€4</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 4,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#87CEEB\" stroke=\"#4169E1\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#00008B\">✈️</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#00008B\">x5</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 5,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFDAB9\" stroke=\"#D2691E\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">🏃</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">x10</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 6,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFA07A\" stroke=\"#FF4500\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">🔥</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">3d</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 7,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFD700\" stroke=\"#FF8C00\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#B8860B\">⚔️</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#B8860B\">7d</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 8,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#90EE90\" stroke=\"#32CD32\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">📆</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">4w</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 9,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#F0E68C\" stroke=\"#BDB76B\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B7355\">👋</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B7355\">30d</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 10,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#D8BFD8\" stroke=\"#9370DB\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#4B0082\">↩️</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#4B0082\">60d</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 11,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFE4E1\" stroke=\"#FF1493\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">🧟</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">90d</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 12,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFE4B5\" stroke=\"#DEB887\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">📝</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">€15</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 13,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFDAB9\" stroke=\"#CD853F\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">💳</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">€20</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 14,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFB6C1\" stroke=\"#FF69B4\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">💸</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">€25</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 15,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFD700\" stroke=\"#FFA500\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#B8860B\">💯</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#B8860B\">€100</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 16,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#E6E6FA\" stroke=\"#9370DB\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#4B0082\">🍷</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#4B0082\">€150</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 17,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#87CEEB\" stroke=\"#4169E1\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#00008B\">🏆</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#00008B\">€200</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 18,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#E6E6FA\" stroke=\"#9370DB\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#4B0082\">📦</text><text x=\"60\" y=\"85\" font-size=\"24\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#4B0082\">€5</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 19,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#B0E0E6\" stroke=\"#1E90FF\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#00008B\">🐋</text><text x=\"60\" y=\"85\" font-size=\"24\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#00008B\">€6</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 20,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#98FB98\" stroke=\"#228B22\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">🎩</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">x3</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 21,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#DDA0DD\" stroke=\"#9932CC\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#4B0082\">🎯</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#4B0082\">14d</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 22,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FF69B4\" stroke=\"#C71585\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">💉</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">30d</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 23,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFA07A\" stroke=\"#FF6347\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">📞</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">€30</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 24,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FF6B6B\" stroke=\"#DC143C\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"32\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">🚫💰</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">€35</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 25,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#C0C0C0\" stroke=\"#A9A9A9\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#696969\">🥈</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#696969\">€50</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 26,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#DDA0DD\" stroke=\"#BA55D3\" stroke-width=\"3\"/><text x=\"60\" y=\"50\" font-size=\"36\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">👑</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">€300</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 27,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FF69B4\" stroke=\"#FF1493\" stroke-width=\"3\"/><path d=\"M 60 20 L 70 45 L 95 45 L 75 60 L 85 85 L 60 70 L 35 85 L 45 60 L 25 45 L 50 45 Z\" fill=\"#FFD700\" stroke=\"#FFA500\" stroke-width=\"2\"/><text x=\"60\" y=\"65\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">€500</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 28,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#87CEEB\" stroke=\"#4682B4\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#191970\">🐦</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#191970\">&lt;8AM</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 29,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#191970\" stroke=\"#000080\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FFD700\">🦉</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FFD700\">&gt;8PM</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 30,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFE4B5\" stroke=\"#D2691E\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">🍱</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">12-1PM</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 31,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#98FB98\" stroke=\"#228B22\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">🎉</text><text x=\"60\" y=\"85\" font-size=\"14\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">Sat/Sun</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 32,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#ADD8E6\" stroke=\"#4682B4\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#000080\">😔</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#000080\">Monday</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 33,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFD700\" stroke=\"#FFA500\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">🎊</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">Friday</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 34,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFE4E1\" stroke=\"#FF69B4\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">🎈</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">1st</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 35,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#F0E68C\" stroke=\"#DAA520\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">🎫</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B4513\">10</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 36,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#C0C0C0\" stroke=\"#A9A9A9\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#696969\">🎖️</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#696969\">50</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 37,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFD700\" stroke=\"#FFA500\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#B8860B\">🎖️</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#B8860B\">100</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 38,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#DDA0DD\" stroke=\"#BA55D3\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">🥋</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">250</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 39,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FF4500\" stroke=\"#8B0000\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FFD700\">👑</text><text x=\"60\" y=\"85\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FFD700\">500</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 40,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FF6347\" stroke=\"#DC143C\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">⚡</text><text x=\"60\" y=\"85\" font-size=\"14\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B0000\">&lt;1min</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 41,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFB6C1\" stroke=\"#FF69B4\" stroke-width=\"3\"/><text x=\"60\" y=\"65\" font-size=\"50\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">2️⃣</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 42,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#DDA0DD\" stroke=\"#BA55D3\" stroke-width=\"3\"/><text x=\"60\" y=\"65\" font-size=\"50\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">3️⃣</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 43,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#32CD32\" stroke=\"#228B22\" stroke-width=\"3\"/><text x=\"60\" y=\"65\" font-size=\"50\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">7️⃣</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 44,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#E0FFFF\" stroke=\"#00CED1\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#008B8B\">✔️</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#008B8B\">€5/€10</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 45,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FAFAD2\" stroke=\"#BDB76B\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B7355\">🔁</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B7355\">x3</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 46,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#90EE90\" stroke=\"#32CD32\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">✅</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#006400\">€0</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 47,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFD700\" stroke=\"#FFA500\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#B8860B\">💝</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#B8860B\">+€10</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 48,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FFB6C1\" stroke=\"#FF1493\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">🎂</text><text x=\"60\" y=\"85\" font-size=\"14\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#C71585\">1 year</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 49,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#2F4F4F\" stroke=\"#000000\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FFD700\">🖤</text><text x=\"60\" y=\"85\" font-size=\"20\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#FFD700\">€13</text></svg>");

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 50,
                column: "image_url",
                value: "<svg width=\"120\" height=\"120\" viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"60\" cy=\"60\" r=\"55\" fill=\"#FF69B4\" stroke=\"#FF1493\" stroke-width=\"3\"/><text x=\"60\" y=\"55\" font-size=\"40\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">😏</text><text x=\"60\" y=\"85\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#8B008B\">€6.90</text></svg>");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 1,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 2,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 3,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 4,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 5,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 6,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 7,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 8,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 9,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 10,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 11,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 12,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 13,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 14,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 15,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 16,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 17,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 18,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 19,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 20,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 21,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 22,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 23,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 24,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 25,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 26,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 27,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 28,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 29,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 30,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 31,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 32,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 33,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 34,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 35,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 36,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 37,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 38,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 39,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 40,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 41,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 42,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 43,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 44,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 45,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 46,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 47,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 48,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 49,
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "achievements",
                keyColumn: "id",
                keyValue: 50,
                column: "image_url",
                value: null);
        }
    }
}
