namespace MaaslandDiscordBot
{
    using System.Configuration.Install;
    using System.Reflection;

    public class AutoInstaller
    {
        private static readonly string _exePath = Assembly.GetExecutingAssembly().Location;

        public static bool InstallMe()
        {
            try
            {
                ManagedInstallerClass.InstallHelper(new[] {_exePath});
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static bool UninstallMe()
        {
            try
            {
                ManagedInstallerClass.InstallHelper(new[] {"/u", _exePath});
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
