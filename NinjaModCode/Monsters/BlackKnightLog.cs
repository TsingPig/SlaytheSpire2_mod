namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 黑骑士模块的集中日志封装。受 <see cref="BlackKnightConfig.VerboseLogging"/> 控制，
/// 统一加上 <c>[BlackKnight]</c> 前缀，方便在游戏日志中确认状态机、伤害类型与卡牌触发。
/// </summary>
internal static class BlackKnightLog
{
    private const string Prefix = "[BlackKnight] ";

    public static void Info(string message)
    {
        if (!BlackKnightConfig.VerboseLogging) return;
        MainFile.Logger.Info(Prefix + message);
    }

    /// <summary>无视 VerboseLogging 的重要提示（如调试遭遇替换）。</summary>
    public static void Important(string message)
    {
        MainFile.Logger.Info(message);
    }

    public static void Warn(string message)
    {
        MainFile.Logger.Warn(Prefix + message);
    }
}
