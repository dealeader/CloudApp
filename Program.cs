using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ItemDb>(opt => opt.UseInMemoryDatabase("Items"));
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/items", async (CreateItemRequest request, ItemDb db) =>
{
    var newItem = new Item() 
    { 
        Id =Guid.NewGuid(), 
        Header = request.Header, 
        Description = request.Description 
    };
    db.Items.Add(newItem);
    await db.SaveChangesAsync().ConfigureAwait(false);

    return Results.Created($"/tasks/{newItem.Id}", newItem);
}).WithName("PostItem");

app.MapGet("/items/{id}", async (Guid id, ItemDb db) =>
    await db.Items.FindAsync(id) is Item item 
    ? Results.Ok(item) 
    : Results.NotFound()).WithName("GetItem");

app.MapGet("/items", async (ItemDb db) => await db.Items.ToListAsync());

app.MapPut("/items/{id}/", async (Guid id, UpdateItemRequest request, ItemDb db) =>
{
    var item = await db.Items.FindAsync(id);

    if (item is null) return Results.NotFound();

    item.Header = request.Header ?? item.Header;
    item.Description = request.Description ?? item.Description;

    await db.SaveChangesAsync().ConfigureAwait(false);

    return Results.NoContent();
});

app.MapDelete("/items/{id}", async (Guid id, ItemDb db) =>
{
    if (await db.Items.FindAsync(id) is Item item)
    {
        db.Items.Remove(item);
        await db.SaveChangesAsync().ConfigureAwait(false);
        return Results.Ok(item);
    }

    return Results.NotFound();
});

app.Run();

public class Item
{
    public Guid Id { get; set; } 
    public string Header { get; set; }
    public string? Description { get; set; }
}

internal record CreateItemRequest(string Header, string? Description);

internal record UpdateItemRequest(string? Header, string? Description);

class ItemDb : DbContext
{
    public ItemDb(DbContextOptions<ItemDb> options) : base(options) { }

    public DbSet<Item> Items => Set<Item>();
}