using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Owlcat.Blueprints.Server.Utils
{
    public class VCSWatcher
    {
        private enum VCS
        {
            Unknown,
            Git,
            Svn
        }
        
        private VCS m_VCSType;
        private string? m_Revision;
        private string? m_Root;
        private readonly ILogger m_Logger;
        private Action m_OnRevisionChanged;

        public VCSWatcher(Action onRevisionChanged)
        {
            m_OnRevisionChanged = onRevisionChanged;
            m_Logger = Program.LogFactory.CreateLogger("VCSWatcher");
            DetectVCS();
            if (m_VCSType == VCS.Unknown)
            {
                m_Logger.LogWarning("VCS is not detected, server will not restart on repo update");
                return;
            }

            Task.Run(WatchForRevision);
        }
        

        private void DetectVCS()
        {
            m_Root = Directory.GetCurrentDirectory();
            while (m_Root != null)
            {
                if (Directory.Exists(Path.Combine(m_Root, ".svn")))
                {
                    m_VCSType = VCS.Svn;
                    break;
                }
                if (Directory.Exists(Path.Combine(m_Root, ".git")))
                {
                    m_VCSType = VCS.Git;
                    break;
                }
                m_Root = Path.GetDirectoryName(m_Root);
            }
        }

        private void WatchForRevision()
        {
            while (true)
            {
                var revision = (m_VCSType == VCS.Git ? GetGitRevision() : GetSvnRevision())?.Trim();
                if (string.IsNullOrEmpty(m_Revision))
                {
                    m_Revision = revision;
                    m_Logger.LogInformation($"Detected {m_VCSType} at revision {revision}");
                }
                else if (m_Revision != revision)
                {
                    m_Logger.LogInformation($"Revision changed: {m_Revision} -> {revision}");
                    m_OnRevisionChanged.Invoke();
                }
                Thread.Sleep(TimeSpan.FromSeconds(30));
            }
        }
        
        private string? GetGitRevision()
        {
            return CommandOutput(@"git rev-list HEAD -1", m_Root);
        }

        private string? GetSvnRevision()
        {
            return CommandOutput(@"svn info --show-item last-changed-revision", m_Root);
        }

        private string? CommandOutput(string command, string? workingDirectory = null)
        {
            try
            {
                ProcessStartInfo procStartInfo = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                    ? new ProcessStartInfo("cmd", "/c " + command) 
                    : new ProcessStartInfo(command[..3], command[3..]);

                procStartInfo.RedirectStandardError = procStartInfo.RedirectStandardInput = procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                if (!string.IsNullOrEmpty(workingDirectory))
                {
                    procStartInfo.WorkingDirectory = workingDirectory;
                }

                Process proc = new Process();
                proc.StartInfo = procStartInfo;
                proc.Start();

                StringBuilder sb = new StringBuilder();
                proc.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    sb.AppendLine(e.Data);
                };
                proc.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    sb.AppendLine(e.Data);
                };

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
                return sb.ToString();
            }
            catch (Exception objException)
            {
                m_Logger.LogWarning( $"Error in command: {command}, {objException.Message}");
                return null;
            }
        }
    }
}