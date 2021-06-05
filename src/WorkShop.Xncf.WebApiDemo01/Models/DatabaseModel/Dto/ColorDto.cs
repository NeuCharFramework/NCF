using Senparc.Ncf.Core.Models;

namespace WorkShop.Xncf.WebApiDemo01.Models.DatabaseModel.Dto
{
    public class ColorDto : DtoBase
    {
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Red { get; private set; }
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Green { get; private set; }
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Blue { get; private set; }

        private ColorDto() { }
    }
}
