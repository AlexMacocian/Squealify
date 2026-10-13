using Squealify.Tests.Models;
using System.Data.Common;

namespace Squealify.Tests;

/// <summary>
/// Database scenarios shared by every dialect.
/// </summary>
public abstract class ContextTestsBase
{
    private DbConnection? connection;

    protected abstract SqlDialect Dialect { get; }

    protected abstract Task<DbConnection> OpenConnection();

    protected virtual Task DropTables(DbConnection connection) => Task.CompletedTask;

    [TestInitialize]
    public async Task Initialize()
    {
        this.connection = await this.OpenConnection();
        await this.DropTables(this.connection);
        await this.Accounts().CreateTableIfNotExists(CancellationToken.None);
        await this.Characters().CreateTableIfNotExists(CancellationToken.None);
        await this.StorageItems().CreateTableIfNotExists(CancellationToken.None);
        await this.Crests().CreateTableIfNotExists(CancellationToken.None);
        await this.Unlocks().CreateTableIfNotExists(CancellationToken.None);
        await this.AllTypes().CreateTableIfNotExists(CancellationToken.None);
        await this.Sequences().CreateTableIfNotExists(CancellationToken.None);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (this.connection is not null)
        {
            await this.connection.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task AllTypes_RoundTrip_PreservesExtremeValues()
    {
        using var context = this.AllTypes();
        var expected = CreateAllTypes();

        await context.Upsert(expected, CancellationToken.None);
        var actual = await context.Find(expected.Id, CancellationToken.None);

        Assert.IsNotNull(actual);
        AssertAllTypesEqual(expected, actual);
    }

    [TestMethod]
    public async Task AllTypes_RoundTrip_PreservesNullableValues()
    {
        using var context = this.AllTypes();
        var expected = CreateAllTypes();
        expected.NullableInt = int.MinValue;
        expected.NullableUInt = uint.MaxValue;
        expected.NullableGuid = Guid.NewGuid();
        expected.NullableString = "nullable";
        expected.NullableDateTimeOffset = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.FromHours(-5));
        expected.NullableBytes = [9, 8, 7];
        expected.NullableUIntEnum = UIntEnum.Max;
        expected.NullableCustom = new CustomId("custom-nullable");

        await context.Upsert(expected, CancellationToken.None);
        var actual = await context.Find(expected.Id, CancellationToken.None);

        Assert.IsNotNull(actual);
        AssertAllTypesEqual(expected, actual);
    }

    [TestMethod]
    public async Task Account_Insert_ReturnsGeneratedIds()
    {
        using var context = this.Accounts();

        var first = await context.Insert(new AccountDbo { Name = "first", Email = "first@test" }, CancellationToken.None);
        var second = await context.Insert(new AccountDbo { Name = "second" }, CancellationToken.None);

        Assert.IsGreaterThan(first, second);
        var found = await context.FindByName("first", CancellationToken.None);
        Assert.IsNotNull(found);
        Assert.AreEqual(first, found.Id);
        Assert.AreEqual("first@test", found.Email);
        Assert.IsNull((await context.Find(second, CancellationToken.None))!.Email);
    }

    [TestMethod]
    public async Task Account_DuplicateUniqueColumn_Throws()
    {
        using var context = this.Accounts();
        await context.Insert(new AccountDbo { Name = "duplicate" }, CancellationToken.None);

        await Assert.ThrowsAsync<DbException>(async () => await context.Insert(new AccountDbo { Name = "duplicate" }, CancellationToken.None));
    }

    [TestMethod]
    public async Task Character_FindUpdateDelete_Works()
    {
        using var characters = this.Characters();
        var accountId = await this.CreateAccount("characters");
        var id = await characters.Insert(new CharacterDbo { AccountId = accountId, FirstName = "Arisen", LastName = "One", GameMode = 1 }, CancellationToken.None);
        await characters.Insert(new CharacterDbo { AccountId = accountId, FirstName = "Arisen", LastName = "Two", GameMode = 1 }, CancellationToken.None);
        await characters.Insert(new CharacterDbo { AccountId = accountId, FirstName = "Arisen", LastName = "Three", GameMode = 2 }, CancellationToken.None);

        var byName = await characters.FindByFirstNameAndLastName("Arisen", "One", CancellationToken.None);
        var byMode = await ToList(characters.FindByAccountIdAndGameMode(accountId, 1));

        Assert.IsNotNull(byName);
        Assert.AreEqual(id, byName.Id);
        Assert.HasCount(2, byMode);

        byName.GameMode = 2;
        Assert.AreEqual(1, await characters.Update(byName, CancellationToken.None));
        Assert.HasCount(2, await ToList(characters.FindByAccountIdAndGameMode(accountId, 2)));
        Assert.AreEqual(2, await characters.DeleteByAccountIdAndGameMode(accountId, 2, CancellationToken.None));
        Assert.AreEqual(1, await characters.Delete((await ToList(characters.FindAll())).Single().Id, CancellationToken.None));
        Assert.IsEmpty(await ToList(characters.FindAll()));
    }

    [TestMethod]
    public async Task StorageItem_CompositeKey_UpsertFindDelete()
    {
        using var items = this.StorageItems();
        var characterId = await this.CreateCharacter("composite");
        var item = new StorageItemDbo { CharacterId = characterId, StorageType = StorageType.StorageBox, SlotNo = ushort.MaxValue, ItemUId = "uid", Num = uint.MaxValue };

        await items.Upsert(item, CancellationToken.None);
        item.Num = 5;
        await items.Upsert(item, CancellationToken.None);
        var found = await items.Find(characterId, StorageType.StorageBox, ushort.MaxValue, CancellationToken.None);

        Assert.IsNotNull(found);
        Assert.AreEqual(5u, found.Num);
        Assert.AreEqual("uid", found.ItemUId);
        Assert.HasCount(1, await ToList(items.FindByCharacterId(characterId)));
        Assert.IsNull(await items.Find(characterId, StorageType.ItemBag, ushort.MaxValue, CancellationToken.None));
        Assert.AreEqual(1, await items.Delete(characterId, StorageType.StorageBox, ushort.MaxValue, CancellationToken.None));
    }

    [TestMethod]
    public async Task StorageItem_Parameters_UseStorageTypes()
    {
        using var items = this.StorageItems();
        var characterId = await this.CreateCharacter("types");

        await items.Upsert(new StorageItemDbo { CharacterId = characterId, StorageType = StorageType.ItemBag, SlotNo = 1, ItemUId = "a", Num = 1 }, CancellationToken.None);

        CollectionAssert.AreEqual(
            new[] { typeof(long), typeof(short), typeof(int), typeof(string), typeof(long) },
            items.ParameterTypes);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(9)]
    [DataRow(150)]
    public async Task StorageItem_ManyOperations_HandleAllBatchSizes(int count)
    {
        using var items = this.StorageItems();
        var characterId = await this.CreateCharacter($"many{count}");
        var rows = Enumerable.Range(0, count)
            .Select(i => new StorageItemDbo { CharacterId = characterId, StorageType = StorageType.ItemBag, SlotNo = (ushort)i, ItemUId = $"uid{i}", Num = (uint)i })
            .ToList();

        await items.UpsertMany(rows, CancellationToken.None);
        foreach (var row in rows)
        {
            row.Num += 1000;
        }

        await items.UpsertMany(rows, CancellationToken.None);
        var stored = await ToList(items.FindByCharacterId(characterId));

        Assert.HasCount(count, stored);
        Assert.IsTrue(stored.All(s => s.Num == s.SlotNo + 1000u));
        Assert.AreEqual(count, await items.UpdateMany(rows, CancellationToken.None));
        Assert.AreEqual(count, await items.DeleteMany(rows, CancellationToken.None));
        Assert.IsEmpty(await ToList(items.FindByCharacterId(characterId)));
    }

    [TestMethod]
    public async Task Crest_CompositeKey_DeleteManyUsesRowValues()
    {
        using var crests = this.Crests();
        var characterId = await this.CreateCharacter("crests");
        await this.StorageItems().Upsert(new StorageItemDbo { CharacterId = characterId, StorageType = StorageType.ItemBag, SlotNo = 1, ItemUId = "a", Num = 1 }, CancellationToken.None);
        var rows = Enumerable.Range(0, 10)
            .Select(i => new CrestDbo { CharacterId = characterId, StorageType = StorageType.ItemBag, SlotNo = 1, CrestSlot = (byte)i, CrestId = uint.MaxValue - (uint)i })
            .ToList();

        await crests.InsertMany(rows, CancellationToken.None);

        Assert.HasCount(10, await ToList(crests.FindAll()));
        Assert.AreEqual(4, await crests.DeleteMany(rows.Take(4).ToList(), CancellationToken.None));
        Assert.HasCount(6, await ToList(crests.FindAll()));
    }

    [TestMethod]
    public async Task Character_InsertMany_WithAutoIncrement()
    {
        using var characters = this.Characters();
        var accountId = await this.CreateAccount("insertmany");
        var rows = Enumerable.Range(0, 75)
            .Select(i => new CharacterDbo { AccountId = accountId, FirstName = "Bulk", LastName = $"N{i}", GameMode = 3 })
            .ToList();

        await characters.InsertMany(rows, CancellationToken.None);

        var stored = await ToList(characters.FindByAccountIdAndGameMode(accountId, 3));
        Assert.HasCount(75, stored);
        Assert.HasCount(75, stored.Select(s => s.Id).Distinct());
    }

    [TestMethod]
    public async Task Unlock_KeyOnlyTable_UpsertIsIdempotent()
    {
        using var unlocks = this.Unlocks();
        var unlock = new UnlockDbo { CharacterId = 1, Category = 2, Value = uint.MaxValue };

        await unlocks.Upsert(unlock, CancellationToken.None);
        await unlocks.Upsert(unlock, CancellationToken.None);
        await unlocks.UpsertMany([unlock, new UnlockDbo { CharacterId = 1, Category = 2, Value = 1 }], CancellationToken.None);

        Assert.HasCount(2, await ToList(unlocks.FindAll()));
        Assert.IsNotNull(await unlocks.Find(1, 2, uint.MaxValue, CancellationToken.None));
    }

    [TestMethod]
    public async Task ForeignKeys_OnDeleteCascade_RemovesDependents()
    {
        using var accounts = this.Accounts();
        using var crests = this.Crests();
        using var items = this.StorageItems();
        var accountId = await this.CreateAccount("cascade");
        var characterId = await this.Characters().Insert(new CharacterDbo { AccountId = accountId, FirstName = "Cascade", LastName = "C", GameMode = 0 }, CancellationToken.None);
        await items.Upsert(new StorageItemDbo { CharacterId = characterId, StorageType = StorageType.ItemBag, SlotNo = 1, ItemUId = "a", Num = 1 }, CancellationToken.None);
        await crests.Upsert(new CrestDbo { CharacterId = characterId, StorageType = StorageType.ItemBag, SlotNo = 1, CrestSlot = 0, CrestId = 1 }, CancellationToken.None);

        Assert.AreEqual(1, await accounts.Delete(accountId, CancellationToken.None));

        Assert.IsEmpty(await ToList(this.Characters().FindAll()));
        Assert.IsEmpty(await ToList(items.FindAll()));
        Assert.IsEmpty(await ToList(crests.FindAll()));
    }

    [TestMethod]
    public async Task Transaction_Rollback_DiscardsCachedCommandWrites()
    {
        using var unlocks = this.Unlocks();
        await unlocks.Upsert(new UnlockDbo { CharacterId = 7, Category = 1, Value = 1 }, CancellationToken.None);

        await using (var transaction = await this.connection!.BeginTransactionAsync())
        {
            unlocks.Transaction = transaction;
            await unlocks.Upsert(new UnlockDbo { CharacterId = 7, Category = 1, Value = 2 }, CancellationToken.None);
            await unlocks.UpsertMany([new UnlockDbo { CharacterId = 7, Category = 1, Value = 3 }], CancellationToken.None);
            Assert.HasCount(3, await ToList(unlocks.FindAll()));
            await transaction.RollbackAsync();
        }

        unlocks.Transaction = null;
        Assert.HasCount(1, await ToList(unlocks.FindAll()));

        await using (var transaction = await this.connection!.BeginTransactionAsync())
        {
            unlocks.Transaction = transaction;
            await unlocks.Upsert(new UnlockDbo { CharacterId = 7, Category = 1, Value = 4 }, CancellationToken.None);
            await transaction.CommitAsync();
        }

        unlocks.Transaction = null;
        Assert.HasCount(2, await ToList(unlocks.FindAll()));
    }

    [TestMethod]
    public async Task Sequence_AutoIncrementOnly_InsertsDefaultValues()
    {
        using var sequences = this.Sequences();

        var first = await sequences.Insert(new SequenceDbo(), CancellationToken.None);
        var second = await sequences.Insert(new SequenceDbo(), CancellationToken.None);
        await sequences.InsertMany([new SequenceDbo(), new SequenceDbo()], CancellationToken.None);

        Assert.IsGreaterThan(first, second);
        Assert.HasCount(4, await ToList(sequences.FindAll()));
    }

    [TestMethod]
    public async Task Dispose_ThenReuse_RecreatesCommands()
    {
        var unlocks = this.Unlocks();
        await unlocks.Upsert(new UnlockDbo { CharacterId = 8, Category = 1, Value = 1 }, CancellationToken.None);

        await unlocks.DisposeAsync();
        await unlocks.Upsert(new UnlockDbo { CharacterId = 8, Category = 1, Value = 2 }, CancellationToken.None);
        unlocks.Dispose();

        Assert.HasCount(2, await ToList(unlocks.FindAll()));
    }

    protected AccountContext Accounts() => new(this.connection!, this.Dialect);

    protected CharacterContext Characters() => new(this.connection!, this.Dialect);

    protected StorageItemContext StorageItems() => new(this.connection!, this.Dialect);

    protected CrestContext Crests() => new(this.connection!, this.Dialect);

    protected UnlockContext Unlocks() => new(this.connection!, this.Dialect);

    protected AllTypesContext AllTypes() => new(this.connection!, this.Dialect);

    protected SequenceContext Sequences() => new(this.connection!, this.Dialect);

    private async Task<int> CreateAccount(string name)
        => await this.Accounts().Insert(new AccountDbo { Name = name }, CancellationToken.None);

    private async Task<long> CreateCharacter(string name)
        => await this.Characters().Insert(new CharacterDbo { AccountId = await this.CreateAccount(name), FirstName = name, LastName = name, GameMode = 0 }, CancellationToken.None);

    private static async Task<List<T>> ToList<T>(IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source)
        {
            list.Add(item);
        }

        return list;
    }

