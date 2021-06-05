using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Senparc.Xncf.CodeBuilder.Models.DatabaseModel.Dto;
using Senparc.Xncf.CodeBuilder.Services;
using System;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Utility;
using System.Linq;
using Senparc.Xncf.CodeBuilder.Request;
using Infrastructure;
using Senparc.Areas.Admin.Areas.Admin.Pages;

namespace Senparc.Xncf.CodeBuilder.Areas.Admin.Pages.Template
//namespace Senparc.Xncf.CodeBuilder.Areas.CodeBuilder.Pages
{

    public class TableIndexModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public MultipleDatabaseType MultipleDatabaseType { get; set; }
        private readonly IServiceProvider _serviceProvider;
        private readonly BuilderTableService _builderTableService;
        private readonly UserBuilderLogService _userBuilderLogService;

        public TableIndexModel(IServiceProvider serviceProvider, BuilderTableService builderTableService, UserBuilderLogService userBuilderLogService, Lazy<XncfModuleService> xncfModuleService)
            : base(xncfModuleService)
        {
            var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
            var currentDatabaseConfiguration = databaseConfigurationFactory.Current;
            MultipleDatabaseType = currentDatabaseConfiguration.MultipleDatabaseType;
            _serviceProvider = serviceProvider;
            _builderTableService = builderTableService;
            _userBuilderLogService = userBuilderLogService;
        }
        /// <summary>
        /// Handler=Save
        /// </summary>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("role-add", "role-edit")]
        public async Task<IActionResult> OnPostSaveAsync([FromBody] CreateEntityReq builderTableDto)
        {
            _builderTableService.CreateEntity(builderTableDto);
            return Ok(true);
        }

        #region 数据表生成Begin

        /// <summary>
        /// 创建一个代码生成的模版
        /// <para>会自动创建字段明细信息，添加成功后使用BuilderTableColumnsController.Load加载字段明细</para>
        /// <returns>返回添加的模版ID</returns>
        /// </summary>
        public async Task<IActionResult> OnPostAddAsync([FromBody] AddOrUpdateBuilderTableReq builderTableDto)
        {
            var result = new Response<string>();
            try
            {
                result.Result = _builderTableService.Add(builderTableDto);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.InnerException?.Message ?? ex.Message;
            }

            return Ok(result);
        }

        /// <summary>
        /// 只修改表信息，不会更新明细
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostUpdateAsync([FromBody] AddOrUpdateBuilderTableReq obj)
        {
            var result = new Response();
            try
            {
                _builderTableService.Update(obj);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.InnerException?.Message ?? ex.Message;
            }
            return Ok(result);
        }
        /// <summary>
        /// Handler=List
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="orderField">XXX DESC,YYY ASC</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("role-search")]
        public async Task<IActionResult> OnGetListAsync(string keyword, string orderField, int pageIndex, int pageSize)
        {
            orderField = "Id Asc";
            var seh = new SenparcExpressionHelper<BuilderTable>();
            seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(keyword), _ => _.Remark.Contains(keyword));
            var where = seh.BuildWhereExpression();
            var result = await _builderTableService.GetObjectListAsync(pageIndex, pageSize, where, orderField);
            return Ok(new
            {
                result.TotalCount,
                result.PageIndex,
                List =
                result.Select(_ => new { _.Id, _.Namespace, _.TableName, _.ClassName, _.Comment, _.ModuleCode, _.ModuleName, _.Folder, _.LastUpdateTime, _.Remark, _.AddTime, _.AdminRemark })
            });
        }
        /// <summary>
        /// 批量删除代码生成模版和对应的字段信息
        /// </summary>
        public async Task<IActionResult> OnPostDeleteAsync([FromBody] int[] ids)
        {
            var result = new Response();
            try
            {
                _builderTableService.DelTableAndcolumns(ids);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.InnerException?.Message ?? ex.Message;
            }

            return Ok(result);
        }



        /// <summary>
        /// 一键生成所有代码
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostCreateCodeAsync([FromBody] CreateEntityReq obj)
        {
            var result = new Response();
            BuilderTable entity=new BuilderTable();
            try
            {
                entity = _builderTableService.GetObject(x => x.Id == obj.Id);
                CreateBusiReq createBusiReq = new CreateBusiReq { Id = obj.Id };
                CreateVueReq createVueReq = new CreateVueReq { Id = obj.Id };
                _builderTableService.CreateEntity(obj);
                _builderTableService.CreateBusiness(createBusiReq);
                _builderTableService.CreateVue(createVueReq);
                _builderTableService.CreateJs(createVueReq);
                _builderTableService.CreateCss(createVueReq);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.InnerException?.Message ?? ex.Message;
            }
            finally
            {
                AddOrUpdateUserBuilderLogReq addOrUpdateUserBuilderLogReq = new AddOrUpdateUserBuilderLogReq();
                addOrUpdateUserBuilderLogReq.Id = 0;
                addOrUpdateUserBuilderLogReq.TableName = entity.TableName;
                addOrUpdateUserBuilderLogReq.ModuleName = entity.ModuleName;
                await _userBuilderLogService.CreateOrUpdateAsync(addOrUpdateUserBuilderLogReq);
            }
            return Ok(result);
        }

        /// <summary>
        /// 创建实体
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostCreateEntityAsync([FromBody] CreateEntityReq obj)
        {
            var result = new Response();
            try
            {
                _builderTableService.CreateEntity(obj);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.InnerException?.Message ?? ex.Message;
            }
            return Ok(result);
        }

        /// <summary>
        /// 创建业务逻辑层
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostCreateBusinessAsync([FromBody] CreateBusiReq obj)
        {
            var result = new Response();
            try
            {
                _builderTableService.CreateBusiness(obj);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.InnerException?.Message ?? ex.Message;
            }
            return Ok(result);
        }

        /// <summary>
        /// 创建vue界面
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostCreateVueAsync([FromBody] CreateVueReq obj)
        {
            var result = new Response();
            try
            {
                _builderTableService.CreateVue(obj);
                _builderTableService.CreateJs(obj);
                _builderTableService.CreateCss(obj);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.InnerException?.Message ?? ex.Message;
            }

            return Ok(result);
        }

        #endregion 数据表生成End
    }
}
