-- ============================================================
--  seed_data.sql  —  Тестовые данные для демонстрации
--  Запускать ПОСЛЕ того, как уже созданы:
--    • пользователи (Students, Inspectors, Mechanic)
--    • блоки (Blocks) и комнаты (Rooms)
--  Скрипт использует подзапросы — ID вводить вручную не нужно.
-- ============================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

-- ──────────────────────────────────────────────────────────────
-- 0a. Тестовые пользователи для нагрузочного тестирования (NBomber)
--     Логины: student_test, inspector_test, educator_test,
--             manager_test, mechanic_test
--     Пароль (общий): test1234
--     Хэш: SHA-256("test1234") в Base64 — соответствует UserService.HashPassword
-- ──────────────────────────────────────────────────────────────
DECLARE @lt_hash NVARCHAR(255) = 'k36NX7tIvUlJU2zWW401xCa4DS+DDFwwjizexCKuIkQ=';

INSERT INTO Users (Username, PasswordHash, Email, FullName, RoleId, IsActive, CreatedAt)
SELECT 'student_test',   @lt_hash, 'student@test.local',   N'Тестовый Студент',     r.Id, 1, GETDATE()
FROM   Roles r WHERE r.Name = 'Student'   AND NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'student_test');

INSERT INTO Users (Username, PasswordHash, Email, FullName, RoleId, IsActive, CreatedAt)
SELECT 'inspector_test', @lt_hash, 'inspector@test.local', N'Тестовый Проверяющий', r.Id, 1, GETDATE()
FROM   Roles r WHERE r.Name = 'Inspector' AND NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'inspector_test');

INSERT INTO Users (Username, PasswordHash, Email, FullName, RoleId, IsActive, CreatedAt)
SELECT 'educator_test',  @lt_hash, 'educator@test.local',  N'Тестовый Воспитатель', r.Id, 1, GETDATE()
FROM   Roles r WHERE r.Name = 'Educator'  AND NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'educator_test');

INSERT INTO Users (Username, PasswordHash, Email, FullName, RoleId, IsActive, CreatedAt)
SELECT 'manager_test',   @lt_hash, 'manager@test.local',   N'Тестовая Заведующая',  r.Id, 1, GETDATE()
FROM   Roles r WHERE r.Name = 'Manager'   AND NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'manager_test');

INSERT INTO Users (Username, PasswordHash, Email, FullName, RoleId, IsActive, CreatedAt)
SELECT 'mechanic_test',  @lt_hash, 'mechanic@test.local',  N'Тестовый Мастер',      r.Id, 1, GETDATE()
FROM   Roles r WHERE r.Name = 'Mechanic'  AND NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'mechanic_test');

-- ──────────────────────────────────────────────────────────────
-- 0. Зоны проверки (если ещё не созданы)
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM InspectionZones)
BEGIN
    INSERT INTO InspectionZones (Name, DisplayName) VALUES
        ('kitchen',   'Кухня'),
        ('bathroom',  'Санузел'),
        ('corridor',  'Коридор'),
        ('room',      'Комната');
END;

-- ──────────────────────────────────────────────────────────────
-- 1. Баллы студентов (StudentPoints)
--    Стратегия: берём всех активных студентов и инспекторов,
--    добавляем каждому несколько записей Award и Penalty.
-- ──────────────────────────────────────────────────────────────

-- Временная таблица с нужными пользователями
DROP TABLE IF EXISTS #Students;
SELECT u.Id AS UserId
INTO   #Students
FROM   Users u
JOIN   Roles r ON r.Id = u.RoleId
WHERE  r.Name IN ('Student', 'Inspector')
  AND  u.IsActive = 1;

-- Начисления за мероприятия и хорошее поведение
INSERT INTO StudentPoints (UserId, Points, PointsType, SourceType, Reason, CreatedAt)
SELECT
    s.UserId,
    pts.Points,
    'Award',
    pts.SourceType,
    pts.Reason,
    pts.CreatedAt
