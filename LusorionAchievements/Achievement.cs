using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LusorionAchievements
{
    public class Achievement
    {
        // Třída reprezentující jeden konkrétní achievement
        private int achievementID;
        private string achievementName;
        private string gameName;
        private int score;

        public Achievement(int id,string aN, string gN, int s)
        {
            achievementID = id;
            achievementName = aN;
            gameName = gN;
            score = s;
        }

        public string AchievementName
        {
            get
            {
                return achievementName;
            }
          /*  set
            {
                achievementName = value;
            }*/
        }

        public string GameName
        {
            get
            {
                return gameName;
            }
           /* set
            {
                gameName = value;
            }*/
        }

        public int Score
        {
            get
            {
                return score;
            }
         /*   set
            {
                score = value;
            }*/
        }

        public int AchievementID
        {
            get
            {
                return achievementID;
            }
          /*  set
            {
                achievementID = value;
            }*/
        }
    }
}
