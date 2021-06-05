using System;
using System.Linq;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.CodeBuilder;
using Senparc.Xncf.CodeBuilder.Request;

namespace Senparc.Xncf.CodeBuilder
{
    public class UserBuilderLogService : ServiceBase<UserBuilderLog>
    {
        public UserBuilderLogService(IRepositoryBase<UserBuilderLog> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }
        //TODO: 更多业务方法可以写到这里
        public async Task CreateOrUpdateAsync(AddOrUpdateUserBuilderLogReq req)
        {
            UserBuilderLog entity;
            if (req.Id == 0)
            {
                entity = new UserBuilderLog(req);
                entity.AddTime = DateTime.Now;
            }
            else
            {
                entity = await GetObjectAsync(_ => _.Id == req.Id);
                entity.Flag = req.Flag;
                entity.AddTime = req.AddTime;
                entity.LastUpdateTime = req.LastUpdateTime;
                entity.AdminRemark = req.AdminRemark;
                entity.Remark = req.Remark;
                entity.AdditionNote = req.AdditionNote;
                entity.TenantId = req.TenantId;
                entity.UserId = req.UserId;
                entity.TableName = req.TableName;
                entity.ModuleName = req.ModuleName;
                entity.Path = req.Path;
                entity.Count = req.Count;

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
