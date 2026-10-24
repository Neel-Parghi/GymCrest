namespace GymCrest.Application.Modules.Auth.Interface
{
    public interface IPasswordService
    {
        string HashPassword(string password);

        bool VerifyPasword(string password, string hash);
    }
}
