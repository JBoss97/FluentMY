using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FluentMY.ViewModels
{
    public class LessonSubmissionViewModel
    {
        public int LessonId { get; set; }
        public string LessonTitle { get; set; }
        public string CourseTitle { get; set; }

        public List<LessonSubmissionQuestionViewModel> Questions { get; set; } = new List<LessonSubmissionQuestionViewModel>();
    }

    public class LessonSubmissionQuestionViewModel
    {
        public int ExerciseId { get; set; }

        public string ExerciseType { get; set; }
        public string PracticeArea { get; set; }

        public string QuestionText { get; set; }
        public string PromptText { get; set; }

        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }

        public string MatchingPairsJson { get; set; }
        public string SentenceTokensJson { get; set; }
        public string AudioPromptPath { get; set; }

        public string ReferenceText { get; set; }
        public string SyllableGuide { get; set; }
        public string PronunciationHint { get; set; }

        [Display(Name = "Selected Answer")]
        public string SelectedAnswer { get; set; }

        public List<string> SelectedAnswers { get; set; } = new List<string>();

        public string SelectedAnswerJson { get; set; }
    }
}