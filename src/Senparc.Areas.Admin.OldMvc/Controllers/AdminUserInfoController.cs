using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Models.VD;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Mvc;
using Senparc.Mvc.Filter;
using Senparc.Ncf.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Senparc.Ncf.Service;
using Senparc.Core.Models;

namespace Senparc.Areas.Admin.Controllers
{
    [MenuFilter("AdminUserInfo")]
    public class AdminUserInfoController : BaseAdminController
    {
        private readonly AdminUserInfoService _adminUserInfoService;


        public AdminUserInfoController(AdminUserInfoService adminUserInfoService)
        {
            _adminUserInfoService = adminUserInfoService;
        }

        public ActionResult Index(int pageIndex = 1)
        {
            var seh = new SenparcExpressionHelper<AdminUserInfo>();
            var where = seh.BuildWhereExpression();

            var admins = _adminUserInfoService.GetObjectList(pageIndex, 20, where, z => z.Id, OrderingType.Descending);
            var vd = new AdminUserInfo_IndexVD()
            {
                AdminUserInfoList = admins
            };
            return View(vd);
        }
        public ActionResult Edit(int id = 0)
        {
            bool isEdit = id > 0;
            var vd = new AdminUserInfo_EditVD();
            if (isEdit)
            {
                var userInfo = _adminUserInfoService.GetAdminUserInfo(id);
                if (userInfo == null)
                {
                    return RenderError("信息不存在！");
                }
                vd.UserName = userInfo.UserName;
                vd.Note = userInfo.Note;
                vd.Id = userInfo.Id;
            }
            vd.IsEdit = isEdit;
            return View(vd);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(AdminUserInfo_EditVD model)
        {

            bool isEdit = model.Id > 0;
            this.Validator(model.UserName, "用户名", "UserName", false)
                .IsFalse(z => this._adminUserInfoService.CheckUserNameExisted(model.Id, z), "用户名已存在！", true);

            if (!isEdit || !model.Password.IsNullOrEmpty())
            {
                this.Validator(model.Password, "密码", "Password", false).MinLength(6);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            AdminUserInfo userInfo = null;
            if (isEdit)
            {
                userInfo = _adminUserInfoService.GetAdminUserInfo(model.Id);
                if (userInfo == null)
                {
                    return RenderError("信息不存在！");
                }
            }
            else
            {
                var passwordSalt = DateTime.Now.Ticks.ToString();
                userInfo = new AdminUserInfo()
                {
                    PasswordSalt = passwordSalt,
                    LastLoginTime = DateTime.Now,
                    ThisLoginTime = DateTime.Now,
                    AddTime = DateTime.Now,
                };
            }

            if (!model.Password.IsNullOrEmpty())
            {
                userInfo.Password = this._adminUserInfoService.GetPassword(model.Password, userInfo.PasswordSalt, false);//生成密码
            }

            await this.TryUpdateModelAsync(userInfo, "", z => z.Note, z => z.UserName);
            this._adminUserInfoService.SaveObject(userInfo);

            base.SetMessager(MessageType.success, $"{(isEdit ? "修改" : "新增")}成功！");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Delete(List<int> ids)
        {
            var userInfoList = _adminUserInfoService.GetAdminUserInfo(ids);
            _adminUserInfoService.DeleteAll(userInfoList);
            SetMessager(MessageType.success, "删除成功！");
            return RedirectToAction("Index");
        }
    }
}
