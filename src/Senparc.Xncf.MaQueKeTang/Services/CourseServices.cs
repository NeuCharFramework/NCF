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
    public class CourseService : ServiceBase<Course>
    {
        public CourseService(IRepositoryBase<Course> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }
        //TODO: 更多业务方法可以写到这里
        public async Task CreateOrUpdateAsync(AddOrUpdateCourseReq req)
        {
            Course entity;
            if (req.Id == 0)
            {
                entity = new Course(req);
                entity.AddTime = DateTime.Now;
            }
            else
            {
                entity = await GetObjectAsync(_ => _.Id == req.Id);
                entity.AccountId = req.AccountId;
                entity.title = req.title;
                entity.slug = req.slug;
                entity.thumb = req.thumb;
                entity.charge = req.charge;
                entity.short_description = req.short_description;
                entity.original_desc = req.original_desc;
                entity.render_desc = req.render_desc;
                entity.seo_keywords = req.seo_keywords;
                entity.seo_description = req.seo_description;
                entity.published_at = req.published_at;
                entity.is_show = req.is_show;
                entity.category_id = req.category_id;
                entity.is_rec = req.is_rec;
                entity.user_count = req.user_count;
                entity.is_free = req.is_free;
                entity.comment_status = req.comment_status;
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
