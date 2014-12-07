namespace GeoDecisions.Esb.Common.Services
{
    public interface IAuthService
    {
        string Authenticate(string username, string password);

        string Authenticate(string infoHash);
    }
}