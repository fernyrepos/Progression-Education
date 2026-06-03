using Verse;

namespace ProgressionEducation;

public static class EducationLog
{
    private const string Prefix = "[Progression-Education] ";

    public static void Error(string text)
    {
        Log.Error(Prefix + text);
    }

    public static void Message(string text)
    {
        if (EducationMod.settings.debugMode)
        {
            Log.Message(Prefix + text);
            Log.ResetMessageCount();
        }
    }

    public static void Warning(string text)
    {
        if (EducationMod.settings.debugMode)
        {
            Log.Warning(Prefix + text);
        }
    }
}
