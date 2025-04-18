using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Debug = UnityEngine.Debug;

namespace Kingmaker.Editor.Utility
{
	/// <summary>
	/// Non-rumtime editor only class! shouldn't be used in-game
	/// </summary>
	public class ResaveUtilities
	{
		//U B E R C R A P
		public static void RevertLocalRotations()
		{
			ExecuteCommand("cmd.exe", "svn diff Assets >> diff");
			//ExecuteCommand("git", "diff " + path + " >> diff");
			if (!File.Exists("diff"))
			{
				PFLog.Default.Log("No diffs");
				return;
			}
			StreamReader inputFile = new StreamReader("diff", Encoding.UTF8);
			string line;
			string fileName = null;
			List<string> diff = new List<string>();
			while ((line = inputFile.ReadLine()) != null)
			{
				if (line.StartsWith("Index: "))
				{
					CheckTrash(fileName, diff);
					diff.Clear();
					fileName = line.Replace("Index: ", "");
				}
				if (line.StartsWith("+  m_LocalRotation") || line.StartsWith("-  m_LocalRotation"))
				{
					diff.Add(line);
				}
			}
			CheckTrash(fileName, diff);
			inputFile.Close();
			File.Delete("diff");
		}

		private static void CheckTrash(string fileName, List<string> diff)
		{
			if (diff.Count == 0 || diff.Count % 2 == 1)
			{
				return;
			}
			for (int i = 0; i < diff.Count / 2; i++)
			{
				string deletion = diff.ElementAt(i * 2);
				string insertion = diff.ElementAt(i * 2 + 1);
				if (!deletion.StartsWith("- ") || !insertion.StartsWith("+ "))
				{
					return;
				}
				string[] splitDeletion = ParseNumbers(deletion);
				string[] splitInsertion = ParseNumbers(insertion);
				if (splitInsertion == null || splitDeletion == null)
				{
					return;
				}
				for (int j = 0; j < 4; j++)
				{
					if (!IsEqual(splitDeletion[j], splitInsertion[j]))
					{
						return;
					}
				}
				File.WriteAllText(fileName, File.ReadAllText(fileName).Replace(insertion.Substring(1), deletion.Substring(1)));
			}			
		}

		private static bool IsEqual(string insertion, string deletion)
		{
			if (insertion.Equals(deletion))
			{
				return true;
			}
			if (!insertion.Contains(".") || !deletion.Contains("."))
			{
				return false;
			}
			return insertion.Substring(0, insertion.IndexOf('.') + 5).Equals(
				deletion.Substring(0, deletion.IndexOf('.') + 5));
		}

		private static string[] ParseNumbers(string deletion)
		{
			string[] numbers = deletion.Substring(1)
				.Replace("  m_LocalRotation: {x: ", "")
				.Replace(", y:", "")
				.Replace(", z:", "")
				.Replace(", w:", "")
				.Replace("}", "").Split(' ');
			if (numbers.Length != 4)
			{
				return null;
			}
			numbers[0] = numbers[0].Substring(0, numbers[0].Length > 8 ? 8 : numbers[0].Length);
			numbers[1] = numbers[1].Substring(0, numbers[1].Length > 8 ? 8 : numbers[1].Length);
			numbers[2] = numbers[2].Substring(0, numbers[2].Length > 8 ? 8 : numbers[2].Length);
			numbers[3] = numbers[3].Substring(0, numbers[3].Length > 8 ? 8 : numbers[3].Length);
			return numbers;
		}

		public static void ExecuteCommand(string executable, string arguments)
		{
			Process process = Process.Start(executable, "/c " + arguments);
			if (process != null)
			{
				process.WaitForExit();
			}
			else
			{
				Debug.LogErrorFormat("Can start process {} with arguments {} coz it's null", executable, arguments);
			}
		}
	}
}