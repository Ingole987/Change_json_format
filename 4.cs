using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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
                var connectionString = configuration.GetSection("ConnectionString").Value;
                var operatorType = configuration.GetSection("operatorType").Value;
                var IsDaylightSavingDef = configuration.GetSection("IsDaylightSaving").Value;
                var speechAnalyticsIngestionTimeZoneDef = configuration.GetSection("SpeechAnalyticsIngestionTimeZone").Value;
                var speechAnalyticsStoragePointTimeZoneDef = configuration.GetSection("SpeechAnalyticsStoragePointTimeZone").Value;
                var durationFilterDef = configuration.GetSection("DurationFilter").Value;
                //var encryptnewstring = configuration.GetSection("encryptnewstring").Value;
                //Console.WriteLine("Type of Connection string");
                //string resp = Console.ReadLine();

                //string decryptedconnectionString = "";
                //if (resp=="1")
                //{
                //    var encryptedconnectionString = Encrypt(connectionString);
                //     decryptedconnectionString = Decrypt(encryptedconnectionString);
                //}
                //else if (resp=="0")
                //{
                //     decryptedconnectionString = Decrypt(encryptnewstring);
                //}

                string sourceNameDef = configuration.GetSection("SourceName").Value;
                string sqlCommand = "";
                if (string.IsNullOrEmpty(sourceNameDef))
                {
                    sqlCommand = "SELECT SourceName,DestinationJSON FROM CallIngestionSettings";
                }
                else
                {
                    string[] sourceNames = sourceNameDef.Split(',');
                    string sourceNameList = string.Join(",", sourceNames.Select(s => $"'{s.Trim()}'"));

                    sqlCommand = $@"SELECT *
                      FROM CallIngestionSettings 
                      WHERE SourceName IN ({sourceNameList})";
                }

                SqlConnection connection = new SqlConnection(connectionString);
                SqlCommand selectCommand = new SqlCommand(sqlCommand, connection);
                connection.Open();
                selectCommand.CommandTimeout = 200;
                SqlDataReader reader = selectCommand.ExecuteReader();

                while (reader.Read())
                {
                    string sourceName = reader["SourceName"] != DBNull.Value ? (string)reader["SourceName"] : "";
                    string originalValue = reader["DestinationJSON"] != DBNull.Value ? (string)reader["DestinationJSON"] : "";
                    int isDaylightSaving = reader["IsDaylightSaving"] != DBNull.Value && !string.IsNullOrEmpty(reader["IsDaylightSaving"].ToString()) ? Convert.ToInt32(reader["IsDaylightSaving"]) : Convert.ToInt32(IsDaylightSavingDef);
                    int speechAnalyticsIngestionTimeZone = reader["SpeechAnalyticsIngestionTimeZone"] != DBNull.Value && !string.IsNullOrEmpty(reader["SpeechAnalyticsIngestionTimeZone"].ToString()) ? Convert.ToInt32(reader["SpeechAnalyticsIngestionTimeZone"]) : Convert.ToInt32(speechAnalyticsIngestionTimeZoneDef);
                    int speechAnalyticsStoragePointTimeZone = reader["SpeechAnalyticsStoragePointTimeZone"] != DBNull.Value && !string.IsNullOrEmpty(reader["SpeechAnalyticsStoragePointTimeZone"].ToString()) ? Convert.ToInt32(reader["SpeechAnalyticsStoragePointTimeZone"]) : Convert.ToInt32(speechAnalyticsStoragePointTimeZoneDef);
                    string durationFilter = reader["DurationFilter"] != DBNull.Value && !string.IsNullOrEmpty(reader["DurationFilter"].ToString()) ? reader["DurationFilter"].ToString() : durationFilterDef;

                    string processedValue = "";
                    if (originalValue.Contains("operatorType"))
                    {
                        processedValue = ReadJson2(originalValue, operatorType);
                    }
                    else
                    {
                        processedValue = ReadJson(originalValue, operatorType);
                    }

                    SqlCommand updateCommand = new SqlCommand($@"UPDATE CallIngestionSettings SET
                                                               DestinationJSON = @ProcessedValue, 
                                                               IsDaylightSaving = @IsDaylightSaving, 
                                                               SpeechAnalyticsIngestionTimeZone = @SpeechAnalyticsIngestionTimeZone,  
                                                               SpeechAnalyticsStoragePointTimeZone = @SpeechAnalyticsStoragePointTimeZone,
                                                               DurationFilter = @DurationFilter 
                                                               WHERE SourceName = @SourceName", connection);

                    updateCommand.Parameters.AddWithValue("@ProcessedValue", processedValue.Replace("  ", "").Replace(" ", ""));
                    updateCommand.Parameters.AddWithValue("@IsDaylightSaving", isDaylightSaving);
                    updateCommand.Parameters.AddWithValue("@SpeechAnalyticsIngestionTimeZone", speechAnalyticsIngestionTimeZone);
                    updateCommand.Parameters.AddWithValue("@SpeechAnalyticsStoragePointTimeZone", speechAnalyticsStoragePointTimeZone);
                    updateCommand.Parameters.AddWithValue("@DurationFilter", durationFilter);
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



        //public static string EncryptionKey = "Provana#10";

        //public static string Encrypt(string clearText)
        //{
        //    byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
        //    using (Aes encryptor = Aes.Create())
        //    {
        //        Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
        //        encryptor.Key = pdb.GetBytes(32);
        //        encryptor.IV = pdb.GetBytes(16);
        //        using (MemoryStream ms = new MemoryStream())
        //        {
        //            using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
        //            {
        //                cs.Write(clearBytes, 0, clearBytes.Length);
        //                cs.Close();
        //            }
        //            clearText = Convert.ToBase64String(ms.ToArray());
        //        }
        //    }
        //    return clearText;
        //}
        //public static string Decrypt(string cipherText)
        //{
        //    cipherText = cipherText.Replace(" ", "+");
        //    byte[] cipherBytes = Convert.FromBase64String(cipherText);
        //    using (Aes encryptor = Aes.Create())
        //    {
        //        Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
        //        encryptor.Key = pdb.GetBytes(32);
        //        encryptor.IV = pdb.GetBytes(16);
        //        using (MemoryStream ms = new MemoryStream())
        //        {
        //            using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
        //            {
        //                cs.Write(cipherBytes, 0, cipherBytes.Length);
        //                cs.Close();
        //            }
        //            cipherText = Encoding.Unicode.GetString(ms.ToArray());
        //        }
        //    }
        //    return cipherText;
        //}



    }
}

