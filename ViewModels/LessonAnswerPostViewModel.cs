using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FluentMY.ViewModels
{
    public class LessonAnswerPostViewModel
    {
        [Required]
        public int SessionId { get; set; }

        [Required]
        public int ExerciseId { get; set; }

        public string Answer { get; set; }
        public string AnswerJson { get; set; }

        public List<string> SelectedAnswers { get; set; } = new List<string>();
    }
}