# ADR-004: Use Azure Cognitive Services for Transcription

**Date**: 2025-01-15
**Status**: Accepted
**Deciders**: Platform Developer
**Technical Story**: Auto-transcription of video diaries and interviews

---

## Context

The Insight Community Platform requires automatic transcription for qualitative research:
- **Video diaries**: Participants record 2-5 minute videos describing experiences
- **In-depth interviews (IDIs)**: 30-60 minute recorded sessions
- **Focus groups**: 60-90 minute multi-participant discussions
- **Audio responses**: Voice-only submissions

Researchers need transcripts to:
- Code and analyze qualitative responses
- Search for keywords and themes
- Create quotes for insight stories
- Analyze without watching/listening to every recording

Requirements:
- High accuracy (>90% for clear speech)
- Support multiple languages (English primarily, expand later)
- Timestamped transcripts (link to video moments)
- Reasonable turnaround time (minutes to hours)
- Speaker identification (focus groups)
- Cost-effective for 100s of videos monthly

Current situation: Need to choose between building custom solution, using Azure native services, or third-party APIs.

---

## Decision Drivers

- **Accuracy**: Must be usable for research analysis (High priority)
- **Azure integration**: Works with Blob Storage and Media Services (High)
- **Cost**: Affordable for research community scale (High)
- **Turnaround time**: Fast enough for near-real-time needs (Medium)
- **Languages**: English now, multi-language future (Medium)
- **Maintenance**: Managed service preferred (High)
- **Speaker diarization**: Identify who is speaking in focus groups (Medium)

---

## Considered Options

### Option 1: Azure Cognitive Services (Speech-to-Text)

**Description**: Azure's native speech recognition service

**Pros**:
- Native Azure integration (Media Services, Blob Storage)
- Pay-per-use pricing (no monthly minimum)
- Batch transcription for recorded files
- Real-time transcription for live sessions
- Speaker diarization available
- Custom models for domain-specific vocabulary
- Supports 100+ languages
- Timestamped output
- Managed infrastructure
- Can use Managed Identity

**Cons**:
- Accuracy varies by audio quality
- Learning curve for batch transcription API
- Requires audio extraction from video

**Cost**:
- Standard: $1.00 per audio hour
- Custom models: $1.40 per hour
- ~$100-300/month for 100-300 hours

**Implementation Effort**: Medium

---

### Option 2: Third-Party Services (Rev, Otter.ai)

**Description**: Specialized transcription services with human review options

**Pros**:
- Very high accuracy (especially Rev with human review)
- Simple REST API
- Purpose-built for transcription
- Great for important interviews
- Speaker identification works well

**Cons**:
- External service outside Azure
- More expensive ($1.50-3.00 per minute with human review)
- Data leaves Azure (compliance concerns)
- Slower turnaround (hours to days for human)
- Monthly minimums on some plans
- Another vendor to manage

**Cost**:
- Rev AI (machine): $0.02-0.06 per minute (~$1.20-3.60/hour)
- Rev Human: $1.50 per minute ($90/hour)
- Otter.ai: $20-30/month subscription + overage

**Implementation Effort**: Low

---

### Option 3: Open Source (Whisper, Vosk)

**Description**: Self-hosted speech recognition models

**Pros**:
- Full control
- One-time cost (compute only)
- Potentially very accurate (Whisper)
- No data leaves infrastructure

**Cons**:
- Must host and maintain infrastructure
- GPU instances required for reasonable speed
- Deployment complexity
- Model management
- Updates and improvements on us
- Significant development time

**Cost**: $200-500/month (GPU compute)

**Implementation Effort**: Very High

---

## Decision

**We will use Azure Cognitive Services (Speech-to-Text)** because it provides managed transcription that integrates seamlessly with our Azure infrastructure at competitive pricing with sufficient accuracy for research analysis.

**Rationale**:
1. **Native integration**: Works directly with Media Services and Blob Storage
2. **Cost-effective**: $1/hour is reasonable for research budgets
3. **Scalable**: Handles batch processing automatically
4. **Managed**: No infrastructure to maintain
5. **Good enough accuracy**: 85-95% accuracy sufficient for research (not legal/medical)
6. **Future-proof**: Can add custom models for domain vocabulary
7. **Real-time capable**: Can use for live focus groups later

Third-party services would be great for high-stakes interviews but too expensive for routine video diaries. Open source would consume too much development and operational time. We can selectively use Rev for critical transcripts if needed.

---

## Consequences

### Positive
- Unified Azure stack simplifies operations
- Batch API handles video processing pipeline naturally
- Speaker diarization helps with focus group analysis
- Custom vocabulary can improve accuracy over time
- Timestamped output enables video coding interface
- Can process multiple videos in parallel
- Pay only for what we use

