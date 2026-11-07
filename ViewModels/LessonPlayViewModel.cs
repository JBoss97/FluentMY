using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class LessonPlayViewModel
    {
        public int LessonId { get; set; }
        public string LessonTitle { get; set; }
        public string CourseTitle { get; set; }

        public int SessionId { get; set; }

        public int CurrentIndex { get; set; }
        public int TotalQuestions { get; set; }
        public int HeartsRemaining { get; set; }
        public int CurrentStreak { get; set; }
        public int EarnedXP { get; set; }

        public ExercisePlayItemViewModel CurrentExercise { get; set; }
    }
}