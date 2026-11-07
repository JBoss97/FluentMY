using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class LessonFinishViewModel
    {
        public int LessonId { get; set; }
        public int CourseId { get; set; }
        public string LessonTitle { get; set; }
        public string CourseTitle { get; set; }

        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int TotalXP { get; set; }
        public int StarsEarned { get; set; }
        public bool PerfectRun { get; set; }

        public int? NextLessonId { get; set; }
    }
}