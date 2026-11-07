using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FluentMY.Models
{
    public class Exercise
    {
        public int ExerciseId { get; set; }

        [Required]
        public int LessonId { get; set; }
        public virtual Lesson Lesson { get; set; }

        [StringLength(500)]
        [Display(Name = "Question")]
        public string QuestionText { get; set; }

        [StringLength(200)]
        [Display(Name = "Option A")]
        public string OptionA { get; set; }

        [StringLength(200)]
        [Display(Name = "Option B")]
        public string OptionB { get; set; }

        [StringLength(200)]
        [Display(Name = "Option C")]
        public string OptionC { get; set; }

        [StringLength(200)]
        [Display(Name = "Option D")]
        public string OptionD { get; set; }

        [StringLength(500)]
        [Display(Name = "Correct Answer")]
        public string CorrectAnswer { get; set; }

        [Display(Name = "Marks")]
        public int Marks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [StringLength(30)]
        public string PracticeArea { get; set; }      // Vocabulary, Grammar, Listening, Speaking

        [StringLength(30)]
        public string ExerciseType { get; set; }      // MultipleChoice, FillBlank, Matching, Reorder, ListeningChoice, Speaking

        public string PromptText { get; set; }

        public string MatchingPairsJson { get; set; }
        public string SentenceTokensJson { get; set; }

        [StringLength(255)]
        public string AudioPromptPath { get; set; }

        public string ReferenceText { get; set; }
        public string SyllableGuide { get; set; }
        public string PronunciationHint { get; set; }

        public int DisplayOrder { get; set; }
        public int XPReward { get; set; }
        public bool AllowRetry { get; set; }

        [StringLength(200)]
        public string SeedKey { get; set; }
    }
}