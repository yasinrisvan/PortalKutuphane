using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LibraryOfPortall.Models.EntityFramework;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using LibraryOfPortall.Models;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;


namespace LibraryOfPortall.Controllers
{
    public class LibraryController : Controller
    {
        SqlConnection conn = new SqlConnection(MailBaglanti.sql_mail());
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
            {     //kitap teslim süresi dolanlar için otomatik mail gönderme kodu
                DateTime sysdate = DateTime.Now.AddMonths(-1);
                var bul = db.TblReserveds.Where(x => x.EndDate == null && x.SendMail == false && x.AcceptDate < sysdate).ToList();
                foreach (var item in bul)
                {
                    string sorgu = "exec TeslimMail @Reserve_Id";   //100.19 TYHTALEP TeslimMail prosedürünü çalıştırma
                    SqlCommand komut = new SqlCommand(sorgu, conn);
                    komut.Parameters.AddWithValue("@Reserve_Id", item.ID);
                    conn.Open();
                    int sonuc = komut.ExecuteNonQuery();
                    conn.Close();
                }
            }
            //----------------------
            Complete();
            List<TblBook> books = new List<TblBook>();
            TblUser user = (TblUser)Session["user"];

            books = user is null ? db.TblBooks.Where(b=>b.Active==true).ToList() : db.TblBooks.Where(b => b.Status == null && b.Active == true).ToList();
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
                //-------------------------  100.19 daki TYHTALEP talepmail prosedürünü çalıştırma
                string sorgu = "exec TalepMail @Talep_Id";

                SqlCommand komut = new SqlCommand(sorgu, conn);

                komut.Parameters.AddWithValue("@Talep_Id", newReq.ID);
                conn.Open();
                int sonuc = komut.ExecuteNonQuery();
                conn.Close();
                //-----------------------
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
            TblUser user = (TblUser)Session["user"];
            var requests = db.TblRequests.Where(r => r.Status == "Onay Bekleniyor" && r.TblBook.RegionID == user.RegionID).ToList();
            return View(requests);
        }

        [Authorize]
        public ActionResult Accept(int id)
        {
            TblUser user = (TblUser)Session["user"];
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
                    AcceptDate = DateTime.Now,
                    SendMail = false
                };

                db.TblReserveds.Add(newRes);

                db.SaveChanges();
                TempData["MessageSuccess"] = "Onaylandı";
                //-------------------------  100.19 daki TYHTALEP onaymail prosedürünü çalıştırma
                string sorgu = "exec OnayMail @Onay_Id,@Kullanici_Id";

                SqlCommand komut = new SqlCommand(sorgu, conn);

