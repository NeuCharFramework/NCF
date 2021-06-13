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
    public class VideoUserService : ServiceBase<VideoUser>
    {
        public VideoUserService(IRepositoryBase<VideoUser> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }
        //TODO: 更多业务方法可以写到这里
        public async Task CreateOrUpdateAsync(AddOrUpdateVideoUserReq req)
        {
            VideoUser entity;
            if (req.Id == 0)
            {
                entity = new VideoUser(req);
                entity.AddTime = DateTime.Now;
            }
            else
            {
                entity = await GetObjectAsync(_ => _.Id == req.Id);
                       entity.Id= req.Id;
       entity.UserId= req.UserId;
       entity.VideoId= req.VideoId;
       entity.Charge= req.Charge;
       entity.Flag= req.Flag;
       entity.AddTime= req.AddTime;
       entity.AddUserId= req.AddUserId;
       entity.AddUserName= req.AddUserName;
       entity.LastUpdateTime= req.LastUpdateTime;
       entity.UpdateUserId= req.UpdateUserId;
       entity.UpdateUserName= req.UpdateUserName;
       entity.AdminRemark= req.AdminRemark;
       entity.Remark= req.Remark;
       entity.TenantId= req.TenantId;
       entity.deleted_at= req.deleted_at;

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
