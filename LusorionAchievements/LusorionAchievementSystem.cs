using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using EncryptStringSample;

/* 
 * Lusorion Achievements System v. 0.1
 * - API pro připojení/odpojení hráčů od achievement systému,
 * zobrazení hráčových achievementů, zapsání nových, atd...
 * -----------
 * Vytvořil Jan Urubek (XeRi)
 * */

namespace LusorionAchievements
{
    public enum ConnectionStatus
    {
        Disconnected,   // nepřipojen
        Connecting, // připojuje se
        Connected,  // připojen
        InvalidUserNamePassword // zadane spatne jmeno/heslo
    }

    public enum Language    // Jazyk vystupnich hlasek
    {
        Cestina,
        English
    }

    public enum AccountStatus
    {
        Player, // Normální hráč - vše je OK
        Registered, // Registrovaný uživatel, je nutno potvrdit účet na emailu
        Banned, // zabanovaný hráč
        Developer, // developer
        Administrator // Admin
    }

    public class LusorionAchievementSystem : SQLBase
    {
        public Language actualLanguage = Language.English;
        private string loggedPlayerName = "";
        public Player thisPlayer;

        public string LoggedPlayerName
        {
            get{
                return loggedPlayerName;
            }
        }
        private AccountStatus loggedPlayerStatus;
        public AccountStatus LoggedPlayerStatus
        {
            get
            {
                return loggedPlayerStatus;
            }
        }

        AccountStatus convertStatusFromString(string input)
        {
            if (input == "player") return AccountStatus.Player;
            if (input == "banned") return AccountStatus.Banned;
            if (input == "administrator") return AccountStatus.Administrator;
            return AccountStatus.Registered;
        }

        public void setConnectionIP(string input)
        {
            // Nastavení IP na kterou se připojuje
            connectionString = input;
        }

        public bool Login(string userName, string password)
        {
            loggedPlayerName = "";
            // Hlavní metoda pro přihlášení uživatele
            MySqlConnection myConnection = new MySqlConnection(connectionString);
            actualState = ConnectionStatus.Connecting;
            if (actualLanguage == Language.English)
                statusText = "Connecting to server...";
            else statusText = "Připojuji se k serveru...";

            // Select všechny učty a porovnani s učtem hračovym
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            //parameters.Add(new MySqlParameter(
            parameters.Add(new MySqlParameter("@ACC_NAME", userName));

            // encryptovani vstupniho hesla
            string encryptedstring = Hash(password);

            parameters.Add(new MySqlParameter("@ACC_PWD", encryptedstring));
            //List<string>[] queryResult = Select("SELECT * FROM accounts WHERE ACCOUNT_NAME = @ACC_NAME AND PASSWORD = @ACC_PWD", 3, parameters);
            List<string>[] queryResult = Select("SELECT COUNT(*) FROM accounts WHERE ACCOUNT_NAME = @ACC_NAME AND PASSWORD = @ACC_PWD", 1, parameters);

            if (!sqlStatementCompleted)
            {
                // Nebylo navazano spojeni s DB
                actualState = ConnectionStatus.Disconnected;
                return false;
            }

            // Jestli se hrač přihlásí tak musí být vysledek select count(*) roven 1
            if (queryResult[0][0] == "1")
            {
                // Hrač je nalezen, zkontroluj jeho status

                parameters = new List<MySqlParameter>();
                parameters.Add(new MySqlParameter("@ACC_NAME", userName));

                queryResult = Select("SELECT STATUS FROM accounts WHERE ACCOUNT_NAME = @ACC_NAME", 1, parameters);
                loggedPlayerStatus = convertStatusFromString(queryResult[0][0]);

                switch (loggedPlayerStatus)
                {
                    case AccountStatus.Banned:
                        if (actualLanguage == Language.English)
                            statusText = "Your account was banned!";
                        else statusText = "Váš účet byl zabanován!";
                        return false;
                        break;
                   /* case AccountStatus.Registered:
                        if (actualLanguage == Language.English)
                            statusText = "You need to activate your account via link sended to your email address.";
                        else statusText = "Je nutno aktivovat účet pomocí odkazu zaslaném na Vaši emailovou adresu!";
                        return false;
                        break;*/
                    default:
                        break;
                }

                if (actualLanguage == Language.English)
                    statusText = "You was successfully logged in!";
                else statusText = "Úspěšně jste se přihlásil";
                actualState = ConnectionStatus.Connected;
                loggedPlayerName = userName;
                thisPlayer = new Player(userName);
                thisPlayer.achievements = loadPlayersAchievements(thisPlayer);

                return true;
            }
            actualState = ConnectionStatus.InvalidUserNamePassword;
            if (actualLanguage == Language.English)
                statusText = "Incorrect username or password.";
            else statusText = "Nesprávné přihlašovací jméno či heslo.";
            return false;          
        }

