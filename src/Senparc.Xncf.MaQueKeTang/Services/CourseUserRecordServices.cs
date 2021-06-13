using System;
using System.Linq;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.MaQueKeTang;
using Senparc.Xncf.MaQueKeTang.Request;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class CourseUserRecordService : ServiceBase<CourseUserRecord>
    {
        public CourseUserRecordService(IRepositoryBase<CourseUserRecord> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }
        //TODO: 更多业务方法可以写到这里
        public async Task CreateOrUpdateAsync(AddOrUpdateCourseUserRecordReq req)
        {
            CourseUserRecord entity;
            if (req.Id == 0)
            {
                entity = new CourseUserRecord(req);
                entity.AddTime = DateTime.Now;
            }
            else
            {
                entity = await GetObjectAsync(_ => _.Id == req.Id);
                       entity.CourseId= req.CourseId;
       entity.AccountId= req.AccountId;
       entity.deleted_at= req.deleted_at;
       entity.is_watched= req.is_watched;
       entity.watched_at= req.watched_at;
       entity.progress= req.progress;
       entity.Flag= req.Flag;
       entity.AddTime= req.AddTime;
       entity.LastUpdateTime= req.LastUpdateTime;
       entity.AdminRemark= req.AdminRemark;
       entity.Remark= req.Remark;
       entity.TenantId= req.TenantId;

                entity.LastUpdateTime = DateTime.Now;
            }
            await SaveObjectAsync(entity);
        }


        public void DeleteObject(int id)
        {
            var obj = GetObject(z => z.Id == id);
            if (obj == null)
            {
                throw new Exception("信息不存在！");
            }
            DeleteObject(obj);
        }


    }
}
