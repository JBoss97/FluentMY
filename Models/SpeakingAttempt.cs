using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FluentMY.Models
{
    public class SpeakingAttempt
    {
        public int SpeakingAttemptId { get; set; }

        public int ExerciseId { get; set; }
        [ForeignKey("ExerciseId")]
        public virtual Exercise Exercise { get; set; }

        public int LessonId { get; set; }
        [ForeignKey("LessonId")]
        public virtual Lesson Lesson { get; set; }

        [StringLength(128)]
        public string StudentId { get; set; }
        [ForeignKey("StudentId")]
        public virtual ApplicationUser Student { get; set; }

        public int AttemptNumber { get; set; }

        [StringLength(255)]
        public string AudioFilePath { get; set; }

        public string RecognizedText { get; set; }
        public string ReferenceText { get; set; }

        public decimal? AccuracyScore { get; set; }
        public decimal? FluencyScore { get; set; }
        public decimal? CompletenessScore { get; set; }
        public decimal? PronunciationScore { get; set; }

        public string FeedbackJson { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}