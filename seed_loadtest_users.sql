-- Вспомогательный скрипт: только тестовые пользователи для NBomber.
-- Выполняется отдельно, без остального seed_data.sql.
SET NOCOUNT ON;
BEGIN TRANSACTION;

DECLARE @h NVARCHAR(255) = 'k36NX7tIvUlJU2zWW401xCa4DS+DDFwwjizexCKuIkQ=';

INSERT INTO Users (Username, PasswordHash, Email, FullName, RoleId, IsActive, CreatedAt)
SELECT 'student_test',   @h, 'student@test.local',   N'Тестовый Студент',     r.Id, 1, GETDATE()
FROM   Roles r WHERE r.Name = 'Student'   AND NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'student_test');

INSERT INTO Users (Username, PasswordHash, Email, FullName, RoleId, IsActive, CreatedAt)
SELECT 'inspector_test', @h, 'inspector@test.local', N'Тестовый Проверяющий', r.Id, 1, GETDATE()
FROM   Roles r WHERE r.Name = 'Inspector' AND NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'inspector_test');

INSERT INTO Users (Username, PasswordHash, Email, FullName, RoleId, IsActive, CreatedAt)
SELECT 'educator_test',  @h, 'educator@test.local',  N'Тестовый Воспитатель', r.Id, 1, GETDATE()
FROM   Roles r WHERE r.Name = 'Educator'  AND NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'educator_test');

INSERT INTO Users (Username, PasswordHash, Email, FullName, RoleId, IsActive, CreatedAt)
SELECT 'manager_test',   @h, 'manager@test.local',   N'Тестовая Заведующая',  r.Id, 1, GETDATE()
FROM   Roles r WHERE r.Name = 'Manager'   AND NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'manager_test');

INSERT INTO Users (Username, PasswordHash, Email, FullName, RoleId, IsActive, CreatedAt)
SELECT 'mechanic_test',  @h, 'mechanic@test.local',  N'Тестовый Мастер',      r.Id, 1, GETDATE()
FROM   Roles r WHERE r.Name = 'Mechanic'  AND NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'mechanic_test');

COMMIT TRANSACTION;

SELECT Username, FullName, (SELECT Name FROM Roles WHERE Id = u.RoleId) AS Role, IsActive
FROM   Users u
WHERE  Username IN ('student_test','inspector_test','educator_test','manager_test','mechanic_test')
ORDER  BY Username;
