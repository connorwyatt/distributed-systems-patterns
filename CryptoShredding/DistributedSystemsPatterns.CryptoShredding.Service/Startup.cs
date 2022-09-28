using System.Reflection;
using ConnorWyatt.EventSourcing;
using ConnorWyatt.EventSourcing.Subscriptions;
using ConnorWyatt.EventSourcing.Subscriptions.Mongo;
using ConnorWyatt.Mongo;
using DistributedSystemsPatterns.CryptoShredding.Data.Mongo;
using DistributedSystemsPatterns.CryptoShredding.Service.Crypto;
using DistributedSystemsPatterns.CryptoShredding.Service.Customers.Projections;
using MediatR;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace DistributedSystemsPatterns.CryptoShredding.Service;

public class Startup
{
  private readonly IConfiguration _configuration;

  public Startup(IConfiguration configuration) => _configuration = configuration;

  public void ConfigureServices(IServiceCollection services)
  {
    var executingAssembly = Assembly.GetExecutingAssembly();

    services.AddControllers()
      .AddJsonOptions(options => { options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb); });
    services.AddMediatR(executingAssembly);
    services.AddTransient<IClock>(_ => SystemClock.Instance);

    var eventStoreConfiguration = _configuration.GetRequiredSection("EventStore");
    var eventStoreClientOptions = GetEventStoreClientOptions(eventStoreConfiguration);

    var mongoDBConfiguration = _configuration.GetRequiredSection("MongoDB");
    var mongoDBOptions = GetMongoDBOptions(mongoDBConfiguration);

    services.AddEventStore(
        eventStoreClientOptions,
        new[] { GetType().Assembly, })
      .AddMongoSubscriptionCursorsRepository(
        mongoDBConfiguration.GetValue<string>("SubscriptionCursorsCollectionName"));

    services.AddMongoDB(mongoDBOptions);

    services.AddCrypto();

    services
      .AddMongoRepositories()
      .AddSubscriber<CustomersProjection>();
  }

  public void Configure(WebApplication app)
  {
    app.UseHttpsRedirection();

    app.MapControllers();
  }

  private static EventStoreClientOptions GetEventStoreClientOptions(IConfigurationSection eventStoreConfiguration) =>
    new(eventStoreConfiguration.GetValue<string>("ConnectionString"));

  private static MongoDBOptions GetMongoDBOptions(IConfiguration configuration) =>
    new(
      configuration.GetValue<string>("ConnectionString"),
      configuration.GetValue<string>("DatabaseName"));
}
