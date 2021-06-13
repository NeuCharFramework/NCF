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
    public class CourseCategoryService : ServiceBase<CourseCategory>
    {
        public CourseCategoryService(IRepositoryBase<CourseCategory> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }
        //TODO: 更多业务方法可以写到这里
        public async Task CreateOrUpdateAsync(AddOrUpdateCourseCategoryReq req)
        {
            CourseCategory entity;
            if (req.Id == 0)
            {
                entity = new CourseCategory(req);
                entity.AddTime = DateTime.Now;
            }
            else
            {
                entity = await GetObjectAsync(_ => _.Id == req.Id);
                entity.name = req.name;
                entity.parent_id = req.parent_id;
                entity.parent_name = req.parent_name;
                entity.is_show = req.is_show;
                entity.sort = req.sort;
                entity.AdminRemark = req.AdminRemark;
                entity.Remark = req.Remark;
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
