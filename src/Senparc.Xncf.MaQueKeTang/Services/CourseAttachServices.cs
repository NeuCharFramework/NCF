using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.MaQueKeTang;
using Senparc.Xncf.MaQueKeTang.Request;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class CourseAttachService : ServiceBase<CourseAttach>
    {
        public CourseAttachService(IRepositoryBase<CourseAttach> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }
        //TODO: 更多业务方法可以写到这里
        public async Task CreateOrUpdateAsync(AddOrUpdateCourseAttachReq req)
        {
            CourseAttach entity;
            if (req.Id == 0)
            {
                entity = new CourseAttach(req);
                entity.AddTime = DateTime.Now;
            }
            else
            {
                entity = await GetObjectAsync(_ => _.Id == req.Id);
                entity.CourseId = req.CourseId;
                entity.name = req.name;
                entity.path = req.path;
                entity.disk = req.disk;
                entity.size = req.size;
                entity.extension = req.extension;
                entity.only_buyer = req.only_buyer;
                entity.download_times = req.download_times;
                entity.Flag = req.Flag;
                entity.AddTime = req.AddTime;
                entity.LastUpdateTime = req.LastUpdateTime;
                entity.AdminRemark = req.AdminRemark;
                entity.Remark = req.Remark;
                entity.TenantId = req.TenantId;
                entity.deleted_at = req.deleted_at;

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

        public IList<CourseAttach> Upload(IFormFileCollection files)
        {
            throw new NotImplementedException();
        }
    }
}
