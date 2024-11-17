using Microsoft.Identity.Client;

namespace Senparc.Web
{
    public class SenparcWebClient(HttpClient httpClient)
    {
        //private readonly HttpClient _httpClient;

        //public SenparcWebClient {
        //    this._httpClient = httpClient;
        //} 

        public async Task<string> GetHtml(CancellationToken cancellationToken = default) {
            var html = await httpClient.GetAsync("api/Senparc.Xncf.Installer/InstallAppService/Xncf.Installer_InstallAppService.KeepAlive");
            return await html.Content.ReadAsStringAsync();
                } 
    }
}
