using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvertApi.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using AutoMapper;

namespace AdvertApi.Services
{
    public class DynamoDbAdvertStorage : IAdvertStorageService
    {
        private readonly IMapper _mapper;

        public DynamoDbAdvertStorage(IMapper mapper)
        {
            _mapper = mapper;
        }

        public async Task<string> Add(AdvertModel model)
        {
            AdvertDbModel dbModel = _mapper.Map<AdvertDbModel>(model);

            dbModel.Id = new Guid().ToString();
            dbModel.CreationDateTime = DateTime.UtcNow;
            dbModel.Status = AdvertStatus.Pending;
            using (AmazonDynamoDBClient client = new AmazonDynamoDBClient())
            {
                using (DynamoDBContext context = new DynamoDBContext(client))
                {
                    await context.SaveAsync(dbModel);
                }
            }

            return dbModel.Id;
        }

        public async Task Confirm(ConfirmAdvertModel model)
        {
            using (AmazonDynamoDBClient client = new AmazonDynamoDBClient())
            {
                using (DynamoDBContext context = new DynamoDBContext(client))
                {
                    AdvertDbModel record = await context.LoadAsync<AdvertDbModel>(model.Id);
                    if (record == null) throw new KeyNotFoundException($"A record with ID={model.Id} was not found.");

                    if (model.Status == AdvertStatus.Active)
                    {
                        record.Status = AdvertStatus.Active;
                        await context.SaveAsync(record);
                    }
                    else
                    {
                        await context.DeleteAsync(record);
                    }
                }
            }
        }

        public async Task<bool> CheckHealthAsync()
        {
            using (AmazonDynamoDBClient client = new AmazonDynamoDBClient())
            {
                DescribeTableResponse tableData = await client.DescribeTableAsync("Adverts");
                return string.Compare(tableData.Table.TableStatus, "active", true) == 0;
            }
        }
        
        public async Task<List<AdvertModel>> GetAllAsync()
        {
            using (var client = new AmazonDynamoDBClient())
            {
                using (var context = new DynamoDBContext(client))
                {
                    var scanResult =
                        await context.ScanAsync<AdvertDbModel>(new List<ScanCondition>()).GetNextSetAsync();
                    return scanResult.Select(item => _mapper.Map<AdvertModel>(item)).ToList();
                }
            }
        }

        public async Task<AdvertModel> GetByIdAsync(string id)
        {
            using (var client = new AmazonDynamoDBClient())
            {
                using (var context = new DynamoDBContext(client))
                {
                    var dbModel = await context.LoadAsync<AdvertDbModel>(id);
                    if (dbModel != null) return _mapper.Map<AdvertModel>(dbModel);
                }
            }

            throw new KeyNotFoundException();
        }
    }
}