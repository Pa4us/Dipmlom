using BLL.Interfaces;
using DAL.DBContext;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace WebAPI.Infrastructure
{
    /// <summary>
    /// Заполняет БД тестовыми данными при первом запуске в Development.
    /// Вызывается из Program.cs только если данных ещё нет.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await db.Database.MigrateAsync();

            // ── Если роль Manager отсутствует — добавляем её и пользователя ─
            if (!await db.Roles.AnyAsync(r => r.Name == "Manager"))
            {
                var managerRole = new Role { Name = "Manager", Description = "Заведующая (управление аккаунтами и справочниками)" };
                db.Roles.Add(managerRole);
                await db.SaveChangesAsync();

                var manager = new User
                {
                    Username     = "manager",
                    Email        = "manager@ggtu.by",
                    FullName     = "Захарова Светлана Викторовна",
                    PhoneNumber  = "+375291000001",
                    RoleId       = managerRole.Id,
                    PasswordHash = Hash("Manager123"),
                    IsActive     = true,
                    CreatedAt    = DateTime.Now
                };
                db.Users.Add(manager);
                await db.SaveChangesAsync();
            }

            // Если остальные роли уже есть — полный сид не нужен
            if (await db.Roles.CountAsync() > 1) return;

            // ── 1. Роли ────────────────────────────────────────────────────
            var roleStudent   = new Role { Name = "Student",   Description = "Студент общежития" };
            var roleInspector = new Role { Name = "Inspector",  Description = "Инспектор (проводит проверки чистоты)" };
            var roleEducator  = new Role { Name = "Educator",   Description = "Воспитатель (администратор)" };
            var roleMechanic  = new Role { Name = "Mechanic",   Description = "Слесарь (выполняет заявки на ремонт)" };

            db.Roles.AddRange(roleStudent, roleInspector, roleEducator, roleMechanic);
            await db.SaveChangesAsync();

            // ── 2. Зоны проверки ───────────────────────────────────────────
            var zoneRoom       = new InspectionZone { Name = "Room",       DisplayName = "Комната" };
            var zoneCorridor   = new InspectionZone { Name = "Corridor",   DisplayName = "Коридор" };
            var zoneBathroom   = new InspectionZone { Name = "Bathroom",   DisplayName = "Туалет / Душевая" };
            var zoneKitchen    = new InspectionZone { Name = "Kitchen",    DisplayName = "Кухня" };
            var zoneCommonArea = new InspectionZone { Name = "CommonArea", DisplayName = "Места общего пользования" };

            db.InspectionZones.AddRange(zoneRoom, zoneCorridor, zoneBathroom, zoneKitchen, zoneCommonArea);
            await db.SaveChangesAsync();

            // ── 3. Пользователи ────────────────────────────────────────────
            var educator   = new User { Username = "educator",  Email = "educator@ggtu.by",  FullName = "Петрова Ирина Васильевна",   PhoneNumber = "+375291234567", RoleId = roleEducator.Id,  PasswordHash = Hash("Educator123"),  IsActive = true };
            var inspector1 = new User { Username = "inspector1", Email = "inspector1@ggtu.by", FullName = "Сидоров Алексей Петрович",  PhoneNumber = "+375292345678", RoleId = roleInspector.Id, PasswordHash = Hash("Inspector123"), IsActive = true };
            var inspector2 = new User { Username = "inspector2", Email = "inspector2@ggtu.by", FullName = "Козлова Наталья Сергеевна", PhoneNumber = "+375293456789", RoleId = roleInspector.Id, PasswordHash = Hash("Inspector123"), IsActive = true };
            var mechanic1  = new User { Username = "mechanic1",  Email = "mechanic1@ggtu.by",  FullName = "Новиков Дмитрий Олегович",  PhoneNumber = "+375294567890", RoleId = roleMechanic.Id,  PasswordHash = Hash("Mechanic123"),  IsActive = true };
            var ivanov     = new User { Username = "ivanov",     Email = "ivanov@student.ggtu.by",    FullName = "Иванов Иван Иванович",        PhoneNumber = "+375295678901", RoleId = roleStudent.Id, PasswordHash = Hash("Student123"), IsActive = true };
            var petrov     = new User { Username = "petrov",     Email = "petrov@student.ggtu.by",    FullName = "Петров Пётр Петрович",        PhoneNumber = "+375296789012", RoleId = roleStudent.Id, PasswordHash = Hash("Student123"), IsActive = true };
            var sidorova   = new User { Username = "sidorova",   Email = "sidorova@student.ggtu.by",  FullName = "Сидорова Мария Алексеевна",   PhoneNumber = "+375297890123", RoleId = roleStudent.Id, PasswordHash = Hash("Student123"), IsActive = true };
            var kozlov     = new User { Username = "kozlov",     Email = "kozlov@student.ggtu.by",    FullName = "Козлов Андрей Николаевич",    PhoneNumber = "+375298901234", RoleId = roleStudent.Id, PasswordHash = Hash("Student123"), IsActive = true };
            var novikova   = new User { Username = "novikova",   Email = "novikova@student.ggtu.by",  FullName = "Новикова Елена Дмитриевна",   PhoneNumber = "+375299012345", RoleId = roleStudent.Id, PasswordHash = Hash("Student123"), IsActive = true };
            var sokolov    = new User { Username = "sokolov",    Email = "sokolov@student.ggtu.by",   FullName = "Соколов Виктор Игоревич",     PhoneNumber = "+375291123456", RoleId = roleStudent.Id, PasswordHash = Hash("Student123"), IsActive = true };

            db.Users.AddRange(educator, inspector1, inspector2, mechanic1,
                              ivanov, petrov, sidorova, kozlov, novikova, sokolov);
            await db.SaveChangesAsync();

            // ── 4. Блоки ─────────────────────────────────────────────────
            // Формат: FloorBlockIndex без тире (41 = 4 этаж, 1 блок)
            var b41 = new Block { BlockNumber = "41", Floor = 4, BlockIndex = 1 };
            var b42 = new Block { BlockNumber = "42", Floor = 4, BlockIndex = 2 };
            var b43 = new Block { BlockNumber = "43", Floor = 4, BlockIndex = 3 };
            var b51 = new Block { BlockNumber = "51", Floor = 5, BlockIndex = 1 };
            var b52 = new Block { BlockNumber = "52", Floor = 5, BlockIndex = 2 };
            var b53 = new Block { BlockNumber = "53", Floor = 5, BlockIndex = 3 };

            db.Blocks.AddRange(b41, b42, b43, b51, b52, b53);
            await db.SaveChangesAsync();

            // ── 5. Комнаты ────────────────────────────────────────────────
            // В каждом блоке 2 комнаты: "1" и "2"
            // Отображается как: блок 41, комната 1 → "41-1"
            var r401 = new Room { RoomNumber = "1", BlockId = b41.Id, Capacity = 2, CurrentOccupancy = 2, IsActive = true };
            var r402 = new Room { RoomNumber = "2", BlockId = b41.Id, Capacity = 2, CurrentOccupancy = 1, IsActive = true };
            var r403 = new Room { RoomNumber = "1", BlockId = b42.Id, Capacity = 2, CurrentOccupancy = 2, IsActive = true };
            var r404 = new Room { RoomNumber = "2", BlockId = b42.Id, Capacity = 2, CurrentOccupancy = 0, IsActive = true };
            var r405 = new Room { RoomNumber = "1", BlockId = b43.Id, Capacity = 2, CurrentOccupancy = 1, IsActive = true };
            var r406 = new Room { RoomNumber = "2", BlockId = b43.Id, Capacity = 2, CurrentOccupancy = 0, IsActive = true };
            var r501 = new Room { RoomNumber = "1", BlockId = b51.Id, Capacity = 2, CurrentOccupancy = 0, IsActive = true };
            var r502 = new Room { RoomNumber = "2", BlockId = b51.Id, Capacity = 2, CurrentOccupancy = 0, IsActive = true };
            var r503 = new Room { RoomNumber = "1", BlockId = b52.Id, Capacity = 2, CurrentOccupancy = 0, IsActive = true };
            var r504 = new Room { RoomNumber = "2", BlockId = b52.Id, Capacity = 2, CurrentOccupancy = 0, IsActive = true };
            var r505 = new Room { RoomNumber = "1", BlockId = b53.Id, Capacity = 2, CurrentOccupancy = 0, IsActive = true };
            var r506 = new Room { RoomNumber = "2", BlockId = b53.Id, Capacity = 2, CurrentOccupancy = 0, IsActive = true };

            db.Rooms.AddRange(r401, r402, r403, r404, r405, r406,
                              r501, r502, r503, r504, r505, r506);
            await db.SaveChangesAsync();

            // ── 6. Проживание ─────────────────────────────────────────────
            var moveIn = new DateOnly(2024, 9, 1);
            db.Residences.AddRange(
                new Residence { UserId = ivanov.Id,   RoomId = r401.Id, BlockId = b41.Id, MoveInDate = moveIn, IsCurrent = true },
                new Residence { UserId = petrov.Id,   RoomId = r401.Id, BlockId = b41.Id, MoveInDate = moveIn, IsCurrent = true },
                new Residence { UserId = sidorova.Id, RoomId = r402.Id, BlockId = b41.Id, MoveInDate = moveIn, IsCurrent = true },
                new Residence { UserId = kozlov.Id,   RoomId = r403.Id, BlockId = b42.Id, MoveInDate = moveIn, IsCurrent = true },
                new Residence { UserId = novikova.Id, RoomId = r403.Id, BlockId = b42.Id, MoveInDate = moveIn, IsCurrent = true },
                new Residence { UserId = sokolov.Id,  RoomId = r405.Id, BlockId = b43.Id, MoveInDate = moveIn, IsCurrent = true }
            );
            await db.SaveChangesAsync();

            // ── 7. Проверки ───────────────────────────────────────────────
            var today = DateOnly.FromDateTime(DateTime.Today);
            db.Inspections.AddRange(
                new Inspection { BlockId = b41.Id, ZoneId = zoneRoom.Id,     InspectorId = inspector1.Id, InspectionDate = today.AddDays(-7), Score = 8, Comment = "Хорошая чистота, небольшие замечания по углам" },
                new Inspection { BlockId = b41.Id, ZoneId = zoneCorridor.Id, InspectorId = inspector1.Id, InspectionDate = today.AddDays(-7), Score = 7, Comment = "Коридор в норме" },
                new Inspection { BlockId = b42.Id, ZoneId = zoneRoom.Id,     InspectorId = inspector1.Id, InspectionDate = today.AddDays(-7), Score = 9, Comment = "Отлично" },
                new Inspection { BlockId = b42.Id, ZoneId = zoneBathroom.Id, InspectorId = inspector2.Id, InspectionDate = today.AddDays(-7), Score = 6, Comment = "Требуется лучшая уборка душевой" },
                new Inspection { BlockId = b43.Id, ZoneId = zoneRoom.Id,     InspectorId = inspector2.Id, InspectionDate = today.AddDays(-7), Score = 5, Comment = "Обнаружен беспорядок" },
                new Inspection { BlockId = b43.Id, ZoneId = zoneKitchen.Id,  InspectorId = inspector2.Id, InspectionDate = today.AddDays(-7), Score = 4, Comment = "Кухня требует серьёзной уборки" },
                new Inspection { BlockId = b41.Id, ZoneId = zoneRoom.Id,     InspectorId = inspector1.Id, InspectionDate = today,            Score = 9, Comment = "Значительное улучшение" },
                new Inspection { BlockId = b42.Id, ZoneId = zoneRoom.Id,     InspectorId = inspector1.Id, InspectionDate = today,            Score = 8, Comment = "Хорошо" }
            );
            await db.SaveChangesAsync();

            // ── 7b. Пересчёт недельной статистики ─────────────────────────
            // Нужно вызвать RecalculateWeeklyStatsAsync для каждой уникальной
            // пары (блок, дата), чтобы дашборд не показывал «нет данных».
            var statisticsService = scope.ServiceProvider.GetRequiredService<IStatisticsService>();
            var recalcPairs = new[] {
                (b41.Id, today.AddDays(-7)),
                (b42.Id, today.AddDays(-7)),
                (b43.Id, today.AddDays(-7)),
                (b41.Id, today),
                (b42.Id, today),
            };
            foreach (var (blockId, date) in recalcPairs)
                await statisticsService.RecalculateWeeklyStatsAsync(blockId, date);

            // ── 8. Заявки на ремонт ───────────────────────────────────────
            db.RepairRequests.AddRange(
                new RepairRequest
                {
                    Title = "Сломана ручка двери",
                    Description = "В комнате 1 блока 41 сломана ручка входной двери, дверь не закрывается",
                    BlockId = b41.Id, RoomId = r401.Id, RequestedById = ivanov.Id,
                    Status = "Pending", Priority = "Normal", CreatedAt = DateTime.Now.AddDays(-3)
                },
                new RepairRequest
                {
                    Title = "Не работает розетка",
                    Description = "В комнате 1 блока 42 не работает розетка у окна",
                    BlockId = b42.Id, RoomId = r403.Id, RequestedById = kozlov.Id,
                    Status = "InProgress", Priority = "High", AssignedToId = mechanic1.Id,
                    CreatedAt = DateTime.Now.AddDays(-5)
                },
                new RepairRequest
                {
                    Title = "Засор в душевой",
                    Description = "В блоке 42 засор в сливе душевой кабины",
                    BlockId = b42.Id, RoomId = null, RequestedById = novikova.Id,
                    Status = "Completed", Priority = "Normal", AssignedToId = mechanic1.Id,
                    CreatedAt = DateTime.Now.AddDays(-10), CompletedAt = DateTime.Now.AddDays(-7)
                },
                new RepairRequest
                {
                    Title = "Мигает лампочка",
                    Description = "В коридоре блока 43 постоянно мигает освещение",
                    BlockId = b43.Id, RoomId = null, RequestedById = sokolov.Id,
                    Status = "Pending", Priority = "Low", CreatedAt = DateTime.Now.AddDays(-1)
                }
            );
            await db.SaveChangesAsync();

            // ── 9. Мероприятия ────────────────────────────────────────────
            var ev1 = new Event
            {
                Title = "Субботник по уборке общежития",
                Description = "Общая уборка территории и помещений общежития",
                EventDate = today.AddDays(7), Location = "Территория общежития",
                OrganizerId = educator.Id, PointsAwarded = 5
            };
            var ev2 = new Event
            {
                Title = "Конкурс «Лучший блок месяца»",
                Description = "Ежемесячный конкурс на самый чистый и уютный блок",
                EventDate = today.AddDays(14), Location = "Холл общежития, 1 этаж",
                OrganizerId = educator.Id, PointsAwarded = 10
            };
            var ev3 = new Event
            {
                Title = "Собрание жильцов",
                Description = "Ежеквартальное собрание — обсуждение правил проживания",
                EventDate = today.AddDays(-2), Location = "Актовый зал",
                OrganizerId = educator.Id, PointsAwarded = 2
            };
            db.Events.AddRange(ev1, ev2, ev3);
            await db.SaveChangesAsync();

            // ── 10. Участники мероприятий ─────────────────────────────────
            db.EventParticipants.AddRange(
                new EventParticipant { EventId = ev3.Id, UserId = ivanov.Id,   PointsEarned = ev3.PointsAwarded ?? 0 },
                new EventParticipant { EventId = ev3.Id, UserId = sidorova.Id, PointsEarned = ev3.PointsAwarded ?? 0 },
                new EventParticipant { EventId = ev3.Id, UserId = sokolov.Id,  PointsEarned = ev3.PointsAwarded ?? 0 }
            );
            await db.SaveChangesAsync();

            // ── 11. Баллы студентов ───────────────────────────────────────
            db.StudentPoints.AddRange(
                new StudentPoint { UserId = ivanov.Id,   Points = 10, PointsType = "Award",   SourceType = "Inspection", Reason = "Лучший результат проверки блока" },
                new StudentPoint { UserId = petrov.Id,   Points = 10, PointsType = "Award",   SourceType = "Inspection", Reason = "Лучший результат проверки блока" },
                new StudentPoint { UserId = sidorova.Id, Points = 5,  PointsType = "Award",   SourceType = "Event",      Reason = "Участие в собрании жильцов" },
                new StudentPoint { UserId = kozlov.Id,   Points = 3,  PointsType = "Penalty", SourceType = "Inspection", Reason = "Нарушение правил проживания" },
                new StudentPoint { UserId = novikova.Id, Points = 10, PointsType = "Award",   SourceType = "Inspection", Reason = "Лучший результат проверки блока" },
                new StudentPoint { UserId = novikova.Id, Points = 5,  PointsType = "Penalty", SourceType = "Manual",     Reason = "Нарушение тишины" },
                new StudentPoint { UserId = sokolov.Id,  Points = 2,  PointsType = "Award",   SourceType = "Event",      Reason = "Участие в собрании жильцов" }
            );
            await db.SaveChangesAsync();
        }

        private static string Hash(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
