using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RestaurantCopilot.Infrastructure.Search;

internal sealed class SearchDocumentConfiguration : IEntityTypeConfiguration<SearchDocument>
{
    public void Configure(EntityTypeBuilder<SearchDocument> builder)
    {
        builder.ToTable("search_documents");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.Source).HasConversion<string>().HasMaxLength(20);
        builder.Property(document => document.Embedding).HasColumnType($"vector({SearchDocument.Dimensions})");

        builder.HasIndex(document => new { document.Source, document.SourceId }).IsUnique();

        // HNSW gives approximate nearest-neighbour search in sub-linear time; the operator class
        // must match the distance used in queries (cosine).
        builder.HasIndex(document => document.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");
    }
}
