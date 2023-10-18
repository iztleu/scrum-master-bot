using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

var connectionString = "mongodb://localhost:27017/?retryWrites=true";

if (connectionString == null)
{
    Console.WriteLine("You must set your 'MONGODB_URI' environment variable. To learn how to set it, see https://www.mongodb.com/docs/drivers/csharp/current/quick-start/#set-your-connection-string");
    Environment.Exit(0);
}
var client = new MongoClient(connectionString);

using var db = MflixDbContext.Create(client.GetDatabase("sample_mflix"));

var director = new Director()
{
    _id = ObjectId.GenerateNewId(),
    name = "John Doe",
    birthdate = "01/01/2000"
};

// var studio = new Studio()
// {
//     _id = ObjectId.GenerateNewId(),
//     name = "Studio 1",
//     address = "Address 1",
//     directors = new Director[] { director }
// };

var movie = new Movie()
{
    _id = ObjectId.GenerateNewId(),
    title = "Movie 1",
    rated = "PG-13",
    plot = "Plot 1",
    fullplot = "Full Plot 1",
    director = new Director(),
};

db.Directors.Add(director);
db.Movies.Add(movie);

await db.SaveChangesAsync();



// var directors = await db.Directors.ToListAsync();
// var studios = await db.Studios.ToListAsync();
var new_movie = await db.Movies.FirstOrDefaultAsync();

Console.WriteLine(new_movie);


internal class MflixDbContext : DbContext
{
    public DbSet<Movie> Movies { get; init; }
    public DbSet<Director> Directors { get; init; } 

    public static MflixDbContext Create(IMongoDatabase database) =>
        new(new DbContextOptionsBuilder<MflixDbContext>()
            .UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName)
            .Options);

    public MflixDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        
       
        var directorBuilder = modelBuilder.Entity<Director>();
        directorBuilder.HasKey(d => d._id);
        directorBuilder
            .HasOne<Movie>()
            .WithOne(m => m.director)
            .HasForeignKey<Director>(d => d._id);
        directorBuilder
            .HasMany<Studio>()
            .WithMany(s => s.directors);
        directorBuilder.ToCollection("directors");
        
        var movieBuilder = modelBuilder.Entity<Movie>();
        movieBuilder.HasKey(m => m._id);
        movieBuilder.ToCollection("movies");
    }
}

internal class Movie
{
    public ObjectId _id { get; set; }
    public string title { get; set; }
    public string rated { get; set; }
    public string plot { get; set; }
    public string fullplot { get; set; }
    
    public ObjectId director_id { get; set; }
    
    public Director director { get; set; }
}

internal class Director
{
    public ObjectId _id { get; set; }
    public string name { get; set; }
    public string birthdate { get; set; }
}

internal class Studio
{
    public ObjectId _id { get; set; }
    public string name { get; set; }
    public string address { get; set; }
    
    public ObjectId[] director_ids { get; set; }
    
    public Director[] directors { get; set; }
}