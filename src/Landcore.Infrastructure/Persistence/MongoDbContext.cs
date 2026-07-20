using Landcore.Domain.Common;
using Landcore.Domain.Entities;
using Landcore.Infrastructure.Configuration;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IMongoClient client, string databaseName)
    {
        _database = client.GetDatabase(databaseName);
    }

    public MongoDbContext(MongoDbSettings settings)
        : this(new MongoClient(settings.ConnectionString), settings.DatabaseName)
    {
    }

    public IMongoCollection<SuperMan> SuperMen => _database.GetCollection<SuperMan>("supermen");

    public IMongoCollection<Admin> Admins => _database.GetCollection<Admin>("admins");

    public IMongoCollection<Subscription> Subscriptions => _database.GetCollection<Subscription>("subscriptions");

    public IMongoCollection<Employee> Employees => _database.GetCollection<Employee>("employees");

    public IMongoCollection<Designation> Designations => _database.GetCollection<Designation>("designations");

    public IMongoCollection<Agent> Agents => _database.GetCollection<Agent>("agents");

    public IMongoCollection<Lead> Leads => _database.GetCollection<Lead>("leads");

    public IMongoCollection<Client> Clients => _database.GetCollection<Client>("clients");

    public IMongoCollection<Society> Societies => _database.GetCollection<Society>("societies");

    public IMongoCollection<Block> Blocks => _database.GetCollection<Block>("blocks");

    public IMongoCollection<Plot> Plots => _database.GetCollection<Plot>("plots");

    public IMongoCollection<Booking> Bookings => _database.GetCollection<Booking>("bookings");

    public IMongoCollection<InstallmentPlan> InstallmentPlans => _database.GetCollection<InstallmentPlan>("installmentPlans");

    public IMongoCollection<Payment> Payments => _database.GetCollection<Payment>("payments");

    public IMongoCollection<Cheque> Cheques => _database.GetCollection<Cheque>("cheques");

    public IMongoCollection<BankAccount> BankAccounts => _database.GetCollection<BankAccount>("bankAccounts");

    public IMongoCollection<Receipt> Receipts => _database.GetCollection<Receipt>("receipts");

    public IMongoCollection<GeneratedDocument> GeneratedDocuments => _database.GetCollection<GeneratedDocument>("generatedDocuments");

    public IMongoCollection<ApprovalRequest> ApprovalRequests => _database.GetCollection<ApprovalRequest>("approvalRequests");

    public IMongoCollection<RefundRecord> RefundRecords => _database.GetCollection<RefundRecord>("refundRecords");

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await SuperMen.Indexes.CreateOneAsync(
            new CreateIndexModel<SuperMan>(
                Builders<SuperMan>.IndexKeys.Ascending(x => x.Email),
                new CreateIndexOptions { Unique = true, Name = "Email_unique" }),
            cancellationToken: cancellationToken);

        await Admins.Indexes.CreateOneAsync(
            new CreateIndexModel<Admin>(
                Builders<Admin>.IndexKeys.Ascending(x => x.ContactEmail),
                new CreateIndexOptions { Unique = true, Name = "ContactEmail_unique" }),
            cancellationToken: cancellationToken);

        await Subscriptions.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Subscription>(
                Builders<Subscription>.IndexKeys.Ascending(x => x.AdminId),
                new CreateIndexOptions { Unique = true, Name = "AdminId_unique" }),
            new CreateIndexModel<Subscription>(
                Builders<Subscription>.IndexKeys.Ascending(x => x.NextDueDate),
                new CreateIndexOptions { Name = "NextDueDate_1" }),
        }, cancellationToken);

        await Employees.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Employee>(
                Builders<Employee>.IndexKeys.Ascending(x => x.AdminId).Ascending(x => x.Email),
                new CreateIndexOptions { Unique = true, Name = "AdminId_Email_unique" }),
            new CreateIndexModel<Employee>(
                Builders<Employee>.IndexKeys.Ascending(x => x.AdminId),
                new CreateIndexOptions { Name = "AdminId_1" }),
        }, cancellationToken);

        await Designations.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Designation>(
                Builders<Designation>.IndexKeys.Ascending(x => x.AdminId).Ascending(x => x.Name),
                new CreateIndexOptions { Unique = true, Name = "AdminId_Name_unique" }),
            new CreateIndexModel<Designation>(
                Builders<Designation>.IndexKeys.Ascending(x => x.AdminId),
                new CreateIndexOptions { Name = "AdminId_1" }),
        }, cancellationToken);

        await CreateAdminIdIndexAsync(Agents, cancellationToken);

        await CreateAdminIdIndexAsync(Leads, cancellationToken);

        await CreateAdminIdIndexAsync(Clients, cancellationToken);

        await Societies.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Society>(
                Builders<Society>.IndexKeys.Ascending(x => x.AdminId).Ascending(x => x.Name),
                new CreateIndexOptions { Unique = true, Name = "AdminId_Name_unique" }),
            new CreateIndexModel<Society>(
                Builders<Society>.IndexKeys.Ascending(x => x.AdminId),
                new CreateIndexOptions { Name = "AdminId_1" }),
        }, cancellationToken);

        await Blocks.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Block>(
                Builders<Block>.IndexKeys.Ascending(x => x.AdminId).Ascending(x => x.SocietyId).Ascending(x => x.Name),
                new CreateIndexOptions { Unique = true, Name = "AdminId_SocietyId_Name_unique" }),
            new CreateIndexModel<Block>(
                Builders<Block>.IndexKeys.Ascending(x => x.SocietyId),
                new CreateIndexOptions { Name = "SocietyId_1" }),
            new CreateIndexModel<Block>(
                Builders<Block>.IndexKeys.Ascending(x => x.AdminId),
                new CreateIndexOptions { Name = "AdminId_1" }),
        }, cancellationToken);

        await Plots.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Plot>(
                Builders<Plot>.IndexKeys.Ascending(x => x.AdminId).Ascending(x => x.BlockId).Ascending(x => x.PlotNumber),
                new CreateIndexOptions { Unique = true, Name = "AdminId_BlockId_PlotNumber_unique" }),
            new CreateIndexModel<Plot>(
                Builders<Plot>.IndexKeys.Ascending(x => x.BlockId),
                new CreateIndexOptions { Name = "BlockId_1" }),
            new CreateIndexModel<Plot>(
                Builders<Plot>.IndexKeys.Ascending(x => x.SocietyId),
                new CreateIndexOptions { Name = "SocietyId_1" }),
            new CreateIndexModel<Plot>(
                Builders<Plot>.IndexKeys.Ascending(x => x.AdminId),
                new CreateIndexOptions { Name = "AdminId_1" }),
        }, cancellationToken);

        await CreateAdminIdIndexAsync(Bookings, cancellationToken);

        await CreateAdminIdIndexAsync(InstallmentPlans, cancellationToken);

        await CreateAdminIdIndexAsync(Payments, cancellationToken);

        await CreateAdminIdIndexAsync(Cheques, cancellationToken);

        await CreateAdminIdIndexAsync(BankAccounts, cancellationToken);

        await Receipts.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Receipt>(
                Builders<Receipt>.IndexKeys.Ascending(x => x.AdminId).Ascending(x => x.ReceiptNumber),
                new CreateIndexOptions { Unique = true, Name = "AdminId_ReceiptNumber_unique" }),
            new CreateIndexModel<Receipt>(
                Builders<Receipt>.IndexKeys.Ascending(x => x.AdminId),
                new CreateIndexOptions { Name = "AdminId_1" }),
        }, cancellationToken);

        await CreateAdminIdIndexAsync(GeneratedDocuments, cancellationToken);

        await CreateAdminIdIndexAsync(ApprovalRequests, cancellationToken);

        await CreateAdminIdIndexAsync(RefundRecords, cancellationToken);
        await RefundRecords.Indexes.CreateOneAsync(
            new CreateIndexModel<RefundRecord>(
                Builders<RefundRecord>.IndexKeys.Ascending(x => x.PlotId),
                new CreateIndexOptions { Name = "PlotId_1" }),
            cancellationToken: cancellationToken);
    }

    private static Task CreateAdminIdIndexAsync<T>(IMongoCollection<T> collection, CancellationToken cancellationToken)
        where T : TenantEntity
    {
        return collection.Indexes.CreateOneAsync(
            new CreateIndexModel<T>(
                Builders<T>.IndexKeys.Ascending(x => x.AdminId),
                new CreateIndexOptions { Name = "AdminId_1" }),
            cancellationToken: cancellationToken);
    }
}
