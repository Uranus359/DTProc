using System;
using System.Diagnostics;
using WRing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

class Program
{
    static ConsoleColor defColor;

    static void Main()
    {
        Console.Title = $"DTProc";
        string input;
        defColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Press <help> to show command list");
        Console.ForegroundColor = defColor;
        while (true)
        {
            Console.Write("> ");
            input = Console.ReadLine().ToLower();
            string[] words = input.Split(' ');
            int wcount = words.Length;
            if (wcount == 1)
            {
                switch (words[0])
                {
                    case "exit":
                        return;
                    case "cls":
                        Console.Clear();
                        break;
                    case "clone":
                        Clone();
                        break;
                    case "help":
                        ShowHelp();
                        break;
                    case "list":
                        ListProcess();
                        break;
                    default:
                        break;
                }
            }
            else if (wcount == 2)
            {
                int val;
                switch (words[0])
                {
                    case "kill":
                        if (int.TryParse(words[1], out val))
                            KillProcess(val);
                        else
                            Cerr("Invalid Id!");
                        break;
                    case "focus":
                        if (int.TryParse(words[1], out val))
                            FocusProcess(val);
                        else
                            Cerr("Invalid Id!");
                        break;
                    case "show":
                        if (int.TryParse(words[1], out val))
                            ShowProcess(val, true);
                        else
                            Cerr("Invalid Id!");
                        break;
                    case "hide":
                        if (int.TryParse(words[1], out val))
                            ShowProcess(val, false);
                        else
                            Cerr("Invalid Id!");
                        break;
                    case "suspend":
                        if (int.TryParse(words[1], out val))
                            SuspendProcess(val);
                        else
                            Cerr("Invalid Id!");
                        break;
                    case "resume":
                        if (int.TryParse(words[1], out val))
                            ResumeProcess(val);
                        else
                            Cerr("Invalid Id!");
                        break;
                    case "help":
                        if (int.TryParse(words[1], out val))
                            HelpProcess(val);
                        else
                            Cerr("Invalid Id!");
                        break;
                    default:
                        break;
                }
            }
            else if (wcount == 3)
            {
                switch (words[0])
                {
                    case "list":
                        if (words[1] == "by")
                        {
                            if (words[2] == "id")
                            {
                                ListProcess(SortType.Id);
                            }
                            else if (words[2] == "name")
                            {
                                ListProcess(SortType.Name);
                            }
                            else if (words[2] == "wnd")
                            {
                                ListProcess(SortType.Wnd);
                            }
                            else if (words[2] == "mem")
                            {
                                ListProcess(SortType.Mem);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }

    static void Clone()
    {
        Process.Start(Application.ExecutablePath);
    }

    static void ShowHelp()
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("cls => clear text from screen");
        Console.WriteLine("clone => launch one more window of this program");
        Console.WriteLine("list => show all process with short info");
        Console.WriteLine("list by <id|name|wnd|mem> => show all process with short info as sorted");
        Console.WriteLine("kill <id> => kill process");
        Console.WriteLine("suspend <id> => suspend process");
        Console.WriteLine("resume <id> => resume suspended process");
        Console.WriteLine("focus <id> => focus main window of process");
        Console.WriteLine("show <id> => show main window of process");
        Console.WriteLine("hide <id> => hide main window of process");
        Console.WriteLine("help <id> => show info about porcess");
        Console.ForegroundColor = defColor;
    }

    static void HelpProcess(int pid)
    {
        Process proc;
        try
        {
            proc = Process.GetProcessById(pid);
        } catch
        {
            Cerr("Not process by Id!");
            return;
        }
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"Name: {proc.ProcessName}");
        string st;
        try
        {
            st = proc.StartTime.ToString();
        } catch
        {
            st = "<not access>";
        }
        Console.WriteLine($"Priority: {proc.BasePriority}");
        Console.WriteLine($"Start time: {st}");
        Console.WriteLine($"Threads count: {proc.Threads.Count}");
        string args = proc.StartInfo.Arguments;
        Console.WriteLine($"Arguments: {((string.IsNullOrEmpty(args))?"<->":args)}");
        string mod;
        try
        {
            mod = proc.MainModule.FileName;
        }
        catch
        {
            mod = "<->";
        }
        Console.WriteLine($"Main module: {mod}");
        Console.ForegroundColor = defColor;
    }

    static void ResumeProcess(int pid)
    {
        WinAPI.ResumeProcess(pid);
    }

    static void SuspendProcess(int pid)
    {
        WinAPI.SuspendProcess(pid);
    }

    static void ShowProcess(int pid, bool flag)
    {
        Window.Find(Process.GetProcessById(pid).MainWindowHandle).Show(flag);
    }

    static void FocusProcess(int pid)
    {
        if (!Window.Find(Process.GetProcessById(pid).MainWindowHandle).SetForeground())
            Cerr("Window not exist!");
    }

    static void KillProcess(int pid)
    {
        var proc = Process.GetProcessById(pid);
        try
        {
            proc.Kill();
        } catch
        {
            if (!proc.CloseMainWindow())
            {
                try
                {
                    WinAPI.SuspendProcess(proc.Id);
                    var threads = proc.Threads;
                    foreach (Thread t in threads)
                    {
                        t.Interrupt();
                    }
                } catch
                {
                    Cerr("Unknown error!");
                }
            }
        }
    }

    static void ListProcess(SortType stype = SortType.None)
    {
        Console.WriteLine("[Id]         [Name]                                           [Memory]           [Window]");
        int thisId = Process.GetCurrentProcess().Id;
        IEnumerable<Process> list = Process.GetProcesses();
        switch (stype)
        {
            case SortType.Id:
                list = list.OrderByDescending(p => p.Id);
                break;
            case SortType.Name:
                list = list.OrderBy(p => p.ProcessName).ThenByDescending(p => p.Id);
                break;
            case SortType.Wnd:
                list = list.OrderByDescending(p => p.MainWindowHandle != IntPtr.Zero).ThenBy(p => p.ProcessName).ThenByDescending(p => p.Id);
                break;
            case SortType.Mem:
                list = list.OrderByDescending(p => p.PrivateMemorySize64).ThenBy(p => p.ProcessName).ThenByDescending(p => p.Id);
                break;
            default:
                break;
        }
        foreach (var proc in list)
        {
            int id = proc.Id;
            string ids = id.ToString();
            if (ids.Length < 8) ids = ids.PadRight(8);
            string pname = proc.ProcessName;
            if (pname.Length < 42) pname = pname.PadRight(42);
            string sz = string.Empty;
            long szkb = proc.PrivateMemorySize64 / 1024;
            if (szkb >= 1024)
            {
                var mb = szkb / 1024;
                var more = szkb % 1024;
                if (more == 0)
                {
                    sz = $"{mb}mb";
                } else
                {
                    sz = $"{mb}.{more}mb";
                }
            } else
            {
                sz = $"{szkb}kb";
            }
            if (sz.Length < 13) sz = sz.PadRight(13);
            bool wnd = (proc.MainWindowHandle != IntPtr.Zero);
            if (wnd && (id != thisId))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Id: {ids} Name: {pname} Mem: {sz} Wnd: {wnd}");
                Console.ForegroundColor = defColor;
            }
            else if (id == thisId)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"Id: {ids} Name: {pname} Mem: {sz} Wnd: {wnd}");
                Console.ForegroundColor = defColor;
            }
            else
            {
                Console.WriteLine($"Id: {ids} Name: {pname} Mem: {sz} Wnd: {wnd}");
            }
        }
    }

    static void Cerr(string text)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(text);
        Console.ForegroundColor = defColor;
    }
}

public enum SortType
{
    None,
    Id,
    Name,
    Wnd,
    Mem
}