    private static AllTypesDbo CreateAllTypes() => new()
    {
        Id = Guid.NewGuid(),
        Bool = true,
        Byte = byte.MaxValue,
        SByte = sbyte.MinValue,
        Short = short.MinValue,
        UShort = ushort.MaxValue,
        Int = int.MinValue,
        UInt = uint.MaxValue,
        Long = long.MinValue,
        ULong = ulong.MaxValue,
        Float = 1.5f,
        Double = Math.PI,
        Decimal = 1234567890.123456789m,
        Char = 'Ω',
        String = "Dragon's Dogma \"Online\" ドラゴンズドグマ",
        DateTime = new DateTime(2026, 10, 13, 10, 11, 12, DateTimeKind.Utc),
        DateTimeOffset = new DateTimeOffset(2026, 10, 13, 10, 11, 12, TimeSpan.FromHours(2)),
        DateOnly = new DateOnly(2026, 10, 13),
        TimeOnly = new TimeOnly(23, 59, 58),
        TimeSpan = new TimeSpan(1, 2, 3, 4),
        Bytes = [0, 1, 255],
        ByteEnum = ByteEnum.Max,
        UIntEnum = UIntEnum.Max,
        Custom = new CustomId("custom")
    };

    private static void AssertAllTypesEqual(AllTypesDbo expected, AllTypesDbo actual)
    {
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.Bool, actual.Bool);
        Assert.AreEqual(expected.Byte, actual.Byte);
        Assert.AreEqual(expected.SByte, actual.SByte);
        Assert.AreEqual(expected.Short, actual.Short);
        Assert.AreEqual(expected.UShort, actual.UShort);
        Assert.AreEqual(expected.Int, actual.Int);
        Assert.AreEqual(expected.UInt, actual.UInt);
        Assert.AreEqual(expected.Long, actual.Long);
        Assert.AreEqual(expected.ULong, actual.ULong);
        Assert.AreEqual(expected.Float, actual.Float);
        Assert.AreEqual(expected.Double, actual.Double);
        Assert.AreEqual(expected.Decimal, actual.Decimal);
        Assert.AreEqual(expected.Char, actual.Char);
        Assert.AreEqual(expected.String, actual.String);
        Assert.AreEqual(expected.DateTime.Ticks, actual.DateTime.Ticks);
        Assert.AreEqual(expected.DateTimeOffset.UtcDateTime, actual.DateTimeOffset.UtcDateTime);
        Assert.AreEqual(expected.DateOnly, actual.DateOnly);
        Assert.AreEqual(expected.TimeOnly, actual.TimeOnly);
        Assert.AreEqual(expected.TimeSpan, actual.TimeSpan);
        CollectionAssert.AreEqual(expected.Bytes, actual.Bytes);
        Assert.AreEqual(expected.ByteEnum, actual.ByteEnum);
        Assert.AreEqual(expected.UIntEnum, actual.UIntEnum);
        Assert.AreEqual(expected.Custom, actual.Custom);
        Assert.AreEqual(expected.NullableInt, actual.NullableInt);
        Assert.AreEqual(expected.NullableUInt, actual.NullableUInt);
        Assert.AreEqual(expected.NullableGuid, actual.NullableGuid);
        Assert.AreEqual(expected.NullableString, actual.NullableString);
        Assert.AreEqual(expected.NullableDateTimeOffset?.UtcDateTime, actual.NullableDateTimeOffset?.UtcDateTime);
        CollectionAssert.AreEqual(expected.NullableBytes, actual.NullableBytes);
        Assert.AreEqual(expected.NullableUIntEnum, actual.NullableUIntEnum);
        Assert.AreEqual(expected.NullableCustom, actual.NullableCustom);
    }
}
