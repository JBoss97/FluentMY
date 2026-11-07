using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class LessonSpeakingExerciseItemViewModel
    {
        public int ExerciseId { get; set; }
        public string PromptText { get; set; }
        public string ReferenceText { get; set; }
        public string SyllableGuide { get; set; }
        public string PronunciationHint { get; set; }
        public string AudioPromptPath { get; set; }
    }
}