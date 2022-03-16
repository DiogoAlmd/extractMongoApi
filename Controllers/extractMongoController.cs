using extractMongoApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace extractMongoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class extractMongoController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public extractMongoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public FileResult POST([FromBody] RequestData data)
        {
            Config config = new Config()
            {
                url = _configuration.GetValue<string>("Settings:ConnectionStringMDB"),
                sortParams = _configuration.GetValue<string>("Settings:sortParams"),
                sort = _configuration.GetValue<int>("Settings:sort"),
                limit = _configuration.GetValue<int>("Settings:limit")

            };

            MongoClient dbClient = new MongoClient(config.url);

            IMongoDatabase database = dbClient.GetDatabase(_configuration.GetValue<string>("Settings:banco"));
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(_configuration.GetValue<string>("Settings:colecao"));

            SortDefinition<BsonDocument> sortDefinition;

            //SORT
            if (config.sort == -1)
            {
                sortDefinition = Builders<BsonDocument>.Sort.Descending(config.sortParams);
            }
            else
            {
                sortDefinition = Builders<BsonDocument>.Sort.Ascending(config.sortParams);
            }

            //FILTERS
            FilterDefinition<BsonDocument> filter = createFilters(data.filters);

            //FIELDS
            string projectionString = getFields(data.fields);

            //LIMIT
            List<BsonDocument> documents = new List<BsonDocument>();

            if (config.limit != -1)
            {
                documents = collection.Aggregate().Match(filter).Sort(sortDefinition).Limit(config.limit).Project(projectionString).ToList();
            }
            else
            {
                documents = collection.Aggregate().Match(filter).Sort(sortDefinition).Project(projectionString).ToList();
            }
            
            JsonWriterSettings jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson };
            byte[] dataBytes = Encoding.ASCII.GetBytes(documents.ToJson(jsonWriterSettings));

            return File(dataBytes, "application/json", "LogConversation.json");
        }

        public FilterDefinition<BsonDocument> createFilters(string filters)
        {
            List<FilterDefinition<BsonDocument>> filterDefinitions = new List<FilterDefinition<BsonDocument>>();

            List<string> listOfParams = filters.Split(';').ToList();

            foreach (var param in listOfParams)
            {

                if (string.IsNullOrEmpty(param)) continue;


                List<string> filterLine = param.Split(":").ToList();

                string parameter = filterLine[0];
                string value = filterLine[1];

                if (parameter == "userTime" || parameter == "robotTime")
                {
                    List<int> listDate = value.Split("-").ToList().Select(s => int.Parse(s)).ToList();
                    //List<int> listDate = dateParams.Select(s => int.Parse(s)).ToList();

                    DateTime startDate = new DateTime(listDate[0], listDate[1], listDate[2]);
                    DateTime endDate = new DateTime(listDate[3], listDate[4], listDate[5]);

                    filterDefinitions.Add(Builders<BsonDocument>.Filter.Gte(parameter, startDate));
                    filterDefinitions.Add(Builders<BsonDocument>.Filter.Lt(parameter, endDate));

                }
                else if (parameter == "integrationMessages")
                {

                    filterDefinitions.Add(Builders<BsonDocument>.Filter
                        .Regex(parameter, new BsonRegularExpression("/.*" + value + "*./i")));

                }
                else
                {
                    filterDefinitions.Add(Builders<BsonDocument>.Filter.Eq(parameter, value));
                }

            }

            return Builders<BsonDocument>.Filter.And(filterDefinitions);
        }

        public string getFields (string fields)
        {
            List<string> fieldsList = fields.Split(";").ToList();

            string projectionString = "";
            foreach (string field in fieldsList)
            {
                if (string.IsNullOrEmpty(field)) continue;

                projectionString += field + ": 1,";
            }

            if (fields.IndexOf("_id") == -1)
            {
                projectionString += " _id : 0,";
            }

            return "{" + projectionString.Remove(projectionString.Length - 1, 1) + "}";
        } 
    }

    
}
