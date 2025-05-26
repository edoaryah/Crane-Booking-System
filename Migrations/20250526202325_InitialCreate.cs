using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AspnetCoreMvcFull.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cranes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Ownership = table.Column<string>(type: "text", nullable: false),
                    ImagePath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cranes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hazards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hazards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShiftDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsageSubcategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageSubcategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LdapUser = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RoleName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentNumber = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    BookingNumber = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Department = table.Column<string>(type: "text", nullable: false),
                    LdapUser = table.Column<string>(type: "text", nullable: false),
                    CraneId = table.Column<int>(type: "integer", nullable: true),
                    CraneCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CraneCapacity = table.Column<int>(type: "integer", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SubmitTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProjectSupervisor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CostCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CustomHazard = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ManagerName = table.Column<string>(type: "text", nullable: true),
                    ManagerApprovalTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ManagerRejectReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovedByPIC = table.Column<string>(type: "text", nullable: true),
                    ApprovedAtByPIC = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PICRejectReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DoneByPIC = table.Column<string>(type: "text", nullable: true),
                    DoneAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CancelledBy = table.Column<int>(type: "integer", nullable: false),
                    CancelledByName = table.Column<string>(type: "text", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CancelledReason = table.Column<string>(type: "text", nullable: true),
                    RevisionCount = table.Column<int>(type: "integer", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: false),
                    IsBilled = table.Column<bool>(type: "boolean", nullable: false),
                    BilledDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BilledBy = table.Column<string>(type: "text", nullable: true),
                    BillingNotes = table.Column<string>(type: "text", nullable: true),
                    ReminderEmailSent = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderEmailSentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReminderEmailSentTo = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Cranes_CraneId",
                        column: x => x.CraneId,
                        principalTable: "Cranes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Breakdowns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CraneId = table.Column<int>(type: "integer", nullable: true),
                    CraneCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CraneCapacity = table.Column<int>(type: "integer", nullable: true),
                    UrgentStartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UrgentEndTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ActualUrgentEndTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    HangfireJobId = table.Column<string>(type: "text", nullable: true),
                    Reasons = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Breakdowns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Breakdowns_Cranes_CraneId",
                        column: x => x.CraneId,
                        principalTable: "Cranes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CraneUsageRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CraneId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false),
                    FinalizedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CraneUsageRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CraneUsageRecords_Cranes_CraneId",
                        column: x => x.CraneId,
                        principalTable: "Cranes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentNumber = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CraneId = table.Column<int>(type: "integer", nullable: true),
                    CraneCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CraneCapacity = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceSchedules_Cranes_CraneId",
                        column: x => x.CraneId,
                        principalTable: "Cranes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BookingHazards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookingId = table.Column<int>(type: "integer", nullable: false),
                    HazardId = table.Column<int>(type: "integer", nullable: true),
                    HazardName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingHazards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingHazards_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingHazards_Hazards_HazardId",
                        column: x => x.HazardId,
                        principalTable: "Hazards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BookingItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookingId = table.Column<int>(type: "integer", nullable: false),
                    ItemName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Height = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingItems_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookingId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ShiftDefinitionId = table.Column<int>(type: "integer", nullable: true),
                    ShiftName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ShiftStartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ShiftEndTime = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingShifts_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingShifts_ShiftDefinitions_ShiftDefinitionId",
                        column: x => x.ShiftDefinitionId,
                        principalTable: "ShiftDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CraneUsageEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CraneUsageRecordId = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    UsageSubcategoryId = table.Column<int>(type: "integer", nullable: true),
                    SubcategoryName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BookingId = table.Column<int>(type: "integer", nullable: true),
                    MaintenanceScheduleId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OperatorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CraneUsageEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CraneUsageEntries_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CraneUsageEntries_CraneUsageRecords_CraneUsageRecordId",
                        column: x => x.CraneUsageRecordId,
                        principalTable: "CraneUsageRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CraneUsageEntries_MaintenanceSchedules_MaintenanceScheduleId",
                        column: x => x.MaintenanceScheduleId,
                        principalTable: "MaintenanceSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CraneUsageEntries_UsageSubcategories_UsageSubcategoryId",
                        column: x => x.UsageSubcategoryId,
                        principalTable: "UsageSubcategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceScheduleShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaintenanceScheduleId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ShiftDefinitionId = table.Column<int>(type: "integer", nullable: true),
                    ShiftName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ShiftStartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ShiftEndTime = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceScheduleShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceScheduleShifts_MaintenanceSchedules_MaintenanceS~",
                        column: x => x.MaintenanceScheduleId,
                        principalTable: "MaintenanceSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceScheduleShifts_ShiftDefinitions_ShiftDefinitionId",
                        column: x => x.ShiftDefinitionId,
                        principalTable: "ShiftDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Cranes",
                columns: new[] { "Id", "Capacity", "Code", "ImagePath", "Ownership", "Status" },
                values: new object[,]
                {
                    { 1, 250, "LC008", null, "KPC", "Available" },
                    { 2, 150, "LC009", null, "KPC", "Available" },
                    { 3, 100, "LC010", null, "KPC", "Available" },
                    { 4, 150, "LC011", null, "KPC", "Available" },
                    { 5, 35, "LC012", null, "KPC", "Available" },
                    { 6, 15, "LC013", null, "KPC", "Available" }
                });

            migrationBuilder.InsertData(
                table: "Hazards",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Listrik Tegangan Tinggi" },
                    { 2, "Kondisi Tanah" },
                    { 3, "Bekerja di Dekat Bangunan" },
                    { 4, "Bekerja di Dekat Area Mining" },
                    { 5, "Bekerja di Dekat Air" }
                });

            migrationBuilder.InsertData(
                table: "ShiftDefinitions",
                columns: new[] { "Id", "EndTime", "IsActive", "Name", "StartTime" },
                values: new object[,]
                {
                    { 1, new TimeSpan(0, 15, 0, 0, 0), true, "Shift 1", new TimeSpan(0, 7, 0, 0, 0) },
                    { 2, new TimeSpan(0, 23, 0, 0, 0), true, "Shift 2", new TimeSpan(0, 15, 0, 0, 0) },
                    { 3, new TimeSpan(0, 7, 0, 0, 0), true, "Shift 3", new TimeSpan(0, 0, 0, 0, 0) }
                });

            migrationBuilder.InsertData(
                table: "UsageSubcategories",
                columns: new[] { "Id", "Category", "Description", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "Operating", "Crane used for production lifting operations", true, "Pengangkatan" },
                    { 2, "Operating", "Crane used for equipment installation", true, "Menggantung Beban" },
                    { 3, "Delay", "Delay due to bad weather conditions", true, "Traveling" },
                    { 4, "Delay", "Delay due to bad weather conditions", true, "Prestart Check" },
                    { 5, "Delay", "Delay due to planning or coordination issues", true, "Menunggu User" },
                    { 6, "Delay", "Scheduled operator break time", true, "Menunggu Kesiapan Pengangkatan" },
                    { 7, "Delay", "Delay for refueling operations", true, "Menunggu Pengawalan" },
                    { 8, "Standby", "Crane on standby at work site", true, "Tidak ada Operator" },
                    { 9, "Standby", "Crane available but not assigned any tasks", true, "Tidak diperlukan" },
                    { 10, "Standby", "Planned standby period", true, "Tidak ada pengawal" },
                    { 11, "Service", "Regular scheduled maintenance", true, "Servis Rutin Terjadwal" },
                    { 15, "Breakdown", "Breakdown due to mechanical problems", true, "Rusak" },
                    { 16, "Breakdown", "Breakdown due to electrical problems", true, "Perbaikan" },
                    { 20, "Delay", "Delay for refueling operations", true, "Fueling" },
                    { 21, "Delay", "Delay for refueling operations", true, "Cuaca" },
                    { 22, "Standby", "Planned standby period", true, "Istirahat" },
                    { 23, "Standby", "Planned standby period", true, "Ganti Shift" },
                    { 24, "Standby", "Planned standby period", true, "Tidak Bisa Lewat" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingHazards_BookingId",
                table: "BookingHazards",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingHazards_HazardId",
                table: "BookingHazards",
                column: "HazardId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingItems_BookingId",
                table: "BookingItems",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CraneId",
                table: "Bookings",
                column: "CraneId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingShifts_BookingId",
                table: "BookingShifts",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingShifts_ShiftDefinitionId",
                table: "BookingShifts",
                column: "ShiftDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Breakdowns_CraneId",
                table: "Breakdowns",
                column: "CraneId");

            migrationBuilder.CreateIndex(
                name: "IX_CraneUsageEntries_BookingId",
                table: "CraneUsageEntries",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_CraneUsageEntries_CraneUsageRecordId",
                table: "CraneUsageEntries",
                column: "CraneUsageRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_CraneUsageEntries_MaintenanceScheduleId",
                table: "CraneUsageEntries",
                column: "MaintenanceScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_CraneUsageEntries_UsageSubcategoryId",
                table: "CraneUsageEntries",
                column: "UsageSubcategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CraneUsageRecords_CraneId",
                table: "CraneUsageRecords",
                column: "CraneId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedules_CraneId",
                table: "MaintenanceSchedules",
                column: "CraneId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceScheduleShifts_MaintenanceScheduleId",
                table: "MaintenanceScheduleShifts",
                column: "MaintenanceScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceScheduleShifts_ShiftDefinitionId",
                table: "MaintenanceScheduleShifts",
                column: "ShiftDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_LdapUser_RoleName",
                table: "UserRoles",
                columns: new[] { "LdapUser", "RoleName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingHazards");

            migrationBuilder.DropTable(
                name: "BookingItems");

            migrationBuilder.DropTable(
                name: "BookingShifts");

            migrationBuilder.DropTable(
                name: "Breakdowns");

            migrationBuilder.DropTable(
                name: "CraneUsageEntries");

            migrationBuilder.DropTable(
                name: "MaintenanceScheduleShifts");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Hazards");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "CraneUsageRecords");

            migrationBuilder.DropTable(
                name: "UsageSubcategories");

            migrationBuilder.DropTable(
                name: "MaintenanceSchedules");

            migrationBuilder.DropTable(
                name: "ShiftDefinitions");

            migrationBuilder.DropTable(
                name: "Cranes");
        }
    }
}
