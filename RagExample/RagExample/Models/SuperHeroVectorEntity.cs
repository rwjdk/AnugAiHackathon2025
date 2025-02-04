using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace RagExample.Models;
#pragma warning disable SKEXP0001

public class SuperHeroVectorEntity
{
    [VectorStoreRecordKey]
    public string Id { get; set; }

    [VectorStoreRecordData(IsFilterable = true)]
    [TextSearchResultName]
    public string Name { get; set; }

    [VectorStoreRecordData(IsFullTextSearchable = true)]
    [TextSearchResultValue]
    public string Description { get; set; }

    [VectorStoreRecordVector(Dimensions: 1536, DistanceFunction.CosineSimilarity, IndexKind.Hnsw)]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }

    //Needed for Cosmos DB
    //[VectorStoreRecordVector(500)]
    //public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }
}