using Senparc.Ncf.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Dto
{
    public class XncfModuleDto
    {
        public int Id { get; set; }
        public string Name { get; private set; }

        public string Uid { get; private set; }

        public string MenuName { get; private set; }

        public string Version { get; private set; }

        public string Description { get; set; }

        public string UpdateLog { get; private set; }

        public bool AllowRemove { get; private set; }

        public string MenuId { get; private set; }

        public string Icon { get; private set; }

        public XncfModules_State State { get; private set; }

    }
}
