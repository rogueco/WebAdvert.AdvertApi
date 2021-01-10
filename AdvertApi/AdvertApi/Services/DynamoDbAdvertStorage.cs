using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdvertApi.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
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
    }
}