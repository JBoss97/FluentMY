using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class EducatorExerciseEditViewModel
    {
        public int ExerciseId { get; set; }

        [Required]
        public int LessonId { get; set; }

        [Required]
        [Display(Name = "Practice Area")]
        public string PracticeArea { get; set; }

        [Required]
        [Display(Name = "Exercise Type")]
        public string ExerciseType { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Prompt / Question")]
        public string PromptText { get; set; }

        [StringLength(255)]
        [Display(Name = "Option A")]
        public string OptionA { get; set; }

        [StringLength(255)]
        [Display(Name = "Option B")]
        public string OptionB { get; set; }

        [StringLength(255)]
        [Display(Name = "Option C")]
        public string OptionC { get; set; }

        [StringLength(255)]
        [Display(Name = "Option D")]
        public string OptionD { get; set; }

        [StringLength(500)]
        [Display(Name = "Correct Answer")]
        public string CorrectAnswer { get; set; }

        [Display(Name = "Matching Pairs JSON")]
        public string MatchingPairsJson { get; set; }

        [Display(Name = "Sentence Tokens JSON")]
        public string SentenceTokensJson { get; set; }

        [StringLength(255)]
        [Display(Name = "Audio Prompt Path")]
        public string AudioPromptPath { get; set; }

        [Display(Name = "Upload Audio File")]
        public HttpPostedFileBase AudioPromptFile { get; set; }
        public string ExistingAudioPromptPath { get; set; }

        [Display(Name = "Reference Text")]
        public string ReferenceText { get; set; }

        [Display(Name = "Syllable Guide")]
        public string SyllableGuide { get; set; }

        [Display(Name = "Pronunciation Hint")]
        public string PronunciationHint { get; set; }

        [Display(Name = "Marks")]
        public int Marks { get; set; } = 1;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "XP Reward")]
        public int XPReward { get; set; } = 10;

        [Display(Name = "Allow Retry")]
        public bool AllowRetry { get; set; } = true;
    }
}