using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class LessonDetailsViewModel
    {
        public int LessonId { get; set; }
        public int CourseId { get; set; }

        public string LessonTitle { get; set; }
        public string CourseTitle { get; set; }
        public string DifficultyLevel { get; set; }
        public string Content { get; set; }

        public bool CanStartQuiz { get; set; }
        public bool IsStudent { get; set; }
        public bool IsEnrolled { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsEducatorPreview { get; set; }
        public int? NextLessonId { get; set; }

        public List<LessonSpeakingExerciseItemViewModel> SpeakingExercises { get; set; } = new List<LessonSpeakingExerciseItemViewModel>();
        public bool HasSpeakingExercises => SpeakingExercises != null && SpeakingExercises.Count > 0;
    }
}