### Negative
- Accuracy not perfect (mitigate: researchers review transcripts, not verbatim quotes)
- Must extract audio from video files (Azure Media Services does this)
- Initial setup requires learning batch transcription API
- May need human review for critical interviews (acceptable trade-off)

### Neutral
- Transcripts need formatting for readability
- Must build UI for transcript review/editing
- Storage costs for transcript files (minimal)

---

## Implementation Notes

**Steps**:
1. Create Azure Cognitive Services Speech resource
2. Implement audio extraction from video (via Media Services)
3. Create batch transcription job submitter
4. Build webhook handler for completion notifications
5. Store transcripts with timestamps in database
6. Create transcript viewer UI component
7. Implement search/filtering on transcripts

**Timeline**: 2 weeks for full implementation

**Batch Transcription Flow**:
```
Video Upload → Media Services (extract audio) → Blob Storage (audio file)
    ↓
Submit to Speech Service (batch transcription)
    ↓
Webhook notification on completion
    ↓
Retrieve transcript with timestamps
    ↓
Store in database + link to video
    ↓
Display in transcript viewer
```

**Code Example**:
```csharp
public class TranscriptionService
{
    private readonly SpeechConfig _speechConfig;
    private readonly BlobServiceClient _blobClient;
    
    public async Task<string> TranscribeVideoAsync(string videoUrl)
    {
        // Extract audio from video (via Media Services)
        var audioUrl = await ExtractAudioAsync(videoUrl);
        
        // Create transcription job
        var transcriptionUri = await CreateBatchTranscriptionAsync(audioUrl);
        
        // Poll or wait for webhook
        var transcript = await WaitForCompletionAsync(transcriptionUri);
        
        return transcript;
    }
    
    private async Task<Uri> CreateBatchTranscriptionAsync(string audioUrl)
    {
        var transcriptionDefinition = new TranscriptionDefinition
        {
            DisplayName = "Video Diary Transcription",
            Locale = "en-US",
            ContentUrls = new[] { new Uri(audioUrl) },
            Properties = new TranscriptionProperties
            {
                DiarizationEnabled = true,  // Speaker identification
                WordLevelTimestampsEnabled = true,
                PunctuationMode = PunctuationMode.DictatedAndAutomatic
            }
        };
        
        var transcription = await _speechClient.CreateTranscriptionAsync(
            transcriptionDefinition
        );
        
        return transcription.Self;
    }
}
```

**Transcript Format**:
```json
{
  "source": "video_diary_123.mp4",
  "duration": "00:03:45",
  "confidence": 0.89,
  "phrases": [
    {
      "speaker": 1,
      "startTime": "00:00:01.2",
      "endTime": "00:00:05.8",
      "text": "So today I went to the store and I noticed something interesting about the packaging.",
      "confidence": 0.92
    }
  ]
}
```

---

## Quality Assurance Strategy

**Accuracy Improvement**:
1. Start with standard model
2. Collect common misheard words
3. Create custom phrase list
4. Train custom model for brand/product terms
5. Monitor accuracy via confidence scores

**Human Review Triggers**:
- Confidence score <75%
- Critical interviews (C-suite, sensitive topics)
- Legal/compliance requirements
- Client requests verbatim accuracy

**Fallback to Human Transcription**:
- Use Rev.ai API for critical cases
- Researchers can flag for re-transcription
- Budget $500/month for human review

---

## Validation

**Success Criteria**:
- [ ] Average accuracy >85% on clear audio
- [ ] Transcripts available within 30 minutes of upload
- [ ] Speaker diarization correctly identifies 2-4 speakers
- [ ] Timestamped output accurate to ±2 seconds
- [ ] Cost <$300/month for first 200 hours
- [ ] 95% of transcripts usable without human review

**Review Date**: 2025-07-15 (6 months after implementation)

**Metrics to Track**:
- Average confidence score
- Turnaround time (upload to transcript)
- Cost per hour of audio
- Percentage requiring human review
- User satisfaction with accuracy

---

## Future Enhancements

**Phase 2+**:
- Sentiment analysis on transcripts
- Automatic theme detection via text analytics
- Translation for multi-language studies
- Real-time transcription for live focus groups
- Custom acoustic models for specific environments

---

## References

- [Azure Speech Services Documentation](https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/)
- [Batch Transcription Guide](https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/batch-transcription)
- [Speaker Diarization](https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/get-started-stt-diarization)
- [Custom Speech Models](https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/custom-speech-overview)
- Related: `contexts/integrations-extensibility.md` - Transcription section
- Related: ADR-002 (Azure Media Services for audio extraction)

---

## Decision Log

| Date | Author | Change |
|------|--------|--------|
| 2025-01-15 | Platform Developer | Decision accepted, chosen over Rev.ai and self-hosted |

