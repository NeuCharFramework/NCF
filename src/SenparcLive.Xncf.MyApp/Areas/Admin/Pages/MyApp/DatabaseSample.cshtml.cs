using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using SenparcLive.Xncf.MyApp.Models.DatabaseModel.Dto;
using SenparcLive.Xncf.MyApp.Services;
using System;
using System.Threading.Tasks;

namespace SenparcLive.Xncf.MyApp.Areas.MyApp.Pages
{
    public class DatabaseSample : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public ColorDto ColorDto { get; set; }

        private readonly ColorService _colorService;
        private readonly IServiceProvider _serviceProvider;
        public DatabaseSample(IServiceProvider serviceProvider, ColorService colorService, Lazy<XncfModuleService> xncfModuleService)
            : base(xncfModuleService)
        {
            _colorService = colorService;
            _serviceProvider = serviceProvider;
        }

        public Task OnGetAsync()
        {
            var color = _colorService.GetObject(z => true, z => z.Id, OrderingType.Descending);
            ColorDto = _colorService.Mapper.Map<ColorDto>(color);
            return Task.CompletedTask;
        }

        public IActionResult OnGetDetail()
        {
            var color = _colorService.GetObject(z => true, z => z.Id, OrderingType.Descending);
            var colorDto = _colorService.Mapper.Map<ColorDto>(color);
            //return Task.CompletedTask;
            return Ok(new { colorDto, XncfModuleDto });
        }

        public async Task<IActionResult> OnGetBrightenAsync()
        {
            var colorDto = await _colorService.Brighten().ConfigureAwait(false);
            return Ok(colorDto);
        }

        public async Task<IActionResult> OnGetDarkenAsync()
        {
            var colorDto = await _colorService.Darken().ConfigureAwait(false);
            return Ok(colorDto);
        }
        public async Task<IActionResult> OnGetRandomAsync()
        {
            var colorDto = await _colorService.Random().ConfigureAwait(false);
            return Ok(colorDto);
        }
    }
}
