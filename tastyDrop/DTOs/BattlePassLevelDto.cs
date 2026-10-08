namespace tastyDrop.Api.DTOs;

//батл пасс для отдачи списка лвлов и прогресса
public class BattlePassLevelDto
{
    public int LevelId { get; set; }
    public int LevelNumber { get; set; }

    public string QuestName { get; set; } = string.Empty;
    public string GoalType { get; set; } = string.Empty;
    public int GoalValue { get; set; }


    //UserBattlePassQuests прогресс (если 0 то не дошли до лвлав)
    public int CurrentProgress { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsRewardClaimed { get; set; }

    //инфа о награде за лвл
    public string? RewardText { get; set; }
    public int? RewardCurrency { get; set; }
    public int? RewardCaseId { get; set; }
}