        public bool activateAccount(string accountName)
        {
            // Aktivace účtu (změna ze stavu registered na player)
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            //parameters.Add(new MySqlParameter(
            parameters.Add(new MySqlParameter("@ENC_NAME", accountName));

            List<string>[] queryResult = Select("SELECT COUNT(*) FROM accounts WHERE ENC_NAME = @ENC_NAME", 1, parameters);
            if(!sqlStatementCompleted){
                // Nebylo navazano spojeni s DB
                actualState = ConnectionStatus.Disconnected;
                return false;
            }

            if (queryResult[0][0] == "1")
            {
                // Účet k ativaci byl nalezen
                Update("UPDATE accounts SET STATUS = 'player' WHERE ENC_NAME = @ENC_NAME", parameters);
                if (!sqlStatementCompleted)
                {
                    // Nebylo navazano spojeni s DB
                    actualState = ConnectionStatus.Disconnected;
                    return false;
                }
                if (accountName == loggedPlayerName)
                    loggedPlayerStatus = AccountStatus.Player;

                if (actualLanguage == Language.Cestina) statusText = "Váš herní účet byl aktivován.";
                else statusText = "Your game account was activated.";
                return true;

            }
            if (actualLanguage == Language.Cestina) statusText = "Účet nebyl nalezen. Aktivace se nezdařila.";
            else statusText = "Account was not found. Activation aborted.";
            return false;
        }


        string Hash(string input)
        {
            // Hashovani stringu
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }


        public bool Register(string userName, string password, string email)
        {
            MySqlConnection myConnection = new MySqlConnection(connectionString);
            // Select všechny učty a porovnani s učtem hračovym

            List<MySqlParameter> parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@ACC_NAME", userName));

            // Kontrola zda zadane jmeno uctu jiz existuje v DB
            List<string>[] queryResult = Select("SELECT COUNT(*) FROM accounts WHERE ACCOUNT_NAME = @ACC_NAME", 1, parameters);
            if (!sqlStatementCompleted)
            {
                // Nebylo navazano spojeni s DB
                actualState = ConnectionStatus.Disconnected;
                return false;
            }
            // Jestli již existuje hrač se stejnym jmenem, je vracen vysledek > 0
            if (Convert.ToInt32(queryResult[0][0]) > 1)
            {
                if (actualLanguage == Language.English)
                    statusText = "Player with nick "+userName+" already exists!";
                else statusText = "Hráč se jménem " + userName + " již existuje!";
                return false;
            }

            parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@ACC_MAIL", email));

            // Kontrola zda zadany email již existuje v DB
            queryResult = Select("SELECT COUNT(*) FROM accounts WHERE EMAIL = @ACC_MAIL", 1, parameters);
            if (Convert.ToInt32(queryResult[0][0]) > 1)
            {
                if (actualLanguage == Language.English)
                    statusText = "Someone with email \""+email+"\" was already registered!";
                else statusText = "Někdo s emailem \"" + email + "\" již byl zaregistrován!";
                return false;
            }

            // Vše je OK! Je možno zaregistrovat nového hráče!
            parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@ACC_NAME", userName));

            /*
            string encryptedstring = StringCipher.Encrypt("6DFdsx65v23eRe3yx2De6qer", password);
            string encryptedAccName = StringCipher.Encrypt("okv2pPOWdc665YYxX333", userName);*/

            string encryptedstring = Hash(password);
            string encryptedAccName = Hash(userName);
            Console.WriteLine("Vygeneroval jsem " + encryptedstring);
            parameters.Add(new MySqlParameter("@ACC_PWD", encryptedstring));
            parameters.Add(new MySqlParameter("@ACC_MAIL", email));
            parameters.Add(new MySqlParameter("@ENC_NAME", encryptedAccName));
            if (Insert("INSERT INTO ACCOUNTS VALUES(@ACC_NAME,@ACC_PWD,@ACC_MAIL,'registered',@ENC_NAME)", parameters))
            {
                if (actualLanguage == Language.English)
                    statusText = "Your game account was successfully registered!";
                else statusText = "Váš herni učet byl úspěšně zaregistrován!";
                actualState = ConnectionStatus.Connected;
                loggedPlayerName = userName;
                thisPlayer = new Player(userName);
                thisPlayer.achievements = loadPlayersAchievements(thisPlayer);
                loggedPlayerStatus = AccountStatus.Registered;
                return true;
            }

            return false;


        }

