using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Extensions.Configuration;
using conectarMongo.Models;
using Newtonsoft.Json;
using MongoDB.Bson.IO;
using System.Text;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using JsonConvert = Newtonsoft.Json.JsonConvert;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace conectarMongo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class extractMongoApi : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public extractMongoApi(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public FileResult POST([FromBody] requestData data)
        {

            string url = _configuration.GetValue<string>("Settings:ConnectionStringMDB");

            string sortParams = _configuration.GetValue<string>("Settings:sortParams");

            int sort = _configuration.GetValue<int>("Settings:sort");
            int limit = _configuration.GetValue<int>("Settings:limit");

            MongoClient dbClient = new MongoClient(url);

            IMongoDatabase database = dbClient.GetDatabase(_configuration.GetValue<string>("Settings:banco"));
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(_configuration.GetValue<string>("Settings:colecao"));

            SortDefinition<BsonDocument> sortDefinition;
            FilterDefinition<BsonDocument> filterDefinition;
            filterDefinition = Builders<BsonDocument>.Filter.Empty;

            //SORT
            if (sort == -1)
            {
                sortDefinition = Builders<BsonDocument>.Sort.Descending(sortParams);
            }
            else
            {
                sortDefinition = Builders<BsonDocument>.Sort.Ascending(sortParams);
            }

            //FILTERS
            List<string> listOfParams = data.filters.Split(';').ToList();

            List<FilterDefinition<BsonDocument>> filterDefinitions = new List<FilterDefinition<BsonDocument>>();

            foreach (var param in listOfParams)
            {

                if (String.IsNullOrEmpty(param))
                {
                    continue;
                }

                List<string> diogo_putinha = param.Split(":").ToList();

                var parameter = diogo_putinha[0];
                var value = diogo_putinha[1];

                if (parameter == "userTime" || parameter == "robotTime")
                {
                    List<int> listDate = value.Split("-").ToList().Select(s => int.Parse(s)).ToList();
                    //List<int> listDate = dateParams.Select(s => int.Parse(s)).ToList();

                    DateTime startDate = new DateTime(listDate[0], listDate[1], listDate[2]);
                    DateTime endDate = new DateTime(listDate[3], listDate[4], listDate[5]);

                    filterDefinitions.Add(Builders<BsonDocument>.Filter.Gte(parameter, startDate));
                    filterDefinitions.Add(Builders<BsonDocument>.Filter.Lt(parameter, endDate));

                }
                else if (parameter == "integrationMessages") {

                    filterDefinitions.Add(Builders<BsonDocument>.Filter
                        .Regex(parameter, new BsonRegularExpression("/.*" + value + "*./i")));

                } else {
                    filterDefinitions.Add(Builders<BsonDocument>.Filter.Eq(parameter, value));
                }

            }

            var filters = Builders<BsonDocument>.Filter.And(filterDefinitions);

            //FIELDS

            List<string> projectionStaging = data.fields.Split(";").ToList();

            string projectionString = "{ _id : 0";

            foreach (string para in projectionStaging)
            {
                projectionString += ", " +para + ": 1";
            }

            projectionString += "}";

            //LIMIT
            List<BsonDocument> documents = new List<BsonDocument>();

            if (limit != -1)
            {
                documents = collection.Aggregate().Match(filters).Sort(sortDefinition).Limit(limit).Project(projectionString).ToList();
            }
            else
            {
                documents = collection.Aggregate().Match(filters).Sort(sortDefinition).Project(projectionString).ToList();
            }


            
            var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson };
            var jsonResult = documents.ToJson(jsonWriterSettings);
            byte[] bytes = Encoding.ASCII.GetBytes(jsonResult.ToString());

            return File(bytes, "application/json", "LogConversation.json");
        }
    }
}
