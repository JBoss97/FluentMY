using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentMY.Models;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace FluentMY.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminEnquiriesController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public async Task<ActionResult> Index()
        {
            var items = await db.ContactEnquiries
                .OrderBy(x => x.IsRead)
                .ThenByDescending(x => x.SubmittedAtUtc)
                .ToListAsync();

            return View(items);
        }

        public async Task<ActionResult> Details(int id)
        {
            var item = await db.ContactEnquiries.FirstOrDefaultAsync(x => x.ContactEnquiryId == id);
            if (item == null)
                return HttpNotFound();

            if (!item.IsRead)
            {
                item.IsRead = true;
                await db.SaveChangesAsync();
            }

            return View(item);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}