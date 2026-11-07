using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class LessonExerciseViewModel
    {
        public int LessonId { get; set; }
        public string LessonTitle { get; set; }
        public string CourseTitle { get; set; }

        public List<ExerciseItemViewModel> Exercises { get; set; } = new List<ExerciseItemViewModel>();
    }

    public class ExerciseItemViewModel
    {
        public int ExerciseId { get; set; }
        public string QuestionText { get; set; }
        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }

        public string SelectedAnswer { get; set; }
    }
}