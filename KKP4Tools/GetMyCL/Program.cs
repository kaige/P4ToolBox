using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Perforce.P4;
using System.Text.RegularExpressions;

// This program get all change-lists from an user in a depot path, then save it to a text file that's readable from Excel
//
namespace GetMyCL
{
    class Program
    {
        static bool IsIntegrationChangeList(Changelist cl)
        {
            IList<FileMetaData> files = cl.Files;
            foreach (FileMetaData data in files)
            {
                if (data.Action == FileAction.Integrate)
                    return true;
            }
            return false;
        }

        static void MatchNumbers(string pat, string text, List<int> matchedNumbers)
        {
            // Instantiate the regular expression object.
            Regex r = new Regex(pat, RegexOptions.IgnoreCase);

            // Match the regular expression pattern against a text string.
            Match m = r.Match(text);
            while (m.Success)
            {
                for (int i = 1; i <= m.Groups.Count; i++)
                {
                    System.Text.RegularExpressions.Group g = m.Groups[i];
                    CaptureCollection cc = g.Captures;
                    for (int j = 0; j < cc.Count; j++)
                    {
                        Capture cap = cc[j];
                        matchedNumbers.Add(Int32.Parse(cap.Value));
                    }
                }
                m = m.NextMatch();
            } 
        }

        static void GetSum(ref int sum, int startIndex, int size, List<int> nums)
        {
            for (int i = startIndex; i < nums.Count; i += size)
            {
                sum += nums[i];
            }
        }

        static void GetAllChangeList(Repository rep, string path, string user)
        {
            ChangesCmdOptions opts = new ChangesCmdOptions(ChangesCmdFlags.None, null, 0, ChangeListStatus.Submitted, user);
            FileSpec fileSpec = new FileSpec(new DepotPath(path), null, null, null);

            IList<Changelist> lists = rep.GetChangelists(opts, fileSpec);
            Console.WriteLine("{0} lists found.", lists.Count);
            foreach (Changelist list in lists)
            {
                Changelist c = rep.GetChangelist(752325);
                if (IsIntegrationChangeList(c))
                    continue;

                string cmdArguments = String.Format("{0}", c.Id);
                P4Command cmd = new P4Command(rep, "describe", false, cmdArguments);
                Options cmdOpts = new DescribeCmdOptions(DescribeChangelistCmdFlags.Summary, 20, 20);
                P4CommandResult results = cmd.Run(cmdOpts);
                string outputText = (results.TextOutput);

                int numAddChunks = 0;
                int numAddLines = 0;
                int numDeleteChunks = 0;
                int numDeleteLines = 0;
                int numChangeChunks = 0;
                int numBeforeChangeLines = 0;
                int numAfterChangeLines = 0;

                string pat = @"add\s([0-9]+)\schunks\s([0-9]+)\slines";
                List<int> nums = new List<int>();
                MatchNumbers(pat, outputText, nums);
                GetSum(ref numAddChunks, 0, 2, nums);
                GetSum(ref numAddLines, 1, 2, nums);

                pat = @"deleted\s([0-9]+)\schunks\s([0-9]+)\slines";
                nums.Clear();
                MatchNumbers(pat, outputText, nums);
                GetSum(ref numDeleteChunks, 0, 2, nums);
                GetSum(ref numDeleteLines, 1, 2, nums);

                pat = @"changed\s([0-9]+)\schunks\s([0-9]+)\s/\s([0-9]+)\slines";
                nums.Clear();
                MatchNumbers(pat, outputText, nums);
                GetSum(ref numChangeChunks, 0, 3, nums);
                GetSum(ref numBeforeChangeLines, 1, 3, nums);
                GetSum(ref numAfterChangeLines, 2, 3, nums);

                Console.WriteLine("total num of lines: {0}", numAddLines + numDeleteLines + numAfterChangeLines);

            }
        }

        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("incorrect number of args. please type: GetMyCL <server:port> <user_name> <client_name> <depot_path>");
                return;
            }

            string uri = args[0];
            string userName = args[1];
            string ws_client = args[2];
            string depotPath = args[3];

            Server server = new Server(new ServerAddress(uri));
            Repository rep = new Repository(server);
            Connection con = rep.Connection;

            con.UserName = userName;
            con.Client = new Client();
            con.Client.Name = ws_client;

            con.Connect(null);

            Console.WriteLine("connection status: {0}", con.Status);

            GetAllChangeList(rep, depotPath, userName);

        }
    }
}
