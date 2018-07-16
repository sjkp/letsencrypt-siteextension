using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core
{
    public interface IDnsProviderService
    {
        Task PersistChallenge(string recordSetName, string recordValue);
        Task Cleanup(string dnsTxt);
    }
}