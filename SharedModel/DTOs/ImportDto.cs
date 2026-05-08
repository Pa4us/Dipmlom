namespace SharedModel.DTOs;

// ─── Импорт аккаунтов (Заведующая) ───────────────────────────────────────────

public class ImportUserRowDto
{
    public int RowNumber { get; set; }
    public string FullName  { get; set; } = "";
    public string Username  { get; set; } = "";
    public string Email     { get; set; } = "";
    public string RoleName  { get; set; } = "Student";
    /// <summary>Заполняется API при валидации — автосгенерированный пароль</summary>
    public string? GeneratedPassword { get; set; }
    /// <summary>Описание ошибки. Пусто — строка валидна</summary>
    public string? Error { get; set; }
    public bool IsValid => string.IsNullOrEmpty(Error);
}

public class ImportUsersRequestDto
{
    public List<ImportUserRowDto> Rows { get; set; } = new();
    /// <summary>true — только проверить, false — реально создать аккаунты</summary>
    public bool DryRun { get; set; } = true;
}

public class ImportUsersResultDto
{
    public List<ImportUserRowDto> ValidRows   { get; set; } = new();
    public List<ImportUserRowDto> InvalidRows { get; set; } = new();
    public int CreatedCount { get; set; }
}

// ─── Импорт заселения (Воспитатель) ──────────────────────────────────────────

public class ImportResidenceRowDto
{
    public int RowNumber { get; set; }
    public string  Username    { get; set; } = "";
    public string? FullName    { get; set; }        // справочно
    public string  BlockNumber { get; set; } = "";
    public string? RoomNumber  { get; set; }        // если не указана — первая свободная в блоке
    /// <summary>Заполняется API: какая комната будет назначена (для превью)</summary>
    public string? AssignedRoom { get; set; }
    public string? Error { get; set; }
    public bool IsValid => string.IsNullOrEmpty(Error);
}

public class ImportResidencesRequestDto
{
    public List<ImportResidenceRowDto> Rows { get; set; } = new();
    public bool DryRun { get; set; } = true;
}

public class ImportResidencesResultDto
{
    public List<ImportResidenceRowDto> ValidRows   { get; set; } = new();
    public List<ImportResidenceRowDto> InvalidRows { get; set; } = new();
    public int CreatedCount { get; set; }
}
