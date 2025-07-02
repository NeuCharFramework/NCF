# NCF Release 发布流程

## 🚀 如何创建Release

### 1. 创建Release PR的要求

要触发自动GitHub Release，PR必须满足以下条件：

- ✅ **目标分支**: Pull Request到 `master` 分支
- ✅ **PR标题**: 必须包含 `[Release]` 标签（不区分大小写）
- ✅ **构建成功**: 所有CI/CD检查通过

### 2. PR标题示例

**✅ 正确的PR标题：**
```
[Release] NCF v0.29.7 - 修复macOS兼容性问题
[RELEASE] 新增用户权限管理模块 v0.30.0
修复关键Bug并发布 [Release] v0.29.8
[release] 性能优化更新
```

**❌ 错误的PR标题（不会触发Release）：**
```
修复数据库连接问题
更新依赖包版本
NCF v0.29.7 发布  # 缺少[Release]标签
```

### 3. 发布流程

1. **准备工作**
   - 确保所有功能开发完成
   - 更新版本号（在 `src/NCF.Template.csproj` 中的 `PackageVersion`）
   - 更新 CHANGELOG 或 Release Notes

2. **创建PR**
   - 从开发分支创建到 `master` 的PR
   - **重要**: 在PR标题中包含 `[Release]` 标签
   - 填写详细的PR描述，说明变更内容

3. **Review和合并**
   - 等待Code Review完成
   - 确保CI/CD构建通过
   - 合并PR到master分支

4. **自动发布**
   - CI/CD会自动检测到`[Release]`标签
   - 自动构建多平台版本（Windows, Linux, macOS）
   - 自动创建GitHub Release
   - 自动发布NuGet包
   - 自动部署到Web站点

### 4. Release资产说明

每个Release会自动生成以下平台的构建包：

- `ncf-win-x64-v*.zip` - Windows 64位
- `ncf-win-arm64-v*.zip` - Windows ARM64 (Surface Pro X等)
- `ncf-linux-x64-v*.zip` - Linux 64位
- `ncf-linux-arm64-v*.zip` - Linux ARM64
- `ncf-osx-x64-v*.zip` - macOS Intel
- `ncf-osx-arm64-v*.zip` - macOS Apple Silicon

### 5. macOS特殊支持

每个macOS包都包含：
- `start-ncf-macos.sh` - 自动化启动脚本
- `MACOS_USAGE_GUIDE.md` - 详细使用指南
- 自动处理权限和编码问题

### 6. 故障排除

#### 问题：PR没有触发Release
**检查清单：**
- [ ] PR标题是否包含`[Release]`？
- [ ] 是否目标到`master`分支？
- [ ] CI/CD构建是否成功？

#### 问题：构建失败
**常见原因：**
- NuGet包版本冲突
- 测试失败
- 代码编译错误

查看Azure DevOps构建日志获取详细错误信息。

---

## 📋 版本管理

### 版本号格式
使用语义化版本号：`MAJOR.MINOR.PATCH[-PRERELEASE]`

**示例：**
- `0.29.7` - 正式版本
- `0.30.0-preview1` - 预览版本
- `1.0.0-beta` - Beta版本

### 更新版本号
编辑 `src/NCF.Template.csproj` 文件中的 `PackageVersion` 字段：

```xml
<PackageVersion>0.29.7</PackageVersion>
```

---

**注意**: 此流程适用于NCF v0.29.7+版本。对于早期版本，请参考相应分支的文档。 