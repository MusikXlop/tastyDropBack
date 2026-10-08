using tastyDrop.Api.DTOs;

namespace tastyDrop.Api.Services;

//интерфейс (
public interface IDbService
{
    /*контракт метода регистрации
     принимает dto, готовый хеш и код и возвращает id созданного юзера*/ 
    Task<int> RegisterUserAsync(RegisterDto dto, string passwordHash, string code);


    Task<UserDto?> ConfirmEmailAsync(string email, string code);

    // Метод обновления кода подтверждения при повторном запросе / логине
    Task UpdateConfirmationCodeAsync(int userId, string NewCode);

    //поиск по email(может вернуть null)
    Task<UserDto?> GetUserByEmailAsync(string email);

    //поиску по айди(может вернут нуль)
    Task<UserDto?> GetUserByIdAsync(int userId);

    //фри кейс
    Task<bool> CheckUserFreeCaseStatusAsync(int userId, int caseId);

    //для выбора фулл шмоток из бд 
    Task<IEnumerable<ItemDto>> GetAllItemsAsync();

    //для апгрейда
    Task<UpgradeResultDto> ExecuteUpgradeAsync(int userId, int inventoryId, int targetItemId);

    //для прогресса юзера по бп 
    Task<List<BattlePassLevelDto>> GetUserBattlePassAsync(int userId);

    //для выдачи наград
    Task<ClaimRewardResultDto> ClaimBattlePassRewardAsync(int userId, int levelId);

    //для продажи 
    Task<decimal> SellItemAsync(int userId, int inventoryId);

    //список всех кейсов
    Task<IEnumerable<CaseDto>> GetAllCasesAsync();

    //предметы внутри кейса 
    Task<IEnumerable<ItemDto>> GetCaseItemsAsync(int caseId);

    //инвентарь юзера
    Task<IEnumerable<InventoryDtos>> GetUserInventoryAsync(int userId);


    // Вывод предмета из инвентаря
    Task WithdrawInventoryItemAsync(int inventoryId, int userId);

    //ник при тыке на профайл
    Task<string?> GetUsernameByIdAsync(int userId);

    //запись свежего дропа в livedrops 
    Task<int> AddLiveDropAsync(int userId, int itemId, int caseId);

    //для бест оф зе бест дропа
    Task<BestDropDto?> GetUserBestDropAsync(int userId);

    /*
     Контракт метода открытия кейсов 
    Возвращает кортеж  с списоком выбитых предметов и нью балансом 
     */
    Task<(List<WonItemDto> items, decimal newBalance, int newBpBalance)> 
        OpenCaseAsync(int userId, int caseId, int amount);
    /*
    Task<(List<WonItemDto> items, decimal newBalance)> 
    кортеж(tuple) позволяет вернуть из одного метода сразу два значения
    (скисок предмиетов и число баланса)
     */


    //метол для получения текущегор лвла батл пасса юзера
    Task<int> GetUserCurrentBpLevelAsync(int userId);

    Task<decimal> TopUpBalanceAsync(int userId, decimal amount);

    //получаем ласт 25 дропов
    Task<IEnumerable<LiveDropDto>> GetRecentDropsAsync();


    //для мин 
    //статрт  ньью игры
    Task<MinesGameStatusDto> MinesStartGameAsync(int userId, decimal betAmount, int minesCount, int gridSize = 5);
    
    //открыть клетку
    Task<MinesGameStatusDto> MinesRevealCellAsync(int userId, int gameId, int cellIndex);
    //забрать вин
    Task<MinesGameStatusDto> MinesCashoutAsync(int userId, int gameId);


    //получение текущен неоконченной игры (при перезапуске страницы
    Task<MinesGameStatusDto?> GetActiveMinesGameAsync(int userId);

    //для колеса фортуны (режим)
    Task<WheelResultDto> WheelSpinAsync(int userId, decimal betAmount, string targetColor);


    //для краша 

    Task<CrashGameStatusDto> CrashStartGameAsync(int userId, decimal betAmount);

    Task<CrashGameStatusDto> CrashCashoutAsync(int userId, int gameId, decimal requestedMultiplier);

    Task<CrashGameStatusDto?> GetActiveCrashGameAsync(int userId);

    Task CrashSetCrashedAsync(int gameId);

    //ресет пароля
    Task<bool> ResetPasswordAsync(string email, string code, string newPasswordHash);
}
