using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using tastyDrop.Api.Services;

namespace tastyDrop.Api.Hubs;


[Authorize]
public class CrashHub : Hub
{
    private readonly IDbService _dbService;

    private readonly IHubContext<CrashHub> _hubContext;

    //хранилище таймеров и сотояния игры 
    private static readonly ConcurrentDictionary<int,
        CancellationTokenSource> _gameLoops = new();
    //слоарарь ключ connectionId клиента
    //знрачение токен отмены для игрового цикла


    public CrashHub(IDbService dbService, IHubContext<CrashHub> hubContext)
    {
        _dbService = dbService;
        _hubContext = hubContext;

    }



    public override async Task OnConnectedAsync()
    {
        //получаем юзерайди из jwt 
        int userId = GetUserId();
        if (userId > 0)
        {
            //подключаем юзера к персональной группе
            /*типо UserId = 15 
             если перезагружаем страницу старое соеденение как и было UserId = 15 
            так и новое UserId = 15*/
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
        await base.OnConnectedAsync();
    }

    public async Task StartGame(decimal betAmount)
    {

        //получаем юзерайди из jwt 
        int userId = GetUserId();
        if (userId <= 0)
        {
            await Clients.Caller.SendAsync("OnError", "Неверный токен авторизации");
            return;
        }
        try
        {
            /*тут создание игры в базе
             проверка баланса
            спимсание днег
            генерим CrashMultiplier
            создаем запись в CrashGames
            получаем GameId */


            //старт игры в бд и получаение точки краша
            var initialStatus = await _dbService.CrashStartGameAsync(userId, betAmount);

            //чтоб получить нзачение точки для краса сервера считываем ее через GetActive
            //получаем созданную игру
            var activeGame = await _dbService.GetActiveCrashGameAsync(userId);
            if(activeGame == null || !activeGame.CrashPoint.HasValue)
            {
                //если игра не найдена иои нема крашпоинта то ошибка
                await Clients.Caller.SendAsync("OnError", "Не удалось запустить игру.");
                return;

            }

            decimal targetCrashPoint = activeGame.CrashPoint.Value; //точка краа
            int gameId = initialStatus.GameId; //id game


            //уведомлялка он старте игры 
            
            await Clients.Group($"User_{userId}").SendAsync("OnGameStarted", new
            {
                gameId = gameId,
                betAmount = betAmount,
                newBalance = initialStatus.NewBalance
            });
            // отправляем клиенту событие "OnGameStarted" с данными




            //канселим предыдущий цикл если он был (oldCancellationTokenSource )
            //короч это защита шоб не работало несколько игр(циклов) одновременно
            if (_gameLoops.TryRemove(userId, out var oldCts))
            {
                oldCts.Cancel(); //отмена старого игрового цикла
            }
            //создаем CancellationTokenSource для нью игры
            var cts = new CancellationTokenSource(); 

            //сохраняем цикл по userid
            _gameLoops[userId] = cts; 

            //фоновый цикл расчета множителя (запущется в отедльном потоке)
            _ = Task.Run(() => RunGameLoopAsync(_hubContext, _dbService,userId, gameId, 
                targetCrashPoint, cts.Token));
        }
        //любая ошибка сюды
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("OnError", ex.Message);
        }
    }

    private async Task RunGameLoopAsync(
        IHubContext<CrashHub> hubContext,
        IDbService dbService,
        int userId, int gameId, 
        decimal targetCrashPoint, CancellationToken token)
    {

        var startTime = DateTime.UtcNow; //фикс врем ястарта 

        try
        {
            //пока не отменили цикл
            while (!token.IsCancellationRequested)
            {
                //считаем скок сек прошло с начала игры
                double elapsed = (DateTime.UtcNow - startTime).TotalSeconds;

                // Формула роста множителя(такая же как и на фронте)
                //беек сам рассчитывает текущи множитель
                double pow = Math.Pow(1.08, elapsed * 2.5);
                if (pow > 10000.0) pow = 10000.0;

                decimal currentMultiplier = (decimal)Math.Round(pow, 2);


                //проверка дошел ли краш
                if (currentMultiplier >= targetCrashPoint)
                {

                    //автоматически помечаем краш в бд
                    //до отправки инвента клиенту
                    await _dbService.CrashSetCrashedAsync(gameId);

                    //точка краша(Уведомляем клиента только после краша)
                    await hubContext.Clients.Group($"User_{userId}").SendAsync("OnCrash", new
                    {
                        gameId = gameId,
                        crashPoint = targetCrashPoint

                    }, CancellationToken.None); //отправка none при отмене

                    //удаляем игровой цикл пользователя
                    _gameLoops.TryRemove(userId, out _);
                    break; //выход з  цикла
                }

                //отправка текущео Х кадлые 50 мс для анимации
                // отправляем клиенту событие OnTick с текущим множителем
                await hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("OnTick", currentMultiplier);

                await Task.Delay(50, token); //жедм 50мс
            }
        }
        catch (TaskCanceledException) { } // если отменили задачу  ничего не делаем
        catch (Exception)
        {
            _gameLoops.TryRemove(userId, out _); // удаляем цикл при ошибке
        }
    }

    private int GetUserId()
    {
        //достаем userId из токена 
        var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? Context.User?.FindFirst("sub")?.Value;
        return int.TryParse(claim, out int id) ? id : 0;

    }
}
