/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：GetDefaultInstallOptionsResponseDto.cs
    文件功能描述：安装器域模型数据传输对象


    创建标识：Senparc - 20260916

    修改标识：Senparc - 20260916
    修改描述：v0.5.7 整理安装器 DTO 归属并保持安装服务接口兼容

----------------------------------------------------------------*/

using System.Collections.Generic;

namespace Senparc.Xncf.Installer.Domain.Dto
{
    public class GetDefaultInstallOptionsResponseDto
    {
        public string SystemName { get; set; }
        public string AdminUserName { get; set; }
        public string DbConnectionString { get; set; }
        public List<XncfRegisterDto> NeedModelList { get; set; } //需要安装的模块列表
    }
}
