# ADR-002: Use Azure Media Services for Video Processing

**Date**: 2025-01-15
**Status**: Accepted
**Deciders**: Platform Developer
**Technical Story**: Video diary capture and processing

---

## Context

The Insight Community Platform requires video processing capabilities for research participants to:
- Upload video diaries (potentially large files, 100MB+)
- Transcode videos to standard formats (H.264, 720p/1080p)
- Generate thumbnails for preview
- Stream videos efficiently to researchers
- Store videos securely with access controls

Current situation: Building video capture and processing from scratch. Need managed service that handles encoding, storage, and streaming without building custom infrastructure.

---

## Decision Drivers

- **Managed service**: Avoid building custom video pipeline (High priority)
- **Azure integration**: Works seamlessly with Blob Storage and other Azure services (High)
- **Format support**: Handles multiple input formats from mobile/web (High)
- **Scalability**: Can process multiple concurrent uploads (Medium)
- **Cost**: Reasonable pricing for research community scale (Medium)
- **Streaming**: Adaptive bitrate streaming for various bandwidths (Medium)

---

## Considered Options

### Option 1: Azure Media Services

**Description**: Fully managed video encoding, streaming, and delivery service on Azure

**Pros**:
- Built-in video transcoding with multiple presets
- Automatic thumbnail generation
- Adaptive bitrate streaming (HLS, DASH)
- Native Azure Blob Storage integration
- Content protection and DRM support
- Live streaming capability (future focus groups)
- Scales automatically

**Cons**:
- More expensive than basic Blob Storage alone
- Learning curve for Media Services SDK
- Overkill if only doing simple storage

**Cost**: ~$100-300/month for moderate use (encoding + streaming)

**Implementation Effort**: Medium

---

### Option 2: Third-Party (Vimeo, Wistia)

**Description**: Use external video hosting platforms

**Pros**:
- Simpler API
- Beautiful embedded players
- Analytics included
- No infrastructure management

**Cons**:
- External dependency outside Azure
- Data leaves Azure (compliance concerns)
- Monthly subscription per user/video
- Less control over processing
- Can't customize player fully

**Cost**: $300-1000/month depending on plan

**Implementation Effort**: Low

---

### Option 3: Custom FFmpeg Pipeline

**Description**: Build custom video processing with FFmpeg on VMs or containers

**Pros**:
- Full control over encoding
- Lower direct costs
- Can optimize for specific needs

**Cons**:
- Must manage infrastructure (VMs, scaling)
- Build encoding queue system
- Handle failures and retries manually
- Security and updates on us
- Significant development time
- Ongoing maintenance burden

**Cost**: ~$100/month (VM costs) but high development cost

**Implementation Effort**: Very High

---

## Decision

**We will use Azure Media Services** because it provides managed video processing that integrates seamlessly with our Azure infrastructure while avoiding the operational burden of custom encoding pipelines.

**Rationale**:
- As a solo developer, managed services free up time for feature development
- Native Azure integration reduces complexity
- Built-in streaming and thumbnail generation solve multiple needs
- Can scale automatically as research communities grow
- Content protection available if needed for sensitive research

Third-party services would create data residency issues and external dependencies. Custom FFmpeg would consume too much development and maintenance time.

---

## Consequences

### Positive
- No video encoding infrastructure to manage
- Automatic format conversion from mobile/web uploads
- Adaptive streaming improves researcher experience on various connections
- Thumbnails generated automatically for UI previews
- Can add live streaming for focus groups later
- Azure integration simplifies authentication and access control

### Negative
- Azure vendor lock-in for video infrastructure (mitigate: abstract behind IVideoService interface)
- Monthly costs scale with video volume (acceptable for B2B SaaS model)
- Must learn Media Services SDK (one-time investment)

### Neutral
- Videos stored in Blob Storage (would use anyway)
- Need to establish encoding presets for consistency
- Must implement upload chunking for large files

---

## Implementation Notes

**Steps**:
1. Provision Media Services account in Azure
2. Create encoding transform (720p H.264 preset)
3. Implement upload API with chunked uploads
4. Create background job to trigger encoding
5. Generate streaming URLs for playback
6. Implement player component in frontend

**Timeline**: 1-2 weeks for initial implementation

**Key Components**:
- Media Services account
- Input asset container (Blob Storage)
- Output asset container (encoded videos)
- Streaming endpoint
- Transform (encoding preset)
- Jobs (per-video encoding tasks)

**Example Code Structure**:
```csharp
public class VideoProcessingService
{
    public async Task<string> ProcessVideo(string inputBlobUrl)
    {
        // Create input asset
        var inputAsset = await CreateAssetAsync();
        
        // Copy blob to asset
        await CopyBlobToAssetAsync(inputBlobUrl, inputAsset);
        
        // Submit encoding job
        var job = await SubmitEncodingJobAsync(inputAsset);
        
        // Wait for completion (or webhook)
        await WaitForJobCompletionAsync(job);
        
        // Get streaming URL
        return await GetStreamingUrlAsync(outputAsset);
    }
}
```

---

## Validation

**Success Criteria**:
- [ ] Videos transcode successfully within 2x real-time (e.g., 10 min video encodes in <20 min)
- [ ] Streaming works on mobile and desktop browsers
- [ ] Thumbnails generate automatically
- [ ] Cost stays under $300/month for first 100 videos
- [ ] Upload reliability >99%

**Review Date**: 2025-07-15 (6 months after implementation)

**Metrics to Track**:
- Average encoding time per video
- Encoding success rate
- Monthly costs
- Streaming performance (buffering rate)

---

## References

- [Azure Media Services Documentation](https://docs.microsoft.com/en-us/azure/media-services/)
- [Video Encoding Presets](https://docs.microsoft.com/en-us/azure/media-services/latest/encode-concept)
- [Streaming Endpoints](https://docs.microsoft.com/en-us/azure/media-services/latest/stream-streaming-endpoint-concept)
- Related: Will create ADR for video storage strategy
- Related: `contexts/integrations-extensibility.md` - Media processing section

---

## Decision Log

| Date | Author | Change |
|------|--------|--------|
| 2025-01-15 | Platform Developer | Decision accepted |

