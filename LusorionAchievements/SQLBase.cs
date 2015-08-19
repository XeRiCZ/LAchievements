using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;


/*
 * Třída obsahující základní SQL příkazy
 * SELECT,INSERT
 * */

namespace LusorionAchievements
{
    public class SQLBase
    {
        protected string connectionString = "SERVER=xx.xx.xx.xx;DATABASE=achievements;UID=aUser;PASSWORD=xx;Encrypt=true;";
        public ConnectionStatus actualState = ConnectionStatus.Disconnected;
        MySqlConnection myConnection;
        protected bool sqlStatementCompleted = false;   // indikuje zda se SQL přikaz zdařil (hrač byl připojen na DB)
        public string statusText = "";
        protected int numberOfSelectedRows;


        protected bool openConnection()
        {
            // Hlavní metoda pro připojení do databáze
            myConnection = new MySqlConnection(connectionString);

            try
            {
                myConnection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                statusText = "Failed to connect to database.";
                return false;
            }
        }

        //Close connection
        protected bool CloseConnection()
        {
            try
            {
                myConnection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                return false;
            }
        }

        protected bool Insert(string insertStatement, List<MySqlParameter> parameters)
        {
            if (this.openConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(insertStatement, myConnection);
                foreach (MySqlParameter param in parameters)
                    cmd.Parameters.Add(param);
                // Proveď Insert příkaz
                cmd.ExecuteNonQuery();
                return true;
            }
            return false;   // nepřipojeno na DB
        }

        protected bool Update(string updateStatement, List<MySqlParameter> parameters)
        {
            if (this.openConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(updateStatement, myConnection);
                foreach (MySqlParameter param in parameters)
                    cmd.Parameters.Add(param);
                // Proveď Insert příkaz
                cmd.ExecuteNonQuery();
                return true;
            }
            return false;   // nepřipojeno na DB
        }
        protected bool foundSomeResult = false;
        // Klasicka operace Select  - numRows indikuje počet sloupcu u vychozi tabulky
        protected List<string>[] Select(string selectStatement, int numRows ,List<MySqlParameter> parameters)
        {
            string query = selectStatement;
           // Console.WriteLine(" - Creating select statement");

            foundSomeResult = false;
            //Vytvoření listu obsahujícího výsledky operace Select
            // - výsledky jsou listy typu <string>
            List<string>[] list = new List<string>[numRows];
            for (int i = 0; i < numRows; i++)
            {
                list[i] = new List<string>();
            }
            numberOfSelectedRows = 0;
            // Otevři připojení
            if (this.openConnection() == true)
            {
                sqlStatementCompleted = true;
                //Vytvoř přikaz
                MySqlCommand cmd = new MySqlCommand(selectStatement, myConnection);
                foreach(MySqlParameter param in parameters)
                    cmd.Parameters.Add(param);
                //Vytvoř dataReader a spusť příkaz
                MySqlDataReader dataReader = cmd.ExecuteReader();

                //Přečti všechny data
                while (dataReader.Read())
                {
                    numberOfSelectedRows++;
                    for (int i = 0; i < numRows; i++)
                    {
                        list[i].Add(dataReader[i] + "");
                        foundSomeResult = true;
                        
                        //Console.WriteLine("   - Adding to result query " + dataReader[i] + "");
                    }
                }

                //zavři data reader
                dataReader.Close();
               // Console.WriteLine(" - Select statement ended");
                //zavři připojení
                this.CloseConnection();
                
                //vrať list
                return list;
            }
            else
            {
                sqlStatementCompleted = false;
                return list;
            }
        }

    }
}
