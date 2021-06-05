using Infrastructure;
using Infrastructure.Extensions;
using Infrastructure.Helpers;
using Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.CodeBuilder.Request;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.CodeBuilder.Services
{
    public class BuilderTableService : ServiceBase<BuilderTable>
    {
        private readonly BuilderTableColumnService __builderTableColumnService;
        private CategoryService _categoryService;
        private DbExtension _dbExtension;
        private DbContext _context;
        //private string[] NoShowColumnAttr = { "flag", "deleted_at", "tenantid" };
        //private string[] NoBuildColumnAttr = { "addtime", "adduserid","addusername", "lastupdatetime", "updateuserid", "updateusername", "flag", "deleted_at", "tenantid" };

        public BuilderTableService(IRepositoryBase<BuilderTable> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
            __builderTableColumnService = _serviceProvider.GetService(typeof(BuilderTableColumnService)) as BuilderTableColumnService;
            _categoryService = _serviceProvider.GetService(typeof(CategoryService)) as CategoryService;
            _dbExtension = new DbExtension(__builderTableColumnService.BaseData.BaseDB.BaseDataContext);
        }
        private string _webProject = null;
        private string _apiNameSpace = null;
        private string _startName = "";
        private IOptions<AppSetting> _appConfiguration;

        private string WebProject
        {
            get
            {
                if (_webProject != null)
                    return _webProject;
                _webProject = ProjectPath.GetLastIndexOfDirectoryName(".WebApi") ??
                             ProjectPath.GetLastIndexOfDirectoryName("Api") ??
                             ProjectPath.GetLastIndexOfDirectoryName(".Mvc");
                if (_webProject == null)
                {
                    throw new Exception("未获取到以.WebApi结尾的项目名称,无法创建页面");
                }

                return _webProject;
            }
        }

        /// <summary>
        /// 导入数据库表结构
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public string Add(AddOrUpdateBuilderTableReq req)
        {
            if (string.IsNullOrEmpty(req.TableName))
            {
                throw new Exception("英文表名不能为空");
            }

            if (string.IsNullOrEmpty(req.ModuleName))
            {
                throw new Exception("模块名称不能为空");
            }

            if (string.IsNullOrEmpty(req.Namespace))
            {
                throw new Exception("命名空间不能为空");
            }
            var Connection = __builderTableColumnService.BaseData.BaseDB.BaseDataContext.Database.GetDbConnection();

            var columns = _dbExtension.GetDbTableStructure(req.TableName);
            if (!columns.Any())
            {
                throw new Exception($"未能找到{req.TableName}表结构定义");
            }

            var obj = req.MapTo<BuilderTable>();
            if (string.IsNullOrEmpty(obj.ClassName)) obj.ClassName = obj.TableName;
            if (string.IsNullOrEmpty(obj.ModuleCode)) obj.ModuleCode = obj.TableName;

            //todo:补充或调整自己需要的字段
            obj.CreateTime = DateTime.Now;
            //var user = _auth.GetCurrentUser().User;
            //obj.CreateUserId = user.Id;
            //obj.CreateUserName = user.Name;
            SaveObject(obj);

            foreach (var column in columns)
            {
                var builderColumn = new BuilderTableColumn
                {
                    ColumnName = column.ColumnName,
                    Comment = column.Comment,
                    ColumnType = column.ColumnType,
                    EntityType = column.EntityType,
                    EntityName = column.ColumnName,

                    IsKey = column.IsKey == 1,
                    IsRequired = column.IsNull != 1,
                    IsEdit = true,
                    IsInsert = true,
                    IsList = column.IsDisplay == 1,
                    MaxLength = column.MaxLength,
                    TableName = obj.TableName,
                    TableId = obj.Id,

                    //CreateUserId = user.Id,
                    //CreateUserName = user.Name,
                    CreateTime = DateTime.Now
                };
                __builderTableColumnService.SaveObject(builderColumn);
            }

            //UnitWork.Save();
            return obj.Id.ToString();
        }

        /// <summary>
        /// 更新表信息
        /// </summary>
        /// <param name="obj"></param>
        public void Update(AddOrUpdateBuilderTableReq obj)
        {

            var objDto = this.GetObject(z => z.Id == obj.Id);
            if (obj == null)
            {
                throw new Exception("信息不存在！");
            }
            objDto.TableName = obj.TableName;
            objDto.Comment = obj.Comment;
            objDto.DetailTableName = obj.DetailTableName;
            objDto.DetailComment = obj.DetailComment;
            objDto.ClassName = obj.ClassName;
            objDto.Namespace = obj.Namespace;
            objDto.ModuleCode = obj.ModuleCode;
            objDto.ModuleName = obj.ModuleName;
            objDto.Folder = obj.Folder;
            objDto.Options = obj.Options;
            objDto.TypeId = obj.TypeId;
            objDto.TypeName = obj.TypeName;
            objDto.UpdateTime = DateTime.Now;
            SaveObject(objDto);
        }

        /// <summary>
        /// 删除头和字段明细
        /// </summary>
        /// <param name="ids"></param>
        public void DelTableAndcolumns(int[] ids)
        {
            DeleteAllAsync(u => ids.ToList().Contains(u.Id));
            __builderTableColumnService.DeleteAllAsync(u => ids.ToList().Contains(u.TableId));

            //BeginTransaction(() =>
            //{
            //    DeleteAllAsync(u => ids.ToList().Contains(u.Id));
            //    __builderTableColumnService.DeleteAllAsync(u => ids.ToList().Contains(u.TableId));
            //    //SaveObject(accountPayLog);
            //}, ex =>
            //{
            //    LogUtility.AccountPayLog.Error($"删除生成表失败，发生错误：{ex.Message}", ex);
            //});
        }

        /// <summary>
        /// 生成实体Model
        /// </summary> 
        /// <returns></returns>
        //public void CreateEntity(CreateEntityReq req)
        public void CreateEntity(CreateEntityReq req)
        {
            var sysTableInfo = GetObject(_ => _.Id == req.Id);
            var tableColumns = __builderTableColumnService.Find(req.Id);
            //var tableColumns = __builderTableColumnService.GetFullList(_ => _.Id.ToString() == req.Id, z => z.Id, OrderingType.Ascending);
            if (sysTableInfo == null
                || tableColumns == null
                || tableColumns.Count == 0)
                throw new Exception("未能找到正确的模版信息");

            //CheckExistsModule(sysTableInfo.ClassName);
            CreateEntityModel(tableColumns, sysTableInfo);
            CreateCto(tableColumns, sysTableInfo);
        }

        /// <summary>
        /// 创建业务逻辑层
        /// </summary>
        /// <returns></returns>
        public void CreateBusiness(CreateBusiReq req)
        {
            //var sysTableInfo = Repository.FirstOrDefault(u => u.Id == req.Id);
            var sysTableInfo = GetObject(u => u.Id == req.Id);
            var tableColumns = __builderTableColumnService.Find(req.Id);
            if (sysTableInfo == null
                || tableColumns == null
                || tableColumns.Count == 0)
                throw new Exception("未能找到正确的模版信息");

            //生成应用层
            GenerateApp(sysTableInfo, tableColumns);

            //生成应用层的请求参数
            GenerateAppReq(sysTableInfo, tableColumns);

            //生成WebApI接口
            GenerateWebApi(sysTableInfo, tableColumns);

            //生成创建数据库表的代码
            GenerateTable(sysTableInfo, tableColumns);
        }

        /// <summary>
        /// 创建应用层
        /// </summary>
        /// <param name="sysTableInfo"></param>
        /// <exception cref="Exception"></exception>
        private void GenerateApp(BuilderTable sysTableInfo, List<BuilderTableColumn> sysColumn)
        {
            //string appRootPath = ProjectPath.GetProjectDirectoryInfo()
            //    .GetDirectories().FirstOrDefault(x => x.Name.ToLower().EndsWith(".app"))?.FullName;
            string appRootPath =
      CO2NET.Utilities.ServerUtility.ContentRootMapPath("~/App_Data/TemplateCode/");
            if (string.IsNullOrEmpty(appRootPath))
            {
                throw new Exception("未找到openauth.app类库,请确认是否存在");
            }
            //CheckExistsModule(sysTableInfo.ModuleCode);

            StringBuilder updateBuilder = new StringBuilder();   //生成带参数构造函数初始化值
            sysColumn = sysColumn.OrderByDescending(c => c.Sort).ToList();
            foreach (BuilderTableColumn column in sysColumn)
            {
                if (column.IsKey) continue;
                updateBuilder.Append("       entity." + column.EntityName
                                                   + "= req." + column.EntityName
                                                   + ";\r\n");
            }

            string template = "BuildService.html";
            string domainContent = GetTemplate(template);
            //string domainContent = FileHelper.ReadFile(@"Template\\BuildService.html")
            domainContent = domainContent
                .Replace("{Namespace}", sysTableInfo.Namespace)
                .Replace("{TableName}", sysTableInfo.TableName)
                .Replace("{ModuleCode}", sysTableInfo.ModuleCode)
                .Replace("{LowerModuleCode}", sysTableInfo.ModuleCode.ToLower())
                .Replace("{ModuleName}", sysTableInfo.ModuleName)
                .Replace("{ClassName}", sysTableInfo.ClassName)
                .Replace("{updateBuilder}", updateBuilder.ToString());
            FileHelper.WriteFile(appRootPath + $"Services/", sysTableInfo.ModuleCode + "Services.cs", domainContent);
        }

        /// <summary>
        /// 生成APP层的请求参数
        /// </summary>
        /// <param name="sysTableInfo"></param>
        /// <param name="tableColumns"></param>
        private void GenerateAppReq(BuilderTable sysTableInfo, List<BuilderTableColumn> tableColumns)
        {
            //string appRootPath = ProjectPath.GetProjectDirectoryInfo()
            //    .GetDirectories().FirstOrDefault(x => x.Name.ToLower().EndsWith(".app"))?.FullName;
            string appRootPath =
      CO2NET.Utilities.ServerUtility.ContentRootMapPath("~/App_Data/TemplateCode/");
            if (string.IsNullOrEmpty(appRootPath))
            {
                throw new Exception("未找到openauth.app类库,请确认是否存在");
            }
            string domainContent;
            //生成列表请求参数
            string template = "BuildQueryReq.html";
            domainContent = GetTemplate(template);
            domainContent = domainContent
                .Replace("{Namespace}", sysTableInfo.Namespace)
                .Replace("{TableName}", sysTableInfo.TableName)
                .Replace("{ModuleCode}", sysTableInfo.ModuleCode)
                .Replace("{LowerModuleCode}", sysTableInfo.ModuleCode.ToLower())
                .Replace("{ModuleName}", sysTableInfo.ModuleName)
                .Replace("{ClassName}", sysTableInfo.ClassName);

            FileHelper.WriteFile(Path.Combine(appRootPath, "Request"), $"Query{sysTableInfo.ClassName}ListReq.cs", domainContent);

            //生成新增和编辑参数
            template = "BuildUpdateReq.html";
            domainContent = GetTemplate(template);
            StringBuilder attributeBuilder = new StringBuilder();
            var sysColumn = tableColumns.OrderByDescending(c => c.Sort).ToList();
            foreach (BuilderTableColumn column in sysColumn)
            {
                attributeBuilder.Append("/// <summary>");
                attributeBuilder.Append("\r\n");
                attributeBuilder.Append("       ///" + column.Comment + "");
                attributeBuilder.Append("\r\n");
                attributeBuilder.Append("       /// </summary>");
                attributeBuilder.Append("\r\n");

                string entityType = column.EntityType;
                if (!column.IsRequired && column.EntityType != "string")
                {
                    entityType = entityType + "?";
                }
                attributeBuilder.Append("       public " + entityType + " " + column.EntityName + " { get; set; }");
                attributeBuilder.Append("\r\n\r\n       ");
            }

            domainContent = domainContent
                .Replace("{ClassName}", sysTableInfo.ClassName)
                .Replace("{Namespace}", sysTableInfo.Namespace)
                .Replace("{AttributeList}", attributeBuilder.ToString());

            var tableAttr = new StringBuilder();
            tableAttr.Append("/// <summary>");
            tableAttr.Append("\r\n");
            tableAttr.Append("       ///" + sysTableInfo.Comment + "");
            tableAttr.Append("\r\n");
            tableAttr.Append("       /// </summary>");
            tableAttr.Append("\r\n");
            domainContent = domainContent.Replace("{AttributeManager}", tableAttr.ToString());

            FileHelper.WriteFile(Path.Combine(appRootPath, "Request"), $"AddOrUpdate{sysTableInfo.ClassName}Req.cs",
                domainContent);
        }

        /// <summary>
        /// 创建WebAPI接口
        /// </summary>
        /// <param name="sysTableInfo"></param>
        /// <exception cref="Exception"></exception>
        private void GenerateWebApi(BuilderTable sysTableInfo, List<BuilderTableColumn> sysColumn)
        {
            string domainContent;
            //string apiPath = ProjectPath.GetProjectDirectoryInfo()
            //    .GetDirectories().FirstOrDefault(x => x.Name.ToLower().EndsWith(".webapi"))?.FullName;
            string apiPath =
      CO2NET.Utilities.ServerUtility.ContentRootMapPath("~/App_Data/TemplateCode/");

            if (string.IsNullOrEmpty(apiPath))
            {
                throw new Exception("未找到webapi类库,请确认是存在weiapi类库命名以.webapi结尾");
            }

            sysColumn = sysColumn.OrderByDescending(c => c.Sort).ToList();
            StringBuilder constructionBuilder = new StringBuilder();   //生成构造函数初始化值

            foreach (BuilderTableColumn column in sysColumn)
            {
                constructionBuilder.Append("properties.Add(new KeyDescription { Key = " + "\"" + column.EntityName + "\""
                                                   + ", Description = " + "\"" + column.Comment + "\""
                                                   + ", Browsable = " + (column.IsList ? "true" : "false")
                                                   + ", Type = " + "\"" + column.EntityType + "\""
                                                   + "});\r\n");
            }
            var controllerName = "Index.cshtml";
            //CheckExistsModule(controllerName); //单元测试下无效，因为没有执行webapi项目
            var controllerPath = apiPath + $"Areas/Admin/Pages/{sysTableInfo.ClassName}/";
            string template = "BuildControllerApi.html";
            domainContent = GetTemplate(template);
            domainContent = domainContent
                .Replace("{Namespace}", sysTableInfo.Namespace)
                .Replace("{TableName}", sysTableInfo.TableName)
                .Replace("{ModuleCode}", sysTableInfo.ModuleCode)
                .Replace("{LowerModuleCode}", sysTableInfo.ModuleCode.ToLower())
                .Replace("{ModuleName}", sysTableInfo.ModuleName)
                .Replace("{ClassName}", sysTableInfo.ClassName)
                .Replace("{CamelCaseClassName}", sysTableInfo.ClassName.ToCamelCase())
                .Replace("{KeyDescriptionList}", constructionBuilder.ToString());
            FileHelper.WriteFile(controllerPath, controllerName + ".cs", domainContent);
        }
        /// <summary>
        /// 生成数据库表
        /// </summary>
        /// <param name="sysTableInfo"></param>
        /// <param name="sysColumn"></param>
        private void GenerateTable(BuilderTable sysTableInfo, List<BuilderTableColumn> sysColumn)
        {
            string apiPath = CO2NET.Utilities.ServerUtility.ContentRootMapPath("~/App_Data/TemplateCode/");

            if (string.IsNullOrEmpty(apiPath))
            {
                throw new Exception("未找到webapi类库,请确认是存在weiapi类库命名以.webapi结尾");
            }
            sysColumn = sysColumn.OrderByDescending(c => c.Sort).ToList();
            StringBuilder attributeBuilder = new StringBuilder();
            attributeBuilder.Append("//将以下内容放在项目的Migrations/Migrations.你所使用的数据库/AddSample文件中");
            attributeBuilder.Append("\r\n");
            attributeBuilder.Append("------------------------------");
            attributeBuilder.Append("\r\n");
            attributeBuilder.Append("migrationBuilder.CreateTable(");
            attributeBuilder.Append("\r\n");
            attributeBuilder.Append("name: " + "\"" + sysTableInfo.TableName + "\",");
            attributeBuilder.Append("\r\n");
            attributeBuilder.Append("columns: table => new");
            attributeBuilder.Append("\r\n");
            attributeBuilder.Append("{");
            attributeBuilder.Append("Id = table.Column<int>(nullable: false, comment: \"Id\").Annotation(\"SqlServer: Identity\", \"1, 1\"),");
            string[] columnAttr = { "Id", "AddTime", "AddUserId", "AddUserName", "LastUpdateTime", "UpdateUserId", "UpdateUserName", "AdminRemark", "Remark", "TenantId", "Flag" };
            foreach (BuilderTableColumn column in sysColumn)
            {
                if (column.IsKey || columnAttr.Contains(column.EntityName)) continue;
                attributeBuilder.Append(column.EntityName + "= table.Column<" + column.EntityType + ">(" + (column.IsRequired ? "nullable:false" : "nullable: true") + (column.ColumnType.ToLower() == "string" && column.MaxLength != null ? ",maxLength: " + column.MaxLength : "") + ", comment: " + "\"" + column.Comment + "\"" + "),");
                attributeBuilder.Append("\r\n");
            }
            if (sysColumn.Select(s => s.EntityName).Contains("AddTime")) attributeBuilder.Append("AddTime = table.Column<DateTime>(nullable: false, comment: \"添加时间\"),");
            attributeBuilder.Append("\r\n");
            if (sysColumn.Select(s => s.EntityName).Contains("AddUserId"))
            {
                attributeBuilder.Append("AddUserId = table.Column<string>(nullable: false, comment: \"添加者Id\"),");
                attributeBuilder.Append("\r\n");
            }
            if (sysColumn.Select(s => s.EntityName).Contains("AddUserName"))
            {
                attributeBuilder.Append("AddUserName = table.Column<string>(nullable: false, comment: \"添加者名称\"),");
                attributeBuilder.Append("\r\n");
            }
            if (sysColumn.Select(s => s.EntityName).Contains("LastUpdateTime")) attributeBuilder.Append("LastUpdateTime = table.Column<DateTime>(nullable: false, comment: \"最后更新时间\"),");
            attributeBuilder.Append("\r\n");
            if (sysColumn.Select(s => s.EntityName).Contains("UpdateUserId"))
            {
                attributeBuilder.Append("UpdateUserId = table.Column<string>(nullable: false, comment: \"更新者Id\"),");
                attributeBuilder.Append("\r\n");
            }
            if (sysColumn.Select(s => s.EntityName).Contains("UpdateUserName"))
            {
                attributeBuilder.Append("UpdateUserName = table.Column<string>(nullable: false, comment: \"更新者名称\"),");
                attributeBuilder.Append("\r\n");
            }
            if (sysColumn.Select(s => s.EntityName).Contains("AdminRemark")) attributeBuilder.Append("AdminRemark = table.Column<string>(nullable: false, comment: \"备注\"),");
            attributeBuilder.Append("\r\n");
            if (sysColumn.Select(s => s.EntityName).Contains("Remark")) attributeBuilder.Append("Remark = table.Column<string>(nullable: false, comment: \"说明\"),");
            attributeBuilder.Append("\r\n");
            if (sysColumn.Select(s => s.EntityName).Contains("TenantId")) attributeBuilder.Append("TenantId = table.Column<int>(nullable: false, comment: \"租户Id\"),");
            attributeBuilder.Append("\r\n");
            if (sysColumn.Select(s => s.EntityName).Contains("Flag")) attributeBuilder.Append("Flag = table.Column<bool>(nullable: false, comment: \"删除状态\")");

            attributeBuilder.Append("\r\n");
            attributeBuilder.Append("},");
            attributeBuilder.Append("\r\n");
            attributeBuilder.Append("constraints: table =>{");
            attributeBuilder.Append("\r\n");
            attributeBuilder.Append(" table.PrimaryKey(" + "\"PK_" + sysTableInfo.TableName + "\",x => x.Id);");
            attributeBuilder.Append("\r\n");
            attributeBuilder.Append("});");
            attributeBuilder.Append("\r\n");
            attributeBuilder.Append("------------------------------");
            attributeBuilder.Append("\r\n");
            attributeBuilder.Append("//将以下内容放在项目的Models/DatabaseModel/SenparcEntities文件中");
            attributeBuilder.Append("\r\n");
            attributeBuilder.Append("------------------------------");
            attributeBuilder.Append("\r\n");
            attributeBuilder.Append("public DbSet<" + sysTableInfo.ClassName + "> " + sysTableInfo.ClassName + "s { get; set; }");
            attributeBuilder.Append("\r\n");
            attributeBuilder.Append("------------------------------");
            attributeBuilder.Append("\r\n");
            attributeBuilder.Append("//将以下内容放在项目的Register.Area.cs文件中");
            attributeBuilder.Append("\r\n");
            attributeBuilder.Append("------------------------------");
            attributeBuilder.Append("\r\n");
            attributeBuilder.Append("new AreaPageMenuItem(GetAreaUrl($" + "\"/Admin/" + sysTableInfo.ClassName + "/Index\"),\"" + sysTableInfo.ModuleName + "\"," + "\"" + "fa fa-laptop" + "\"),");
            attributeBuilder.Append("\r\n");
            attributeBuilder.Append("------------------------------");

            FileHelper.WriteFile(apiPath, sysTableInfo.ClassName + "_Migration.txt", attributeBuilder.ToString());
        }
        /// <summary>
        /// 创建Dto
        /// </summary>
        /// <param name="tableColumns"></param>
        /// <param name="sysTableInfo"></param>
        private void CreateCto(List<BuilderTableColumn> sysColumn, BuilderTable tableInfo)
        {
            string template = "BuildDto.html";
            string domainContent = GetTemplate(template);


            StringBuilder attributeBuilder = new StringBuilder();
            StringBuilder constructionBuilder = new StringBuilder();   //生成构造函数初始化值
            StringBuilder constructionBuilderReq = new StringBuilder();   //生成带参数构造函数初始化值
            sysColumn = sysColumn.OrderByDescending(c => c.Sort).ToList();
            foreach (BuilderTableColumn column in sysColumn)
            {
                if (column.IsKey) continue;

                attributeBuilder.Append("/// <summary>");
                attributeBuilder.Append("\r\n");
                attributeBuilder.Append("       ///" + column.Comment + "");
                attributeBuilder.Append("\r\n");
                attributeBuilder.Append("       /// </summary>");
                attributeBuilder.Append("\r\n");

                attributeBuilder.Append("       [Description(\"" + column.Comment + "\")]");
                attributeBuilder.Append("\r\n");

                string entityType = column.EntityType;
                if (!column.IsRequired && column.EntityType != "string")
                {
                    entityType = entityType + "?";
                }

                attributeBuilder.Append("       public " + entityType + " " + column.EntityName + " { get; set; }");
                attributeBuilder.Append("\r\n\r\n       ");
                string? defaultValue = GetDefault(column.EntityType);
                defaultValue = defaultValue == "False" ? "false" : defaultValue;
                constructionBuilder.Append("       this." + column.EntityName
                                                   + "=" + (defaultValue ?? "\"\"")
                                                   + ";\r\n");
                constructionBuilderReq.Append("       this." + column.EntityName
                                                   + "= req." + column.EntityName
                                                   + ";\r\n");
            }

            //获取的是本地开发代码所在目录，不是发布后的目录
            //string mapPath =
            //    ProjectPath.GetProjectDirectoryInfo()?.FullName; //new DirectoryInfo(("~/").MapPath()).Parent.FullName;
            string mapPath =
           CO2NET.Utilities.ServerUtility.ContentRootMapPath("~/App_Data/TemplateCode/");
            if (string.IsNullOrEmpty(mapPath))
            {
                throw new Exception("未找到生成的目录!");
            }

            domainContent = domainContent
                .Replace("{Namespace}", tableInfo.Namespace)
                .Replace("{ClassName}", tableInfo.ClassName)
                .Replace("{ModuleCode}", tableInfo.ModuleCode)
                .Replace("{LowerModuleCode}", tableInfo.ModuleCode.ToLower())
                .Replace("{ModuleName}", tableInfo.ModuleName)
                .Replace("{AttributeList}", attributeBuilder.ToString())
                .Replace("{Construction}", constructionBuilder.ToString())
                .Replace("{ConstructionReq}", constructionBuilderReq.ToString());



            var tableAttr = new StringBuilder();

            tableAttr.Append("/// <summary>");
            tableAttr.Append("\r\n");
            tableAttr.Append("///" + tableInfo.Comment + "实体类");
            tableAttr.Append("\r\n");
            tableAttr.Append("/// </summary>");
            tableAttr.Append("\r\n");
            //tableAttr.Append("       [Table(\"" + tableInfo.TableName + "\")]");
            domainContent = domainContent.Replace("{AttributeManager}", tableAttr.ToString());

            FileHelper.WriteFile(
                mapPath +
                $"\\Models\\DatabaseModel\\Dto\\", tableInfo.ClassName + "Dto.cs",
                domainContent);
        }
        /// <summary>
        /// 创建实体
        /// </summary>
        /// <param name="tableColumns"></param>
        /// <param name="sysTableInfo"></param>
        private void CreateEntityModel(List<BuilderTableColumn> sysColumn, BuilderTable tableInfo)
        {
            string template = "BuildModel.html";
            string domainContent = GetTemplate(template);


            StringBuilder attributeBuilder = new StringBuilder();
            StringBuilder constructionBuilder = new StringBuilder();   //生成构造函数初始化值
            StringBuilder constructionBuilderReq = new StringBuilder();   //生成带参数构造函数初始化值
            sysColumn = sysColumn.OrderByDescending(c => c.Sort).ToList();
            foreach (BuilderTableColumn column in sysColumn)
            {
                if (column.IsKey) continue;

                attributeBuilder.Append("/// <summary>");
                attributeBuilder.Append("\r\n");
                attributeBuilder.Append("       ///" + column.Comment + "");
                attributeBuilder.Append("\r\n");
                attributeBuilder.Append("       /// </summary>");
                attributeBuilder.Append("\r\n");

                attributeBuilder.Append("       [Description(\"" + column.Comment + "\")]");
                attributeBuilder.Append("\r\n");

                string entityType = column.EntityType;
                if (!column.IsRequired && column.EntityType != "string")
                {
                    entityType = entityType + "?";
                }

                attributeBuilder.Append("       public " + entityType + " " + column.EntityName + " { get; set; }");
                attributeBuilder.Append("\r\n\r\n       ");

                string? defaultValue = GetDefault(column.EntityType);
                defaultValue = defaultValue == "False" ? "false" : defaultValue;

                constructionBuilder.Append("       this." + column.EntityName
                                                   + "=" + (defaultValue ?? "\"\"")
                                                   + ";\r\n");
                constructionBuilderReq.Append("       this." + column.EntityName
                                          + "= req." + column.EntityName
                                          + ";\r\n");
            }

            //获取的是本地开发代码所在目录，不是发布后的目录
            //string mapPath =
            //    ProjectPath.GetProjectDirectoryInfo()?.FullName; //new DirectoryInfo(("~/").MapPath()).Parent.FullName;
            string mapPath =
           CO2NET.Utilities.ServerUtility.ContentRootMapPath("~/App_Data/TemplateCode/");
            if (string.IsNullOrEmpty(mapPath))
            {
                throw new Exception("未找到生成的目录!");
            }

            domainContent = domainContent
                .Replace("{Namespace}", tableInfo.Namespace)
                .Replace("{ClassName}", tableInfo.ClassName)
                .Replace("{ModuleCode}", tableInfo.ModuleCode)
                .Replace("{LowerModuleCode}", tableInfo.ModuleCode.ToLower())
                .Replace("{ModuleName}", tableInfo.ModuleName)
                .Replace("{AttributeList}", attributeBuilder.ToString())
                .Replace("{Construction}", constructionBuilder.ToString())
                .Replace("{ConstructionReq}", constructionBuilderReq.ToString());


            var tableAttr = new StringBuilder();

            tableAttr.Append("/// <summary>");
            tableAttr.Append("\r\n");
            tableAttr.Append("///" + tableInfo.Comment + "实体类");
            tableAttr.Append("\r\n");
            tableAttr.Append("/// </summary>");
            tableAttr.Append("\r\n");
            //tableAttr.Append("       [Table(\"" + tableInfo.TableName + "\")]");
            domainContent = domainContent.Replace("{AttributeManager}", tableAttr.ToString());

            FileHelper.WriteFile(
                mapPath +
                $"\\Models\\DatabaseModel\\", tableInfo.ClassName + ".cs",
                domainContent);
        }
        /// <summary>
        /// 获取模板
        /// </summary>
        /// <param name="templateName"></param>
        /// <returns></returns>
        private string GetTemplate(string templateName)
        {
            string body = "";
            //读取模板
            using (var srEmailTemplate = new StreamReader(CO2NET.Utilities.ServerUtility.ContentRootMapPath("~/App_Data/Template/" + templateName), Encoding.UTF8))
            {
                string emailTemplate = srEmailTemplate.ReadToEnd();
                body = emailTemplate;
            }
            return body;
        }
        private bool IsMysql()
        {
            return (_appConfiguration.Value.DbType == Define.DBTYPE_MYSQL);
        }

        Dictionary<string, Type> PrimitiveTypes = new Dictionary<string, Type>()
        {
            {"int", typeof(int)}
            ,{"long", typeof(long)}
            ,{"string", typeof(string)}
            ,{"bool", typeof(bool)}
            ,{"byte", typeof(byte)}
            ,{"char", typeof(char)}
            ,{"decimal", typeof(decimal)}
            ,{"double", typeof(double)}
            ,{"DateTime", typeof(DateTime)}
        };
        string? GetDefault(string type)
        {
            Type t = PrimitiveTypes[type];
            if (t == null)
            {
                return null;
            }

            if (t.IsValueType)
            {
                if (type == "DateTime")
                {
                    return "DateTime.Now;";
                }
                return Activator.CreateInstance(t).ToString();
            }

            return null;
        }


        /// <summary>
        /// 校验模块是否已经存在
        /// </summary>
        /// <param name="moduleName"></param>
        /// <param name="moduleCode"></param>
        /// <exception cref="Exception"></exception>
        public void CheckExistsModule(string moduleCode)
        {
            //如果是第一次创建model，此处反射获取到的是已经缓存过的文件，必须重新运行项目否则新增的文件无法做判断文件是否创建，需要重新做反射实际文件，待修改...
            var compilationLibrary = DependencyContext
                .Default
                .CompileLibraries
                .Where(x => !x.Serviceable && x.Type == "project");
            foreach (var compilation in compilationLibrary)
            {
                //var types = AssemblyLoadContext.Default
                //    .LoadFromAssemblyName(new AssemblyName(compilation.Name))
                //    .GetTypes().Where(x => x.GetTypeInfo().BaseType != null
                //                           && x.BaseType == typeof(Entity));
                var types = AssemblyLoadContext.Default
                 .LoadFromAssemblyName(new AssemblyName(compilation.Name))
                 .GetTypes().Where(x => x.GetTypeInfo().BaseType != null
                                        && x.BaseType == typeof(EntityBase<int>));

                foreach (var entity in types)
                {
                    if (entity.Name == moduleCode)
                        throw new Exception($"实际表名【{moduleCode}】已创建实体，不能创建实体");

                    if (entity.Name != moduleCode)
                    {
                        var tableAttr = entity.GetCustomAttribute<TableAttribute>();
                        if (tableAttr != null && tableAttr.Name == moduleCode)
                        {
                            throw new Exception(
                                $"实际表名【{moduleCode}】已被创建建实体,不能创建");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 创建vue界面
        /// </summary>
        /// <param name="req"></param>
        /// <exception cref="Exception"></exception>
        public void CreateVue(CreateVueReq req)
        {
            if (string.IsNullOrEmpty(req.VueProjRootPath))
            {
                req.VueProjRootPath =
           CO2NET.Utilities.ServerUtility.ContentRootMapPath("~/App_Data/TemplateCode/");
                //throw new Exception("请提供vue项目的根目录,如：C:\\OpenAuth.Pro\\Client");
            }
            //var sysTableInfo = Repository.FirstOrDefault(u => u.Id == req.Id);
            var sysTableInfo = GetObject(u => u.Id == req.Id);
            var tableColumns = __builderTableColumnService.Find(req.Id);
            if (sysTableInfo == null
                || tableColumns == null
                || tableColumns.Count == 0)
                throw new Exception("未能找到正确的模版信息");

            //var domainContent = FileHelper.ReadFile(@"Template\\BuildCshtml.html");
            string template = "BuildCshtml.html";
            string domainContent = GetTemplate(template);

            StringBuilder dialogStrBuilder = new StringBuilder();   //编辑对话框
            StringBuilder tempBuilder = new StringBuilder();   //临时类的默认值属性
            var syscolums = tableColumns.OrderByDescending(c => c.Sort).ToList();

            string[] eidtTye = new string[] { "select", "selectList", "checkbox" };
            if (syscolums.Exists(x => eidtTye.Contains(x.EditType) && string.IsNullOrEmpty(x.DataSource)))
            {
                throw new Exception($"编辑类型为[{string.Join(',', eidtTye)}]时必须选择数据源");
            }

            foreach (BuilderTableColumn column in syscolums)
            {
                if (!column.IsEdit || column.IsKey) continue;

                tempBuilder.Append($"                    {column.ColumnName.ToCamelCase()}: ");
                dialogStrBuilder.Append($"                   <el-form-item size=\"small\" :label=\"'{column.Comment}'\" prop=\"{column.ColumnName.ToCamelCase()}\" v-if=\"Object.keys(dialog.data).indexOf('{column.ColumnName.ToCamelCase()}')>=0\">\r\n");
                if (column.EditType == "switch")
                {
                    dialogStrBuilder.Append($"                     <el-switch v-model=\"dialog.data.{column.ColumnName.ToCamelCase()}\" ></el-switch>\r\n");
                    tempBuilder.Append($"false, //{column.Comment} \r\n");
                }
                else if (column.EditType == "date")
                {
                    dialogStrBuilder.Append($"                     <el-date-picker  v-model=\"dialog.data.{column.ColumnName.ToCamelCase()}\" type=\"date\" placeholder=\"选择日期\"> </el-date-picker>\r\n");
                    tempBuilder.Append($"'', //{column.Comment} \r\n");
                }
                else if (column.EditType == "datetime")
                {
                    dialogStrBuilder.Append($"                     <el-date-picker  v-model=\"dialog.data.{column.ColumnName.ToCamelCase()}\" type=\"datetime\" placeholder=\"选择日期时间\"> </el-date-picker>\r\n");
                    tempBuilder.Append($"'', //{column.Comment} \r\n");
                }
                else if (column.EditType == "decimal")  //小数
                {
                    dialogStrBuilder.Append($"                     <el-input-number v-model=\"dialog.data.{column.ColumnName.ToCamelCase()}\" :min=\"1\" :max=\"100\" ></el-input-number>\r\n");
                    tempBuilder.Append($"0, //{column.Comment} \r\n");
                }
                else if (column.EditType == "number") //整数
                {
                    dialogStrBuilder.Append($"                     <el-input-number v-model=\"dialog.data.{column.ColumnName.ToCamelCase()}\" :min=\"1\" :max=\"100\" ></el-input-number>\r\n");
                    tempBuilder.Append($"0, //{column.Comment} \r\n");
                }
                else if (column.EditType == "textarea")
                {
                    dialogStrBuilder.Append($"                     <el-input type=\"textarea\" :rows=\"3\"  v-model=\"dialog.data.{column.ColumnName.ToCamelCase()}\"></el-input>\r\n");
                    tempBuilder.Append($"'', //{column.Comment} \r\n");
                }
                else if (column.EditType == "select")
                {
                    var categories = _categoryService.LoadByTypeId(column.DataSource);
                    if (categories.IsNullOrEmpty())
                    {
                        throw new Exception($"未能找到{column.DataSource}对应的值，请在分类管理里面添加");
                    }

                    dialogStrBuilder.Append($"                     <el-select v-model=\"dialog.data.{column.ColumnName.ToCamelCase()}\" placeholder=\"请选择\">\r\n");
                    foreach (var category in categories)
                    {
                        dialogStrBuilder.Append($"                          <el-option label=\"{category.Name}\" value=\"{category.DtValue}\"> </el-option>\r\n");
                    }
                    dialogStrBuilder.Append("                     </el-select>\r\n");
                    tempBuilder.Append($"'', //{column.Comment} \r\n");
                }
                else if (column.EditType == "checkbox")
                {
                    var categories = _categoryService.LoadByTypeId(column.DataSource);
                    if (categories.IsNullOrEmpty())
                    {
                        throw new Exception($"未能找到{column.DataSource}对应的值，请在分类管理里面添加");
                    }

                    dialogStrBuilder.Append($"                     <el-checkbox-group v-model=\"dialog.data.{column.ColumnName.ToCamelCase()}\">\r\n");
                    foreach (var category in categories)
                    {
                        dialogStrBuilder.Append($"                         <el-checkbox label=\"{category.DtValue}\"></el-checkbox>\r\n");
                    }
                    dialogStrBuilder.Append("                     </el-checkbox-group>\r\n");
                    tempBuilder.Append($"[], //{column.Comment} \r\n");
                }
                else
                {
                    dialogStrBuilder.Append($"                     <el-input v-model=\"dialog.data.{column.ColumnName.ToCamelCase()}\"></el-input>\r\n");
                    tempBuilder.Append($"'', //{column.Comment} \r\n");
                }

                dialogStrBuilder.Append("                   </el-form-item>\r\n");
                dialogStrBuilder.Append("\r\n");
            }

            tempBuilder.Append("                    nothing:''  //代码生成时的占位符，看不顺眼可以删除 \r\n");

            domainContent = domainContent
                .Replace("{Namespace}", sysTableInfo.Namespace)
                .Replace("{ClassName}", sysTableInfo.ClassName)
                .Replace("{TableName}", sysTableInfo.ClassName.ToCamelCase())
                .Replace("{ModuleCode}", sysTableInfo.ModuleCode)
                .Replace("{LowerModuleCode}", sysTableInfo.ModuleCode.ToLower())
                .Replace("{ModuleName}", sysTableInfo.ModuleName)
                .Replace("{Temp}", tempBuilder.ToString())
                .Replace("{DialogFormItem}", dialogStrBuilder.ToString());

            FileHelper.WriteFile(Path.Combine(req.VueProjRootPath, $"Areas/Admin/Pages/{sysTableInfo.ClassName}/"),
                $"Index.cshtml",
                domainContent);
        }

        /// <summary>
        /// 创建vue接口
        /// </summary>
        /// <param name="req"></param>
        /// <exception cref="Exception"></exception>
        public void CreateJs(CreateVueReq req)
        {
            //首先根据提供的项目根目录生成，如果没有则存放在TemplateCode地址下
            if (string.IsNullOrEmpty(req.VueProjRootPath))
            {
                req.VueProjRootPath = CO2NET.Utilities.ServerUtility.ContentRootMapPath("~/App_Data/TemplateCode/");
            }
            var sysTableInfo = GetObject(u => u.Id == req.Id);
            var tableColumns = __builderTableColumnService.Find(req.Id);
            if (sysTableInfo == null
                || tableColumns == null
                || tableColumns.Count == 0)
                throw new Exception("未能找到正确的模版信息");
            StringBuilder dialogDataInitBuilder = new StringBuilder();
            StringBuilder dialogDataLetBuilder = new StringBuilder();
            StringBuilder dialogDataHandleBuilder = new StringBuilder();
            StringBuilder requiredHandleBuilder = new StringBuilder();
            List<BuilderTableColumn> sysColumn = tableColumns.OrderByDescending(c => c.Sort).ToList();
            foreach (BuilderTableColumn column in sysColumn)
            {
                if (!column.IsEdit) continue;

                dialogDataInitBuilder.Append(column.EntityName.ToCamelCase()
                                                + " : " + (column.EntityType == "int" ? "0" : "\"\"")
                                                + ",\r\n");
                dialogDataLetBuilder.Append(column.EntityName.ToCamelCase() + ",");
                dialogDataHandleBuilder.Append(column.EntityName.ToCamelCase()
                                                 + " : this.dialog.data." + column.EntityName.ToCamelCase() + (column.EntityType == "int" ? "*1" : "")
                                                 + ",\r\n");
                if (column.IsRequired)
                {
                    requiredHandleBuilder.Append(column.EntityName.ToCamelCase() + ":[{required:true, message:\"" + column.Comment + "为必填项\",trigger:\"blur\"}],");
                    requiredHandleBuilder.Append("\r\n");
                }
            }

            string template = "BuildJs.html";
            string domainContent = GetTemplate(template);
            domainContent = domainContent
                .Replace("{Namespace}", sysTableInfo.Namespace)
                .Replace("{TableName}", sysTableInfo.TableName)
                .Replace("{ModuleCode}", sysTableInfo.ModuleCode)
                .Replace("{LowerModuleCode}", sysTableInfo.ModuleCode.ToLower())
                .Replace("{ModuleName}", sysTableInfo.ModuleName)
                .Replace("{ClassName}", sysTableInfo.ClassName)
                .Replace("{dialogDataInitBuilder}", dialogDataInitBuilder.ToString())
                .Replace("{dialogDataLetBuilder}", dialogDataLetBuilder.ToString())
                .Replace("{dialogDataHandleBuilder}", dialogDataHandleBuilder.ToString())
                .Replace("{requiredHandleBuilder}", requiredHandleBuilder.ToString());

            FileHelper.WriteFile(Path.Combine(req.VueProjRootPath, $"wwwroot/js/Admin/Pages/{sysTableInfo.ClassName}/"), $"Index.js",
                domainContent);
        }
        /// <summary>
        /// 创建样式
        /// </summary>
        /// <param name="req"></param>
        /// <exception cref="Exception"></exception>
        public void CreateCss(CreateVueReq req)
        {
            if (string.IsNullOrEmpty(req.VueProjRootPath))
            {
                req.VueProjRootPath = CO2NET.Utilities.ServerUtility.ContentRootMapPath("~/App_Data/TemplateCode/");
            }
            var sysTableInfo = GetObject(u => u.Id == req.Id);
            var tableColumns = __builderTableColumnService.Find(req.Id);
            if (sysTableInfo == null
                || tableColumns == null
                || tableColumns.Count == 0)
                throw new Exception("未能找到正确的模版信息");
            string template = "BuildCss.html";
            string domainContent = GetTemplate(template);
            domainContent = domainContent
                .Replace("{Namespace}", sysTableInfo.Namespace)
                .Replace("{TableName}", sysTableInfo.TableName)
                .Replace("{ModuleCode}", sysTableInfo.ModuleCode)
                .Replace("{LowerModuleCode}", sysTableInfo.ModuleCode.ToLower())
                .Replace("{ModuleName}", sysTableInfo.ModuleName)
                .Replace("{ClassName}", sysTableInfo.ClassName);

            FileHelper.WriteFile(Path.Combine(req.VueProjRootPath, $"wwwroot/css/Admin/Pages/{sysTableInfo.ClassName}/"), $"Index.css",
                domainContent);
        }
    }
}
