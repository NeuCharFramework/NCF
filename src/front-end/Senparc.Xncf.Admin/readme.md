## 文件夹说明

NeuCharFramework 前端代码源码文件夹


# Senparc.Xncf.OpenAI模块使用步骤

Senparc.Xncf.OpenAI 模块地址：https://github.com/NeuCharFramework/Senparc.Xncf.OpenAI

1. 打开 NCF 后端代码。路径：NCF\src\back-end。双击 NCF.sln 文件，运行后端代码

2. Senparc.Xncf.OpenAI 模块安装依赖。命令：npm install 或者 cnpm install（若依赖已安装好请进行下一步骤）。

3. Senparc.Xncf.OpenAI 模块打包。命令：npm run build:module

4. Senparc.Xncf.OpenAI 打包完成后出现的 dist 文件放置 Senparc.Xncf.Admin 模块下。放置路径：NCF\src\front-end\Senparc.Xncf.Admin\public

5. Senparc.Xncf.Admin 进行依赖安装 ：npm install 或者 cnpm install（若依赖已安装好请进行下一步骤）。

6. 运行 Senparc.Xncf.Admin 模块。命令：npm run dev

7. Senparc.Xncf.Admin 模块登录。进入路由 /Module/home 。点击加载模块，加载 Senparc.Xncf.OpenAI 分布式路由。

8. 使用 NCF 分布式 Senparc.Xncf.OpenAI 模块。