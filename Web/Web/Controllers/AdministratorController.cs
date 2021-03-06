﻿using Ganss.XSS;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Web.Models;

namespace Web.Controllers
{
    public class AdministratorController : Controller
    {
        private BaseDbContext db = new BaseDbContext();
        private int pageSize = 15;

        #region 用户管理容器
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
        #endregion

        // GET: Administrator
        public ActionResult Index(AdminOperationStatus? status)
        {
            ViewBag.StatusMessage =
                status == AdminOperationStatus.Error ? "操作失败。"
                : status == AdminOperationStatus.Success ? "操作成功。" : "";

            return View();
        }
        //4 Views
        #region 文章管理模块
        public ActionResult Articles(int page = 0)
        {

            return View(new ListPage<Article>(db.Articles, page, pageSize));
        }

        public ActionResult ArticleCreate()
        {
            ViewData["StatusList"] = EnumExtension.GetSelectList(typeof(ArticleStatus));
            ViewData["ClassList"] = EnumExtension.GetSelectList(typeof(ArticleClass));

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult ArticleCreate(Article model)
        {
            if (ModelState.IsValid)
            {
                if (Request.Files.Count != 1)
                {
                    ViewData["StatusList"] = EnumExtension.GetSelectList(typeof(ArticleStatus));
                    ViewData["ClassList"] = EnumExtension.GetSelectList(typeof(ArticleClass));
                    return View();
                }
                var file = Request.Files[0];

                var s = new HtmlSanitizer();
                model.Content = Server.HtmlDecode(s.Sanitize(Request.Params["ck"]));
                model.Image = Material.Create("", MaterialType.Avatar, file, db);
                model.NewArticle();
                db.Articles.Add(model);
                db.SaveChanges();
                return RedirectToAction("Index", new { status = AdminOperationStatus.Success });
            }

            ViewData["StatusList"] = EnumExtension.GetSelectList(typeof(ArticleStatus));
            ViewData["ClassList"] = EnumExtension.GetSelectList(typeof(ArticleClass));
            return View();
        }

        public ActionResult ArticleEdit(Guid? Id)
        {
            if (Id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Article model = db.Articles.Find(Id);
            if (model == null)
                return HttpNotFound();
            ViewData["StatusList"] = EnumExtension.GetSelectList(typeof(ArticleStatus));
            ViewData["ClassList"] = EnumExtension.GetSelectList(typeof(ArticleClass));

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult ArticleEdit(Article model)
        {
            if (ModelState.IsValid)
            {
                var s = new HtmlSanitizer();
                model.Content = Server.HtmlDecode(s.Sanitize(Request.Params["ck"])); ;
                db.Articles.Attach(model);
                db.Entry(model).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index", new { status = AdminOperationStatus.Success });
            }

            ViewData["StatusList"] = EnumExtension.GetSelectList(typeof(ArticleStatus));
            ViewData["ClassList"] = EnumExtension.GetSelectList(typeof(ArticleClass));
            return View();
        }

        public ActionResult ArticleDeleteConfirm(Guid? Id)
        {
            if (Id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Article model = db.Articles.Find(Id);
            if (model == null)
                return HttpNotFound();

            return View(model);
        }

        public ActionResult ArticleDelete(Guid? Id)
        {
            if (Id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Article model = db.Articles.Find(Id);
            if (model == null)
                return HttpNotFound();

            db.Articles.Remove(model);
            db.SaveChanges();

            return RedirectToAction("Index", new { status = AdminOperationStatus.Success });
        }
        #endregion
        //4 Views
        #region 活动管理模块
        public ActionResult Activities(int page = 0)
        {
            return View(new ListPage<ActivityOperation>(db.ActivityOperations, page, pageSize));
        }

        public ActionResult ActivityCreate()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult ActivityCreate(ActivityOperation model)
        {
            if (ModelState.IsValid)
            {
                if (model.StartTime > model.EndTime)
                {
                    ViewData["ErrorInfo"] = "活动开始时间必须在结束时间之前。";
                    return View();
                }
                var s = new HtmlSanitizer();
                model.Content = Server.HtmlDecode(s.Sanitize(Request.Params["ck"])); ;
                model.NewActivity(db);
                db.ActivityOperations.Add(model);
                db.SaveChanges();
                return RedirectToAction("Index", new { status = AdminOperationStatus.Success });
            }
            return View();
        }

        public ActionResult ActivityEdit(Guid? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var model = db.ActivityOperations.Find(id);
            if (model == null)
                return HttpNotFound();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult ActivityEdit(ActivityOperation model)
        {
            if (ModelState.IsValid)
            {
                var s = new HtmlSanitizer();
                model.Content = Server.HtmlDecode(s.Sanitize(Request.Params["ck"])); ;
                db.ActivityOperations.Attach(model);
                db.Entry(model).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index", new { status = AdminOperationStatus.Success });
            }
            return View();
        }

        public ActionResult ActivityDelete(Guid? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var model = db.ActivityOperations.Find(id);
            if (model == null)
                return HttpNotFound();
            db.Entry(model).State = System.Data.Entity.EntityState.Deleted;
            db.SaveChanges();
            return RedirectToAction("Index", new { status = AdminOperationStatus.Success });
        }

        public ActionResult DeleteConfirm(Guid? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var model = db.ActivityOperations.Find(id);
            if (model == null)
                return HttpNotFound();

            return View(model);
        }
        #endregion
        //5 Views
        #region 上传文件模块
        // GET: Materials
        public ActionResult Materials(int page = 0)

        {
            IListPage test = new Material();
            var model = new ListPage<Material>(db.Materials, page, pageSize); //实现分页功能
            return View(model);
        }

        // GET: Materials/Details/5
        public ActionResult MaterialDetails(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Material material = db.Materials.Find(id);
            if (material == null)
            {
                return HttpNotFound();
            }
            return View(material);
        }

        // GET: Materials/Create
        public ActionResult MaterialCreate()
        {
            ViewData["TypeList"] = EnumExtension.GetSelectList(typeof(MaterialType));
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult MaterialCreate([Bind(Include = "Name,Description,Type")] Material material)
        {
            if (ModelState.IsValid)
            {
                if (Request.Files.Count != 1)//如果文件列表为空则返回
                    return View();
                var file = Request.Files[0];//只上传第一个文件
                Material.Create(material.Description, material.Type, file, db);
                return RedirectToAction("Materials");
            }

            return View(material);
        }

        // GET: Materials/Edit/5
        public ActionResult MaterialEdit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Material material = db.Materials.Find(id);
            if (material == null)
            {
                return HttpNotFound();
            }
            ViewData["TypeList"] = EnumExtension.GetSelectList(typeof(MaterialType));
            return View(material);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MaterialEdit([Bind(Include = "ID,Description,Type")] Material material)
        {
            if (ModelState.IsValid)
            {
                db.Materials.Find(material.Id).Description = material.Description;
                db.Materials.Find(material.Id).Type = material.Type;
                db.SaveChanges();
                return RedirectToAction("Materials");
            }
            return View(material);
        }

        // GET: Materials/Delete/5
        public ActionResult MaterialDelete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Material material = db.Materials.Find(id);
            if (material == null)
            {
                return HttpNotFound();
            }
            return View(material);
        }

        // POST: Materials/Delete/5
        [HttpPost, ActionName("MaterialDelete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(Guid id)
        {
            Material material = db.Materials.Find(id);
            db.Materials.Remove(material);
            db.SaveChanges();
            return RedirectToAction("Materials");
        }
        #endregion
        //3 Views
        #region 用户管理模块
        public ActionResult UserList()
        {
            return View(db.Users.ToList());
        }
        public ActionResult TutorCreate()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<ActionResult> TutorCreate(CreateTutorViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (Request.Files.Count != 1)//如果文件列表为空则返回
                {
                    ViewBag.Status = "请检查上传文件！";

                    return View();
                }

                var file = Request.Files[0];//只上传第一个文件

                var user = Models.User.Create(model.Email, model.DisplayName);
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    //为账户添加角色
                    var roleName = "tutor";
                    ApplicationRoleManager roleManager = new ApplicationRoleManager(new RoleStore<IdentityRole>(db));

                    //判断角色是否存在
                    if (!roleManager.RoleExists(roleName))
                    {
                        //角色不存在则建立角色
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                    //将用户加入角色
                    await UserManager.AddToRoleAsync(user.Id, roleName);

                    // 有关如何启用帐户确认和密码重置的详细信息，请访问 http://go.microsoft.com/fwlink/?LinkID=320771
                    // 发送包含此链接的电子邮件
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "确认你的帐户", "请通过单击 <a href=\"" + callbackUrl + "\">這裏</a>来确认你的帐户");

                    var tutor = new TutorInformation { Id = Guid.NewGuid(), Tutor = db.Users.Find(user.Id), Avatar = Material.Create(user.DisplayName, MaterialType.Avatar, file, db), Position = model.Position, Introduction = model.Introduction };
                    db.TutorInformations.Add(tutor);
                    db.SaveChanges();
                    ViewBag.Status = "操作成功！";

                    return View();
                }
            }
            ViewBag.Status = "操作失败！";

            return View();
        }

        public ActionResult UserEdit(string id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            User model = db.Users.Find(id);
            if (model == null)
                return HttpNotFound();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserEdit(User model)
        {
            if (ModelState.IsValid)
            {
                db.Users.Attach(model);
                db.Entry(model).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index", new { status = AdminOperationStatus.Success });
            }
            return View();
        }
        #endregion
        //2 Views
        #region 审核认证记录模块
        public IdentityType Type { get; set; }
        public ActionResult IdentityRecords(int page = 0)
        {
            var user = new ListPage<IdentityRecord>(db.IdentityRecords.Where(i => i.Status == IdentityStatus.ToApprove), page, pageSize);
            return View("IdentityRecords", user);
        }

        public ActionResult ProjectIdentityRecords(int page = 0)
        {
            var project = new ListPage<Project>(db.Projects.Where(i => i.Status == ProjectStatus.ToApprove), page, pageSize);
            return View("ProjectIdentityRecords", project);
        }

        public ActionResult CompanyIdentityRecords(int page = 0)
        {
            var company = new ListPage<Company>(db.Companys.Where(i => i.Status == CompanyStatus.ToApprove), page, pageSize);
            return View("CompanyIdentityRecords", company);

        }

        public ActionResult IdentityRecordDetails(Guid? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            IdentityRecord user = db.IdentityRecords.Find(id);
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);

            return View("IdentityRecordDetails", user);
        }

        public ActionResult ProjectIdentityRecordDetails(Guid? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Project project = db.Projects.Find(id);
            if (project == null)
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);

            return View("ProjectIdentityRecordDetails", project);
        }

        public ActionResult CompanyIdentityRecordDetails(Guid? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Company company = db.Companys.Find(id);
            if (company == null)
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);

            return View("CompanyIdentityRecordDetails", company);
        }

        public ActionResult ProjectIdentityRecordApprove(Guid? id, bool isApprove)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Project project = db.Projects.Find(id);
            if (project == null)
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            if (project.Status != ProjectStatus.ToApprove)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (isApprove)
            {
                project.Status = ProjectStatus.Done;
                db.Messages.Add(new Message(project.Admin.Id, MessageType.System, MessageTemplate.ProjectSuccess, db));
                Team Team = new Team();
                Team.NewTeam(ref project);
            }
            else
            {
                project.Status = ProjectStatus.Denied;
                db.Messages.Add(new Message(project.Admin.Id, MessageType.System, MessageTemplate.ProjectFailure, db));
            }
            db.SaveChanges();
            return RedirectToAction("ProjectIdentityRecords");
        }

        public ActionResult IdentityRecordApprove(Guid? id, bool isApprove)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            IdentityRecord user = db.IdentityRecords.Find(id);
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            if (user.Status != IdentityStatus.ToApprove)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (isApprove)
            {
                user.Status = IdentityStatus.Done;
                user.User.Identitied = true;
                db.Messages.Add(new Message(user.User.Id, MessageType.System, MessageTemplate.IdentityRecordSuccess, db));
            }
            else
            {
                user.Status = IdentityStatus.Denied;
                user.User.Identitied = false;
                db.Messages.Add(new Message(user.User.Id, MessageType.System, MessageTemplate.IdentityRecordFailure, db));
            }
            db.SaveChanges();
            return RedirectToAction("IdentityRecords");
        }
        public ActionResult CompanyIdentityRecordApprove(Guid? id, bool isApprove)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Company company = db.Companys.Find(id);
            if (company == null)
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            if (company.Status != CompanyStatus.ToApprove)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (isApprove)
            {
                company.Status = CompanyStatus.Done;
                db.Messages.Add(new Message(company.Admin.Id, MessageType.System, MessageTemplate.CompanySuccess, db));
            }
            else
            {
                company.Status = CompanyStatus.Denied;
                db.Messages.Add(new Message(company.Admin.Id, MessageType.System, MessageTemplate.CompanyFailure, db));
            }
            db.SaveChanges();
            return RedirectToAction("CompanyIdentityRecords");
        }
        #endregion

        #region 释放资源模块
        protected override void Dispose(bool disposing)
        {
            if (db != null)
            {
                db.Dispose();
                db = null;
            }
            base.Dispose(disposing);
        }
        #endregion

        public enum AdminOperationStatus
        {
            Success,
            Error,
        }
    }
}