                komut.Parameters.AddWithValue("@Onay_Id", id);
                komut.Parameters.AddWithValue("@Kullanici_Id", user.ID);
                conn.Open();
                int sonuc = komut.ExecuteNonQuery();
                conn.Close();
                //-----------------------
            }
            return RedirectToAction("GetAllRequest");

        }
        public ActionResult Decline(int id)
        {
            var request = db.TblRequests.Where(r => r.ID == id).FirstOrDefault();
            if (request is null)
            {
                TempData["Message"] = "Islem Gerceklestirilemedi. Tekrar Deneyin";
                return RedirectToAction("Index");
            }
            else
            {
                var book = db.TblBooks.Where(b => b.ID == request.BookID).FirstOrDefault();
                book.Status = null;
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
            TempData["sart"] = "GetReserve";
            return View(reserves);
        }

        public ActionResult ReserveBook()
        {
            TblUser user = (TblUser)Session["user"];
            var reserves = db.TblReserveds.Where(r => r.TblBook.RegionID == user.RegionID && r.Status == null).ToList();
            TempData["sart"] = "ReserveBook";
            return View("GetReserve", reserves);
        }

        public ActionResult CancelRequest(int id)
        {
            var request = db.TblRequests.Where(r => r.ID == id).FirstOrDefault();
            if (request is null) TempData["Message"] = "İşlem Başarısız. Tekrar Deneyin";
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
        [HttpGet]
        public ActionResult AddBook()
        {
            List<SelectListItem> category = (from i in db.TblCategories.ToList()
                                             select new SelectListItem
                                             {
                                                 Text = i.Name,
                                                 Value = i.ID.ToString()
                                             }).OrderBy(x => x.Text).ToList();
            ViewBag.Category = category;
            return View("AddBook", new TblBook());
        }
        [HttpPost]
        public ActionResult AddBook(TblBook book)
        {
            TblUser user = (TblUser)Session["user"];
            if (book.ID == 0)
            {
                book.RegionID = user.RegionID;
                book.Active = true;
                db.TblBooks.Add(book);
            }
            else
            {
                var find = db.TblBooks.Find(book.ID);
                if (find == null)
                {
                    return HttpNotFound();
                }
                find.BookName = book.BookName;
                find.Author = book.Author;
                find.Code = book.Code;
                find.CategoryID = book.CategoryID;
                find.DateOfEntry = book.DateOfEntry;
            }
            db.SaveChanges();
            return RedirectToAction("Index", "Library");
        }
        public ActionResult BookUpdate(int id)
        {
            List<SelectListItem> category = (from i in db.TblCategories.ToList()
                                             select new SelectListItem
                                             {
                                                 Text = i.Name,
                                                 Value = i.ID.ToString()
                                             }).OrderBy(x => x.Text).ToList();
            ViewBag.Category = category;
            var find = db.TblBooks.Find(id);
            return View("AddBook", find);
        }
        public ActionResult BookDelete(int id)
        {
            var book = db.TblBooks.Find(id);
            book.Active = false;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public JsonResult EditCategory(int Kitap_Id)
        {
            var model = new TblUnregisteredReserve();    //burada modeli oluşturmaya gerek kalmadan sadece kitap_id yi jsonconvertle gönderebilirim.
            model.BookID = Kitap_Id;                      //tabii buna göre editcategory fonksiyonunda obj. olan değişkeni buna göre değiştirmek gerekiyor.
            string value = " ";
            value = JsonConvert.SerializeObject(model, Formatting.Indented, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore

            });
            return Json(value, JsonRequestBehavior.AllowGet);
        }
        public ActionResult KitapVer(int? Kitap_Id,string Ad,string Soyad,string Sicil)
        {
            TblUnregisteredReserve table = new TblUnregisteredReserve();
            table.Name = Ad;
            table.Surname = Soyad;
            table.RegistryNo = Sicil;
            table.BookID = Kitap_Id;
            table.Status = true;
            db.TblUnregisteredReserves.Add(table);    //buraya kadar gelen verilerle kayıt işlemi yaptım
            //--------------------------------------
            var bul = db.TblBooks.Where(m=>m.ID==Kitap_Id).FirstOrDefault();     //burada eklediğim kitabı bulup durumunu rezerve edilmiş gibi true yaptım.
            bul.Status = true;
            db.SaveChanges();
            return RedirectToAction("Index", "Library");

        }
        [HttpGet]
        public ActionResult UnRegisteredReserve()
        {
            var liste = db.TblUnregisteredReserves.Where(m => m.Status == true).ToList();
            return View(liste);
        }
        public ActionResult TeslimAl(int id)    //kayıt olmayan kullanıcılara verilen kitabı teslim alınca çalışacak olan action
        {
            var bul = db.TblUnregisteredReserves.Where(m => m.ID == id).FirstOrDefault();
            bul.Status = false;
            var book = db.TblBooks.Where(m => m.ID == bul.BookID).FirstOrDefault();
            book.Status = null;
            db.SaveChanges();
            TempData["Message"] = "Kitap Teslim Alindi.";
            return RedirectToAction("UnRegisteredReserve");
        }
    }
}