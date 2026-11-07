using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class StudentLearningHistoryPageViewModel
    {
        public List<StudentLearningHistoryItemViewModel> Items { get; set; }
            = new List<StudentLearningHistoryItemViewModel>();

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public string SelectedType { get; set; }

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