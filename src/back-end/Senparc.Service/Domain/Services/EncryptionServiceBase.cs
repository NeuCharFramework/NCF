using Senparc.Ncf.Core.Utility;

namespace Senparc.Service
{
    public partial interface IEncryptionServiceBase //: IBaseServiceData
    {
        string GetEncodedContent(string content, string encodeKey);
        string GetDecodedContent(string content, string encodeKey);
    }

    public class EncryptionServiceBase :/* BaseServiceData,*/ IEncryptionServiceBase
    {
        public EncryptionServiceBase()//(IBaseData baseData): base(baseData)
        { }

        public string GetEncodedContent(string content, string encodeKey)
        {
            return DesUtility.EncryptDES(content, encodeKey);
        }

        public string GetDecodedContent(string content, string encodeKey)
        {
            return DesUtility.DecryptDES(content, encodeKey);
        }
    }
}
