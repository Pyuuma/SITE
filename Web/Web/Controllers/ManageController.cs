﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Web.Models;
using System.IO;
using System.Net;

namespace Web.Controllers
{
    [Authorize(Roles = "student")]
    public class ManageController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private BaseDbContext db = new BaseDbContext();

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

        public ActionResult Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.Error ? "出现错误。"
                : message == ManageMessageId.AddEducationSuccess ? "已添加你的一条教育经历。"
                : message == ManageMessageId.AddWorkSuccess ? "已添加一条你的工作经历。"
                : message == ManageMessageId.ChangePasswordSuccess ? "修改密码成功。"
                : message == ManageMessageId.AcessDenied ? "你没有权限进行这项操作。"
                : message == ManageMessageId.ApplySuccess ? "申请加入成功，请等待团队管理员审批。"
                : message == ManageMessageId.ProjectSuccess ? "项目申请成功，请等待管理员审批。"
                : message == ManageMessageId.RecruitSuccess ? "招募请求发送成功，请等待该用户响应。"
                : message == ManageMessageId.UpdateUserProfileSuccess ? "修改个人信息成功。"
                : message == ManageMessageId.OperationSuccess ? "操作成功。"
                : "";

            var userId = User.Identity.GetUserId();
            var model = new ManageIndexViewModel
            {
            };
            return View(model);
        }

        #region 个人履历模块
        public ActionResult RecordList()
        {
            using (BaseDbContext db = new BaseDbContext())
            {
                User user = db.Users.Find(User.Identity.GetUserId());
                var educations = user.Education;
                var works = user.Work;
                educations.OrderBy(e => e.DegreeType);
                ViewBag.Educations = educations;
                ViewBag.Works = works;

                return View();
            }
        }

        public ActionResult AddEducationRecord()
        {

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEducationRecord(EducationRecord educationRecord)
        {
            using (BaseDbContext db = new BaseDbContext())
            {
                if (ModelState.IsValid)
                {
                    educationRecord.Id = Guid.NewGuid();
                    if (db.Users.Find(User.Identity.GetUserId()).Education == null)
                        db.Users.Find(User.Identity.GetUserId()).Education = new System.Collections.Generic.List<EducationRecord>();
                    db.Users.Find(User.Identity.GetUserId()).Education.Add(educationRecord);
                    db.SaveChanges();
                    return RedirectToAction("Index", new { Message = ManageMessageId.AddEducationSuccess });
                }
            }

            return RedirectToAction("Index", new { Message = ManageMessageId.Error });
        }

        public ActionResult AddWorkRecord()
        {

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddWorkRecord(WorkRecord workRecord)
        {
            using (BaseDbContext db = new BaseDbContext())
            {
                if (ModelState.IsValid)
                {
                    workRecord.Id = Guid.NewGuid();
                    if (db.Users.Find(User.Identity.GetUserId()).Work == null)
                        db.Users.Find(User.Identity.GetUserId()).Work = new System.Collections.Generic.List<WorkRecord>();
                    db.Users.Find(User.Identity.GetUserId()).Work.Add(workRecord);
                    db.SaveChanges();
                    return RedirectToAction("Index", new { Message = ManageMessageId.AddWorkSuccess });
                }
            }

            return RedirectToAction("Index", new { Message = ManageMessageId.Error });
        }
        #endregion

        #region 用户信息与认证模块
        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        public ActionResult UserProfile()
        {
            User user = UserManager.FindById(User.Identity.GetUserId());
            Profile model = user.Profile;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserProfile(Profile model)
        {
            if (ModelState.IsValid)
            {
                using (BaseDbContext db = new BaseDbContext())
                {
                    db.Users.Find(User.Identity.GetUserId()).Profile = model;
                    db.SaveChanges();
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.UpdateUserProfileSuccess });
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.Error });
        }

        public ActionResult UserIdentity()
        {
            var status = db.Users.Find(User.Identity.GetUserId()).IdentityRecord;
            ViewBag.Status = status != null ? status.Status : IdentityStatus.None;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserIdentity(IdentityRecord model)
        {
            if (ModelState.IsValid)
            {
                if (Request.Files.Count != 2)
                    return View();
                /*var allowExtensions = new string[] { ".jpg", ".png", ".jpeg" };
                foreach ( string f in Request.Files)
                {
                    if (!allowExtensions.Contains(Path.GetExtension(Request.Files.Get(f).FileName)))
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }*/
                model.FrontIdCard = Material.Create("", MaterialType.Identity, Request.Files[0], db);
                model.BackIdCard = Material.Create("", MaterialType.Identity, Request.Files[1], db);
                model.Status = IdentityStatus.ToApprove;
                model.Time = DateTime.Now;
                model.Id = Guid.NewGuid();
                model.User = db.Users.Find(User.Identity.GetUserId());
                db.IdentityRecords.Add(model);
                db.SaveChanges();
                return RedirectToAction("Index", new { Message = ManageMessageId.UserIdentitySuccess });
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.Error });
        }
        #endregion

        #region 项目、团队与公司模块
        public bool IllegalIdentity()
        {
            if (Extensions.GetContextUser(db).TeamRecord == null | Extensions.GetContextUser(db).Project == null)
                return false;
            if (Extensions.GetContextUser(db).Project.Status != ProjectStatus.Done | Extensions.GetContextUser(db).TeamRecord.Status != TeamMemberStatus.Admin)
                return false;
            return true;
        }
        public ActionResult Project()
        {
            User user = db.Users.Find(Extensions.GetUserId());
            ViewData["ProgressList"] = EnumExtension.GetSelectList(typeof(ProjectProgressType));
            if (user.TeamRecord != null && user.TeamRecord.Status != TeamMemberStatus.Admin)
                return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });
            if (user.Project == null)
                return View(new Project());
            if (user.Project.Status == ProjectStatus.Denied)
            {
                TempData["DeniedInfo"] = "项目未通过";
                return View(user.Project);
            }

            return RedirectToAction("ProjectProfile");
        }

        public ActionResult ProjectProfile()
        {
            User user = db.Users.Find(Extensions.GetUserId());
            if (user.Project.Status == ProjectStatus.Denied)
            {
                TempData["DeniedInfo"] = "项目未通过,请重新申请。";
                return RedirectToAction("Project",user.Project);
            }

            return View(user.Project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Project(Project model)
        {
            if (ModelState.IsValid)
            {
                if (Request.Files.Count != 1)//如果文件列表为空则返回
                    return View();
                var file = Request.Files[0];//只上传第一个文件

                if (model.Avatar == null)
                {
                    model.Avatar = Material.Create("", MaterialType.Avatar, file, db);
                }
                else
                {
                    model.Avatar = Material.ChangeFile(model.Avatar.Id, file, db);
                }
                if (Extensions.GetContextUser(db).Project != null)
                    db.Entry(model).State = System.Data.Entity.EntityState.Modified;
                else
                {
                    model.NewProject(db);
                    db.Projects.Add(model);
                    db.SaveChanges();
                }
                db.SaveChanges();

                return RedirectToAction("Index", new { Message = ManageMessageId.ProjectSuccess });
            }

            return RedirectToAction("Index", new { Message = ManageMessageId.Error });
        }

        public ActionResult TeamApply(int page = 0)
        {
            if (Extensions.GetContextUser(db).TeamRecord != null)
                return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });
            int pageSize = 10;
            var list = new ListPage<Team>(db.Teams.Where(u => u.Searchable == true), page, pageSize);

            return View(list);
        }

        [ActionName("DoTeamApply")]
        public ActionResult TeamApply(Guid teamId)
        {
            if (Extensions.GetContextUser(db).TeamRecord != null)
                return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });
            Team team = db.Teams.Find(teamId);
            if (team == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            db.TeamRecords.Add(new TeamRecord(team, TeamMemberStatus.Apply));
            db.Messages.Add(new Message(team.Admin.Id, MessageType.System, MessageTemplate.TeamApply, db));
            db.SaveChanges();

            return RedirectToAction("Index", new { Message = ManageMessageId.ApplySuccess });
        }

        public ActionResult TeamRecruit(int page = 0)
        {
            if(IllegalIdentity())
                return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });
            int pageSize = 10;
            var list = new ListPage<User>(db.Users.Where(u => u.Profile.Searchable == true), page, pageSize);

            return View(list);
        }

        [ActionName("DoTeamRecruit")]
        public ActionResult TeamRecruit(string userId)
        {
            if (IllegalIdentity())
                return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });
            User user = db.Users.Find(userId);
            Team team = db.Teams.First(u => u.Admin.Id == Extensions.GetContextUser(db).Id);
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            db.TeamRecords.Add(new TeamRecord(team, TeamMemberStatus.Recruit, user));
            db.Messages.Add(new Message(user.Id, MessageType.System, MessageTemplate.TeamRecruit, db));
            db.SaveChanges();

            return RedirectToAction("Index", new { Message = ManageMessageId.RecruitSuccess });
        }

        public ActionResult UserDetails(string userId)
        {
            User user = db.Users.Find(userId);
            if (user == null)
                return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });

            return View(user);
        }

        public ActionResult TeamAccess(int page = 0)
        {
            if (IllegalIdentity())
                return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });
            int pageSize = 10;
            var list = new ListPage<User>((from u in db.TeamRecords
                                           where u.Team.Admin.Id == Extensions.GetContextUser(db).Id &&   //团队管理为该用户的团队
                                           u.Status == TeamMemberStatus.Apply                             //状态为申请
                                           select u.Receiver), page, pageSize);

            return View(list);
        }

        [ActionName("DoTeamAccess")]
        public ActionResult TeamAccess(string userId, bool IsApprove)
        {
            if (IllegalIdentity())
                return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });
            User applicant = db.Users.Find(userId); 
            Team team = db.Teams.First(u => u.Admin.Id == Extensions.GetContextUser(db).Id);
            if (applicant == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var ApplyRecord = db.TeamRecords.First(t => t.Team.Id == team.Id && t.Receiver.Id == applicant.Id && t.Status == TeamMemberStatus.Apply);
            if (ApplyRecord == null)
                return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });
            if (IsApprove)
            {
                ApplyRecord.Status = TeamMemberStatus.Normal;
                applicant.Project = Extensions.GetContextUser(db).Project;
                db.Entry(ApplyRecord).State = System.Data.Entity.EntityState.Modified;
                db.Messages.Add(new Message(applicant.Id, MessageType.System, MessageTemplate.TeamApplySuccess, db));
            }
            else
            {
                db.Entry(ApplyRecord).State = System.Data.Entity.EntityState.Deleted;
                db.Messages.Add(new Message(applicant.Id, MessageType.System, MessageTemplate.ProjectFailure, db));
            }
            db.SaveChanges();

            return RedirectToAction("Index", new { Message = ManageMessageId.RecruitSuccess });
        }

        public ActionResult TeamMember(int page = 0)
        {
            if (Extensions.GetContextUser(db).TeamRecord == null)
                return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });
            Team team = db.Users.Find(Extensions.GetUserId()).TeamRecord.Team;
            if (team == null)
                return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });
            int pageSize = 10;
            var teamMember = team.Member.Where(m => m.Status == TeamMemberStatus.Normal | m.Status == TeamMemberStatus.Admin);
            var list = new ListPage<TeamRecord>(teamMember, page, pageSize);

            return View(list);
        }
        public ActionResult TeamMemberDelete(string userId)
        {
            if (IllegalIdentity())
                return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });
            User member = db.Users.Find(userId);
            if (member.TeamRecord.Status == TeamMemberStatus.Admin | Extensions.GetContextUser(db).TeamRecord.Status!= TeamMemberStatus.Admin)
                return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });
            member.Project = null;
            db.Entry(member.TeamRecord).State = System.Data.Entity.EntityState.Deleted;
            db.SaveChanges();

            return RedirectToAction("Index", new { Message = ManageMessageId.OperationSuccess });
        }

        public ActionResult TeamMemberQuit()
        {
            if (Extensions.GetContextUser(db).TeamRecord == null)
                return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });
            if(!IllegalIdentity())
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });
            User member = db.Users.Find(Extensions.GetUserId());
            member.Project = null;
            db.Entry(member.TeamRecord).State = System.Data.Entity.EntityState.Deleted;
            db.SaveChanges();

            return RedirectToAction("Index", new { Message = ManageMessageId.OperationSuccess });
        }

        public ActionResult TeamProfile()
        {
            if (IllegalIdentity())
                return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });
            Team team = Extensions.GetContextUser(db).TeamRecord.Team;
            TeamProfileViewModel model = new TeamProfileViewModel
            {
                Id = team.Id,
                Name = team.Name,
                Administrator = team.Admin.DisplayName,
                Time = team.Time,
                Introduction = team.Introduction,
                Announcement = team.Announcement
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TeamProfile([Bind(Include = "Annoucement,Introduction,Searchable")]TeamProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (IllegalIdentity())
                    return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });
                Team team = db.Teams.First(t => t.Id == Extensions.GetContextUser(db).TeamRecord.Team.Id);
                team.Announcement = model.Announcement;
                team.Introduction = model.Introduction;
                team.Searchable = model.Searchable;
                db.SaveChanges();
                return RedirectToAction("Index", new { Message = ManageMessageId.OperationSuccess });
            }

            return RedirectToAction("Index", new { Message = ManageMessageId.Error });
        }

        public ActionResult Company()
        {
            if (IllegalIdentity())
                return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });
            if (Extensions.GetContextUser(db).TeamRecord.Team.Company != null)
            {
                ViewBag.Status = Extensions.GetContextUser(db).TeamRecord.Team.Company.Status;
                return View(Extensions.GetContextUser(db).TeamRecord.Team.Company);
            }

            ViewBag.Status = CompanyStatus.None;
            return View(new Company());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Company(Company model)
        {
            if (IllegalIdentity())
                return RedirectToAction("Index", new { Message = ManageMessageId.AcessDenied });
            if (ModelState.IsValid)
            {
                db.Teams.Find(Extensions.GetContextUser(db).TeamRecord.Team.Id).Company = model;
                db.SaveChanges();
                return View();
            }

            return View();
        }

        public ActionResult Avatar(Guid? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var model = db.Materials.Find(id);
            if (model == null)
                return HttpNotFound();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Avatar(Material model)
        {
            if (ModelState.IsValid)
            {
                if (Request.Files.Count != 1)//如果文件列表为空则返回
                    return View();
                var file = Request.Files[0];//只上传第一个文件
                //根据日期生成服务器端文件名
                string uploadFileName = model.Id + Path.GetExtension(file.FileName);
                //生成服务器端绝对路径
                string absolutFileName = Server.MapPath("~/") + "UserUpload/Avatar/" + uploadFileName;
                //执行上传
                if (System.IO.File.Exists(absolutFileName))
                {
                    System.IO.File.Delete(absolutFileName);
                }
                file.SaveAs(absolutFileName);
                model.Name = uploadFileName;
                //添加Material记录
                db.Materials.Attach(model);
                db.Entry(model).State = System.Data.Entity.EntityState.Modified;
                //保存更改
                db.SaveChanges();
                return RedirectToAction("Index", new { Message = ManageMessageId.OperationSuccess });
            }

            ViewBag.Error = "存在错误，请检查输入。";
            return View();
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        #region 帮助程序

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasIdentitied()
        {
            using (BaseDbContext db = new BaseDbContext())
            {
                return db.Users.Find(User.Identity.GetUserId()).Identitied;
            }
        }

        public enum ManageMessageId
        {
            AddEducationSuccess,
            AddWorkSuccess,
            UpdateUserProfileSuccess,
            ChangePasswordSuccess,
            UserIdentitySuccess,
            ProjectSuccess,
            ApplySuccess,
            RecruitSuccess,
            OperationSuccess,
            AcessDenied,
            Error
        }

        #endregion
    }
}