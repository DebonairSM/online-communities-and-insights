using OnlineCommunities.Application.Common.CQRS;
using OnlineCommunities.Core.Entities.CDM;

namespace OnlineCommunities.Application.Commands.CDM;

/// <summary>
/// Command to ingest Common Data Model data from external systems.
/// </summary>
/// <param name="SourceSystem">The source system identifier.</param>
/// <param name="EntityType">The type of entity being ingested.</param>
/// <param name="ExternalId">The external ID of the entity.</param>
/// <param name="RawData">The raw data payload.</param>
/// <param name="TenantId">The tenant ID for multi-tenant support.</param>
/// <param name="CorrelationId">Optional correlation ID for tracking.</param>
/// <param name="BatchId">Optional batch ID for grouping related data.</param>
/// <param name="Priority">The processing priority.</param>
public record IngestCDMDataCommand(
    string SourceSystem,
    string EntityType,
    string ExternalId,
    string RawData,
    Guid TenantId,
    string? CorrelationId = null,
    string? BatchId = null,
    ProcessingPriority Priority = ProcessingPriority.Normal
) : ICommand<Guid>;
