namespace SICore.Helpers;

public static class DifficultyHelper
{
    public static double GetDifficulty(int questionIndex, int questionCount)
    {
        if (questionCount <= 1)
        {
            return 0.5;
        }

        var difficultyStep = 1.0 / (questionCount - 1);
        return questionIndex * difficultyStep;
    }
}
