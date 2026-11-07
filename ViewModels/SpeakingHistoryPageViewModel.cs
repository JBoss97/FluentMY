using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class SpeakingHistoryPageViewModel
    {
        public List<SpeakingHistoryItemViewModel> Items { get; set; }
           = new List<SpeakingHistoryItemViewModel>();

        public List<System.Web.Mvc.SelectListItem> LessonOptions { get; set; }
            = new List<System.Web.Mvc.SelectListItem>();

        public int? SelectedLessonId { get; set; }

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public int TotalAttempts { get; set; }
        public int AvgAccuracy { get; set; }
        public int AvgFluency { get; set; }
        public int AvgCompleteness { get; set; }
        public int AvgPronunciation { get; set; }
        public int AvgOverall { get; set; }

        public bool HasPreviousPage
        {
            get { return CurrentPage > 1; }
        }

        public bool HasNextPage
        {
            get { return CurrentPage < TotalPages; }
        }

        public int StartItemNumber
        {
            get
            {
                if (TotalItems == 0) return 0;
                return ((CurrentPage - 1) * PageSize) + 1;
            }
        }

        public int EndItemNumber
        {
            get
            {
                var end = CurrentPage * PageSize;
                return end > TotalItems ? TotalItems : end;
            }
        }
    }
}