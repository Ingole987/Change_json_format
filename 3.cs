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
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfigurationRoot config = configuration.Build();
            DestinationJSON(config);
        }

        public static void DestinationJSON(IConfiguration configuration)
        {
            try
            {
                var connectionString = configuration.GetConnectionString("ConnectionString");
                var operatorType = configuration.GetSection("operatorType").Value;

                SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand selectCommand = new SqlCommand("SELECT Id,ClientId,Ftp_UserName,SourceName,DestinationJSON FROM CallIngestionSettings", connection);
                connection.Open();
                SqlDataReader reader = selectCommand.ExecuteReader();

                while (reader.Read())
                {
                    int clientId = reader["ClientId"] != DBNull.Value ? (int)reader["ClientId"] : 0;
                    string ftpUserName = reader["Ftp_UserName"] != DBNull.Value ? (string)reader["Ftp_UserName"] : "";
                    string sourceName = reader["SourceName"] != DBNull.Value ? (string)reader["SourceName"] : "";
                    int primaryKey = reader["Id"] != DBNull.Value ? (int)reader["Id"] : 0;
                    string originalValue = reader["DestinationJSON"] != DBNull.Value ? (string)reader["DestinationJSON"] : "";

                    string processedValue;
                    if (originalValue.Contains("operatorType"))
                    {
                        processedValue = ReadJson2(originalValue, operatorType);
                    }
                    else
                    {
                        processedValue = ReadJson(originalValue, operatorType);
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

        public static string ReadJson(string inputJson, string operatorType)
        {
            try
            {
                List<string> stringList = new List<string>();
                JArray inputArray = JArray.Parse(inputJson);
                foreach (JObject inputObj in inputArray.Children<JObject>())
                {
                    var outputArray = new JArray();

                    JArray addRecogFilterArray = (JArray)inputObj["addRecogFilter"];
                    JValue oldInclusionValue = (JValue)addRecogFilterArray[0]["inclusion"];
                    JValue oldExclusionValue = (JValue)addRecogFilterArray[0]["exclusion"];
                    string inclusionValue1 = (string)oldInclusionValue.Value;
                    string exclusionValue1 = (string)oldExclusionValue.Value;

                    var newInclusionObject = new JObject();
                    newInclusionObject["operatorType"] = operatorType;
                    newInclusionObject["value1"] = inclusionValue1;
                    newInclusionObject["value2"] = "";

                    var newExclusionObject = new JObject();
                    newExclusionObject["operatorType"] = operatorType;
                    newExclusionObject["value1"] = exclusionValue1;
                    newExclusionObject["value2"] = "";

                    var newFilterObject = new JObject();
                    newFilterObject["inclusion"] = newInclusionObject;
                    newFilterObject["exclusion"] = newExclusionObject;

                    var newFilterArray = new JArray();
                    newFilterArray.Add(newFilterObject);

                    var outputObj = new JObject(inputObj);
                    outputObj["addRecogFilter"] = newFilterArray;
                    outputArray.Add(outputObj);

                    string outputArray1 = outputArray.ToString().TrimStart('[').TrimEnd(']');
                    stringList.Add(outputArray1);
                }
                string outputJson1 = "[" + string.Join(",", stringList) + "]";
                return outputJson1;
            }
            catch (Exception e)
            {
                string error = e.Message;

                Console.WriteLine("Error: " + error);
                return null;
            }
        }

        public static string ReadJson2(string inputJson, string operatorType)
        {
            List<string> stringList = new List<string>();
            JArray inputArray = JArray.Parse(inputJson);
            foreach (JObject inputObj in inputArray.Children<JObject>())
            {
                JValue oldInclusionOperatorType = (JValue)inputObj["addRecogFilter"][0]["inclusion"]["operatorType"];
                JValue oldExclusionOperatorType = (JValue)inputObj["addRecogFilter"][0]["exclusion"]["operatorType"];

                oldInclusionOperatorType.Value = operatorType;
                oldExclusionOperatorType.Value = operatorType;

                string jsonString = inputObj.ToString();
                stringList.Add(jsonString);
            }
            string outputJson = "[" + string.Join(",", stringList) + "]";
            return outputJson;
        }

        //public static string ReadJson(string inputJson, string operatorType)
        //{
        //    try
        //    {
        //        List<string> stringList = new List<string>();
        //        string[] jsonObjects = inputJson.TrimStart('[').TrimEnd(']').Split(new[] { "}," }, StringSplitOptions.RemoveEmptyEntries);

        //        foreach (string jsonObject in jsonObjects)
        //        {
        //            if (!string.IsNullOrEmpty(jsonObject))
        //            {
        //                string trimmedJsonObject = jsonObject.Trim();
        //                if (!jsonObject.StartsWith("{"))
        //                {
        //                    trimmedJsonObject = "{" + jsonObject;
        //                }
        //                if (!jsonObject.EndsWith("}"))
        //                {
        //                    trimmedJsonObject = jsonObject + "}";
        //                }

        //                JObject inputObj = JObject.Parse(trimmedJsonObject);

        //                JValue oldInclusionValue = (JValue)inputObj["addRecogFilter"][0]["inclusion"];
        //                JValue oldExclusionValue = (JValue)inputObj["addRecogFilter"][0]["exclusion"];
        //                string inclusionValue1 = (string)oldInclusionValue.Value;
        //                string exclusionValue1 = (string)oldExclusionValue.Value;


        //                var newInclusionObject = new JObject();
        //                newInclusionObject["operatorType"] = operatorType;
        //                newInclusionObject["value1"] = inclusionValue1;
        //                newInclusionObject["value2"] = "";

        //                var newExclusionObject = new JObject();
        //                newExclusionObject["operatorType"] = operatorType;
        //                newExclusionObject["value1"] = exclusionValue1;
        //                newExclusionObject["value2"] = "";

        //                var newFilterObject = new JObject();
        //                newFilterObject["inclusion"] = newInclusionObject;
        //                newFilterObject["exclusion"] = newExclusionObject;

        //                var newFilterArray = new JArray();
        //                newFilterArray.Add(newFilterObject);

        //                var outputObj = new JObject();
        //                outputObj["uniqueIdent"] = inputObj["uniqueIdent"];
        //                outputObj["sourceField"] = inputObj["sourceField"];
        //                outputObj["destinationField"] = inputObj["destinationField"];
        //                outputObj["exclude"] = inputObj["exclude"];
        //                outputObj["addRecogFilter"] = newFilterArray;

        //                string jsonString = outputObj.ToString();
        //                stringList.Add(jsonString);
        //            }
        //        }
        //        string outputJson1 = "[" + string.Join(",", stringList) + "]";
        //        return outputJson1;
        //    }
        //    catch (Exception e)
        //    {
        //        string error = e.Message;

        //        Console.WriteLine("Error: " + error);
        //        return null;
        //    }

        //}



    }
}