        bool isAdmin()
        {
            if (actualState == ConnectionStatus.Connected)
            {
                MySqlConnection myConnection = new MySqlConnection(connectionString);
                // Select všechny učty a porovnani s učtem hračovym

                List<MySqlParameter> parameters = new List<MySqlParameter>();
                parameters.Add(new MySqlParameter("@ACC_NAME", loggedPlayerName));

                // Kontrola zda zadane jmeno uctu jiz existuje v DB
                List<string>[] queryResult = Select("SELECT COUNT(*) FROM accounts WHERE ACCOUNT_NAME = @ACC_NAME", 1, parameters);
                if (!sqlStatementCompleted)
                {
                    // Nebylo navazano spojeni s DB
                    actualState = ConnectionStatus.Disconnected;
                    return false;
                }

            }
            return false;
        }

        public bool PlayerRecievedAchievement(int idAchievement)
        {
            if (actualState == ConnectionStatus.Disconnected) return false;
            // Metoda ktera se zavola při obdržení nového achievementu
            // Kontrola zda hráč achievement již neobdržel
            foreach (Achievement ach in thisPlayer.achievements)
            {
                if (ach.AchievementID == idAchievement)
                {
                    // Hráč už tento achievement má
                    return false;
                }
            }

            // Kontrola zda achievement nebyl vymazan
            MySqlConnection myConnection = new MySqlConnection(connectionString);

            List<MySqlParameter> parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@ID_ACHIEVEMENT", idAchievement));

            // Kontrola zda zadana hra existuje
            List<string>[] queryResult = Select("SELECT * FROM achievements WHERE ID_ACHIEVEMENT = @ID_ACHIEVEMENT", 5, parameters);
            if (!sqlStatementCompleted)
            {
                // Nebylo navazano spojeni s DB
                actualState = ConnectionStatus.Disconnected;
                return false;
            }

            if (!foundSomeResult)
            {
                // Achievement s timto ID nebyl nalezen
                if (actualLanguage == Language.Cestina) statusText = "Achievement s ID "+idAchievement+" nebyl nalezen...";
                else statusText = "Achievement with ID " + idAchievement + " was not found...";
                return false;
            }

            // Přidání nového achievementu hráčovi
            
            // Zapsani do databaze
            parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@ACCOUNT_NAME", loggedPlayerName));
            parameters.Add(new MySqlParameter("@ID_ACHIEVEMENT", idAchievement));

            if (Insert("INSERT INTO players_achievements VALUES(@ACCOUNT_NAME,@ID_ACHIEVEMENT)", parameters))
            {
                thisPlayer.achievements.Add(new Achievement(idAchievement, queryResult[0][1], queryResult[0][2], Convert.ToInt32(queryResult[0][3])));
                if (actualLanguage == Language.Cestina) statusText = "Obdržel jsi nový achievement! " + queryResult[0][1];
                else statusText = "You have recieved new achievement! " + queryResult[0][1];
                return true;
            }
            return false;
      
        }

