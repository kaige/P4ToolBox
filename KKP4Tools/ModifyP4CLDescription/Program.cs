using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Perforce.P4;

namespace ModifyP4CLDescription
{
    class Program
    {
        static string descriptionTemplate = @"NEW_DESCRIPTION_TEMPLATE";

        static void ModifyCLDescription(Repository rep, int changeId)
        {
            Changelist c = rep.GetChangelist(changeId);

            Console.WriteLine("\r\n");
            Console.WriteLine("Modify CL {0}", changeId);
            Console.Write(c.Description);
            Console.WriteLine("\r\n");

            string buff = descriptionTemplate;

            string newDiscription = buff.Replace("TO_BE_REPLACED_DESCRIPTION", c.Description);
            c.Description = newDiscription;
            ChangeCmdOptions opts = new ChangeCmdOptions(ChangeCmdFlags.Update);
            c = rep.UpdateChangelist(c, opts);
            Console.Write(newDiscription);
        }

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

        static void ModifySubmittedChangeLists(Repository rep, string path, string user)
        {
            ChangesCmdOptions opts = new ChangesCmdOptions(ChangesCmdFlags.None, null, 0, ChangeListStatus.Submitted, user);
            FileSpec fileSpec = new FileSpec(new DepotPath(path), null, null, null);

            IList<Changelist> lists = rep.GetChangelists(opts, fileSpec);
            Console.WriteLine("{0} lists found.", lists.Count);
            foreach (Changelist list in lists)
            {
                Changelist c = rep.GetChangelist(list.Id);
                if (IsIntegrationChangeList(c))
                    continue;

                ModifyCLDescription(rep, list.Id);
            }
        }

        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("incorrect number of args. please type: ModifyP4CLDescription <server:port> <user_name> <client_name> <depot_path>");
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

            ModifySubmittedChangeLists(rep, depotPath, userName);
        }
    }
}
