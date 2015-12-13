﻿using System;
using System.Data.Entity;
using System.Linq;

namespace Web.Models
{
    public class Article : IListPage
    {
        public Guid ID { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public ArticleStatus Status { get; set; }

        public ArticleClass Class { get; set; }

        public DateTime Time { get; set; }

        public void NewArticle()
        {
            ID = Guid.NewGuid();
            Time = DateTime.Now;
            Status = ArticleStatus.Enabled;
        }
    }

    public class Message
    {
        public Guid ID { get; set; }

        public User Publisher { get; set; }

        public User Receiver { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public MessageType Type { get; set; }

        public bool HaveRead { get; set; }

        public DateTime Time { get; set; }

        public Message(string title, string content, string userId, MessageType type, BaseDbContext db)
        {
            ID = Guid.NewGuid();
            Publisher = Extensions.GetContextUser(db);
            Receiver = db.Users.Find(userId);
            Title = title;
            Content = content;
            Type = type;
            HaveRead = false;
            Time = DateTime.Now;
        }

        public Message(string userId, MessageType type, MessageTemplate template, BaseDbContext db)
        {
            ID = Guid.NewGuid();
            Publisher = Extensions.GetContextUser(db);
            Receiver = db.Users.Find(userId);
            Type = type;
            HaveRead = false;
            Time = DateTime.Now;
            switch (template)
            {
                case MessageTemplate.TeamFailure:
                    Title = "";
                    Content = "";
                    break;
                default:
                    Title = "";
                    Content = "";
                    break;
            }
        }

        public Message(string userId, MessageType type, MessageTemplate template, string personal, BaseDbContext db)
        {
            ID = Guid.NewGuid();
            Publisher = Extensions.GetContextUser(db);
            Receiver = db.Users.Find(userId);
            Type = type;
            HaveRead = false;
            Time = DateTime.Now;
            switch (template)
            {
                case MessageTemplate.TeamFailure:
                    Title = "项目申请被驳回";
                    Content = "很遗憾，你的项目申请被管理员驳回，请返回项目申请页面查看，并对照管理员批复予以修改。";
                    break;
                case MessageTemplate.TeamApply:
                    Title = "新成员申请加入团队";
                    Content = "有新成员申请加入你的团队，请及时处理。";
                    break;
                default:
                    Title = "";
                    Content = "";
                    break;
            }
        }

        public bool Publish()
        {
            try
            {
                using (BaseDbContext db = new BaseDbContext())
                {
                    db.Messages.Add(this);
                    db.SaveChanges();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }

    public enum MessageType
    {
        System,
        Team,
        Person
    }

    public enum MessageTemplate
    {
        TeamFailure,
        TeamSuccess,
        TeamApply,
        TeamRecruit,
        ProjectFailure,
        ProjectSuccess,
        CompanyFailure,
        CompanySuccess,
    }

    public enum ArticleStatus
    {
        Enabled,
        Disabled
    }

    public enum ArticleClass
    {
        //随意加了用的，删除即可
        a,
        b
    }
}
