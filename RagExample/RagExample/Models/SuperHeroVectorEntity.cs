using Microsoft.Extensions.VectorData;

namespace RagExample.Models;
#pragma warning disable SKEXP0001

public class SuperHeroVectorEntity
{
    [VectorStoreRecordKey]
    public string Id { get; set; }

    [VectorStoreRecordData]
    public string Name { get; set; }

    [VectorStoreRecordData(IsFilterable = true)]
    public string Sex { get; set; }

    [VectorStoreRecordData]
    public string Description { get; set; }

    [VectorStoreRecordVector(Dimensions: 1536, DistanceFunction.CosineSimilarity, IndexKind.Hnsw)]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }

    //Needed for Cosmos DB
    //[VectorStoreRecordVector(Dimensions: 500)]
    //public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }
}