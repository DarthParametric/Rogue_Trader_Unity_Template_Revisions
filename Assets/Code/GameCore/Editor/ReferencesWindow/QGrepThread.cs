using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Kingmaker.Editor.ReferencesWindow
{
	public class QGrepThread
	{
		private readonly Action m_FinishCallback;
		private readonly Action<string> m_LineCallback;
		private readonly Action<string> m_ErrorCallback;
		private Process m_Proc;
		private bool m_Started;

		public static QGrepThread Start(string arguments, Action<string> lineCallback, Action<string> errorCallback, Action finishCallback)
			=> new QGrepThread(arguments, lineCallback, errorCallback, finishCallback);

		public QGrepThread(string arguments, Action<string> lineCallback, Action<string> errorCallback, Action finishCallback)
		{
			m_LineCallback = lineCallback;
			m_ErrorCallback = errorCallback;
			m_FinishCallback = finishCallback;

			Task.Run(() => RunProcess(arguments));
		}

		private void RunProcess(string arguments)
		{
			try
			{

				m_Proc = new Process()
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = Path.Combine(Directory.GetCurrentDirectory(), ReferencesWindow.BasePath, "qgrep.exe"),
						Arguments = arguments,
						WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), ReferencesWindow.BasePath),
						UseShellExecute = false,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						CreateNoWindow = true,
					}
				};
				m_Started = m_Proc.Start();
			}
			catch (Exception ex)
			{
				m_Proc = null;
				PFLog.Default.Exception(ex);
				return;
			}

			try
			{
				while (!m_Proc.StandardOutput.EndOfStream || !m_Proc.StandardError.EndOfStream)
				{
					var line = m_Proc.StandardOutput.ReadLine();
					m_LineCallback?.Invoke(line);
				}
			}
			catch (Exception ex)
			{
				m_Proc = null;
				PFLog.Default.Exception(ex);
				return;
			}

			var err = m_Proc.StandardError.ReadToEnd();
			if (!string.IsNullOrEmpty(err))
			{
				m_ErrorCallback?.Invoke(err);
			}

			m_FinishCallback?.Invoke();
			m_Started = false;
			m_Proc.Dispose();
			m_Proc = null;
		}

		public bool IsRunning => m_Proc != null && m_Started && !m_Proc.HasExited;

		public void Kill()
		{
			m_Proc?.Kill();
			m_FinishCallback?.Invoke();
		}
	}
}