FROM #Students s
CROSS JOIN (VALUES
    (10, 'Event',  'Участие в субботнике',                      DATEADD(day, -30, GETDATE())),
    (5,  'Event',  'Участие в спортивном мероприятии',           DATEADD(day, -20, GETDATE())),
    (15, 'Event',  'Призовое место в олимпиаде',                 DATEADD(day, -10, GETDATE())),
    (5,  'Manual', 'Помощь в организации культурного мероприятия',DATEADD(day,  -5, GETDATE()))
) AS pts(Points, SourceType, Reason, CreatedAt)
WHERE NOT EXISTS (
    SELECT 1 FROM StudentPoints sp
    WHERE sp.UserId = s.UserId AND sp.Reason = pts.Reason
);

-- Взыскания
INSERT INTO StudentPoints (UserId, Points, PointsType, SourceType, Reason, CreatedAt)
SELECT
    s.UserId,
    pen.Points,
    'Penalty',
    pen.SourceType,
    pen.Reason,
    pen.CreatedAt
FROM #Students s
CROSS JOIN (VALUES
    (3, 'Sanitary', 'Неудовлетворительное санитарное состояние блока', DATEADD(day, -15, GETDATE())),
    (2, 'Manual',   'Нарушение правил внутреннего распорядка',          DATEADD(day,  -7, GETDATE()))
) AS pen(Points, SourceType, Reason, CreatedAt)
-- Взыскание добавляем только половине студентов (каждому второму)
WHERE s.UserId % 2 = 0
  AND NOT EXISTS (
    SELECT 1 FROM StudentPoints sp
    WHERE sp.UserId = s.UserId AND sp.Reason = pen.Reason
);

DROP TABLE IF EXISTS #Students;

-- ──────────────────────────────────────────────────────────────
-- 2. Проверки (Inspections)
--    По одной проверке для каждого блока за каждую из 4 недель,
--    распределяем по зонам и инспекторам.
-- ──────────────────────────────────────────────────────────────

-- Инспекторы
DROP TABLE IF EXISTS #Inspectors;
SELECT u.Id AS InspectorId,
       ROW_NUMBER() OVER (ORDER BY u.Id) AS Rn
INTO   #Inspectors
FROM   Users u
JOIN   Roles r ON r.Id = u.RoleId
WHERE  r.Name = 'Inspector' AND u.IsActive = 1;

-- Зоны
DROP TABLE IF EXISTS #Zones;
SELECT Id AS ZoneId,
       ROW_NUMBER() OVER (ORDER BY Id) AS Rn
INTO   #Zones
FROM   InspectionZones;

-- Блоки
DROP TABLE IF EXISTS #Blocks;
SELECT Id AS BlockId,
       ROW_NUMBER() OVER (ORDER BY Id) AS Rn
INTO   #Blocks
FROM   Blocks;

