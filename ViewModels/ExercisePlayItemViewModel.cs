using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class ExercisePlayItemViewModel
    {
        public int ExerciseId { get; set; }
        public string PracticeArea { get; set; }
        public string ExerciseType { get; set; }
        public string PromptText { get; set; }

        public List<ExerciseOptionViewModel> Options { get; set; } = new List<ExerciseOptionViewModel>();

        public string MatchingPairsJson { get; set; }
        public string SentenceTokensJson { get; set; }
        public string AudioPromptPath { get; set; }

        public string ReferenceText { get; set; }
        public string SyllableGuide { get; set; }
        public string PronunciationHint { get; set; }
    }
}