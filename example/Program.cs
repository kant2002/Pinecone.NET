using Pinecone;

using var pinecone = new PineconeClient("[api-key]", "[pinecone-env]");

// Check if the index exists and create it if it doesn't
// Depending on the storage type and infrastructure state this may take a while
// Free tier is limited to 1 index only
var indexName = "test-index";
var indexList = await pinecone.ListIndexes();

if (!indexList.Contains(indexName))
{
    await pinecone.CreateIndex(indexName, 1536, Metric.Cosine);
}

// Get the Pinecone index by name (uses gRPC by default).
// The index client is thread-safe, consider caching and/or
// injecting it as a singleton into your DI container.
using var index = await pinecone.GetIndex(indexName);

var first = new Vector
{
    Id = "first",
    // Zeroed-out placeholder vector, this is where you put the embeddings unless using sparse vectors
    Values = new float[1536],
    Metadata = new()
    {
        ["new"] = true,
        ["price"] = 50,
        ["tags"] = new string[] { "tag1", "tag2" }
    }
};

var second = new Vector
{
    Id = "second",
    Values = new float[1536],
    Metadata = new() { ["price"] = 100 }
};

// Upsert vectors into the index
await index.Upsert([first, second]);

// Specify metadata filter to query the index with
var priceRange = new MetadataMap
{
    ["price"] = new MetadataMap
    {
        ["$gte"] = 75,
        ["$lte"] = 125
    }
};

// Partially update a vector (allows to update dense/sparse/metadata properties only)
await index.Update("second", metadata: new() { ["price"] = 99 });

// Query the index by embedding and metadata filter
var results = await index.Query(
    new float[1536],
    topK: 3,
    filter: priceRange,
    includeMetadata: true);

Console.WriteLine(string.Join('\n', results.SelectMany(v => v.Metadata!)));

// Remove the example vectors we just added
await index.Delete(["first", "second"]);