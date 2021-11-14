using System;
using Senparc.Core.Models;
using Senparc.Repository;
using Senparc.Ncf.Log;

namespace Senparc.Service
{
    public class FeedBackService : BaseClientService<FeedBack>
    {
        public FeedBackService(FeedBackRepository repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        public FeedBack CreateOrUpdate( string content, int userId, int id = 0)
        {
            var obj = GetObject(z => z.Id == id) ?? new FeedBack()
            {
                AccountId = userId,
                Content = content,
                AddTime = DateTime.Now
            };
            SaveObject(obj);
            return obj;
        }

        public override void SaveObject(FeedBack obj)
        {
            var isInsert = base.IsInsert(obj);
            base.SaveObject(obj);
            LogUtility.WebLogger.InfoFormat("{1}数据（ID：{0}）", obj.Id, isInsert ? "新增" : "编辑");
        }

        public override void DeleteObject(FeedBack obj)
        {
            obj.Flag = true;
            base.SaveObject(obj);
            LogUtility.WebLogger.Info($"删除数据：（ID：{obj.Id}）");
        }
    }
}