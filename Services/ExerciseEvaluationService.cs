using FluentMY.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;

namespace FluentMY.Services
{
    public class ExerciseEvaluationResult
    {
        public bool IsCorrect { get; set; }
        public string CorrectAnswer { get; set; }
        public string FeedbackText { get; set; }
        public int XPGranted { get; set; }
    }

    public class ExerciseEvaluationService
    {
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public ExerciseEvaluationResult Evaluate(
            Exercise exercise,
            string answer,
            string answerJson,
            IEnumerable<string> selectedAnswers = null)
        {
            var type = NormalizeKey(exercise.ExerciseType);
            if (string.IsNullOrWhiteSpace(type))
                type = "multiplechoice";

            switch (type)
            {
                case "multiplechoice":
                case "listeningchoice":
                    return EvaluateMultipleChoice(exercise, answer);

                case "multipleselect":
                    return EvaluateMultipleSelect(exercise, selectedAnswers);

                case "fillblank":
                    return EvaluateFillBlank(exercise, answer);

                case "matching":
                    return EvaluateMatching(exercise, answerJson);

                case "reorder":
                    return EvaluateReorder(exercise, answerJson);

                default:
                    return EvaluateMultipleChoice(exercise, answer);
            }
        }

        private ExerciseEvaluationResult EvaluateMultipleChoice(Exercise exercise, string answer)
        {
            var submitted = (answer ?? "").Trim().ToUpperInvariant();
            var expected = (exercise.CorrectAnswer ?? "").Trim().ToUpperInvariant();
            var correct = submitted == expected;

            return new ExerciseEvaluationResult
            {
                IsCorrect = correct,
                CorrectAnswer = expected,
                FeedbackText = correct ? "Correct!" : "Not quite.",
                XPGranted = correct ? (exercise.XPReward > 0 ? exercise.XPReward : 10) : 0
            };
        }

        private ExerciseEvaluationResult EvaluateMultipleSelect(Exercise exercise, IEnumerable<string> selectedAnswers)
        {
            var submitted = NormalizeMultiAnswer(selectedAnswers);
            var expected = NormalizeMultiAnswer((exercise.CorrectAnswer ?? "").Split(','));
            var correct = submitted == expected;

            return new ExerciseEvaluationResult
            {
                IsCorrect = correct,
                CorrectAnswer = expected,
                FeedbackText = correct ? "Correct combination!" : "Not quite. Check the correct combination.",
                XPGranted = correct ? (exercise.XPReward > 0 ? exercise.XPReward : 12) : 0
            };
        }

        private ExerciseEvaluationResult EvaluateFillBlank(Exercise exercise, string answer)
        {
            var submitted = NormalizeAnswer(answer);
            var expected = NormalizeAnswer(exercise.CorrectAnswer);
            var correct = submitted == expected;

            return new ExerciseEvaluationResult
            {
                IsCorrect = correct,
                CorrectAnswer = exercise.CorrectAnswer,
                FeedbackText = correct ? "Correct!" : "Not quite.",
                XPGranted = correct ? (exercise.XPReward > 0 ? exercise.XPReward : 10) : 0
            };
        }

        private ExerciseEvaluationResult EvaluateMatching(Exercise exercise, string answerJson)
        {
            bool isCorrect = false;

            try
            {
                var submitted = serializer.Deserialize<Dictionary<string, string>>(answerJson ?? "{}");
                var expected = serializer.Deserialize<Dictionary<string, string>>(exercise.MatchingPairsJson ?? "{}");

                isCorrect = submitted.Count == expected.Count &&
                            submitted.All(x => expected.ContainsKey(x.Key) &&
                                               NormalizeAnswer(expected[x.Key]) == NormalizeAnswer(x.Value));
            }
            catch
            {
                isCorrect = false;
            }

            return new ExerciseEvaluationResult
            {
                IsCorrect = isCorrect,
                CorrectAnswer = exercise.MatchingPairsJson,
                FeedbackText = isCorrect ? "Great matching!" : "Some matches are incorrect.",
                XPGranted = isCorrect ? (exercise.XPReward > 0 ? exercise.XPReward : 12) : 0
            };
        }

        private ExerciseEvaluationResult EvaluateReorder(Exercise exercise, string answerJson)
        {
            bool isCorrect = false;
            string correctAnswer = "";

            try
            {
                var submitted = serializer.Deserialize<List<string>>(answerJson ?? "[]");
                var expected = serializer.Deserialize<List<string>>(exercise.SentenceTokensJson ?? "[]");

                correctAnswer = string.Join(" ", expected);

                isCorrect = submitted.Count == expected.Count &&
                            !submitted.Where((token, index) => NormalizeAnswer(token) != NormalizeAnswer(expected[index])).Any();
            }
            catch
            {
                isCorrect = false;
            }

            return new ExerciseEvaluationResult
            {
                IsCorrect = isCorrect,
                CorrectAnswer = correctAnswer,
                FeedbackText = isCorrect ? "Sentence order is correct!" : "The order is not correct yet.",
                XPGranted = isCorrect ? (exercise.XPReward > 0 ? exercise.XPReward : 12) : 0
            };
        }

        private string NormalizeMultiAnswer(IEnumerable<string> values)
        {
            if (values == null)
                return "";

            return string.Join(",",
                values
                    .Select(x => (x ?? "").Trim().ToUpperInvariant())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .OrderBy(x => x));
        }

        private string NormalizeAnswer(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return string.Join(" ",
                value.Trim()
                     .ToLowerInvariant()
                     .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private string NormalizeKey(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? ""
                : value.Trim().ToLowerInvariant();
        }
    }
}