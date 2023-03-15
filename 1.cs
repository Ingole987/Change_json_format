using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace Json_Convert_string_to_keyvaluepairs_
{
    class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("MyConnectionString");

            CallIngestionSettings(connectionString);
            //ReadJson();
        }

        public static void CallIngestionSettings(string connectionString)
        {
            try
            {

                SqlConnection connection = new SqlConnection(connectionString);

                SqlCommand selectCommand = new SqlCommand("SELECT Id,ClientId,Ftp_UserName,SourceName,DestinationJSON FROM CallIngestionSettings", connection);
                connection.Open();
                SqlDataReader reader = selectCommand.ExecuteReader();

                while (reader.Read())
                {
                    //int clientId = (int)reader["ClientId"];
                    //string ftpUserName = (string)reader["Ftp_UserName"];
                    //string sourceName = (string)reader["SourceName"];
                    //int primaryKey = (int)reader["Id"];
                    //string originalValue = (string)reader["DestinationJSON"];

                    int clientId = reader["ClientId"] != DBNull.Value ? (int)reader["ClientId"] : 0;
                    string ftpUserName = reader["Ftp_UserName"] != DBNull.Value ? (string)reader["Ftp_UserName"] : "";
                    string sourceName = reader["SourceName"] != DBNull.Value ? (string)reader["SourceName"] : "";
                    int primaryKey = reader["Id"] != DBNull.Value ? (int)reader["Id"] : 0;
                    string originalValue = reader["DestinationJSON"] != DBNull.Value ? (string)reader["DestinationJSON"] : "";

                    string processedValue;
                    if (originalValue.Contains("operatorType"))
                    {
                        processedValue = originalValue;
                    }
                    else
                    {
                        processedValue = ReadJson(originalValue);
                    }

                    SqlCommand updateCommand = new SqlCommand("UPDATE CallIngestionSettings SET DestinationJSON = @ProcessedValue WHERE ID = @PrimaryKey AND ClientId = @ClientId AND Ftp_UserName = @Ftp_UserName AND SourceName = @SourceName", connection);
                    updateCommand.Parameters.AddWithValue("@ProcessedValue", processedValue.Replace("  ", "").Replace(" ", ""));
                    updateCommand.Parameters.AddWithValue("@PrimaryKey", primaryKey);
                    updateCommand.Parameters.AddWithValue("@ClientId", clientId);
                    updateCommand.Parameters.AddWithValue("@Ftp_UserName", ftpUserName);
                    updateCommand.Parameters.AddWithValue("@SourceName", sourceName);
                    updateCommand.ExecuteNonQuery();
                }

                connection.Close();
            }
            catch (Exception e)
            {
                string error = e.Message;

                Console.WriteLine("Error: " + error);
            }
        }

        public static string ReadJson(string inputJson)
        {
            try
            {
                //string filePath = @"C:\Users\pratikdevidas.ingole\Desktop\jsonfilestring.txt";
                //string inputJson = File.ReadAllText(filePath);

                List<string> stringList = new List<string>();
                string[] jsonObjects = inputJson.TrimStart('[').TrimEnd(']').Split(new[] { "}," }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string jsonObject in jsonObjects)
                {
                    if (!string.IsNullOrEmpty(jsonObject))
                    {
                        string trimmedJsonObject = jsonObject.Trim();
                        if (!jsonObject.StartsWith("{"))
                        {
                            trimmedJsonObject = "{" + jsonObject;
                        }
                        if (!jsonObject.EndsWith("}"))
                        {
                            trimmedJsonObject = jsonObject + "}";
                        }

                        JObject inputObj = JObject.Parse(trimmedJsonObject);

                        JValue oldInclusionValue = (JValue)inputObj["addRecogFilter"][0]["inclusion"];
                        JValue oldExclusionValue = (JValue)inputObj["addRecogFilter"][0]["exclusion"];
                        string inclusionValue1 = (string)oldInclusionValue.Value;
                        string exclusionValue1 = (string)oldExclusionValue.Value;

                        //var newInclusionArray = new JArray();
                        //var newExclusionArray = new JArray();
                        var newInclusionObject = new JObject();
                        var newExclusionObject = new JObject();

                        newInclusionObject["operatorType"] = "Exactly";
                        newInclusionObject["value1"] = inclusionValue1;
                        newInclusionObject["value2"] = "";

                        newExclusionObject["operatorType"] = "Exactly";
                        newExclusionObject["value1"] = exclusionValue1;
                        newExclusionObject["value2"] = "";

                        //newInclusionArray.Add(newInclusionObject);
                        //newExclusionArray.Add(newExclusionObject);

                        //var newFilterArray = new JArray();
                        //var newFilterObject = new JObject();
                        //newFilterObject["inclusion"] = newInclusionArray;
                        //newFilterObject["exclusion"] = newExclusionArray;
                        //newFilterArray.Add(newFilterObject);

                        var newFilterObject = new JObject();
                        newFilterObject["inclusion"] = newInclusionObject;
                        newFilterObject["exclusion"] = newExclusionObject;

                        var newFilterArray = new JArray();
                        newFilterArray.Add(newFilterObject);

                        var outputObj = new JObject();
                        outputObj["uniqueIdent"] = inputObj["uniqueIdent"];
                        outputObj["sourceField"] = inputObj["sourceField"];
                        outputObj["destinationField"] = inputObj["destinationField"];
                        outputObj["exclude"] = inputObj["exclude"];
                        outputObj["addRecogFilter"] = newFilterArray;

                        //outputJson = outputJson.Trim('[', ']');
                        string jsonString = outputObj.ToString();
                        stringList.Add(jsonString);
                    }
                }
                string outputJson1 = "[" + string.Join(",", stringList) + "]";
                //WriteToFile(outputJson1);
                return outputJson1;
            }
            catch (Exception e)
            {
                string error = e.Message;

                Console.WriteLine("Error: " + error);
                return null;
            }

        }

        //public static void WriteToFile(string outputJson)
        //{
        //    string PathwithfileName = @"C:\Users\pratikdevidas.ingole\Desktop\outputJson.txt";
        //    IEnumerable<string> txtMessage = new string[] { outputJson };
        //    File.AppendAllLines(PathwithfileName, txtMessage);
        //    return;
        //}
    }
}

