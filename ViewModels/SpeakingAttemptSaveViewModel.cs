using System.ComponentModel.DataAnnotations;

namespace FluentMY.ViewModels
{
    public class SpeakingAttemptSaveViewModel
    {
        [Required]
        public int ExerciseId { get; set; }

        [Required]
        public int LessonId { get; set; }

        [Required]
        [StringLength(4000)]
        public string RecognizedText { get; set; }

        [StringLength(4000)]
        public string ReferenceText { get; set; }
    }
}