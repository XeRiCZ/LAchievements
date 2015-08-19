using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LusorionAchievements
{
    public class Player
    {
        public string name;
        public List<Achievement> achievements;
        public AccountStatus status;

        public Player(string n)
        {
            name = n;
        }

    }
}
