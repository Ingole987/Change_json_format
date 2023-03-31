using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Json_Convert_string_to_keyvaluepairs_
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfigurationRoot config = configuration.Build();
            await DestinationJSON(config);
        }

        public static async Task DestinationJSON(IConfiguration configuration)
        {
            try
            {
                var connectionString = configuration.GetSection("ConnectionString").Value;
                var operatorType = configuration.GetSection("operatorType").Value;
                var IsDaylightSavingDef = configuration.GetSection("IsDaylightSaving").Value;
                var speechAnalyticsIngestionTimeZoneDef = configuration.GetSection("SpeechAnalyticsIngestionTimeZone").Value;
                var speechAnalyticsStoragePointTimeZoneDef = configuration.GetSection("SpeechAnalyticsStoragePointTimeZone").Value;
                var durationFilterDef = configuration.GetSection("DurationFilter").Value;
                string sourceNameDef = configuration.GetSection("SourceName").Value;
                string typeChange = configuration.GetSection("typeChange").Value;
                string isMatch = configuration.GetSection("isMatch").Value;

                string sqlCommand = "";
                if (string.IsNullOrEmpty(sourceNameDef))
                {
                    sqlCommand = $@"SELECT * FROM CallIngestionSettings";
                }
                else
                {
                    string[] sourceNames = sourceNameDef.Split(',');
                    string sourceNameList = string.Join(",", sourceNames.Select(s => $"'{s.Trim()}'"));

                    sqlCommand = $@"SELECT *
                      FROM CallIngestionSettings 
                      WHERE SourceName IN ({sourceNameList})";
                }

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(sqlCommand, connection))
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            try
                            {
                                string sourceName = reader["SourceName"] != DBNull.Value ? (string)reader["SourceName"] : "";
                                string originalValue2 = reader["DestinationJSON"] != DBNull.Value ? (string)reader["DestinationJSON"] : "";
                                int primaryKey = reader["Id"] != DBNull.Value ? (int)reader["Id"] : 0;
                                string originalValue1 = reader["TransformJSON"] != DBNull.Value ? (string)reader["TransformJSON"] : "";

                                string processedValue = "";
                                if (originalValue2.Contains("operatorType") && typeChange == "operator")
                                {
                                    processedValue = ReadJson2(originalValue1, operatorType);
                                    using (SqlCommand updateCommand = new SqlCommand($@"UPDATE CallIngestionSettings SET
                                                           DestinationJSON = @ProcessedValue 
                                                           WHERE ID = @PrimaryKey AND SourceName = @SourceName", connection))
                                    {
                                        updateCommand.Parameters.AddWithValue("@ProcessedValue", processedValue);
                                        updateCommand.Parameters.AddWithValue("@SourceName", sourceName);
                                        updateCommand.Parameters.AddWithValue("@PrimaryKey", primaryKey);
                                        Console.WriteLine("Successful for :" + sourceName);
                                        await updateCommand.ExecuteNonQueryAsync();
                                    }
                                }
                                else if (originalValue2.Contains("operatorType") && typeChange == "header")
                                {
                                    processedValue = ReadJson3(originalValue1, originalValue2, isMatch);
                                    using (SqlCommand updateCommand = new SqlCommand($@"UPDATE CallIngestionSettings SET
                                                           DestinationJSON = @ProcessedValue
                                                           WHERE ID = @PrimaryKey AND SourceName = @SourceName", connection))
                                    {
                                        updateCommand.Parameters.AddWithValue("@ProcessedValue", processedValue);
                                        updateCommand.Parameters.AddWithValue("@SourceName", sourceName);
                                        updateCommand.Parameters.AddWithValue("@PrimaryKey", primaryKey);
                                        Console.WriteLine("Successful for :" + sourceName);
                                        await updateCommand.ExecuteNonQueryAsync();
                                    }
                                }
                                else if (typeChange == "migration")
                                {
                                    bool isDaylightSaving = reader["IsDaylightSaving"] != DBNull.Value || !string.IsNullOrEmpty(reader["IsDaylightSaving"].ToString()) ? reader.GetBoolean(reader.GetOrdinal("IsDaylightSaving")) : Convert.ToBoolean(IsDaylightSavingDef);
                                    int speechAnalyticsIngestionTimeZone = reader["SpeechAnalyticsIngestionTimeZone"] != DBNull.Value || !string.IsNullOrEmpty(reader["SpeechAnalyticsIngestionTimeZone"].ToString()) ? Convert.ToInt32(reader["SpeechAnalyticsIngestionTimeZone"]) : Convert.ToInt32(speechAnalyticsIngestionTimeZoneDef);
                                    int speechAnalyticsStoragePointTimeZone = reader["SpeechAnalyticsStoragePointTimeZone"] != DBNull.Value || !string.IsNullOrEmpty(reader["SpeechAnalyticsStoragePointTimeZone"].ToString()) ? Convert.ToInt32(reader["SpeechAnalyticsStoragePointTimeZone"]) : Convert.ToInt32(speechAnalyticsStoragePointTimeZoneDef);
                                    string durationFilter = reader["DurationFilter"] != DBNull.Value || !string.IsNullOrEmpty(reader["DurationFilter"].ToString()) ? (string)reader["DurationFilter"] : durationFilterDef;


                                    processedValue = ReadJson(originalValue2, operatorType);
                                    using (SqlCommand updateCommand = new SqlCommand($@"UPDATE CallIngestionSettings SET
                                                           DestinationJSON = @ProcessedValue, 
                                                           IsDaylightSaving = @IsDaylightSaving, 
                                                           SpeechAnalyticsIngestionTimeZone = @SpeechAnalyticsIngestionTimeZone,  
                                                           SpeechAnalyticsStoragePointTimeZone = @SpeechAnalyticsStoragePointTimeZone,
                                                           DurationFilter = @DurationFilter 
                                                           WHERE ID = @PrimaryKey AND SourceName = @SourceName", connection))
                                    {
                                        updateCommand.Parameters.AddWithValue("@ProcessedValue", processedValue);
                                        updateCommand.Parameters.AddWithValue("@IsDaylightSaving", isDaylightSaving);
                                        updateCommand.Parameters.AddWithValue("@SpeechAnalyticsIngestionTimeZone", speechAnalyticsIngestionTimeZone);
                                        updateCommand.Parameters.AddWithValue("@SpeechAnalyticsStoragePointTimeZone", speechAnalyticsStoragePointTimeZone);
                                        updateCommand.Parameters.AddWithValue("@DurationFilter", durationFilter);
                                        updateCommand.Parameters.AddWithValue("@SourceName", sourceName);
                                        updateCommand.Parameters.AddWithValue("@PrimaryKey", primaryKey);
                                        Console.WriteLine("Successful for :" + sourceName);
                                        await updateCommand.ExecuteNonQueryAsync();
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("No action was selected");
                                    return;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message, ex.Data);
                            }
                        }
                    }
                }
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
            try
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
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public static string ReadJson3(string inputJson1, string inputJson2 , string isMatch)
        {
            try
            {
                List<string> stringList = new List<string>();
                JArray inputArray1 = JArray.Parse(inputJson1);
                JArray inputArray2 = JArray.Parse(inputJson2);

                JObject inputObj1 = inputArray1[1] as JObject;

                Dictionary<string, string> metadataDict = new Dictionary<string, string>();

                if (inputObj1.ContainsKey("sourceMetadata") && inputObj1["sourceMetadata"] != null && inputObj1["sourceMetadata"].Any())
                {
                    var sourceMetadataDict = inputObj1["sourceMetadata"]
                        .Select(obj => new
                        {
                            UniqueIdent = (string)obj["uniqueIdent"],
                            HeaderName = (string.IsNullOrEmpty((string)obj["headerName"]) ?
                                         (string.IsNullOrEmpty((string)obj["headerOriginal"]) ?
                                         (string)obj["headerActualName"] :
                                         (string)obj["headerOriginal"]) :
                                         (string)obj["headerName"])
                        })
                        .ToDictionary(obj => obj.UniqueIdent, obj => obj.HeaderName);

                    metadataDict = metadataDict.Concat(sourceMetadataDict).ToDictionary(pair => pair.Key, pair => pair.Value);
                }

                if (inputObj1.ContainsKey("customMetadata") && inputObj1["customMetadata"] != null && inputObj1["customMetadata"].Any())
                {
                    var customMetadataDict = inputObj1["customMetadata"]
                        .Select(obj => new
                        {
                            UniqueIdent = (string)obj["uniqueIdent"],
                            HeaderName = (string.IsNullOrEmpty((string)obj["headerName"]) ?
                                         (string.IsNullOrEmpty((string)obj["headerOriginal"]) ?
                                         (string)obj["headerActualName"] :
                                         (string)obj["headerOriginal"]) :
                                         (string)obj["headerName"])
                        })
                        .ToDictionary(obj => obj.UniqueIdent, obj => obj.HeaderName);

                    metadataDict = metadataDict.Concat(customMetadataDict).ToDictionary(pair => pair.Key, pair => pair.Value);
                }

                if (inputObj1.ContainsKey("supplementalMetadata") && inputObj1["supplementalMetadata"] != null && inputObj1["supplementalMetadata"].Any())
                {
                    var supplementalMetadataDict = inputObj1["supplementalMetadata"]
                        .Select(obj => new
                        {
                            UniqueIdent = (string)obj["uniqueIdent"],
                            HeaderName = (string.IsNullOrEmpty((string)obj["headerName"]) ?
                                         (string.IsNullOrEmpty((string)obj["headerOriginal"]) ?
                                         (string)obj["headerActualName"] :
                                         (string)obj["headerOriginal"]) :
                                         (string)obj["headerName"])
                        })
                        .ToDictionary(obj => obj.UniqueIdent, obj => obj.HeaderName);

                    metadataDict = metadataDict.Concat(supplementalMetadataDict).ToDictionary(pair => pair.Key, pair => pair.Value);
                }

                if (inputObj1.ContainsKey("supplementalStaticData") && inputObj1["supplementalStaticData"] != null && inputObj1["supplementalStaticData"].Any())
                {
                    var supplementalStaticDataDict = inputObj1["supplementalStaticData"]
                        .Select(obj => new
                        {
                            UniqueIdent = (string)obj["uniqueIdent"],
                            HeaderName = (string.IsNullOrEmpty((string)obj["headerName"]) ?
                                         (string.IsNullOrEmpty((string)obj["headerOriginal"]) ?
                                         (string)obj["headerActualName"] :
                                         (string)obj["headerOriginal"]) :
                                         (string)obj["headerName"])
                        })
                        .ToDictionary(obj => obj.UniqueIdent, obj => obj.HeaderName);

                    metadataDict = metadataDict.Concat(supplementalStaticDataDict).ToDictionary(pair => pair.Key, pair => pair.Value);
                }

                foreach (JObject inputObj2 in inputArray2.Children<JObject>())
                {
                    JValue oldsourceField = (JValue)inputObj2["sourceField"];
                    JValue uniqueIdentDestination = (JValue)inputObj2["uniqueIdent"];

                    if (metadataDict.ContainsKey(uniqueIdentDestination.ToString()) && !string.IsNullOrEmpty(metadataDict[uniqueIdentDestination.ToString()]))
                    {
                        string tranformsourceField = metadataDict[uniqueIdentDestination.ToString()];
                        string tranformsourceField2 = tranformsourceField.Replace(" ", "").Replace("  ", "");
                        if (oldsourceField.Value.ToString() == tranformsourceField2 && isMatch == "1")
                        {
                            oldsourceField.Value = metadataDict[uniqueIdentDestination.ToString()];
                        }else 
                        { 
                            oldsourceField.Value = metadataDict[uniqueIdentDestination.ToString()];
                        }
                    }

                    string jsonString = inputObj2.ToString();
                    stringList.Add(jsonString);
                }
                string outputJson = "[" + string.Join(",", stringList) + "]";
                return outputJson;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        //public static string ReadJson3(string inputJson1, string inputJson2)
        //{
        //    try
        //    {
        //        List<string> stringList = new List<string>();
        //        JArray inputArray1 = JArray.Parse(inputJson1);
        //        JArray inputArray2 = JArray.Parse(inputJson2);

        //        JObject inputObj1 = inputArray1[1] as JObject;

        //        JArray sourceMetadataArray = (JArray)inputObj1["sourceMetadata"];

        //        Dictionary<string, string> metadataDict = new Dictionary<string, string>();

        //        foreach (JObject sourceMetadataObj in sourceMetadataArray.Children<JObject>())
        //        {
        //            JValue uniqueIdentValue = (JValue)sourceMetadataObj["uniqueIdent"];
        //            JValue headerNameValue1 = (JValue)sourceMetadataObj["headerOriginal"];
        //            JValue headerNameValue2 = (JValue)sourceMetadataObj["headerName"];
        //            JValue headerNameValue3 = (JValue)sourceMetadataObj["headerActualName"];

        //            string headerNameValue = "";
        //            if (headerNameValue1 != null && !string.IsNullOrEmpty(headerNameValue1.ToString()))
        //            {
        //                headerNameValue = headerNameValue1.ToString();
        //            }
        //            else if (headerNameValue2 != null && !string.IsNullOrEmpty(headerNameValue2.ToString()))
        //            {
        //                headerNameValue = headerNameValue2.ToString();
        //            }
        //            else if (headerNameValue3 != null && !string.IsNullOrEmpty(headerNameValue3.ToString()))
        //            {
        //                headerNameValue = headerNameValue3.ToString();
        //            }
        //            else
        //            {
        //                headerNameValue = "";
        //            }

        //            metadataDict.Add(uniqueIdentValue.ToString(), headerNameValue.ToString());

        //        }
        //        foreach (JObject inputObj2 in inputArray2.Children<JObject>())
        //        {
        //            JValue oldsourceField = (JValue)inputObj2["sourceField"];
        //            JValue uniqueIdentDestination = (JValue)inputObj2["uniqueIdent"];

        //            if (metadataDict.ContainsKey(uniqueIdentDestination.ToString()))
        //            {
        //                oldsourceField.Value = metadataDict[uniqueIdentDestination.ToString()];
        //            }
        //            string jsonString = inputObj2.ToString();
        //            stringList.Add(jsonString);
        //        }
        //        string outputJson = "[" + string.Join(",", stringList) + "]";
        //        return outputJson;
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex.Message;
        //    }
        //}
    }
}

