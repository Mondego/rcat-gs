using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alchemy.Server.Classes;


namespace RCAT
{
    /// <summary>
    /// Holds the name and context instance for an online user
    /// </summary>
    /// 

    public struct Position
    {
        public int top, left;

        public Position(int p1, int p2)
        {
            top = p1;
            left = p2;
        }

        public Position(String p1, String p2)
        {
            top = int.Parse(p1);
            left = int.Parse(p2);
        }
    }

    public class User
    {
        public string Name = String.Empty;
        public UserContext Context { get; set; }
        public Position pos;

    }


}
