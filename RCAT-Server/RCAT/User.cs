using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alchemy.Server.Classes;
using Newtonsoft.Json;


namespace RCAT
{
    /// <summary>
    /// Holds the name and context instance for an online user
    /// </summary>
    /// 

    public struct Position
    {
        public int t, l, z;

        public Position(int p1, int p2, int p3)
        {
            t = p1;
            l = p2;
            z = p3;
        }

        public Position(String p1, String p2, String p3)
        {
            t = int.Parse(p1);
            l = int.Parse(p2);
            z = int.Parse(p3);
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class User
    {
        [JsonProperty]
        public string n = String.Empty;

        public UserContext Context = null;

        [JsonProperty]
        public Position p = new Position(0,0,0);

    }
}
