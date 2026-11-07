using FluentMY.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace FluentMY.Services
{
    public class SpeechAssessmentService
    {
        public Task<SpeakingResultViewModel> AssessAsync(string recognizedText, string referenceText)
        {
            recognizedText = recognizedText ?? string.Empty;
            referenceText = referenceText ?? string.Empty;

            var expectedWords = Tokenize(referenceText);
            var actualWords = Tokenize(recognizedText);

            var usedIndexes = new HashSet<int>();
            var missingWords = new List<string>();
            var extraWords = new List<string>();

            int matchedCount = 0;

            for (int i = 0; i < expectedWords.Count; i++)
            {
                bool found = false;

                for (int j = 0; j < actualWords.Count; j++)
                {
                    if (!usedIndexes.Contains(j) && expectedWords[i] == actualWords[j])
                    {
                        usedIndexes.Add(j);
                        matchedCount++;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    missingWords.Add(expectedWords[i]);
                }
            }

            for (int j = 0; j < actualWords.Count; j++)
            {
                if (!usedIndexes.Contains(j))
                {
                    extraWords.Add(actualWords[j]);
                }
            }

            decimal completenessScore = expectedWords.Count == 0
                ? 0
                : Math.Round((decimal)matchedCount / expectedWords.Count * 100m, 2);

            decimal accuracyScore = Math.Max(expectedWords.Count, actualWords.Count) == 0
                ? 0
                : Math.Round((decimal)matchedCount / Math.Max(expectedWords.Count, actualWords.Count) * 100m, 2);

            decimal pronunciationScore = accuracyScore;

            string status;
            if (accuracyScore >= 90)
                status = "Excellent";
            else if (accuracyScore >= 70)
                status = "Good";
            else if (accuracyScore >= 50)
                status = "Fair";
            else
                status = "Needs improvement";

            var serializer = new JavaScriptSerializer();
            var feedbackJson = serializer.Serialize(new
            {
                matchedCount = matchedCount,
                totalExpected = expectedWords.Count,
                totalActual = actualWords.Count,
                missingWords = missingWords,
                extraWords = extraWords,
                status = status
            });

            var result = new SpeakingResultViewModel
            {
                RecognizedText = recognizedText,
                ReferenceText = referenceText,
                AccuracyScore = accuracyScore,
                FluencyScore = null,
                CompletenessScore = completenessScore,
                PronunciationScore = pronunciationScore,
                FeedbackJson = feedbackJson
            };

            return Task.FromResult(result);
        }

        private static List<string> Tokenize(string text)
        {
            return NormalizeText(text)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }

        private static string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var cleaned = new string(
                text.ToLowerInvariant()
                    .Where(c => !char.IsPunctuation(c))
                    .ToArray()
            );

            return string.Join(" ",
                cleaned.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}