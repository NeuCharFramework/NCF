/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LoginCaptchaService.cs
    文件功能描述：管理员登录图形验证码服务（纯 SVG 生成，无第三方依赖；答案存入 CO2NET 缓存，一次性、短时效）

    创建标识：Senparc - 20260926

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 add image captcha for admin login (required after 2 failed attempts)

----------------------------------------------------------------*/

using Senparc.CO2NET.Cache;
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    /// 管理员登录图形验证码：
    /// 1. 纯 C# 生成高干扰 SVG 验证码（字符随机旋转/位移/字号/颜色 + 干扰曲线 + 噪点），不依赖任何第三方组件；
    /// 2. 验证码答案通过 CO2NET 对象缓存保存（一次性、2 分钟时效），防止重放；
    /// 3. 同一用户名连续密码错误达到 <see cref="CaptchaRequiredFailedCount"/> 次后，登录必须通过图形验证码校验。
    /// </summary>
    public class LoginCaptchaService
    {
        /// <summary>
        /// 连续密码错误达到该次数后，登录需要输入图形验证码
        /// </summary>
        public const int CaptchaRequiredFailedCount = 2;

        private const string CacheKeyPrefix = "LoginCaptcha:";
        private static readonly TimeSpan CaptchaLifeTime = TimeSpan.FromMinutes(2);
        // 去除易混淆字符（0/O、1/I/L），大写 + 数字
        private const string CodeChars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        private const int CodeLength = 6;

        private readonly IBaseObjectCacheStrategy _cache;

        public LoginCaptchaService()
        {
            // 与 AdminUserInfoService 保持一致：直接取 CO2NET 对象缓存策略实例
            _cache = CO2NET.Cache.CacheStrategyFactory.GetObjectCacheStrategyInstance();
        }

        /// <summary>
        /// 验证码生成结果
        /// </summary>
        public class CaptchaModel
        {
            /// <summary>
            /// 验证码标识（登录请求时需要随表单提交）
            /// </summary>
            public string Token { get; set; }

            /// <summary>
            /// 形如 data:image/svg+xml;base64,... 的数据 URI，可直接用于 img 的 src
            /// </summary>
            public string Image { get; set; }
        }

        /// <summary>
        /// 生成新的图形验证码（答案写入缓存，一次性有效）
        /// </summary>
        public async Task<CaptchaModel> CreateAsync()
        {
            string code = GenerateCode();
            string token = Guid.NewGuid().ToString("N");
            await _cache.SetAsync(CacheKeyPrefix + token, code, CaptchaLifeTime);

            string svg = SvgCaptchaGenerator.Generate(code);
            return new CaptchaModel
            {
                Token = token,
                Image = "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg))
            };
        }

        /// <summary>
        /// 校验图形验证码（一次性：无论成功失败都会使该 Token 失效）
        /// </summary>
        /// <param name="token">CreateAsync 返回的 Token</param>
        /// <param name="input">用户输入的验证码</param>
        public async Task<bool> ValidateAsync(string token, string input)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            string key = CacheKeyPrefix + token.Trim();
            string code = await _cache.GetAsync<string>(key);
            await _cache.RemoveFromCacheAsync(key);

            if (string.IsNullOrEmpty(code))
            {
                return false;
            }

            // 忽略大小写比较，降低用户输入负担
            return string.Equals(code, input.Trim().ToUpperInvariant(), StringComparison.Ordinal);
        }

        private static string GenerateCode()
        {
            byte[] buffer = new byte[CodeLength];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }

            var builder = new StringBuilder(CodeLength);
            for (int i = 0; i < CodeLength; i++)
            {
                builder.Append(CodeChars[buffer[i] % CodeChars.Length]);
            }
            return builder.ToString();
        }
    }

    /// <summary>
    /// 高干扰 SVG 验证码渲染器（纯 C# 实现）：
    /// 每个字符随机旋转（-32°~32°）、随机垂直位移、随机字号（26~38）、随机字体与颜色，
    /// 并叠加 3 条干扰贝塞尔曲线与 35 个噪点。
    /// </summary>
    public static class SvgCaptchaGenerator
    {
        private static readonly string[] CharColors =
        {
            "#1a2b4a", "#5a1a2e", "#1a4a2b", "#4a3a12", "#2e1a4a", "#0e3a3a", "#3a1a1a"
        };

        private static readonly string[] NoiseColors =
        {
            "#9fb2c8", "#c8b29f", "#b29fb2", "#9fc8b2"
        };

        private static readonly string[] Fonts =
        {
            "Arial", "Verdana", "Georgia", "Courier New", "Times New Roman"
        };

        public static string Generate(string code)
        {
            int width = 150;
            int height = 50;
            var sb = new StringBuilder(2048);

            sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(width)
              .Append("\" height=\"").Append(height)
              .Append("\" viewBox=\"0 0 ").Append(width).Append(' ').Append(height).Append("\">");

            // 背景
            sb.Append("<rect width=\"").Append(width).Append("\" height=\"").Append(height).Append("\" fill=\"#f5f7fa\"/>");

            // 干扰曲线
            for (int i = 0; i < 3; i++)
            {
                int x0 = Random.Shared.Next(0, width);
                int y0 = Random.Shared.Next(0, height);
                int x1 = Random.Shared.Next(0, width);
                int y1 = Random.Shared.Next(0, height);
                int cx = Random.Shared.Next(0, width);
                int cy = Random.Shared.Next(0, height);

                sb.Append("<path d=\"M ").Append(x0).Append(' ').Append(y0)
                  .Append(" Q ").Append(cx).Append(' ').Append(cy)
                  .Append(" ").Append(x1).Append(' ').Append(y1).Append("\" ")
                  .Append("stroke=\"").Append(NoiseColors[Random.Shared.Next(NoiseColors.Length)])
                  .Append("\" stroke-width=\"").Append(1 + Random.Shared.Next(2))
                  .Append("\" fill=\"none\" opacity=\"0.7\"/>");
            }

            // 验证码字符
            double charStep = (width - 24) / (double)code.Length;
            for (int i = 0; i < code.Length; i++)
            {
                double x = 12 + charStep * i + charStep / 2 + (Random.Shared.NextDouble() * 6 - 3);
                double y = height / 2 + (Random.Shared.NextDouble() * 10 - 5);
                int rotation = Random.Shared.Next(-32, 33);
                int fontSize = 26 + Random.Shared.Next(13);
                string color = CharColors[Random.Shared.Next(CharColors.Length)];
                string font = Fonts[Random.Shared.Next(Fonts.Length)];
                string weight = Random.Shared.Next(2) == 0 ? "bold" : "normal";

                sb.Append("<text x=\"").Append(Format(x))
                  .Append("\" y=\"").Append(Format(y))
                  .Append("\" font-family=\"").Append(font)
                  .Append("\" font-size=\"").Append(fontSize)
                  .Append("\" font-weight=\"").Append(weight)
                  .Append("\" fill=\"").Append(color)
                  .Append("\" text-anchor=\"middle\" dominant-baseline=\"central\"")
                  .Append(" transform=\"rotate(").Append(rotation)
                  .Append(' ').Append(Format(x)).Append(' ').Append(Format(y)).Append(")\">")
                  .Append(code[i])
                  .Append("</text>");
            }

            // 噪点
            for (int i = 0; i < 35; i++)
            {
                double x = Random.Shared.NextDouble() * width;
                double y = Random.Shared.NextDouble() * height;
                double r = 0.5 + Random.Shared.NextDouble();

                sb.Append("<circle cx=\"").Append(Format(x))
                  .Append("\" cy=\"").Append(Format(y))
                  .Append("\" r=\"").Append(Format(r))
                  .Append("\" fill=\"").Append(CharColors[Random.Shared.Next(CharColors.Length)])
                  .Append("\" opacity=\"0.55\"/>");
            }

            sb.Append("</svg>");
            return sb.ToString();
        }

        private static string Format(double value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
