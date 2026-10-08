using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using tastyDrop.Api.DTOs;

namespace tastyDrop.Api.Services;

public class DbService : IDbService
{
    //переменная для хранениря строки подключения к бд 
    private readonly string _connectionString;

    // IConfiguration чтоб счатать appsettings.json
    public DbService(IConfiguration configuration)
    {
        //достем строку  DefaultConnection из appsettings.json
        _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string not found.");
    }

    //создаем подключение к бд
    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    //для получения ника при тыка на профайл
    public async Task<string?> GetUsernameByIdAsync(int userId)
    {
        using var connection = CreateConnection();
        string sql = "SELECT Username FROM Users WHERE UserId = @UserId";
        return await connection.QueryFirstOrDefaultAsync<string>(sql, new { UserId = userId });
    }



    //вызов sp_registerUser   
    public async Task<int> RegisterUserAsync(RegisterDto dto, string passwordHash, string code)
    {
        Console.WriteLine($"→ Вход: Username={dto.Username}, Email={dto.Email}, Code={code}");

        //using var  (автоматически закроет и уничтожит
        //соеденение с бд после выполнения  
        using var connection = CreateConnection();


        //DynamicParameters класс библиотеки Dapper для безопасной
        //передачи параметров в sql
        var parameters = new DynamicParameters();
        parameters.Add("@Username", dto.Username); //передаем логин почту и т д
        parameters.Add("@Email", dto.Email);
        parameters.Add("@PasswordHash", passwordHash);
        parameters.Add("@EmailConfirmationCode", code);

        //регистрируем выходной(OUTPUT) параметр куда процедура запишет ID нью юзера 
        parameters.Add("@NewUserId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        //ExecuteAsync (метод Drapper) для выполнения команд/процедур без возврата таблицы
        await connection.ExecuteAsync("sp_RegisterUser", parameters, commandType: CommandType.StoredProcedure);

        //достаем из заполнившихся параметров значения OUTPUT переменной @NewUserId 
        return parameters.Get<int>("@NewUserId");




    }


    //метод обновления кода подтверждения в бд 
    public async Task UpdateConfirmationCodeAsync(int userId, string newCode)
    {
        using var connection = CreateConnection();
        string sql = @"
            UPDATE Users
            SET EmailConfirmationCode = @NewCode
            WHERE UserId = @UserId";

        //асинхоронно выполяет команду sql 
        await connection.ExecuteAsync(sql, new { UserId = userId, NewCode = newCode });
    }



    //получаем пользователя по email(для входа)
    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        //using var  (автоматически закроет и уничтожит
        //соеденение с бд после выполнения  
        using var connection = CreateConnection();
        //sql запрос
        string sql = @"
            SELECT 
                UserId,
                Username AS UserName,
                Email AS UserEmail,
                PasswordHash,
                Balance,
                BattlePassBalance,
                Role,
                IsEmailConfirmed
            FROM Users
            WHERE Email = @Email";
        //выполняем запрос и мапим в UserDto
        return await connection.QueryFirstOrDefaultAsync<UserDto?>(sql, new { Email = email });
    }