DECLARE @InspectorCount INT = (SELECT COUNT(*) FROM #Inspectors);
DECLARE @ZoneCount      INT = (SELECT COUNT(*) FROM #Zones);
DECLARE @BlockCount     INT = (SELECT COUNT(*) FROM #Blocks);

-- Добавляем проверки за 4 недели назад по сегодня (раз в неделю)
DECLARE @WeekOffset INT = 0;
WHILE @WeekOffset <= 3
BEGIN
    DECLARE @Date DATE = DATEADD(week, -@WeekOffset, CAST(GETDATE() AS DATE));

    INSERT INTO Inspections (BlockId, RoomId, ZoneId, InspectorId, InspectionDate, Score, Comment, CreatedAt)
    SELECT
        b.BlockId,
        NULL,   -- оценка всего блока, без комнаты
        z.ZoneId,
        i.InspectorId,
        @Date,
        -- Оценка: псевдослучайная через остаток от деления
        CASE ((b.Rn + z.Rn + @WeekOffset) % 10)
            WHEN 0 THEN 4  WHEN 1 THEN 6  WHEN 2 THEN 8
            WHEN 3 THEN 9  WHEN 4 THEN 5  WHEN 5 THEN 7
            WHEN 6 THEN 3  WHEN 7 THEN 10 WHEN 8 THEN 6
            ELSE 8
        END,
        CASE ((b.Rn + z.Rn + @WeekOffset) % 5)
            WHEN 0 THEN 'Требует уборки'
            WHEN 1 THEN 'Чисто'
            WHEN 2 THEN 'Удовлетворительно'
            WHEN 3 THEN 'Хорошее состояние'
            ELSE NULL
        END,
        GETDATE()
    FROM #Blocks b
    CROSS JOIN #Zones z
    CROSS JOIN (
        SELECT TOP 1 InspectorId
        FROM #Inspectors
        WHERE Rn = ((b.Rn + @WeekOffset) % NULLIF(@InspectorCount, 0)) + 1
    ) i
    WHERE NOT EXISTS (
        SELECT 1 FROM Inspections ins
        WHERE ins.BlockId = b.BlockId
          AND ins.ZoneId  = z.ZoneId
          AND ins.InspectionDate = @Date
    );

    SET @WeekOffset = @WeekOffset + 1;
END;

DROP TABLE IF EXISTS #Inspectors;
DROP TABLE IF EXISTS #Zones;
DROP TABLE IF EXISTS #Blocks;

-- ──────────────────────────────────────────────────────────────
-- 3. Заявки на ремонт (RepairRequests)
-- ──────────────────────────────────────────────────────────────

-- Берём несколько студентов для создания заявок
DROP TABLE IF EXISTS #ReqStudents;
SELECT TOP 5
    u.Id   AS UserId,
    res.BlockId,
    res.RoomId,
    ROW_NUMBER() OVER (ORDER BY u.Id) AS Rn
INTO #ReqStudents
FROM Users u
JOIN Roles r ON r.Id = u.RoleId
LEFT JOIN Residences res ON res.UserId = u.Id AND res.IsCurrent = 1
WHERE r.Name IN ('Student', 'Inspector') AND u.IsActive = 1;

-- Если ни один студент не имеет проживания — берём первый блок
DECLARE @FallbackBlockId INT = (SELECT TOP 1 Id FROM Blocks ORDER BY Id);

-- Заявки с разными статусами
INSERT INTO RepairRequests
    (Title, Description, BlockId, RoomId, RequestedById, Status, Priority, CreatedAt)
SELECT
    req.Title,
    req.Description,
    COALESCE(s.BlockId, @FallbackBlockId),
    s.RoomId,
    s.UserId,
    req.Status,
    req.Priority,
    req.CreatedAt
FROM #ReqStudents s
CROSS JOIN (VALUES
    ('Не работает розетка',    'В комнате не работает одна из розеток у окна',      'Pending',    'Normal', DATEADD(day,  -2, GETDATE())),
    ('Протекает кран',          'На кухне течёт кран горячей воды',                  'InProgress', 'High',   DATEADD(day,  -5, GETDATE())),
    ('Перегорела лампочка',     'В коридоре блока не работает освещение',            'Completed',  'Low',    DATEADD(day, -14, GETDATE())),
    ('Сломана дверная ручка',   'Входная дверь блока не закрывается нормально',      'Pending',    'High',   DATEADD(day,  -1, GETDATE())),
    ('Сломан смеситель',        'В ванной комнате сломан смеситель холодной воды',   'Cancelled',  'Normal', DATEADD(day, -20, GETDATE()))
) AS req(Title, Description, Status, Priority, CreatedAt)
-- Чтобы не дублировать — одна заявка на студента на тип проблемы
WHERE NOT EXISTS (
    SELECT 1 FROM RepairRequests rr
    WHERE rr.RequestedById = s.UserId AND rr.Title = req.Title
);

-- Назначаем слесаря на заявки со статусом InProgress
UPDATE rr
SET    rr.AssignedToId = (
           SELECT TOP 1 u.Id
           FROM   Users u
           JOIN   Roles r ON r.Id = u.RoleId
           WHERE  r.Name = 'Mechanic' AND u.IsActive = 1
           ORDER  BY u.Id
       )
FROM   RepairRequests rr
WHERE  rr.Status = 'InProgress'
  AND  rr.AssignedToId IS NULL;

-- Дата завершения для выполненных заявок
UPDATE RepairRequests
SET    CompletedAt = DATEADD(day, 3, CreatedAt)
WHERE  Status = 'Completed' AND CompletedAt IS NULL;

DROP TABLE IF EXISTS #ReqStudents;

-- ──────────────────────────────────────────────────────────────
-- Готово
-- ──────────────────────────────────────────────────────────────
COMMIT TRANSACTION;
PRINT 'Seed завершён успешно.';
