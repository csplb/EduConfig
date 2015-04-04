/**
 * 
 * EduConfig
 * 
 * Copyright (C) Politechnika Lubelska 2013-2015
 * Copyright (C) Marcin Badurowicz <m.badurowicz at pollub dot pl>
 * 
 * Aplikacja umożliwiająca automatyczną konfigurację profilu sieci 
 * bezprzewodowej eduroam, wraz z instalacją certyfikatu CA.
 *
 * EduConfig is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * EduConfig is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with EduConfig. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;
using Ktos.Common;

namespace Pollub.EduConfig
{
    /// <summary>
    /// Możliwe kody wyjścia programu w postaci listy flag
    /// </summary>
    [Flags]
    enum ExitCode
    {
        NoError = 0,
        CertInstallError = 1,
        ProfileInstallError = 2,
        SystemNotSupported = 4,
        NoAdmin = 8,
        UnhandledException = 16
    }

    /// <summary>
    /// Typy profili do zainstalowania
    /// </summary>
    enum ProfileType
    {
        Peap,
        Tls
    }

    class Program
    {
        private static ArgumentParser _pp;
        private static bool _silentMode;

        static void Main()
        {
            Application.EnableVisualStyles();

            try
            {
                _pp = new ArgumentParser(Environment.GetCommandLineArgs());
                _pp.AddExpanded("/s", "/silent");
                _pp.AddExpanded("/?", "--help");
                _pp.AddExpanded("/tls", "--tls");

                _pp.Parse();
                _silentMode = _pp.SwitchExists("/silent");

                if (_pp.VersionRequested())
                {
                    ShowVersion();
                    Exit(ExitCode.NoError);
                }

                if (_pp.HelpRequested())
                    ShowHelp();

                if (!IsRunningAsAdministrator())
                {
                    var selfName = Process.GetCurrentProcess().MainModule.FileName;
                    ProcessStartInfo self = new ProcessStartInfo(selfName);
                    self.Verb = "runas";
                    self.WindowStyle = ProcessWindowStyle.Hidden;
                    if (_silentMode) self.Arguments = "/silent";

                    try
                    {
                        Process.Start(self);
                    }
                    catch (Win32Exception)
                    {
                        if (!_silentMode)
                            MessageBox.Show(AppResources.NeedAdmin, AppResources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);

                        Exit(ExitCode.NoAdmin);
                    }

                    Exit(ExitCode.NoError);
                }

                if ((!_silentMode) && (!IsSystemSupported()))
                {
                    if (MessageBox.Show(AppResources.SystemNotSupported, AppResources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        Exit(ExitCode.SystemNotSupported);
                }

                if ((_silentMode) || (MessageBox.Show(AppResources.Info, AppResources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes))
                {
                    var exc = ExitCode.NoError;
                    try
                    {
                        InstallCaCertificate();
                    }
                    catch (CommandException e)
                    {                        
                        ErrorMessage(AppResources.CertFail, e);
                        exc = ExitCode.CertInstallError;
                    }

                    try
                    {
                        if (_pp.SwitchExists("--tls"))
                            InstallNetworkProfile(ProfileType.Tls);
                        else
                            InstallNetworkProfile(ProfileType.Peap);
                    }
                    catch (CommandException e)
                    {
                        ErrorMessage(AppResources.ProfFail, e);                        
                        exc = exc | ExitCode.ProfileInstallError;
                    }

                    if (exc == ExitCode.NoError)
                        if (!_silentMode)
                            MessageBox.Show(AppResources.Success, AppResources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Exit(exc);
                }

                Exit(ExitCode.NoError);
            }
            catch (Exception ex)
            {
                ErrorMessage(AppResources.UnhandledException, ex);
                Exit(ExitCode.UnhandledException);
            }

        }

        private static void ErrorMessage(string message, Exception e)
        {
            var errorMessage = string.Format("{0} {1}", message, e.Message);
            if (!_silentMode)
                MessageBox.Show(errorMessage, AppResources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                Console.Error.WriteLine(errorMessage);
        }

        /// <summary>
        /// Pokazuje informacje o wersji
        /// </summary>
        private static void ShowVersion()
        {
            var assemblyInformationalVersionAttribute = Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof (AssemblyInformationalVersionAttribute), false)[0] as
                AssemblyInformationalVersionAttribute;
#if DEBUG
            var version = assemblyInformationalVersionAttribute != null ? assemblyInformationalVersionAttribute.InformationalVersion : Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
#else
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
#endif
            Console.WriteLine(@"{0} {1}", AppResources.AppName, version);
            Console.WriteLine(AppResources.Copyright);
        }

        /// <summary>
        /// Pokazuje pomoc
        /// </summary>
        private static void ShowHelp()
        {
            ShowVersion();
            Console.WriteLine();
            Console.WriteLine(AppResources.Help);
            Exit(ExitCode.NoError);
        }

        /// <summary>
        /// Kończy działanie aplikacji z określonym kodem błędu
        /// </summary>
        /// <param name="e"></param>
        private static void Exit(ExitCode e)
        {
            Environment.Exit((int)e);
        }

        /// <summary>
        /// Instaluje certyfikat w Trusted Root CA
        /// </summary>
        private static void InstallCaCertificate()
        {
            var cert = Path.GetTempFileName().Replace(".tmp", ".der");
            File.WriteAllBytes(cert, AppResources.plca_cert);

            ProcessStartInfo p = new ProcessStartInfo("certutil", String.Format("-addstore Root \"{0}\"", cert));
            p.Verb = "runas";
            p.WindowStyle = ProcessWindowStyle.Hidden;
            p.RedirectStandardError = true;
            p.UseShellExecute = false;
            var certutil = Process.Start(p);
            if (certutil == null)
                throw new Exception(AppResources.ProcessAlreadyRun);

            // oczekiwanie na zakończenie certutila
            while (!certutil.HasExited) ;

            File.Delete(cert);

            if (certutil.ExitCode != 0)
                throw new CommandException(certutil.ExitCode.ToString());

        }

        /// <summary>
        /// Instaluje profil sieciowy na podstawie pliku XML
        /// </summary>
        private static void InstallNetworkProfile(ProfileType type)
        {
            var prof = Path.GetTempFileName().Replace(".tmp", ".xml");
            if (type == ProfileType.Peap)
                File.WriteAllBytes(prof, AppResources.eduroam_peap);
            else if (type == ProfileType.Tls)
                File.WriteAllBytes(prof, AppResources.eduroam_tls);

            ProcessStartInfo p = new ProcessStartInfo("netsh", String.Format("wlan add profile filename=\"{0}\" user=all", prof));
            p.Verb = "runas";
            p.WindowStyle = ProcessWindowStyle.Hidden;
            p.RedirectStandardError = true;
            p.UseShellExecute = false;
            var netsh = Process.Start(p);
            if (netsh == null)
                throw new Exception(AppResources.ProcessAlreadyRun);

            // oczekiwanie na zakończenie działania netsh
            while (!netsh.HasExited) ;

            File.Delete(prof);

            if (netsh.ExitCode != 0)
                throw new CommandException(netsh.ExitCode.ToString());
        }

        /// <summary>
        /// Sprawdza czy aplikacja jest uruchomiona z podwyższonymi uprawnieniami
        /// </summary>
        /// <returns></returns>
        private static bool IsRunningAsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            if (identity != null)
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            return false;
        }

        /// <summary>
        /// Sprawdza czy system jest obsługiwany oficjalnie
        /// Oficjalnie wspierane są tylko systemy Windows NT z obsługą netsh wlan (tj. Windows Vista lub nowszy),
        /// prawdopodobnie będzie też działać dla Windows XP
        /// </summary>
        /// <returns></returns>
        private static bool IsSystemSupported()
        {
            return (Environment.OSVersion.Platform == PlatformID.Win32NT) && (Environment.OSVersion.Version.Major >= 6);
        }
    }
}
