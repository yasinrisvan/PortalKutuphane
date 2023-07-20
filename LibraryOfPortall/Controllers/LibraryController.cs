using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LibraryOfPortall.Models.EntityFramework;

namespace LibraryOfPortall.Controllers
{
    public class LibraryController : Controller
    {
        TYHKUTUPHANEEntities db = new TYHKUTUPHANEEntities();

        public void Complete()
        {
            List<TblRegion> regions = new List<TblRegion>();
            regions = db.TblRegions.ToList();
            ViewBag.Regions = regions.ToString();
        }
        // GET: Library
        public ActionResult Index()
        {

            Complete();
            List<TblBook> books = new List<TblBook>();
            TblUser user = (TblUser)Session["user"];

            books = user is null ? db.TblBooks.ToList() : db.TblBooks.Where(b => b.Status != false && b.Status != true).ToList();

            return View(books);
        }

        [Authorize]
        public ActionResult Request(int id)
        {
            TblUser user = (TblUser)Session["user"];
            var book = db.TblBooks.Find(id);
            if (book is null)
            {
                TempData["Message"] = "Kitap Bulunamadı";
            }
            else if (book.Status == null)
            {
                TblRequest newReq = new TblRequest
                {
                    BookID = id,
                    UserID = user.ID,
                    ApplyDate = DateTime.Now,
                    Status = "Onay Bekleniyor"
                };
                book.Status = false;
                db.TblRequests.Add(newReq);
                db.SaveChanges();
                TempData["MessageSuccess"] = "Talebiniz Alındı Onay Bekleniyor";
            }
            else
            {
                TempData["Message"] = "Kitap Rezervli";
            }
            return RedirectToAction("Index");
        }

        [Authorize]
        public ActionResult GetAllRequest()
        {
            var requests = db.TblRequests.Where(r => r.Status == "Onay Bekleniyor").ToList();
            return View(requests);
        }

        [Authorize]
        public ActionResult Accept(int id)
        {
            var requests = db.TblRequests.Where(r => r.ID == id).FirstOrDefault();
            if (requests is null)
            {
                TempData["Message"] = "Onaylanmadı. Tekrar Deneyin";
                return RedirectToAction("Index");
            }
            else
            {
                var bookId = requests.BookID;
                var book = db.TblBooks.Where(b => b.ID == bookId).FirstOrDefault();
                book.Status = true; // Rezerve Edildi
                requests.EndDate = DateTime.Now;  //Kabul Tarihi
                requests.Status = "Onaylandı"; //İstek Tablosunda onaylandı

                TblReserved newRes = new TblReserved //Onay tablosuna ekleme
                {
                    BookID = bookId,
                    UserID = requests.UserID,
                    AcceptDate = DateTime.Now
                };

                db.TblReserveds.Add(newRes);

                db.SaveChanges();
                TempData["MessageSuccess"] = "Onaylandı";
            }
            return RedirectToAction("GetAllRequest");

        }
        public ActionResult Decline(int id)
        {
            var request = db.TblRequests.Where(r => r.ID != id).FirstOrDefault();
            if (request is null)
            {
                TempData["Message"] = "Islem Gerceklestirilemedi. Tekrar Deneyin";
                return RedirectToAction("Index");
            }
            else
            {
                var bookId = request.BookID;
                var book = db.TblBooks.Where(b => b.ID == bookId).FirstOrDefault();
                book.Status= null;
                request.EndDate = DateTime.Now;
                request.Status = "Reddedildi";
            }
            db.SaveChanges();
            TempData["MessageSuccess"] = "Reddetme Basarılı";
            return RedirectToAction("GetAllRequest");
        }


        [Authorize]
        public ActionResult GetRequest()
        {
            TblUser user = (TblUser)Session["user"];
            var requests = db.TblRequests.Where(r => r.UserID == user.ID).ToList();
            return View(requests);
        }
        /// <summary>
        /// Kişiye rezerve edilmiş tüm kitaplar burada gözükür
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult GetReserve()
        {
            TblUser user = (TblUser)Session["user"];
            var reserves = db.TblReserveds.Where(r => r.UserID == user.ID).ToList();

            return View(reserves);
        }

        public ActionResult CancelRequest(int id)
        {
            var request = db.TblRequests.Where(r => r.ID == id).FirstOrDefault();
            if(request is null) TempData["Message"] = "İşlem Başarısız. Tekrar Deneyin";
            else
            {
                var book = db.TblBooks.Where(b => b.ID == request.BookID).FirstOrDefault();
                book.Status = null;
                request.Status = "İptal Edildi";
                db.SaveChanges();
                TempData["MessageSuccess"] = "İptal Edildi";

            }
            return RedirectToAction("GetRequest");
        }
        public ActionResult PickUp(int id)
        {
            //Check reserve
            var reverse = db.TblReserveds.Where(r => r.ID == id).FirstOrDefault();
            if (reverse is null) TempData["Message"] = "İşlem Başarısız. Tekrar Deneyin";
            else
            {
                reverse.EndDate = DateTime.Now;
                reverse.Status = false;
                //Check Book
                var book = db.TblBooks.Where(b => b.ID == reverse.BookID).FirstOrDefault();
                if (book is null) TempData["Message"] = "İşlem Başarısız. Tekrar Deneyin";
                else book.Status = null;

                db.SaveChanges();
                TempData["MessageSuccess"] = "Teslim Edildi";

            }
            return RedirectToAction("GetReserve");
        }
    }
}