        public void addNewAchievement(string achievementName,string gameName,int score)
        {
            if (actualState == ConnectionStatus.Disconnected) return;
            // Metoda pro přidání nového druhu achievementu do databaze
            // musí být administrator
            if (loggedPlayerStatus != AccountStatus.Administrator)
            {
                if (actualLanguage == Language.Cestina) statusText = "Nemáš opravnění přidávat nové achievementy.";
                else statusText = "You don't have permission to add new achievements.";
                return;
            }
            MySqlConnection myConnection = new MySqlConnection(connectionString);

            List<MySqlParameter> parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@GAME_NAME", gameName));

            // Kontrola zda zadana hra existuje
            List<string>[] queryResult = Select("SELECT COUNT(*) FROM games WHERE GAME_NAME = @GAME_NAME", 1, parameters);
            if (!sqlStatementCompleted)
            {
                // Nebylo navazano spojeni s DB
                actualState = ConnectionStatus.Disconnected;
                return;
            }
            // Jestli neexistuje dana hra vrat chybu
            if (Convert.ToInt32(queryResult[0][0]) == 0)
            {
                if (actualLanguage == Language.Cestina) statusText = "Hra " + gameName + " neexistuje!";
                else statusText = "Game " + gameName + " does not exist!";
                return;
            }


            parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@ACHIEVEMENT_NAME", achievementName));
            parameters.Add(new MySqlParameter("@GAME_NAME", gameName));
            parameters.Add(new MySqlParameter("@SCORE", score));
            if (Insert("INSERT INTO ACHIEVEMENTS VALUES(null,@ACHIEVEMENT_NAME,@GAME_NAME,@SCORE,0)", parameters))
            {
                if (actualLanguage == Language.English)
                    statusText = "New achievement was added!";
                else statusText = "Novy achievement byl přidán!";
                return;
            }
            if (actualLanguage == Language.Cestina) statusText = "Adding new achievement failed.";
            else statusText = "Při vkladani zaznamu nastala chyba.";
            return;
        }

        List<Achievement> loadPlayersAchievements(Player player)
        {
            MySqlConnection myConnection = new MySqlConnection(connectionString);

            List<MySqlParameter> parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@ACC_NAME", player.name));


            List<Achievement> outputAchievements = new List<Achievement>();
            if (actualState == ConnectionStatus.Disconnected) return outputAchievements;
            // Kontrola zda zadane jmeno uctu jiz existuje v DB
            List<string>[] queryResult = Select("SELECT * FROM players_achievements WHERE ACCOUNT_NAME = @ACC_NAME", 2, parameters);
            if (!sqlStatementCompleted) return outputAchievements;

            // Naplnění listu vysledku
            Console.WriteLine("loading players  " + player.name + " achievements :");
            for (int i = 0; i < numberOfSelectedRows; i++)
            {
                //Vyhledani informaci o achievementu na zaklade id achievementu
                string foundAchievementID = queryResult[i][0];

                // Vytvořeni parametru
                List<MySqlParameter> parameters2 = new List<MySqlParameter>();
                parameters2.Add(new MySqlParameter("@ID_ACHIEVEMENT", foundAchievementID));

                List<string>[] queryResult2 = Select("SELECT * FROM ACHIEVEMENTS WHERE ID_ACHIEVEMENT = @ID_ACHIEVEMENT",5,parameters2);
                if (!sqlStatementCompleted) return outputAchievements;
                if (queryResult2[0][4] == "1") // achievement byl smazan
                    continue;

                Achievement newAchievement = new Achievement(Convert.ToInt32(queryResult2[0][0]),queryResult2[0][1],queryResult2[0][2],Convert.ToInt32(queryResult2[0][3]));
                Console.WriteLine(" achievement ["+newAchievement.AchievementID+"] " + newAchievement.AchievementName + " from game " + newAchievement.GameName + " with score - " + newAchievement.Score);
                outputAchievements.Add(newAchievement);

            }
            return outputAchievements;
        }


    }
}
