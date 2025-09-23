using Verse;

namespace ProgressionEducation
{
    public static class EducationLog
    {
        private const string Prefix = "[Progression-Education] ";

        public static void Message(string text)
        {
            if (EducationSettings.Instance.debugMode)
            {
                Log.Message(Prefix + text);
                Log.ResetMessageCount();
            }
        }

        public static void Warning(string text)
        {
            if (EducationSettings.Instance.debugMode)
            {
                Log.Warning(Prefix + text);
            }
        }

        public static void Error(string text)
        {
            Log.Error(Prefix + text);
        }
    }
}
