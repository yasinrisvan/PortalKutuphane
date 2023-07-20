using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using LibraryOfPortall.Models.EntityFramework;
namespace LibraryOfPortall.Controllers
{
    public class UserController : Controller
    {
        TYHKUTUPHANEEntities db = new TYHKUTUPHANEEntities();
        public void Complete()
        {
            List<SelectListItem> regions = (from i in db.TblRegions.ToList()
                                          select new SelectListItem
                                          {
                                              Text = i.Name,
                                              Value = i.ID.ToString()
                                          }).OrderBy(x => x.Text).ToList();
            ViewBag.Regions = regions;
            List<SelectListItem> departments = (from i in db.TblDepartments.ToList()
                                            select new SelectListItem
                                            {
                                                Text = i.Name,
                                                Value = i.ID.ToString()
                                            }).OrderBy(x => x.Text).ToList();
            ViewBag.Departments = departments;
        }
        // GET: User
        [AllowAnonymous]
        public ActionResult SignIn() { return View(); }

        [HttpPost, AllowAnonymous]
        public ActionResult SignIn(FormCollection form)
        {
            var registryNo = form["RegistryNo"];
            var password = form["Password"];
            var user = db.TblUsers.Where(u => u.RegistryNo == registryNo).FirstOrDefault();
            if (user is null)
            {
                ViewBag.Message = "Kullanıcı Bulunumadı. Lutfen Kayıt Olunuz..!";
            }
            else if (user.RegistryNo == registryNo && user.Password == password)
            {
                FormsAuthentication.SetAuthCookie(user.ID.ToString(), false);
                Session.Add("user", user);
                return RedirectToAction("Index","Library");
            }
            else
            {
                ViewBag.Message = "Bilgilerinizi Kontrol Ediniz";  
            }
            return View();
        }

        [AllowAnonymous]
        public ActionResult ForgetPassword() { return View(); }

        [HttpPost,AllowAnonymous]
        public ActionResult ForgetPassword(FormCollection form)
        {
            var registryNo = form["RegistryNo"];
            var tc = form["Tc"];
            var regUser = db.TblUsers.Where(u => u.RegistryNo == registryNo && u.Tc == tc).FirstOrDefault();
            if (regUser is null)
            {
                ViewBag.Message = "Bilgileriniz Hatalı";
            }
            else
            {
                if(form["Password"] == form["AgainPassword"])
                {
                    regUser.Password = form["Password"];
                    db.SaveChanges();
                    ViewBag.MessageSuccess = "Şifre Değiştirildi";
                }
                ViewBag.Message = "Şifreler aynı değil";
            }
            return RedirectToAction("SignIn");
        }

        [AllowAnonymous]
        public ActionResult Register() { Complete(); return View(); }

        [HttpPost, AllowAnonymous]
        public ActionResult Register(TblUser user)
        {
            var regUser = db.TblUsers.Where(m => m.RegistryNo == user.RegistryNo).FirstOrDefault();
            if(regUser is null)
            {
                user.AuthorityID = 2;
                user.Name = user.Name.ToUpper();
                user.Surname = user.Surname.ToUpper();
                db.TblUsers.Add(user);
                db.SaveChanges();
                ViewBag.MessageSuccess = "Kaydınız Olusturulmustur";
            }
            else
            {
                ViewBag.Message = "Kaydınız Bulunmaktadır";
            }
            return RedirectToAction("SignIn");
        }

        public ActionResult AccountDetails()
        {
            TblUser user = (TblUser)Session["user"];
            return View(user);
        }

        public ActionResult SignOut()
        {
            TblUser user = (TblUser)Session["user"];
            Session.Remove("user");
            return RedirectToAction("Index", "Library");
        }
    }
}