    //получаем пользователя по его айди
    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        //using var  (автоматически закроет и уничтожит
        //соеденение с бд после выполнения  
        using var connection = CreateConnection();
        //sql запрос для поиска по айди
        string sql = "SELECT UserId, Username, Email, Balance, Role FROM Users WHERE UserId = @UserId";
        //выполняем запрос и мапим в UserDto
        return await connection.QueryFirstOrDefaultAsync<UserDto?>(sql, new { UserId = userId });
    }


    //получаем список всех кейсов
    public async Task<IEnumerable<CaseDto>> GetAllCasesAsync()
    {
        //using var  (автоматически закроет и уничтожит
        //соеденение с бд после выполнения  
        using var connection = CreateConnection();
        //запрос на получение всех кейсов 
        string sql = "SELECT CaseId, Name, Price, ImageUrl, Currency FROM Cases";
        //выполняем запрос и мапим в UserDto
        return await connection.QueryAsync<CaseDto>(sql);
    }



    //получаем содержимое шмоток в кейсе
    public async Task<IEnumerable<ItemDto>> GetCaseItemsAsync(int caseId)
    {
        //using var  (автоматически закроет и уничтожит
        //соеденение с бд после выполнения  
        using var connection = CreateConnection();

        // Соединяем таблицу связей CaseItems с предметами Items по CaseId
        string sql = @"
                         SELECT i.ItemId, i.Name, i.ImageUrl, i.Price, i.Rarity, ci.DropWeight
                         FROM CaseItems ci
                         INNER JOIN Items i ON ci.ItemId = i.ItemId
                         WHERE ci.CaseId = @CaseId"; // 
                                                     //выполняем запрос и мапим в UserDto
        return await connection.QueryAsync<ItemDto>(sql, new { CaseId = caseId });
    }



    //получаем ИНВЕНТАРЬ ЮЗЕРА
    public async Task<IEnumerable<InventoryDtos>> GetUserInventoryAsync(int userId)
    {
        //using var  (автоматически закроет и уничтожит
        //соеденение с бд после выполнения  
        using var connection = CreateConnection();

        // выбираем не проданные/не зщабранные вещи пользователя(сорт по свежости)
        string sql = @"
                         SELECT ui.InventoryId,
                            i.ItemId, 
                            i.Name, 
                            i.ImageUrl, 
                            i.Price,
                            i.Rarity,
                            ui.DroppedAt
                         FROM UserInventory ui
                         INNER JOIN Items i ON ui.ItemId = i.ItemId
                         WHERE ui.UserId = @UserId AND ui.IsClaimed = 0
                         ORDER BY ui.DroppedAt DESC"; // 
                                                      //выполняем запрос и мапим в UserDto
        return await connection.QueryAsync<InventoryDtos>(sql, new { UserId = userId });
    }

    public async Task<UserDto?> ConfirmEmailAsync(string email, string code)
    {
        using var connection = CreateConnection();
        var parametrs = new DynamicParameters();
        parametrs.Add("@Email", email);
        parametrs.Add("@ConfirmationCode", code);

        return await connection.QuerySingleOrDefaultAsync<UserDto>(
        "sp_ConfirmEmail",
        parametrs,
        commandType: CommandType.StoredProcedure
        );
    }

    //вызов sp_openCaseTransaction (мультикасты х2 x3 x4)
    public async Task<(List<WonItemDto> items, decimal newBalance, int newBpBalance)>
                OpenCaseAsync(int userId, int caseId, int amount)
    {

        using var connection = CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@CaseId", caseId);
        parameters.Add("@Amount", amount);

        //указываем output параметр для нового остатка денег на балансе
        //точность decimal(18,2)   
        parameters.Add("@NewBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

        // OUTPUT параметр для Battle Pass баланса
        parameters.Add("@NewBattlePassBalance", dbType: DbType.Int32, direction: ParameterDirection.Output);


        /* QueryAsync<WonItemDto> метод DApper
         * он выполняет хранимку забирает итоговый SELECT из
         * нее и автоматически превращает каждую строчку ответа в 
         * объект WonItemDto */
        var result = await connection.QueryAsync<WonItemDto>(
            "sp_OpenCaseTransaction",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        //забираем вычесленный базой баланс из OUTPUT параметра  
        decimal newBalance = parameters.Get<decimal>("@NewBalance");
        int newBpBalance = parameters.Get<int>("@NewBattlePassBalance");

        //преевращаем результат Dapper в List и возвращаем вмтес с новым балансом
        return (result.ToList(), newBalance, newBpBalance);

    }

    // пополнялка баланса в бд
    public async Task<decimal> TopUpBalanceAsync(int userId, decimal amount)
    {
        using var connection = CreateConnection();//подключание к бд
        /* Увелечение  баланса
         * возврат нью значения баланса 
         * и обнова для конкретного юзера
         */
        string sql = @"
            UPDATE Users
            SET Balance = Balance + @Amount 
            OUTPUT INSERTED.Balance
            WHERE UserId = @UserId;";
        //выполняем запролс и получаем оджно значение decimal
        return await connection.ExecuteScalarAsync<decimal>(sql, new { UserId = userId, Amount = amount });
    }

    public async Task<decimal> SellItemAsync(int userId, int targetId)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            //  Ищем самый свежий непроданный предмет юзера по InventoryId или ItemId
            // И одновременно обновляем баланс пользователя
            string updateBalanceSql = @"
            WITH TargetItem AS (
                SELECT TOP (1) ui.InventoryId, i.Price
                FROM UserInventory ui
                INNER JOIN Items i ON ui.ItemId = i.ItemId
                WHERE ui.UserId = @UserId 
                  AND ui.IsClaimed = 0
                  AND (ui.InventoryId = @TargetId OR ui.ItemId = @TargetId)
                ORDER BY ui.DroppedAt DESC
            )
            UPDATE Users
            SET Balance = Balance + ti.Price
            OUTPUT INSERTED.Balance
            FROM Users u
            INNER JOIN TargetItem ti ON 1=1
            WHERE u.UserId = @UserId;";

            decimal? newBalance = await connection.ExecuteScalarAsync<decimal?>(
                updateBalanceSql,
                new { UserId = userId, TargetId = targetId },
                transaction
            );

            if (newBalance == null)
            {
                throw new Exception("Предмет не найден или уже был продан!");
            }

            // 2. Помечаем предмет как проданный
            string updateInventorySql = @"
            WITH TargetItem AS (
                SELECT TOP (1) ui.InventoryId
                FROM UserInventory ui
                WHERE ui.UserId = @UserId 
                  AND ui.IsClaimed = 0
                  AND (ui.InventoryId = @TargetId OR ui.ItemId = @TargetId)
                ORDER BY ui.DroppedAt DESC
            )
            UPDATE UserInventory
            SET IsClaimed = 1
            FROM UserInventory ui
            INNER JOIN TargetItem ti ON ui.InventoryId = ti.InventoryId;";

            await connection.ExecuteAsync(
                updateInventorySql,
                new { UserId = userId, TargetId = targetId },
                transaction
            );

            transaction.Commit();
            return newBalance.Value;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    //вывод предмета на акк (типо)
    public async Task WithdrawInventoryItemAsync(int inventoryId, int userId)
    {
        using var connection = CreateConnection();

        string sql = @"
        UPDATE UserInventory 
        SET IsClaimed = 1 
        WHERE InventoryId = @InventoryId 
          AND UserId = @UserId 
          AND IsClaimed = 0;";

        int rowsAffected = await connection.ExecuteAsync(sql, new
        {
            InventoryId = inventoryId,
            UserId = userId
        });
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException("Не удалось вывести предмет (возможно, он уже выведен).");
        }
    }

    //сохранение дропа 
    public async Task<int> AddLiveDropAsync(int userId, int itemId, int caseId)
    {
        using var connection = CreateConnection();
        string sql = @"
            INSERT INTO LiveDrops (UserId, ItemId, CaseId, DroppedAt)
            VALUES (@UserId, @ItemId, @CaseId, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() as int);";

        return await connection.QuerySingleAsync<int>(sql, new
        {
            UserId = userId,
            ItemId = itemId,
            CaseId = caseId
        });
    }


    //вывод 25 ласт шмоток
    public async Task<IEnumerable<LiveDropDto>> GetRecentDropsAsync()
    {
        using var connection = CreateConnection();

        //запрос выбирает 25 ласт дропов
        string sql = @"
        SELECT TOP 25 
            ld.DropId,
            ld.UserId,
            u.Username,
            i.Name AS ItemName,
            i.ImageUrl AS ItemImage,
            i.Rarity AS ItemRarity,
            ld.CaseId,
            ISNULL(c.Name, N'Upgrade') AS CaseName,
            c.ImageUrl AS CaseImage,
            ld.DroppedAt AS DropTime
        FROM LiveDrops ld
        INNER JOIN Users u ON ld.UserId = u.UserId
        INNER JOIN Items i ON ld.ItemId = i.ItemId
        LEFT JOIN Cases c ON ld.CaseId = c.CaseId
        ORDER BY ld.DroppedAt DESC";

        return await connection.QueryAsync<LiveDropDto>(sql);
    }




    //для вывоад фулл мшотом из бд
    public async Task<IEnumerable<ItemDto>> GetAllItemsAsync()
    {
        using var connection = CreateConnection();
        string sql = "SELECT ItemId, Name, ImageUrl, Price, Rarity FROM Items";
        return await connection.QueryAsync<ItemDto>(sql);
    }


    //апгрейд 
    public async Task<UpgradeResultDto> ExecuteUpgradeAsync(int userId, int inventoryId, int targetItemId)
    {
        //подключение к бд 
        using var connection = CreateConnection();

        //открываем соеденение с бд 
        connection.Open();

        //транзакцию делаем чтоб все изменения в бд выполнялась как одна операция 
        using var transaction = connection.BeginTransaction();


        try
        {
            //получаем шмотку и его цену 
            // запрос выбирает данные шмотки пользователя 1
            // Получаем запись из таблицы UserInventory и сокращённо называем её ui 2
            // Соединяем UserInventory с таблицей Items чтобы получить информацию о шмотке 3
            // Проверяем конкретную шмотку конкретного пользователя и что шмотка ещё не забран чи нема. 4
            string sourceSql = @"SELECT i.ItemId, i.Price, i.Name, i.ImageUrl, i.Rarity 
                            FROM UserInventory ui
                            INNER JOIN Items i ON ui.ItemId = i.ItemId
                            WHERE ui.InventoryId = @InventoryId AND ui.UserId = @UserId and ui.isClaimed = 0";

            //выполняем sql запрос и получаем первый целый предмет или нуль
            var sourceItem = await connection.QueryFirstOrDefaultAsync(
                //передаем sql щапрпос в dapper 
                sourceSql, new { InventoryId = inventoryId, UserId = userId }, transaction);

            //проверка удалось ли най
            if (sourceItem == null)
                throw new Exception("Не могу айтем найти в иннвентаре");

            //получаем шмотку
            //sql запроc для поиска шмотки который хочет получить пользователоь
            string targetSql = "SELECT ItemId, Name, ImageUrl, Price, Rarity FROM Items WHERE ItemId = @TargetItemId";

            //выполняем запрос и получаем целевый шмотку или нуль 
            var targetItem = await connection.QueryFirstOrDefaultAsync(
                //передаем sql запрос id целевой шмотки и текущу транзацкию
                targetSql, new { TargetItemId = targetItemId }, transaction);

            //проверка существует ли шмотка 
            if (targetItem == null)
                throw new Exception("этой шмотки нет в бд");

            //нашу шмотку переобразуем в decimal 
            decimal sourcePrice = (decimal)sourceItem.Price;

            //ту которую мы хотим получить 
            decimal targetPrice = (decimal)targetItem.Price;


            //проверка шо наша то что мы выбрали больше нуля
            if (targetPrice <= 0)
                throw new Exception("цена меньше нуля КАК ");

            //расчет шанса апгрейда по формуле моя цена / цена шмотки которую я хочу * 100
            double winChance = (double)(sourcePrice / targetPrice) * 100.0;


            //если моя шмотка дороже того чо я хочу то делаю шо шанс не может быть больше 100%
            if (winChance > 100.0)
            {
                winChance = 100.0;
            }

            //если лузнул то помечаю как ЛУз
            string burnSql = "UPDATE UserInventory SET IsClaimed = 1 WHERE InventoryId = @InventoryId";

            await connection.ExecuteAsync(burnSql, new { InventoryId = inventoryId }, transaction);


            //генерим случайное число от 1 до 100
            double roll = Random.Shared.NextDouble() * 100.0;
            //сравниваем рандом число с шансом и определяю вин
            bool isSucces = roll <= winChance;


            //покеа предмета выбитого нема устанавливаем нуль
            WonItemDto? wonItemDto = null;

            //был ли апгрейд успешен
            if (isSucces)
            {
                //начисляем предмет в инвентраь юзера
                /*Добавляем юзеру предмет дату получения и состояние IsClaimed
                 передаем значения параметров и ставим IsClaimed = 0 
                получапем id только что созданной записи*/
                string addSql = @"INSERT INTO UserInventory(UserId,ItemId, DroppedAt, IsClaimed)
                                VALUES(@UserId, @TargetItemId, GETUTCDATE(),0);
                                SELECT SCOPE_IDENTITY();";

                //выполняем insert и получаем id нью предмета в инвенаре
                int newInventoryId = await connection.ExecuteScalarAsync<int>(
                //передаем sql параметры пользователя и нашей шмотки и  текущуб транзацию 
                    addSql, new { UserId = userId, TargetItemId = targetItemId }, transaction);

                //фиксируем выигрыш в liveDrops 
                /*успешный апгрейд будет ткт
                 caseid = null значит что дроп из апгрейдера (не из кейса)*/
                string dropSql = @"INSERT INTO LiveDrops(UserId,ItemId,CaseId, DroppedAt)
                                VALUES (@UserId, @TargetItemId, NULL, GETUTCDATE());
                                SELECT SCOPE_IDENTITY();";

                // Выполняем INSERT в LiveDrops
                int dropId = await connection.ExecuteScalarAsync<int>(
                    dropSql, new { UserId = userId, TargetItemId = targetItemId }, transaction);

                //делаем dto  ПОЛУЧЕННОГО ПРЕДМЕТА для отправки на фронтенд
                wonItemDto = new WonItemDto
                {
                    //передаем id новой записи 
                    DropId = dropId,

                    InventoryId = newInventoryId,
                    //айди полученого предмета 

                    ItemId = (int)targetItem.ItemId,
                    Name = (string)targetItem.Name,
                    //путь к картинк
                    ImageUrl = (string)targetItem.ImageUrl,
                    Price = targetPrice,
                    //редоксть
                    Rarity = (string)targetItem.Rarity
                };

            }
            // фиксируем все изменения транзакции в бд
            transaction.Commit();

            //возвращает результат апгрейдера клиету 
            return new UpgradeResultDto
            {
                //тру фолс в зависимости от рола
                IsSuccess = isSucces,
                //округляем выпавшее число до двух знаков после запятой
                RollValue = Math.Round(roll, 2),

                //округляем шанс до двух знаков после запятой 
                WinChance = Math.Round(winChance, 2),
                //полученный предмет или нуль при лузе
                WonItem = wonItemDto
            };



        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }


    //получение наград батлпаса
    public async Task<ClaimRewardResultDto> ClaimBattlePassRewardAsync(int userId, int levelId)
    {
        //подключение к бд 
        using var connection = CreateConnection();

        //открываем соеденение с бд 
        connection.Open();

        //транзакцию делаем чтоб все изменения в бд выполнялась как одна операция 
        using var transaction = connection.BeginTransaction();

        try
        {
            //считываем инфу о квесте и наградах
            /*1 выполнен ли квест
             забрана ли награда 
            награда в валюте
            получил кейсик*/
            string checkSql = @"
            SELECT 
                ISNULL(ubp.IsCompleted, 0) AS IsCompleted,
                ISNULL(ubp.IsRewardClaimed, 0) AS IsRewardClaimed,
                l.RewardCurrency,
                l.RewardCaseId
            FROM BattlePassLevels l
            INNER JOIN BattlePassQuests q ON l.LevelId = q.LevelId
            LEFT JOIN UserBattlePassQuests ubp ON q.QuestId = ubp.QuestId AND ubp.UserId = @UserId
            WHERE l.LevelId = @LevelId";


            //выполняем запрос и получаем инфу о квесах
            var questInfo = await connection.QuerySingleOrDefaultAsync(checkSql, new { UserId = userId, LevelId = levelId }, transaction);

            //если лвл не нашли 
            if (questInfo == null)
                return new ClaimRewardResultDto { Success = false, Message = "Уровень не найден." };

            // Если квест ещё не выполнен
            if (!questInfo.IsCompleted)
                return new ClaimRewardResultDto { Success = false, Message = "Задание еще не выполнено!" };

            // сли награда уже забрана
            if (questInfo.IsRewardClaimed)
                return new ClaimRewardResultDto { Success = false, Message = "Награда уже получена!" };

            bool caseGiven = false; // флаг выдан ли кейс

            //начисление валюты баттлпаса
            if (questInfo.RewardCurrency != null && questInfo.RewardCurrency > 0)
            {
                string updateBpBalanceSql = @"
                    UPDATE Users 
                    SET BattlePassBalance = BattlePassBalance + @Reward 
                    WHERE UserId = @UserId";


                await connection.ExecuteAsync(updateBpBalanceSql, new
                { Reward = questInfo.RewardCurrency, UserId = userId }, transaction);
            }

            //выдача награды в виде кейса 
            if (questInfo.RewardCaseId != null && questInfo.RewardCaseId > 0)
            {
                string insertFreeCaseSql = @"
                INSERT INTO UserFreeCases (UserId, CaseId, GrantedAt, IsUsed)
                VALUES (@UserId, @CaseId, GETUTCDATE(), 0)";

                await connection.ExecuteAsync(insertFreeCaseSql, new { UserId = userId, CaseId = questInfo.RewardCaseId }, transaction);

                caseGiven = true;
            }

            //помечаем как забранную награду
            string markClaimedSql = @"
            UPDATE UserBattlePassQuests
            SET IsRewardClaimed = 1
            WHERE UserId = @UserId 
              AND QuestId = (SELECT QuestId FROM BattlePassQuests WHERE LevelId = @LevelId)";

            await connection.ExecuteAsync(markClaimedSql, new { UserId = userId, LevelId = levelId }, transaction);

            //получаем баланс пользователя 
            string getBalancesSql = "SELECT Balance AS MainBalance, BattlePassBalance FROM Users WHERE UserId = @UserId";
            var balances = await connection.QuerySingleAsync(getBalancesSql, new { UserId = userId }, transaction);

            transaction.Commit();

            return new ClaimRewardResultDto
            {
                Success = true,
                Message = "Получил он награду",
                MainBalance = balances.MainBalance,
                BattlePassBalance = balances.BattlePassBalance,
                ClaimedCase = caseGiven
            };
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return new ClaimRewardResultDto{ Success = false, Message = $"Ошибка: {ex.Message}" };
        }
    }
    //лвл батл пасов 
    public async Task<List<BattlePassLevelDto>> GetUserBattlePassAsync(int userId)
    {
        using var connectoin = CreateConnection();

        // INNER JOIN гарантирует что мы связываем уровень с его квестом
        // LEFT JOIN подтягивает индивидуальный прогресс конкретного UserId из UserBattlePassQuests
        // ISNULL дефает от NULL возвращая 0 false для еще не начатых уровней.
        string sql = @"
            SELECT 
                l.LevelId,
                l.LevelNumber,
                q.Name AS QuestName,
                q.GoalType,
                q.GoalValue,
                ISNULL(ubp.CurrentProgress, 0) AS CurrentProgress,
                ISNULL(ubp.IsCompleted, 0) AS IsCompleted,
                ISNULL(ubp.IsRewardClaimed, 0) AS IsRewardClaimed,
                l.RewardText,
                l.RewardCurrency,
                l.RewardCaseId
            FROM BattlePassLevels l
            INNER JOIN BattlePassQuests q ON l.LevelId = q.LevelId
            LEFT JOIN UserBattlePassQuests ubp ON q.QuestId = ubp.QuestId AND ubp.UserId = @UserId
            ORDER BY l.LevelNumber ASC";


        var result = await connectoin.QueryAsync<BattlePassLevelDto>(sql, new { UserId = userId });
        return result.ToList();
    }


    public async Task<bool> CheckUserFreeCaseStatusAsync(int userId, int caseId)
    {
        using var connection = CreateConnection();

        //проверка есть ли у юзера фри кейсы 
        string sql = @"
        SELECT COUNT(1) 
        FROM UserFreeCases 
        WHERE UserId = @UserId 
          AND CaseId = @CaseId 
          AND IsUsed = 0;";

        int count = await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId, CaseId = caseId });
        return count > 0;
    }


    //бест оф зе бест дроп юзерка
    public async Task<BestDropDto?> GetUserBestDropAsync(int userId)
    {
        using var connectoin = CreateConnection();

        //ищем самую дорогую мшотку юзера евер
        string sql = @"
        SELECT TOP (1)
            i.ItemId,
            i.Name,
            i.ImageUrl,
            i.Price,
            i.Rarity,
            ld.DroppedAt
        FROM LiveDrops ld
        INNER JOIN Items i ON ld.ItemId = i.ItemId
        WHERE ld.UserId = @UserId
        ORDER BY i.Price DESC, ld.DroppedAt DESC;";
        //выполняем запрос 
        return await connectoin.QueryFirstOrDefaultAsync<BestDropDto?>(sql, new { UserId = userId });
    }


    //лвл батл пасса юзера
    public async Task<int> GetUserCurrentBpLevelAsync(int userId)
    {
        using var connection = CreateConnection();

        //объект для передачи параметров в dapper 
        var parametrs = new DynamicParameters();
        //входной параметр айди юзера
        parametrs.Add("@UserId", userId, DbType.Int32);
        //выходной параметр сюда sql процедура кинет текущий лвл
        parametrs.Add("@CurrentLevel", dbType: DbType.Int32, direction: ParameterDirection.Output);

        //вызов хранимой процедуры sp_GetUserCurrentBpLevel
        await connection.ExecuteAsync(
            "sp_GetUserCurrentBpLevel",
            parametrs,
            commandType: CommandType.StoredProcedure);

        //возврат знач выходного(out) параметра (лвл батл пасса)
        return parametrs.Get<int>("@CurrentLevel");
    }



    //МИНЫ старт игры (режим)
    public async Task<MinesGameStatusDto> MinesStartGameAsync(int userId, decimal betAmount, int minesCount, int gridSize = 5)
    {
        using var connection = CreateConnection();

        //динамическое колкоклеток 3x3=9 5x5 =25
        int totalCells = gridSize * gridSize;

        //генерим рандом расположение мин
        var random = new Random();
        var mineIndexes = Enumerable.Range(0, totalCells) // создаем список индексов клеток 
            .OrderBy(_ => random.Next()) // перемешивает случайным образом
            .Take(minesCount) //берем первые N  индексов как мины 
            .ToList();


        //сереализуем список мин в json 
        string minesJson = JsonSerializer.Serialize(mineIndexes);
        //DynamicParameters класс библиотеки Dapper для безопасной
        //передачи параметров в sql
        var parametrs = new DynamicParameters();
        parametrs.Add("@UserId", userId); //айди игрока
        parametrs.Add("@BetAmount", betAmount); //ставка
        parametrs.Add("@MinesCount", minesCount); //колво мин
        parametrs.Add("@GridSize", gridSize);

        parametrs.Add("@MinesIndexesJson", minesJson); //JSON с индексами мин
        //выходной параметр новый баланс
        parametrs.Add("@NewBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2); //айди игрока
         //выходной параметр id game 
        parametrs.Add("@GameId", dbType: DbType.Int32, direction: ParameterDirection.Output );

        //вызов хранимой процедуры для старта игры 
        await connection.ExecuteAsync("sp_MinesStartGame", parametrs, commandType: CommandType.StoredProcedure);

        //возвращаам dto со статусом игры
        return new MinesGameStatusDto
        {
            GameId = parametrs.Get<int>("@GameId"),
            BetAmount = betAmount,
            MinesCount = minesCount,
            GridSize = gridSize,
            CurrentMultiplier = 1.0m, //множитель всегда 1 на старте
            Status = "InGame",
            NewBalance = parametrs.Get<decimal>("@NewBalance")
        };

    }


    //открытие клетки
    public async Task<MinesGameStatusDto> MinesRevealCellAsync(int userId, int gameId, int cellIndex)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            //получаем инфу об игре 
            string gameSql = @"SELECT GameId, BetAmount, MinesCount, GridSize, CurrentMultiplier, Status 
                           FROM MinesGames WITH (UPDLOCK) 
                           WHERE GameId = @GameId AND UserId = @UserId";
            var game = await connection.QuerySingleOrDefaultAsync<MinesGameEntity>(gameSql, new { GameId = gameId, UserId = userId }, transaction);
           
            
            if (game == null || game.Status != "InGame")
                throw new Exception("Игра не найдена");

            //проверка клетки 
            string cellSql = @"SELECT IsMine, IsRevealed FROM MinesGrid WHERE GameId = @GameId AND CellIndex = @CellIndex";
            var cell = await connection.QuerySingleOrDefaultAsync<MinesGridCellEntity>(cellSql, new { GameId = gameId, CellIndex = cellIndex }, transaction);

            if (cell == null || cell.IsRevealed)
                throw new Exception("Клетка уже открыта");

            //помечаем клетку как открытую
            await connection.ExecuteAsync("UPDATE MinesGrid SET IsRevealed = 1" +
                "WHERE GameId = @GameId AND CellIndex = @CellIndex",
                new { GameId = gameId, CellIndex = cellIndex }, transaction);



            //получаем текущий баланс юзера
            decimal currentBalance = await connection.ExecuteScalarAsync<decimal>("SELECT Balance FROM Users WHERE UserId = @UserId",
                new { UserId = userId }, transaction);

            //если юзер попал на мину
            if(cell.IsMine)
            {
                //обновляем статус игры на луз
                await connection.ExecuteAsync("UPDATE MinesGames SET Status = 'Lost', FinishedAt = GETUTCDATE() WHERE GameId = @GameId",
                    new { GameId = gameId }, transaction);

                //достаем поз всех мин  для показа юзерку который лузнал
                var allMines = (await connection.QueryAsync<int>("SELECT CellIndex FROM MinesGrid WHERE GameId = @GameId AND IsMine = 1",
                new { GameId = gameId }, transaction)).ToList();

                transaction.Commit();

                return new MinesGameStatusDto
                {
                    GameId = gameId,
                    Status = "Lost",
                    IsHitMine = true, //попау на мину
                    NewBalance = currentBalance,
                    MinePositions = allMines
                };
            }

            //клетка безопаснап = пересчитываем множитель
            int revealedCount = await connection.ExecuteScalarAsync<int>(
                            "SELECT COUNT(1) FROM MinesGrid WHERE GameId = @GameId AND IsRevealed = 1", 
                            new {GameId = gameId}, transaction);


            Console.WriteLine($"[MINES REVEAL DB CHECK] GameId: {game.GameId}, MinesCount in DB: {game.MinesCount}, GridSize in DB: {game.GridSize}, RevealedCount in DB: {revealedCount}");

            //формула расчета множителя
            decimal newMultiplier = CalculateMinesMultiplier(game.MinesCount, revealedCount, game.GridSize);

            //обновляем множитель в игре 
            await connection.ExecuteAsync("UPDATE MinesGames SET CurrentMultiplier = @Mult WHERE GameId = @GameId",
                new { Mult = newMultiplier, GameId = gameId }, transaction);

            //получаем список открытых клекто
            var revealedCells = (await connection.QueryAsync<int>("SELECT CellIndex FROM MinesGrid WHERE GameId = @GameId AND IsRevealed = 1",
                new { GameId = gameId }, transaction)).ToList();


            transaction.Commit();
            
            return new MinesGameStatusDto
            {
               GameId = gameId,
               BetAmount = game.BetAmount,
               MinesCount = game.MinesCount,
               GridSize = game.GridSize,
               CurrentMultiplier = newMultiplier,
               Status = "InGame",
               NewBalance = currentBalance,
               RevealedCells = revealedCells
            };
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    //забрать бабки
    public async Task<MinesGameStatusDto> MinesCashoutAsync(int userId, int gameId)
    {
        using var connection = CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@GameId", gameId);
        //ньюю баланс игрока (выходной параметр)
        parameters.Add("@NewBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);
        parameters.Add("@WinAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);
        //выходной параметр сумма вина


        //вызываем хранимую процедуру Cashout (считает выигрыш и обновляет баланс)
        await connection.ExecuteAsync("sp_MinesCashout", parameters, commandType: CommandType.StoredProcedure);

        // достаём все позиции мин чтобы показать игроку при завершении игры
        var allMines = (await connection.QueryAsync<int>("SELECT CellIndex FROM MinesGrid WHERE GameId = @GameId AND IsMine = 1",
            new { GameId = gameId })).ToList();

        return new MinesGameStatusDto
        {
            GameId = gameId, //id иры
            Status = "Won",
            //сумма вина
            WinAmount = parameters.Get<decimal>("@WinAmount"),
            //нью баланс игрока
            NewBalance = parameters.Get<decimal>("@NewBalance"),
            //список всех мин для отображения на фронтен

            //ВОЗВРАЩАЕМ ВСЕ МИНЫ ПОСЛЕ ЗАБранного вина 
            MinePositions = allMines
        };
    }


    //получить текущую неоконеченную игру(при переазугрузке)
    public async Task<MinesGameStatusDto?> GetActiveMinesGameAsync(int userId)
    {
        using var connection = CreateConnection();

        string sql = "SELECT GameId, BetAmount, MinesCount, GridSize, CurrentMultiplier, Status FROM MinesGames WHERE UserId = @UserId AND Status = 'InGame'";
        //  ищем активную игру (статус  InGame) для конкретного пользователя


        //выполняем запрос и получаем первую запись или нуль если гры нема
        var game = await connection.QueryFirstOrDefaultAsync<MinesGameEntity>(sql, new { UserId = userId });

        //если активной игры нет то возвращаем нуль
        if (game == null) return null;

        //получаем список уже открытых клетов чтоб восстановить состояние поля
        var revealed = (await connection.QueryAsync<int>(
            "SELECT CellIndex FROM MinesGrid WHERE GameId = @GameId AND IsRevealed = 1",
        new { GameId = game.GameId })).ToList();

        //получаем текущий баланс пользователей

        decimal balance = await connection.ExecuteScalarAsync<decimal>(
                "SELECT Balance FROM Users WHERE UserId = @UserId",
                new { UserId = userId });

        return new MinesGameStatusDto
        {
            GameId = game.GameId,               // ID игры
            BetAmount = game.BetAmount,         // ставка
            MinesCount = game.MinesCount,       // количество мин
            GridSize = game.GridSize, // размер сетки
            CurrentMultiplier = game.CurrentMultiplier, // текущий множитель
            Status = "InGame",                  // статус игры
            NewBalance = balance,               // баланс игрока
            RevealedCells = revealed            // список открытых клеток
        };
    }


    //вспомонательный метод расчета точного кефа
    private decimal CalculateMinesMultiplier(int minesCount, int revealedCount, int gridSize)
    {
        //кеф возврата игроку  маржа для меня 1%
        decimal houseEdge = 0.99m;
        decimal n = (decimal)gridSize * (decimal)gridSize; // Динамическое число клеток (9, 16, 25, 36...)
        //количество мин
        decimal k = minesCount;
        //вероятность пройти без мины
        decimal p = 1.0m;
        Console.WriteLine($"\n=================== [MINES CALCULATE START] ===================");
        Console.WriteLine($"[MINES INPUT] gridSize: {gridSize}, totalCells (n): {n}, minesCount (k): {k}, revealedCount: {revealedCount}");

        for (int i = 0; i < revealedCount; i++)
        {
            /* формулаа
             вероятсносоь открыть клетку без мины
            (оставшиеся безопасные клетки) / (все оставшиеся клетки)*/

            decimal safeCellsLeft = n - k - (decimal)i;
            decimal totalCellsLeft = n - (decimal)i;


            Console.WriteLine($"[MINES STEP {i}] safeCellsLeft: {safeCellsLeft}, totalCellsLeft: {totalCellsLeft}");

            if (totalCellsLeft <= 0 || safeCellsLeft <= 0)
            {
                Console.WriteLine($"[MINES WARN] safeCellsLeft or totalCellsLeft <= 0. Break loop!");
                break;
            }

            // Вероятность открыть клетку без мины
            decimal stepProbability = safeCellsLeft / totalCellsLeft;
            p *= stepProbability;
            Console.WriteLine($"[MINES STEP {i}] stepProbability: {stepProbability}, total p so far: {p}");
        }


        if (p <= 0)
        {
            Console.WriteLine($"[MINES ERROR] Probability p <= 0, returning 0m");
            Console.WriteLine($"=================== [MINES CALCULATE END] ===================\n");
            return 0m;
        }

        decimal mult = houseEdge / p;
        Console.WriteLine($"[MINES CALC] Raw multiplier (houseEdge / p): {mult}");
        // Ограничиваем максимальный множитель чтобы избежать переполнения Decimal в MSSQL (например, Decimal(18,2))
        decimal maxAllowedMultiplier = 1_000_000m;
        if (mult > maxAllowedMultiplier)
        {
            Console.WriteLine($"[MINES WARN] Multiplier {mult} exceeded max limit! Capped to {maxAllowedMultiplier}");
            mult = maxAllowedMultiplier;
        }

        decimal finalMult = Math.Round(mult, 2);

        Console.WriteLine($"[MINES RESULT] Final Rounded Multiplier: {finalMult}");
        Console.WriteLine($"=================== [MINES CALCULATE END] ===================\n");
        return finalMult;

    }


    //колесо фортуны (режим)
    public async Task<WheelResultDto> WheelSpinAsync(int userId, decimal betAmount, string targetColor)
    {
        //конфигурация сетки колеса 
        var segments = new List<WheelSegmentEntity> {
            new() { Color = "Brown",    Multiplier = 2m,   Weight = 500 },
            new() { Color = "Silver",   Multiplier = 5m,   Weight = 250 },
            new() { Color = "Gold",     Multiplier = 10m,  Weight = 120 },
            new() { Color = "Blue",     Multiplier = 25m,  Weight = 30  },
            new() { Color = "Purple",   Multiplier = 50m,  Weight = 6   },
            new() { Color = "Immortal", Multiplier = 75m,  Weight = 1   },
        };

        //генерация случайного сектора 
        int totalWeight = segments.Sum(s => s.Weight); // сумма всех весов
        var random = new Random();
        int randomValue = random.Next(0, totalWeight);

        WheelSegmentEntity wonSegment = null!;
        int currentSum = 0;
        foreach (var seg in segments)
        {
            currentSum += seg.Weight; //копим вес
            //ели рандом число попало в диапозон этого сегмента
            if (randomValue < currentSum) 
            {
                //выбираем сегмент как выпавший 
                wonSegment = seg;
                break;
            }
        }
        //расчет вина 
        //проверка совпадает ли выбранный юзером цвет с выпавшим
        bool isWin = string.Equals(wonSegment.Color, targetColor , StringComparison.OrdinalIgnoreCase);
        //если совпал   то умножаем если нет то нуль
        decimal multiplier = isWin ? wonSegment.Multiplier : 0m;
        //ставка * на множитель
        decimal winAmount = isWin ? Math.Round(betAmount * multiplier, 2) : 0m;


        //хранимая процедура (обнова баланса и записывает игру в бдл_)
        using var connection = CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);             // айди юзерка 
        parameters.Add("@BetAmount", betAmount);       // ставка
        parameters.Add("@TargetColor", targetColor);   // выбранный цвет
        parameters.Add("@WonColor", wonSegment.Color); // выпавший цвет
        parameters.Add("@Multiplier", multiplier);     // множитель
        parameters.Add("@WinAmount", winAmount);       // сумма выигрыша
        parameters.Add("@NewBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);
        parameters.Add("@GameId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        //процедуоа списываем ставику и начисляем выигры и возвращаем нью ьалоанс
        await connection.ExecuteAsync("sp_WheelSpin", parameters, commandType: CommandType.StoredProcedure);


        //  Возврат результата для фронта
        return new WheelResultDto
        {
            GameId = parameters.Get<int>("@GameId"),          // ID игры
            BetAmount = betAmount,                            // ставка
            TargetColor = targetColor,                        // выбранный цвет
            WonColor = wonSegment.Color,                      // выпавший цвет
            Multiplier = multiplier,                          // множитель
            WinAmount = winAmount,                            // сумма выигрыша
            NewBalance = parameters.Get<decimal>("@NewBalance") // новый баланс игрока
        };
    }


    //кращ (режим)
    public async Task<CrashGameStatusDto> CrashStartGameAsync(int userId, decimal betAmount)
    {
        if (betAmount <= 0)
            throw new Exception("Ставка должна быть больше нуля");

        using var connection = CreateConnection();

        //точка краша генерируется и сохраняется ТОЛЬКО в БД
        decimal crashPoint = GenerateCrashPoint();

        var parameters = new DynamicParameters();    //создаём параметры для процедуры
        parameters.Add("@UserId", userId);           //айд игрока
        parameters.Add("@BetAmount", betAmount);     //ставка
        parameters.Add("@CrashMultiplier", crashPoint); //краша(где будет)
        parameters.Add("@NewBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2); //выходной параметр баланс
        parameters.Add("@GameId", dbType: DbType.Int32, direction: ParameterDirection.Output); //выходной параметр айдт игры

        await connection.ExecuteAsync("sp_CrashStartGame", parameters, commandType: CommandType.StoredProcedure); // вызываем процедуру старта игры
        
        return new CrashGameStatusDto
        {
            GameId = parameters.Get<int>("@GameId"), //получаем айди игры
            BetAmount = betAmount,                   //сохраняем ставку
            CrashPoint = null,                 //точка краша (нуль полтому что клиент не должен знать точку краша во врея игрі)
            Status = "InGame",                       //статус игры
            NewBalance = parameters.Get<decimal>("@NewBalance") //новый баланс
        };
    }
    public async Task<CrashGameStatusDto> CrashCashoutAsync(int userId, int gameId, decimal requestedMultiplier)
    {
        using var connection = CreateConnection();


        //находим игру (ищем игру тока єтого пользователя
        string sql = "SELECT * FROM CrashGames WHERE GameId = @GameId AND UserId = @UserId";
        var game = await connection.QueryFirstOrDefaultAsync<CrashGameEntity>(sql, new { GameId = gameId, UserId = userId });

        //если игры нема или она завершилась
        if(game == null || game.Status != "InGame")
        {
            throw new Exception("Игра не найдена ");
        }

        //бек сам вычисляет время(максимальный х по серверному времени)
        double elapsed = (DateTime.UtcNow - game.CreatedAt).TotalSeconds;
        decimal maxAllowedMultiplier = CalculateMultiplier(elapsed);

        //ошибка если (если клиент отправит 10х а настоящий 2 то отклоняем запрос)_
        if (requestedMultiplier > maxAllowedMultiplier)
        {
            throw new Exception("Недопустимый множитель (попытка сфальсифицировать время)");
        }



       

        var parameters = new DynamicParameters();    //параметры для процедуры
        parameters.Add("@UserId", userId);          
        parameters.Add("@GameId", gameId);          
        parameters.Add("@CashoutMultiplier", requestedMultiplier);
        parameters.Add("@WinAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2); //выходной параметр выигрыш
        parameters.Add("@NewBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2); //выходной параметр баланс

        await connection.ExecuteAsync("sp_CrashCashout", parameters, commandType: CommandType.StoredProcedure);


        decimal winAmount = parameters.Get<decimal>("@WinAmount"); //получаем сумму выигрыша

        return new CrashGameStatusDto
        {
            GameId = gameId,                         //игры
            Status = winAmount > 0 ? "Won" : "Crashed", //статус зависит от выигрыша
            WinAmount = winAmount,                   // сумма выигрыша
            CurrentMultiplier = requestedMultiplier,    //множитель на котором забрал
            NewBalance = parameters.Get<decimal>("@NewBalance") //новый баланс
        };

    }
    public async Task<CrashGameStatusDto?> GetActiveCrashGameAsync(int userId)
    {
        using var connection = CreateConnection();

        //sql для В игре (ПОЛУЧАЕМ игру пользователя)
        string sql = "SELECT GameId, BetAmount, CrashMultiplier, Status, CreatedAt " +
            "FROM CrashGames " +
            "WHERE UserId = @UserId AND Status = 'InGame'";

        //получитьигру
        var game = await connection.QueryFirstOrDefaultAsync<CrashGameEntity>(sql, new { UserId = userId });

        //если игры нема то нуль
        if(game == null) return null;

        //восстановление игры после перезагрузки
        //бек сам определяет скока времени пророшло 
        double elapsed = (DateTime.UtcNow - game.CreatedAt).TotalSeconds;
        if (elapsed < 0) elapsed = 0;//безопачный расшед прошедшего времени

        //защита от переполнения  elapsed (баг фикс чтоб не улетел в бесконечность)
        decimal currentMultiplier = CalculateMultiplier(elapsed);

        //проверка не должна ли крашнуться игра
        //если по времени она должна была уже крашнуться то завершаем ее
        if (currentMultiplier >= game.CrashMultiplier)
        {
            await CrashSetCrashedAsync(game.GameId);
            return null; // игры больше нема
        }


        decimal balance = await connection.ExecuteScalarAsync<decimal>(
                "SELECT Balance FROM Users WHERE UserId = @UserId",
                new { UserId = userId }); //получаем баланс юзерка

        return new CrashGameStatusDto
        {
            GameId = game.GameId,                   //айди игры
            BetAmount = game.BetAmount,             //ставка
            CrashPoint = game.CrashMultiplier,      //точка краша(скрываем)
            CurrentMultiplier = currentMultiplier, //множитель
            Status = "InGame",                      //статус игры
            NewBalance = balance                    //новый баланс
        };

    }

    //принудительно крашит игру(когда цикл на беке доходит до точки краша)
    public async Task CrashSetCrashedAsync(int gameId)
    {
        using var connection = CreateConnection();

        // Вызываем хранимую процедуру sp_CrashSetCrashed
        await connection.ExecuteAsync(
            "sp_CrashSetCrashed",
            new { GameId = gameId },
            commandType: CommandType.StoredProcedure
        );
    }



    //точка краша  
    private decimal GenerateCrashPoint()
    {
        var randomBytes = new byte[4];//массив байтов случайнго числа
        
        using(var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes); //заполнялка случайными байтами  
        }
        //переобразовка в числа
        uint randInt = BitConverter.ToUInt32(randomBytes, 0);




        
        double e = Math.Pow(2, 32); //макс знач
        double h = randInt; //рандом число

        //вероятность краша на 1x  буде 3%
        //если делится на 33 то моментально кравщ на 1х
        if (h % 33 == 0) return 1.00m;




        /* формула краш поинта
         Math.Floor() / 100/0 убирает дробную часть
         (e - h) это оставшийся запас в диапозоне
        когда h мелкое (близщко к 0) e-h почти равно e (запас большой)
        когда h блищкое к  e (редко) e - h строемится к нулю (запаса нема)

        (99.0 * e) / (e - h) 
        чем больше число h тем выже улетаеть будет x 

         */
        double result = Math.Floor((99.0 * e) / (e - h)) / 100.00;

        //гарант шо краш не буде меньше 1 
        decimal crash = (decimal)Math.Max(1.00, result);

        //ограничение для максимального краша
        return Math.Min(crash, 10000.00m);

    }


    //формула 
    private decimal CalculateMultiplier(double elapsedSeconds)
    {
        // Если прошло больше 200 секунд, ограничиваем во избежание OverflowException
        if (elapsedSeconds > 150) elapsedSeconds = 150;

        double powResult = Math.Pow(1.08, elapsedSeconds * 2.5);

        // Защита от NaN / Infinity
        if (double.IsNaN(powResult) || double.IsInfinity(powResult) || powResult > 10000.0)
        {
            return 10000.00m;
        }

        return (decimal)Math.Round(powResult, 2);
    }


    //ресет пароля 
    public async Task<bool> ResetPasswordAsync(string email, string code, string newPasswordHash)
    {
        //подключение к бд 
        using var connection = CreateConnection();

        //создаем парамтр для хранимой процедуры
        var parameters = new DynamicParameters();

        //почта
        parameters.Add("@Email", email);

        //код 
        parameters.Add("@ConfirmationCode", code);

        //зашешированный ньб пароль
        parameters.Add("@NewPasswordHash", newPasswordHash);

        //вызов хранимой процедуры
        await connection.ExecuteAsync("sp_ResetPassword", 
            parameters,
            commandType: CommandType.StoredProcedure
        );
        return true;
    }
}

