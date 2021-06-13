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
    public class CourseCommentService : ServiceBase<CourseComment>
    {
        public CourseCommentService(IRepositoryBase<CourseComment> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }
        //TODO: 更多业务方法可以写到这里
        public async Task CreateOrUpdateAsync(AddOrUpdateCourseCommentReq req)
        {
            CourseComment entity;
            if (req.Id == 0)
            {
                entity = new CourseComment(req);
                entity.AddTime = DateTime.Now;
            }
            else
            {
                entity = await GetObjectAsync(_ => _.Id == req.Id);
                       entity.AccountId= req.AccountId;
       entity.CourseId= req.CourseId;
       entity.original_content= req.original_content;
       entity.render_content= req.render_content;
       entity.deleted_at= req.deleted_at;
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
